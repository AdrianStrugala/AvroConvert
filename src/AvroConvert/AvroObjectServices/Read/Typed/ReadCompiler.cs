using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using SolTechnology.Avro.AvroObjectServices.Schemas;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;
using SolTechnology.Avro.AvroObjectServices.Skip;
using SolTechnology.Avro.Features.JsonEncoding;
using SolTechnology.Avro.Infrastructure.Attributes;
using SolTechnology.Avro.Infrastructure.Exceptions;
using SolTechnology.Avro.Infrastructure.Extensions;
using SolTechnology.Avro.Infrastructure.Reflection;
using SolTechnology.Avro.Policies;
using System.Diagnostics.CodeAnalysis;
using SolTechnology.Avro.Infrastructure;

namespace SolTechnology.Avro.AvroObjectServices.Read.Typed
{
    /// <summary>
    /// Compiles a (writerSchema, readerSchema, T) triple into a strongly typed <c>Func&lt;Reader, T&gt;</c>.
    /// Schema resolution, member lookup, union branch mapping and conversions are decided once at compile time; the
    /// produced delegate performs no boxing for primitives and no per-value schema dispatch. Dynamic targets
    /// (<c>object</c>) materialise as ExpandoObject / List&lt;object&gt; / Dictionary&lt;string, object&gt;, or as a CLR type
    /// whose name matches the record schema.
    /// </summary>
    [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
    internal sealed class ReadCompiler
    {
        private static readonly MethodInfo ReadBooleanMethod = typeof(Reader).GetMethod(nameof(IReader.ReadBoolean))!;
        private static readonly MethodInfo ReadIntMethod = typeof(Reader).GetMethod(nameof(IReader.ReadInt))!;
        private static readonly MethodInfo ReadLongMethod = typeof(Reader).GetMethod(nameof(IReader.ReadLong))!;
        private static readonly MethodInfo ReadFloatMethod = typeof(Reader).GetMethod(nameof(IReader.ReadFloat))!;
        private static readonly MethodInfo ReadDoubleMethod = typeof(Reader).GetMethod(nameof(IReader.ReadDouble))!;
        private static readonly MethodInfo ReadStringMethod = typeof(Reader).GetMethod(nameof(IReader.ReadString))!;
        private static readonly MethodInfo ReadBytesMethod = typeof(Reader).GetMethod(nameof(IReader.ReadBytes))!;
        private static readonly MethodInfo ReadEnumMethod = typeof(Reader).GetMethod(nameof(IReader.ReadEnum))!;
        private static readonly MethodInfo ReadUnionIndexMethod = typeof(Reader).GetMethod(nameof(IReader.ReadUnionIndex))!;
        private static readonly MethodInfo ReadArrayStartMethod = typeof(Reader).GetMethod(nameof(IReader.ReadArrayStart))!;
        private static readonly MethodInfo ReadArrayNextMethod = typeof(Reader).GetMethod(nameof(IReader.ReadArrayNext))!;
        private static readonly MethodInfo ReadFixedMethod = typeof(Reader).GetMethod(nameof(IReader.ReadFixed), new[] { typeof(byte[]) })!;
        private static readonly MethodInfo SkipMethod = typeof(Skipper).GetMethod(nameof(Skipper.Skip), BindingFlags.Instance | BindingFlags.NonPublic)!;
        private static readonly MethodInfo Utf8GetStringMethod = typeof(Encoding).GetMethod(nameof(Encoding.GetString), new[] { typeof(byte[]) })!;
        private static readonly MethodInfo Utf8GetBytesMethod = typeof(Encoding).GetMethod(nameof(Encoding.GetBytes), new[] { typeof(string) })!;
        private static readonly MethodInfo ConvertToLogicalMethod = typeof(LogicalTypeSchema).GetMethod(nameof(LogicalTypeSchema.ConvertToLogicalValue), BindingFlags.Instance | BindingFlags.NonPublic)!;
        private static readonly MethodInfo GetUninitializedObjectMethod = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.GetUninitializedObject))!;
        private static readonly ConstructorInfo ReaderFromBytesCtor = typeof(Reader).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(byte[]) }, null)!;
        private static readonly MethodInfo ReadCountIntoMethod = Helper(nameof(ReadHelpers.ReadCountInto));
        private static readonly MethodInfo ReadArraysIntoMethod = Helper(nameof(ReadHelpers.ReadArraysInto));
        private static readonly MethodInfo ReadMapMethod = Helper(nameof(ReadHelpers.ReadMap));
        private static readonly MethodInfo ReadMapKeyedMethod = Helper(nameof(ReadHelpers.ReadMapKeyed));
        private static readonly MethodInfo ReadDictionaryMethod = Helper(nameof(ReadHelpers.ReadDictionary));
        private static readonly MethodInfo CopyMapMethod = Helper(nameof(ReadHelpers.CopyMap));
        private static readonly MethodInfo FoldMethod = Helper(nameof(ReadHelpers.Fold));
        private static readonly MethodInfo AddAllMethod = Helper(nameof(ReadHelpers.AddAll));
        private static readonly MethodInfo UnionIndexOutOfRangeMethod = Helper(nameof(ReadHelpers.UnionIndexOutOfRange));
        private static readonly MethodInfo ExpandoAddMethod = typeof(IDictionary<string, object>).GetMethod(nameof(IDictionary<string, object>.Add))!;

        private static MethodInfo Helper(string name) => typeof(ReadHelpers).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!;

        private readonly Skipper _skipper = new();
        private readonly IAvroNamingPolicy _namingPolicy;
        private readonly AvroMissingFieldHandling _missingFieldHandling;
        private readonly Dictionary<Type, Func<IReader, object>> _converters;
        private readonly Dictionary<(TypeSchema, TypeSchema, Type), object> _recordHolders = new();
        // Records are inlined into the caller's plan up to this many fields in total; the JIT optimises several small
        // methods better than one very large one, so records past the limit stay separate delegates.
        private const int InlinedFieldsLimit = 100;
        private int _inlinedFields;
        private readonly HashSet<(TypeSchema, TypeSchema, Type)> _recordsInProgress = new();

        internal ReadCompiler(AvroConvertOptions options)
        {
            _namingPolicy = options?.NamingPolicy;
            _missingFieldHandling = options?.MissingFieldHandling ?? AvroMissingFieldHandling.Throw;
            _converters = options?.AvroConverters.Count > 0
                ? options.AvroConverters.ToDictionary(c => c.TypeSchema.RuntimeType, c => (Func<IReader, object>)c.Deserialize)
                : null;
        }

        internal Func<Reader, T> CompileOne<T>(TypeSchema writerSchema, TypeSchema readerSchema)
        {
            var reader = Expression.Parameter(typeof(Reader), "reader");
            var body = Build(writerSchema, readerSchema, typeof(T), reader);
            return Expression.Lambda<Func<Reader, T>>(body, reader).Compile();
        }

        /// <summary>Block-wise reader of top-level container entries into a collection T; null when T is not a collection.</summary>
        internal ManyPlan<T> CompileMany<T>(TypeSchema writerSchema, TypeSchema readerSchema)
        {
            var itemType = CollectionItemType(typeof(T));
            if (itemType == null)
            {
                return null;
            }

            // A container entry is either one item of T (4.x entries layout) or a whole array of items (3.x single-array
            // layout, several entries after Merge). The reader schema derived from T nests as deeply as T does.
            var depth = CollectionDepth(typeof(T), out _);
            var entryIsItem = EntryIsItem(writerSchema, typeof(T));
            int nullIndex = -1;
            var itemWriter = entryIsItem ? writerSchema : AsArray(writerSchema, out nullIndex).ItemSchema;
            var itemReader = readerSchema is ArraySchema ras && ArrayDepth(readerSchema) == depth ? ras.ItemSchema : readerSchema;
            if (itemReader.IsEmpty())
            {
                itemReader = itemWriter;
            }

            var listType = typeof(List<>).MakeGenericType(itemType);
            var reader = Expression.Parameter(typeof(Reader), "reader");
            var count = Expression.Parameter(typeof(long), "count");
            var accumulator = Expression.Parameter(typeof(object), "accumulator");
            var itemFn = CompileItem(itemWriter, itemReader, itemType);

            var finishBody = AdaptCollection(Expression.Convert(accumulator, listType), itemType, typeof(T));
            if (finishBody == null)
            {
                return null;
            }

            var append = entryIsItem
                ? Expression.Call(ReadCountIntoMethod.MakeGenericMethod(itemType), reader, count, Expression.Constant(itemFn), Expression.Convert(accumulator, listType))
                : Expression.Call(ReadArraysIntoMethod.MakeGenericMethod(itemType), reader, count, Expression.Constant(itemFn), Expression.Convert(accumulator, listType), Expression.Constant(nullIndex));

            return new ManyPlan<T>
            {
                NewAccumulator = Expression.Lambda<Func<object>>(Expression.Convert(Expression.New(listType), typeof(object))).Compile(),
                Append = Expression.Lambda<Action<Reader, long, object>>(append, reader, count, accumulator).Compile(),
                Finish = Expression.Lambda<Func<object, T>>(finishBody, accumulator).Compile()
            };
        }

        // ------------------------------------------------------------------ core

        private Expression Build(TypeSchema ws, TypeSchema rs, Type target, ParameterExpression reader, Type[] candidates = null)
        {
            if (_converters != null && _converters.TryGetValue(target, out var converter))
            {
                return Expression.Convert(Expression.Invoke(Expression.Constant(converter), reader), target);
            }

            if (ws.Type != AvroType.Union && rs is UnionSchema readerUnion)
            {
                rs = FindBranch(readerUnion, ws, renamedRecord: true) ?? rs;
            }

            if (target == typeof(object) && ws is PrimitiveTypeSchema && rs is PrimitiveTypeSchema && ws.Type != rs.Type
                && SchemaPromotion.IsSpecPromotion(ws.Type, rs.Type) && SchemaPromotion.ClrType(rs.Type) is { } promoted)
            {
                // No CLR type to guide the conversion: the value takes the reader schema's type (e.g. int written, long read).
                return Expression.Convert(Build(ws, ws, promoted, reader), typeof(object));
            }

            try
            {
                switch (ws.Type)
                {
                    case AvroType.Null:
                        return Expression.Default(target);
                    case AvroType.Boolean:
                        return Convert(Expression.Call(reader, ReadBooleanMethod), target);
                    case AvroType.Int:
                        return Convert(Expression.Call(reader, ReadIntMethod), target);
                    case AvroType.Long:
                        return Convert(Expression.Call(reader, ReadLongMethod), target);
                    case AvroType.Float:
                        return Convert(Expression.Call(reader, ReadFloatMethod), target);
                    case AvroType.Double:
                        return Convert(Expression.Call(reader, ReadDoubleMethod), target);
                    case AvroType.String:
                        return BuildString(reader, target);
                    case AvroType.Bytes:
                        if (target == typeof(string))
                        {
                            // bytes -> string promotion (UTF-8).
                            return Expression.Call(Expression.Constant(Encoding.UTF8), Utf8GetStringMethod, Expression.Call(reader, ReadBytesMethod));
                        }
                        return Convert(Expression.Call(reader, ReadBytesMethod), target);
                    case AvroType.Logical:
                        return BuildLogical((LogicalTypeSchema)ws, rs, target, reader);
                    case AvroType.Record:
                    case AvroType.Error:
                        if (rs is not RecordSchema readerRecord)
                        {
                            throw new NotSupportedException($"Schema mismatch: reader schema [{rs.Type}] cannot read a record");
                        }
                        return BuildRecord((RecordSchema)ws, readerRecord, target, reader, candidates);
                    case AvroType.Enum:
                        return BuildEnum((EnumSchema)ws, rs, target, reader);
                    case AvroType.Fixed:
                        return BuildFixed((FixedSchema)ws, target, reader);
                    case AvroType.Array:
                        return BuildArray((ArraySchema)ws, rs, target, reader);
                    case AvroType.Map:
                        return BuildMap((MapSchema)ws, rs, target, reader);
                    case AvroType.Union:
                        return BuildUnion((UnionSchema)ws, rs, target, reader, candidates);
                    default:
                        throw new NotSupportedException($"Unknown schema type {ws.Type}");
                }
            }
            catch (NotSupportedException e)
            {
                // Compile-time decision: reading this value cannot succeed, so the plan fails exactly where the data is.
                return Expression.Throw(Expression.Constant(new AvroTypeMismatchException(
                    $"Unable to deserialize [{ws.Name}] of schema [{ws.Type}] to the target type [{target}]. {e.Message}")), target);
            }
        }

        private Expression Skip(TypeSchema schema, ParameterExpression reader) =>
            Expression.Call(Expression.Constant(_skipper), SkipMethod, Expression.Constant(schema, typeof(TypeSchema)), reader);

        /// <summary>Numeric / enum / nullable / boxing conversion of a primitive; throws NotSupported for anything else.</summary>
        private static Expression Convert(Expression value, Type target)
        {
            if (value.Type == target)
            {
                return value;
            }

            if (target == typeof(object))
            {
                return Expression.Convert(value, typeof(object));
            }

            var underlying = Nullable.GetUnderlyingType(target) ?? target;
            if (underlying.IsEnum || (underlying.IsPrimitive && underlying != typeof(bool) && underlying != typeof(IntPtr) && underlying != typeof(UIntPtr)) || underlying == typeof(decimal))
            {
                if (value.Type.IsPrimitive && value.Type != typeof(bool))
                {
                    return Expression.Convert(value, target);
                }
            }

            if (target == typeof(bool?) && value.Type == typeof(bool))
            {
                return Expression.Convert(value, target);
            }

            if (target.IsAssignableFrom(value.Type))
            {
                return Expression.Convert(value, target);
            }

            throw new NotSupportedException($"No conversion from {value.Type} to {target}");
        }

        private static Expression BuildString(ParameterExpression reader, Type target)
        {
            var read = Expression.Call(reader, ReadStringMethod);
            var underlying = Nullable.GetUnderlyingType(target) ?? target;

            if (target == typeof(string) || target == typeof(object))
            {
                return Convert(read, target);
            }
            if (target == typeof(byte[]))
            {
                // string -> bytes promotion (UTF-8).
                return Expression.Call(Expression.Constant(Encoding.UTF8), Utf8GetBytesMethod, read);
            }
            if (underlying == typeof(decimal))
            {
                return Expression.Convert(Expression.Call(typeof(decimal).GetMethod(nameof(decimal.Parse), new[] { typeof(string) })!, read), target);
            }
            if (underlying == typeof(Guid))
            {
                return Expression.Convert(Expression.Call(typeof(Guid).GetMethod(nameof(Guid.Parse), new[] { typeof(string) })!, read), target);
            }
            if (TemporalText.IsTemporal(underlying))
            {
                return Expression.Convert(Expression.Call(TemporalText.ParseMethod(underlying), read), target);
            }
            if (target == typeof(Uri))
            {
                return Expression.New(typeof(Uri).GetConstructor(new[] { typeof(string) })!, read);
            }

            throw new NotSupportedException($"No conversion from string to {target}");
        }

        private Expression BuildLogical(LogicalTypeSchema ws, TypeSchema rs, Type target, ParameterExpression reader)
        {
            var typed = BuildTypedLogical(ws, Nullable.GetUnderlyingType(target) ?? target, reader);
            if (typed != null)
            {
                return typed.Type == target ? typed : Expression.Convert(typed, target);
            }

            // Remaining logical types (decimal, duration, big-decimal) and untyped targets go through the schema's converter.
            var baseRs = rs is LogicalTypeSchema logicalRs ? logicalRs.BaseTypeSchema : rs;
            var baseValue = Build(ws.BaseTypeSchema, baseRs, typeof(object), reader);

            var converted = Expression.Call(Expression.Constant(ws), ConvertToLogicalMethod, baseValue, Expression.Constant(ws), Expression.Constant(target));
            return target == typeof(object) ? converted : Expression.Convert(converted, target);
        }

        /// <summary>Unboxed decoding of time and uuid logical types into their CLR type; null when there is no typed path.</summary>
        private static Expression BuildTypedLogical(LogicalTypeSchema ws, Type type, ParameterExpression reader)
        {
            var nanosecondsPerUnit = LogicalReaders.NanosecondsPerUnit(ws);
            if (nanosecondsPerUnit > 0)
            {
                var unit = Expression.Constant(nanosecondsPerUnit);
                if (type == typeof(long))
                {
                    return Expression.Call(reader, ReadLongMethod);
                }
                if (type == typeof(DateTime))
                {
                    var kind = Expression.Constant(LogicalReaders.IsLocalTimestamp(ws) ? DateTimeKind.Unspecified : DateTimeKind.Utc);
                    return Expression.Call(LogicalReader(nameof(LogicalReaders.ReadDateTime)), reader, unit, kind);
                }
                if (type == typeof(DateTimeOffset))
                {
                    return Expression.Call(LogicalReader(nameof(LogicalReaders.ReadDateTimeOffset)), reader, unit);
                }
                if (type == typeof(DateOnly))
                {
                    return Expression.Call(LogicalReader(nameof(LogicalReaders.ReadDateOnlyFromTimestamp)), reader, unit);
                }
                return null;
            }

            return ws switch
            {
                DateSchema when type == typeof(DateOnly) => Expression.Call(LogicalReader(nameof(LogicalReaders.ReadDate)), reader),
                DateSchema when type == typeof(DateTime) => Expression.Call(LogicalReader(nameof(LogicalReaders.ReadDateAsDateTime)), reader),
                TimeMillisecondsSchema when type == typeof(TimeOnly) => Expression.Call(LogicalReader(nameof(LogicalReaders.ReadTimeMillis)), reader),
                TimeMillisecondsSchema when type == typeof(TimeSpan) => Expression.Call(LogicalReader(nameof(LogicalReaders.ReadTimeMillisAsTimeSpan)), reader),
                TimeMicrosecondsSchema when type == typeof(TimeOnly) => Expression.Call(LogicalReader(nameof(LogicalReaders.ReadTimeMicros)), reader),
                TimeMicrosecondsSchema when type == typeof(TimeSpan) => Expression.Call(LogicalReader(nameof(LogicalReaders.ReadTimeMicrosAsTimeSpan)), reader),
                UuidSchema when type == typeof(Guid) => Expression.Call(
                    LogicalReader(ws.BaseTypeSchema.Type == AvroType.Fixed ? nameof(LogicalReaders.ReadUuidFixed) : nameof(LogicalReaders.ReadUuidString)), reader),
                _ => null
            };
        }

        private static MethodInfo LogicalReader(string name) =>
            typeof(LogicalReaders).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!;

        private Expression BuildEnum(EnumSchema ws, TypeSchema rs, Type target, ParameterExpression reader)
        {
            var readerEnum = rs as EnumSchema;
            var index = Expression.Call(reader, ReadEnumMethod);
            var enumType = Nullable.GetUnderlyingType(target) ?? target;
            var cases = new List<SwitchCase>(ws.Symbols.Count);

            for (int i = 0; i < ws.Symbols.Count; i++)
            {
                // Schema resolution: a writer symbol unknown to the reader resolves to the reader's enum default.
                var symbol = ws.Symbols[i];
                if (readerEnum?.Default != null && readerEnum.GetSymbolPosition(symbol) < 0)
                {
                    symbol = readerEnum.Default;
                }

                Expression value;
                if (enumType.IsEnum)
                {
                    try
                    {
                        value = Expression.Convert(Expression.Constant(EnumParser.Parse(enumType, symbol, _namingPolicy), enumType), target);
                    }
                    catch (KeyNotFoundException)
                    {
                        value = Expression.Throw(Expression.Constant(new AvroTypeMismatchException($"Enum symbol '{symbol}' is not defined on {enumType}")), target);
                    }
                }
                else if (target == typeof(string) || target == typeof(object))
                {
                    value = Expression.Constant(symbol, target);
                }
                else
                {
                    throw new NotSupportedException($"Enum cannot be read as {target}");
                }

                cases.Add(Expression.SwitchCase(value, Expression.Constant(i)));
            }

            var outOfRange = Expression.Throw(Expression.Constant(new AvroTypeMismatchException($"Enum position out of range for {ws.FullName}")), target);
            return Expression.Switch(index, outOfRange, cases.ToArray());
        }

        private static Expression BuildFixed(FixedSchema ws, Type target, ParameterExpression reader)
        {
            if (target != typeof(byte[]) && target != typeof(Guid) && target != typeof(Guid?) && target != typeof(object))
            {
                throw new NotSupportedException($"Fixed cannot be read as {target}");
            }

            var buffer = Expression.Variable(typeof(byte[]), "fixed");
            Expression result = target == typeof(Guid) || target == typeof(Guid?)
                ? Expression.Convert(Expression.New(typeof(Guid).GetConstructor(new[] { typeof(byte[]) })!, buffer), target)
                : Expression.Convert(buffer, target);

            return Expression.Block(target, new[] { buffer },
                Expression.Assign(buffer, Expression.NewArrayBounds(typeof(byte), Expression.Constant(ws.Size))),
                Expression.Call(reader, ReadFixedMethod, buffer),
                result);
        }

        private Expression BuildArray(ArraySchema ws, TypeSchema rs, Type target, ParameterExpression reader)
        {
            var itemRs = rs is ArraySchema ras ? ras.ItemSchema : rs;
            if (itemRs.IsEmpty())
            {
                itemRs = ws.ItemSchema;
            }

            // Dictionaries with non-string keys are encoded as an array of {Key, Value} records.
            if (target.GetDictionaryArguments() is { } kv && ws.ItemSchema is RecordSchema itemRecord
                && itemRecord.TryGetField("Key", out var keyField) && itemRecord.TryGetField("Value", out var valueField))
            {
                var readerRecord = itemRs as RecordSchema ?? itemRecord;
                readerRecord.TryGetField("Key", out var readerKey);
                readerRecord.TryGetField("Value", out var readerValue);

                var keyFn = CompileItem(keyField.TypeSchema, readerKey?.TypeSchema ?? keyField.TypeSchema, kv.Key);
                var valueFn = CompileItem(valueField.TypeSchema, readerValue?.TypeSchema ?? valueField.TypeSchema, kv.Value);
                var dictionary = Expression.Call(ReadDictionaryMethod.MakeGenericMethod(kv.Key, kv.Value), reader, Expression.Constant(keyFn), Expression.Constant(valueFn));
                return AdaptMap(dictionary, kv.Key, kv.Value, target);
            }

            var itemType = target == typeof(object) ? typeof(object) : CollectionItemType(target) ?? throw new NotSupportedException($"Array cannot be read as {target}");
            var list = BuildListLoop(Build(ws.ItemSchema, itemRs, itemType, reader), itemType, reader);
            return AdaptCollection(list, itemType, target) ?? throw new NotSupportedException($"Array cannot be read as {target}");
        }

        /// <summary>The array blocks read in a loop inside the plan, with the item expression inlined (no delegate per item).</summary>
        private static Expression BuildListLoop(Expression item, Type itemType, ParameterExpression reader)
        {
            var listType = typeof(List<>).MakeGenericType(itemType);
            var list = Expression.Variable(listType, "list");
            var n = Expression.Variable(typeof(long), "n");
            var i = Expression.Variable(typeof(long), "i");
            var outerEnd = Expression.Label("outerEnd");
            var innerEnd = Expression.Label("innerEnd");

            var capacity = Expression.Condition(
                Expression.AndAlso(Expression.GreaterThan(n, Expression.Constant(0L)), Expression.LessThan(n, Expression.Constant(1024L))),
                Expression.Convert(n, typeof(int)),
                Expression.Constant(4));

            return Expression.Block(listType, new[] { list, n, i },
                Expression.Assign(n, Expression.Call(reader, ReadArrayStartMethod)),
                Expression.Assign(list, Expression.New(listType.GetConstructor(new[] { typeof(int) })!, capacity)),
                Expression.Loop(
                    Expression.Block(
                        Expression.IfThen(Expression.Equal(n, Expression.Constant(0L)), Expression.Break(outerEnd)),
                        Expression.Assign(i, Expression.Constant(0L)),
                        Expression.Loop(
                            Expression.Block(
                                Expression.IfThen(Expression.GreaterThanOrEqual(i, n), Expression.Break(innerEnd)),
                                Expression.Call(list, listType.GetMethod(nameof(List<object>.Add))!, item),
                                Expression.PreIncrementAssign(i)),
                            innerEnd),
                        Expression.Assign(n, Expression.Call(reader, ReadArrayNextMethod))),
                    outerEnd),
                list);
        }

        private Expression BuildMap(MapSchema ws, TypeSchema rs, Type target, ParameterExpression reader)
        {
            var valueRs = rs is MapSchema mrs ? mrs.ValueSchema : rs;

            if (target == typeof(object))
            {
                var dynamicValueFn = CompileItem(ws.ValueSchema, valueRs, typeof(object));
                return Expression.Convert(Expression.Call(ReadMapMethod.MakeGenericMethod(typeof(object)), reader, Expression.Constant(dynamicValueFn)), typeof(object));
            }

            var kv = target.GetDictionaryArguments() ?? throw new NotSupportedException($"Map cannot be read as {target}");
            var valueFn = CompileItem(ws.ValueSchema, valueRs, kv.Value);

            if (kv.Key == typeof(string))
            {
                return AdaptMap(Expression.Call(ReadMapMethod.MakeGenericMethod(kv.Value), reader, Expression.Constant(valueFn)), kv.Key, kv.Value, target);
            }

            // Non-string key type: convert each string key the same way a string value would be converted.
            var keyReader = Expression.Parameter(typeof(Reader), "reader");
            var keyFn = Expression.Lambda(typeof(Func<,>).MakeGenericType(typeof(Reader), kv.Key), BuildString(keyReader, kv.Key), keyReader).Compile();
            var map = Expression.Call(ReadMapKeyedMethod.MakeGenericMethod(kv.Key, kv.Value), reader, Expression.Constant(keyFn), Expression.Constant(valueFn));
            return AdaptMap(map, kv.Key, kv.Value, target);
        }

        /// <summary>
        /// Turns a <c>Dictionary&lt;TKey, TValue&gt;</c> expression into the requested dictionary type: as it is when assignable,
        /// through a constructor taking IDictionary&lt;,&gt; (SortedDictionary, ReadOnlyDictionary), or by copying the entries
        /// into a new instance (a class deriving from Dictionary).
        /// </summary>
        private static Expression AdaptMap(Expression dictionary, Type keyType, Type valueType, Type target)
        {
            if (target.IsAssignableFrom(dictionary.Type))
            {
                return Expression.Convert(dictionary, target);
            }

            var dictionaryInterface = typeof(IDictionary<,>).MakeGenericType(keyType, valueType);
            if (!target.IsAbstract && !target.IsInterface)
            {
                var fromDictionary = target.GetConstructor(new[] { dictionaryInterface });
                if (fromDictionary != null)
                {
                    return Expression.New(fromDictionary, Expression.Convert(dictionary, dictionaryInterface));
                }

                if (dictionaryInterface.IsAssignableFrom(target) && target.GetConstructor(Type.EmptyTypes) != null)
                {
                    return Expression.Call(CopyMapMethod.MakeGenericMethod(target, keyType, valueType), dictionary);
                }
            }

            throw new NotSupportedException($"Map cannot be read as {target}");
        }

        private Expression BuildUnion(UnionSchema ws, TypeSchema rs, Type target, ParameterExpression reader, Type[] candidates = null)
        {
            var index = Expression.Call(reader, ReadUnionIndexMethod);
            var cases = new List<SwitchCase>(ws.Schemas.Count);

            // A writer record of another name maps to the reader's record only when the writer has no other record to choose from.
            var renamedRecord = ws.Schemas.Count(s => s is RecordSchema) == 1;

            for (int i = 0; i < ws.Schemas.Count; i++)
            {
                var branchWs = ws.Schemas[i];
                var branchRs = rs is UnionSchema readerUnion ? FindBranch(readerUnion, branchWs, renamedRecord) : rs;

                Expression body;
                if (branchWs.Type == AvroType.String && TemporalText.IsTemporal(Nullable.GetUnderlyingType(target) ?? target)
                    && (branchRs == null || (rs is not UnionSchema && !rs.CanRead(branchWs))))
                {
                    // Date and time written as ISO 8601 text: the target type decides, not the reader schema.
                    body = Build(branchWs, branchWs, target, reader, candidates);
                }
                else if (branchRs == null || (rs is not UnionSchema && !rs.CanRead(branchWs)))
                {
                    body = Expression.Throw(Expression.Constant(new AvroException($"Schema mismatch. Reader: {rs}, writer: {branchWs}")), target);
                }
                else
                {
                    try
                    {
                        body = Build(branchWs, branchRs, target, reader, candidates);
                    }
                    catch (Exception e) when (e is SerializationException or AvroException)
                    {
                        // A branch that cannot be read fails when the data holds it, not for every value of the union.
                        body = Expression.Throw(Expression.Constant(e), target);
                    }
                }

                cases.Add(Expression.SwitchCase(body, Expression.Constant(i)));
            }

            var outOfRange = Expression.Throw(Expression.Call(UnionIndexOutOfRangeMethod, index, Expression.Constant(ws.Schemas.Count)), target);
            return Expression.Switch(index, outOfRange, cases.ToArray());
        }

        // ------------------------------------------------------------------ records

        private Expression BuildRecord(RecordSchema ws, RecordSchema rs, Type target, ParameterExpression reader, Type[] candidates = null)
        {
            if (candidates != null && (target == typeof(object) || target.IsAbstract || target.IsInterface))
            {
                // [AvroUnion] names the alternatives explicitly, so matching on the simple name is unambiguous.
                var candidate = candidates.FirstOrDefault(t => t.Name == ws.Name) ?? candidates.FirstOrDefault(t => t.Name == rs.Name);
                if (candidate != null && target.IsAssignableFrom(candidate))
                {
                    return Expression.Convert(BuildRecord(ws, rs, candidate, reader), target);
                }
            }

            if (target == typeof(object))
            {
                // A CLR `object` member is described by an empty "System.Object" record; the writer schema is the only useful one.
                if (rs.Fields.Count == 0 && rs.FullName == typeof(object).FullName)
                {
                    rs = ws;
                }

                var clrType = ClrTypeCache.Find(rs);
                if (clrType != null && clrType != typeof(object))
                {
                    return Expression.Convert(BuildRecord(ws, rs, clrType, reader), typeof(object));
                }

                return BuildExpando(ws, rs, reader);
            }

            if (target == typeof(ExpandoObject) || typeof(IDictionary<string, object>) == target)
            {
                return Expression.Convert(BuildExpando(ws, rs, reader), target);
            }

            if (target.IsAbstract || target.IsInterface || target.IsPrimitive || target == typeof(string) || target.IsArray || target.IsEnum
                || target.GetDictionaryArguments() != null || CollectionItemType(target) != null)
            {
                throw new NotSupportedException($"Record cannot be read as {target}");
            }

            // Non-recursive records are inlined into the caller's tree. A record that refers back to itself gets its own
            // delegate (via a holder) so the tree terminates; the reference is detected while its body is being built.
            var key = (ws, (TypeSchema)rs, target);
            if (!_recordHolders.TryGetValue(key, out var holderObj))
            {
                if (_recordsInProgress.Contains(key))
                {
                    // A reference back to a record whose body is being built: filled in when that body is complete.
                    holderObj = Activator.CreateInstance(typeof(DelegateHolder<>).MakeGenericType(target))!;
                    _recordHolders.Add(key, holderObj);
                }
                else if (_inlinedFields + ws.Fields.Count <= InlinedFieldsLimit)
                {
                    _inlinedFields += ws.Fields.Count;
                    _recordsInProgress.Add(key);
                    var inline = BuildRecordBody(ws, rs, target, reader);
                    _recordsInProgress.Remove(key);

                    if (!_recordHolders.TryGetValue(key, out holderObj))
                    {
                        return inline;
                    }

                    holderObj.GetType().GetField(nameof(DelegateHolder<object>.Fn))!.SetValue(holderObj, CompileRecordDelegate(ws, rs, target));
                }
                else
                {
                    holderObj = Activator.CreateInstance(typeof(DelegateHolder<>).MakeGenericType(target))!;
                    _recordHolders.Add(key, holderObj);
                    holderObj.GetType().GetField(nameof(DelegateHolder<object>.Fn))!.SetValue(holderObj, CompileRecordDelegate(ws, rs, target));
                }
            }

            var fn = Expression.Field(Expression.Constant(holderObj), nameof(DelegateHolder<object>.Fn));
            return Expression.Invoke(fn, reader);
        }

        private Delegate CompileRecordDelegate(RecordSchema ws, RecordSchema rs, Type target)
        {
            var reader = Expression.Parameter(typeof(Reader), "reader");
            var lambda = Expression.Lambda(typeof(Func<,>).MakeGenericType(typeof(Reader), target), BuildRecordBody(ws, rs, target, reader), reader);
            return lambda.Compile();
        }

        private Expression BuildRecordBody(RecordSchema ws, RecordSchema rs, Type target, ParameterExpression reader)
        {
            var instance = Expression.Variable(target, "instance");
            var members = TypeMembers.For(target);
            var body = new List<Expression>
            {
                Expression.Assign(instance, NewInstance(target))
            };

            foreach (var wf in ws.Fields)
            {
                if (!rs.TryGetField(wf.Name, out var rf))
                {
                    body.Add(Skip(wf.TypeSchema, reader));
                    continue;
                }

                var member = members.Find(rf.GetAliasOrDefault() ?? wf.Name);
                if (member is not { CanWrite: true })
                {
                    body.Add(Skip(wf.TypeSchema, reader));
                    continue;
                }

                Expression value = Build(wf.TypeSchema, rf.TypeSchema, member.Type, reader, UnionCandidates(member.Info));
                if (wf.HasDefaultValue && wf.DefaultValue != null && (!member.Type.IsValueType || Nullable.GetUnderlyingType(member.Type) != null))
                {
                    // A null read for a field with a writer default yields that default.
                    object fallbackDefault = null;
                    try { fallbackDefault = FormatDefaultValue(wf.DefaultValue, member.Type); }
                    catch (Exception) { /* default not representable as the member type – keep the raw value */ }

                    if (fallbackDefault != null)
                    {
                        value = Expression.Coalesce(value, Expression.Constant(fallbackDefault, member.Type));
                    }
                }

                body.Add(Expression.Assign(Expression.MakeMemberAccess(instance, member.Info), value));
            }

            foreach (var rf in rs.Fields)
            {
                if (ws.TryGetField(rf.Name, out _))
                {
                    continue;
                }

                var member = members.Find(rf.GetAliasOrDefault() ?? rf.Name);
                if (member is not { CanWrite: true })
                {
                    continue;
                }

                bool clrNullable = !member.Type.IsValueType || Nullable.GetUnderlyingType(member.Type) != null;
                var value = ResolveMissingReaderField(rs, rf, member.Type, clrNullable);
                if (value == null)
                {
                    // No value from the schema: keep what the constructor set (as System.Text.Json does).
                    // Only an explicit "default": null overwrites it.
                    if (!rf.HasDefaultValue || !clrNullable)
                    {
                        continue;
                    }

                    value = Expression.Constant(null, member.Type);
                }

                body.Add(Expression.Assign(Expression.MakeMemberAccess(instance, member.Info), value));
            }

            body.Add(instance);
            return Expression.Block(target, new[] { instance }, body);
        }

        /// <summary>
        /// The public parameterless constructor when there is one (field initialisers and constructor logic run, as in
        /// System.Text.Json); otherwise an uninitialized instance (records with only a primary constructor, private constructors).
        /// </summary>
        private static Expression NewInstance(Type target)
        {
            if (target.IsValueType)
            {
                return Expression.New(target);
            }

            var ctor = target.GetConstructor(Type.EmptyTypes);
            return ctor != null
                ? Expression.New(ctor)
                : Expression.Convert(Expression.Call(GetUninitializedObjectMethod, Expression.Constant(target)), target);
        }

        private static Type[] UnionCandidates(MemberInfo member) =>
            member.GetCustomAttribute<AvroUnionAttribute>(false)?.TypeAlternatives.ToArray();

        /// <summary>Dynamic record: an ExpandoObject with one entry per reader field (arrays as List&lt;object&gt;).</summary>
        private Expression BuildExpando(RecordSchema ws, RecordSchema rs, ParameterExpression reader)
        {
            var dictionary = Expression.Variable(typeof(IDictionary<string, object>), "expando");
            var body = new List<Expression>
            {
                Expression.Assign(dictionary, Expression.Convert(Expression.New(typeof(ExpandoObject)), typeof(IDictionary<string, object>)))
            };

            foreach (var wf in ws.Fields)
            {
                if (!rs.TryGetField(wf.Name, out var rf))
                {
                    body.Add(Skip(wf.TypeSchema, reader));
                    continue;
                }

                string name = rf.Aliases.FirstOrDefault() ?? wf.Name;
                var targetType = wf.TypeSchema.Type == AvroType.Array ? typeof(List<object>) : typeof(object);
                Expression value = Expression.Convert(Build(wf.TypeSchema, rf.TypeSchema, targetType, reader), typeof(object));
                if (wf.DefaultValue != null)
                {
                    value = Expression.Coalesce(value, Expression.Constant(wf.DefaultValue, typeof(object)));
                }

                body.Add(Expression.Call(dictionary, ExpandoAddMethod, Expression.Constant(name), value));
            }

            foreach (var rf in rs.Fields)
            {
                if (ws.TryGetField(rf.Name, out _))
                {
                    continue;
                }

                string name = rf.Aliases.FirstOrDefault() ?? rf.Name;
                var value = ResolveMissingReaderField(rs, rf, typeof(object), clrNullable: true);
                body.Add(Expression.Call(dictionary, ExpandoAddMethod, Expression.Constant(name), value ?? Expression.Constant(null, typeof(object))));
            }

            body.Add(Expression.Convert(dictionary, typeof(object)));
            return Expression.Block(typeof(object), new[] { dictionary }, body);
        }

        /// <summary>
        /// Avro schema resolution for a reader field absent from the writer: an expression producing the reader's default
        /// for each record; null when the field gets null (fields that can hold null, or MissingFieldHandling.UseDefault);
        /// otherwise an error.
        /// </summary>
        private Expression ResolveMissingReaderField(RecordSchema readerSchema, RecordFieldSchema rf, Type memberType, bool clrNullable)
        {
            if (rf.DefaultJson != null)
            {
                try
                {
                    return ReadDefault(rf, memberType);
                }
                catch (Exception)
                {
                    // Not readable into this member type through the data path – fall back to the converted CLR default below.
                }
            }

            if (rf.HasDefaultValue)
            {
                return rf.DefaultValue == null ? null : Expression.Constant(FormatDefaultValue(rf.DefaultValue, memberType), memberType);
            }

            bool nullable = clrNullable ||
                            rf.TypeSchema.Type == AvroType.Null ||
                            rf.TypeSchema is UnionSchema union && union.Schemas.Any(s => s.Type == AvroType.Null);
            if (nullable || _missingFieldHandling == AvroMissingFieldHandling.UseDefault)
            {
                return null;
            }

            throw new SerializationException(
                $"Field '{rf.Name}' of record '{readerSchema.FullName}' is not present in the writer schema and has no default value. " +
                $"Add a [DefaultValue] / \"default\" to the field, make it nullable, or set {nameof(AvroConvertOptions)}.{nameof(AvroConvertOptions.MissingFieldHandling)} = {nameof(AvroMissingFieldHandling.UseDefault)}.");
        }

        /// <summary>
        /// A default declared in a parsed schema is encoded to Avro binary and read with the same plan as data, so it
        /// converts to the member type exactly like a value on the wire would (e.g. timestamp-nanos into long or DateTime).
        /// Immutable results become constants; collections, records and bytes are decoded again for every record so
        /// that instances are never shared between results.
        /// </summary>
        private Expression ReadDefault(RecordFieldSchema rf, Type memberType)
        {
            var encoded = JsonEncodingReader.EncodeDefault(rf.TypeSchema, rf.DefaultJson);
            var read = CompileItem(rf.TypeSchema, rf.TypeSchema, memberType);
            var value = read.DynamicInvoke(new Reader(encoded));
            if (value == null || value is string || value.GetType().IsValueType)
            {
                return Expression.Constant(value, memberType);
            }

            return Expression.Invoke(Expression.Constant(read), Expression.New(ReaderFromBytesCtor, Expression.Constant(encoded)));
        }

        private object FormatDefaultValue(object defaultValue, Type memberType)
        {
            if (defaultValue == null || memberType == typeof(object))
            {
                return defaultValue;
            }

            var t = Nullable.GetUnderlyingType(memberType) ?? memberType;
            if (defaultValue.GetType() == t)
            {
                return defaultValue;
            }

            if (t.IsEnum)
            {
                return EnumParser.Parse(t, (string)defaultValue, _namingPolicy);
            }

            // Map and record defaults (Dictionary<string, object>) are not translated to CLR types.
            return System.Convert.ChangeType(defaultValue, t);
        }

        private Delegate CompileItem(TypeSchema ws, TypeSchema rs, Type itemType)
        {
            var reader = Expression.Parameter(typeof(Reader), "reader");
            var body = Build(ws, rs, itemType, reader);
            return Expression.Lambda(typeof(Func<,>).MakeGenericType(typeof(Reader), itemType), body, reader).Compile();
        }

        // ------------------------------------------------------------------ helpers

        /// <summary>The reader branch of the same type (and name) as the writer's, otherwise the first one that can read it.</summary>
        private static TypeSchema FindBranch(UnionSchema readerUnion, TypeSchema writerSchema, bool renamedRecord)
        {
            foreach (var candidate in readerUnion.Schemas)
            {
                if (IsSameType(candidate, writerSchema))
                {
                    return candidate;
                }
            }

            foreach (var candidate in readerUnion.Schemas)
            {
                if (candidate.CanRead(writerSchema))
                {
                    return candidate;
                }
            }

            // A record read into ["null", record] with another name (anonymous type, ExpandoObject, a renamed class):
            // the only record branch is taken, as a record would be read without the union.
            if (renamedRecord && writerSchema is RecordSchema)
            {
                var records = readerUnion.Schemas.Where(s => s is RecordSchema).ToList();
                if (records.Count == 1)
                {
                    return records[0];
                }
            }

            return null;
        }

        private static bool IsSameType(TypeSchema reader, TypeSchema writer) =>
            reader.Type == writer.Type
            && (reader is not NamedSchema readerNamed || writer is NamedSchema writerNamed && readerNamed.Name == writerNamed.Name)
            && (reader is not LogicalTypeSchema readerLogical || writer is LogicalTypeSchema writerLogical && readerLogical.LogicalTypeName == writerLogical.LogicalTypeName);

        /// <summary>Element type for arrays, generic collections and their common interfaces; null when not a collection.</summary>
        /// <summary>
        /// True when each top-level container entry of <paramref name="writerSchema"/> is one item of the collection
        /// <paramref name="collectionType"/> (entries layout), false when an entry is an array holding the items.
        /// An <c>object</c> element leaves the depth of T open, so the writer's own entries layout wins.
        /// </summary>
        internal static bool EntryIsItem(TypeSchema writerSchema, Type collectionType)
        {
            int depth = CollectionDepth(collectionType, out var openEnded);
            int arrayDepth = ArrayDepth(writerSchema);
            return arrayDepth < depth || openEnded && arrayDepth == depth;
        }

        /// <summary>The array schema of <paramref name="schema"/>, also when it is a union of null and one array; null otherwise.</summary>
        private static ArraySchema AsArray(TypeSchema schema, out int nullIndex)
        {
            nullIndex = -1;
            if (schema is ArraySchema array)
            {
                return array;
            }

            if (schema is UnionSchema union && union.Schemas.Count == 2)
            {
                var nullBranch = union.Schemas.ToList().FindIndex(s => s.Type == AvroType.Null);
                if (nullBranch >= 0 && union.Schemas[1 - nullBranch] is ArraySchema branch)
                {
                    nullIndex = nullBranch;
                    return branch;
                }
            }

            return null;
        }

        private static int ArrayDepth(TypeSchema schema)
        {
            int depth = 0;
            for (var array = AsArray(schema, out _); array != null; array = AsArray(array.ItemSchema, out _))
            {
                depth++;
            }

            return depth;
        }

        private static int CollectionDepth(Type type, out bool openEnded)
        {
            int depth = 0;
            var item = CollectionItemType(type);
            for (; item != null; item = CollectionItemType(item))
            {
                depth++;
                type = item;
            }

            openEnded = type == typeof(object);
            return depth;
        }

        internal static Type CollectionItemType(Type type)
        {
            if (type == typeof(string) || type == typeof(byte[]) || type == typeof(object) || type.GetDictionaryArguments() != null)
            {
                return null;
            }

            if (type.IsArray && type.GetArrayRank() == 1)
            {
                return type.GetElementType();
            }

            if (type.IsGenericType && type.GetGenericArguments().Length == 1)
            {
                var itemType = type.GetGenericArguments()[0];
                var enumerable = typeof(IEnumerable<>).MakeGenericType(itemType);
                if (enumerable.IsAssignableFrom(type))
                {
                    return itemType;
                }
            }

            return null;
        }

        /// <summary>Turns a <c>List&lt;TItem&gt;</c> expression into the requested collection type; null when unsupported.</summary>
        private static Expression AdaptCollection(Expression list, Type itemType, Type target)
        {
            var listType = typeof(List<>).MakeGenericType(itemType);
            var enumerableType = typeof(IEnumerable<>).MakeGenericType(itemType);

            if (target == typeof(object))
            {
                return Expression.Convert(list, typeof(object));
            }

            if (target.IsArray)
            {
                return Expression.Call(list, listType.GetMethod(nameof(List<object>.ToArray))!);
            }

            if (target.IsAssignableFrom(listType))
            {
                return target == listType ? list : Expression.Convert(list, target);
            }

            if (target.IsInterface)
            {
                // ISet<T>, IReadOnlySet<T>, IImmutableSet<T>, IImmutableList<T> ...: pick a concrete type that implements it.
                var definition = target.GetGenericTypeDefinition();
                Type concrete = definition == typeof(ISet<>) || definition == typeof(IReadOnlySet<>)
                    ? typeof(HashSet<>).MakeGenericType(itemType)
                    : definition.Name.Contains("Set")
                        ? Type.GetType("System.Collections.Immutable.ImmutableHashSet`1, System.Collections.Immutable")?.MakeGenericType(itemType)
                        : Type.GetType("System.Collections.Immutable.ImmutableList`1, System.Collections.Immutable")?.MakeGenericType(itemType);

                return concrete != null && target.IsAssignableFrom(concrete)
                    ? Expression.Convert(AdaptCollection(list, itemType, concrete), target)
                    : null;
            }

            // Collections constructible from IEnumerable<T>: HashSet, Queue, Stack, LinkedList, SortedSet, ConcurrentBag, ObservableCollection, ...
            var fromEnumerable = target.GetConstructor(new[] { enumerableType });
            if (fromEnumerable != null)
            {
                return Expression.New(fromEnumerable, list);
            }

            var fromList = target.GetConstructor(new[] { typeof(IList<>).MakeGenericType(itemType) });
            if (fromList != null)
            {
                return Expression.New(fromList, list);
            }

            // Immutable collections: static Empty + Add(T) returning a new instance.
            var empty = target.GetField("Empty", BindingFlags.Public | BindingFlags.Static) as MemberInfo
                        ?? target.GetProperty("Empty", BindingFlags.Public | BindingFlags.Static);
            var add = target.GetMethod("Add", new[] { itemType });
            if (empty != null && add != null && add.ReturnType == target)
            {
                var folder = typeof(Func<,,>).MakeGenericType(target, itemType, target);
                var accumulator = Expression.Parameter(target, "acc");
                var item = Expression.Parameter(itemType, "item");
                var addFn = Expression.Lambda(folder, Expression.Call(accumulator, add, item), accumulator, item);
                return Expression.Call(FoldMethod.MakeGenericMethod(target, itemType), list, Expression.MakeMemberAccess(null, empty), addFn);
            }

            // Mutable collections with a parameterless constructor and void Add(T).
            if (target.GetConstructor(Type.EmptyTypes) != null && add != null && add.ReturnType == typeof(void))
            {
                var collection = Expression.Parameter(target, "collection");
                var item = Expression.Parameter(itemType, "item");
                var addFn = Expression.Lambda(typeof(Action<,>).MakeGenericType(target, itemType), Expression.Call(collection, add, item), collection, item);
                return Expression.Call(AddAllMethod.MakeGenericMethod(target, itemType), Expression.New(target), list, addFn);
            }

            return null;
        }

        private sealed class DelegateHolder<T>
        {
#pragma warning disable CS0649 // assigned through reflection in BuildRecord
            public Func<Reader, T> Fn;
#pragma warning restore CS0649
        }
    }
}
