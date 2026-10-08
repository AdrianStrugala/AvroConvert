using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using SolTechnology.Avro.AvroObjectServices.Schemas;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;
using SolTechnology.Avro.AvroObjectServices.Skip;
using SolTechnology.Avro.Infrastructure.Exceptions;
using SolTechnology.Avro.Infrastructure.Reflection;
using SolTechnology.Avro.Policies;

namespace SolTechnology.Avro.AvroObjectServices.Read.Typed
{
    /// <summary>
    /// Compiles a (writerSchema, readerSchema, T) triple into a strongly typed <c>Func&lt;IReader, T&gt;</c>.
    /// Schema resolution, member lookup, union branch mapping and conversions are decided once at compile time; the
    /// produced delegate performs no boxing for primitives and no per-value schema dispatch. Dynamic targets
    /// (<c>object</c>) materialise as ExpandoObject / List&lt;object&gt; / Dictionary&lt;string, object&gt;, or as a CLR type
    /// whose name matches the record schema.
    /// </summary>
    internal sealed class ReadCompiler
    {
        private static readonly MethodInfo ReadBooleanMethod = typeof(IReader).GetMethod(nameof(IReader.ReadBoolean))!;
        private static readonly MethodInfo ReadIntMethod = typeof(IReader).GetMethod(nameof(IReader.ReadInt))!;
        private static readonly MethodInfo ReadLongMethod = typeof(IReader).GetMethod(nameof(IReader.ReadLong))!;
        private static readonly MethodInfo ReadFloatMethod = typeof(IReader).GetMethod(nameof(IReader.ReadFloat))!;
        private static readonly MethodInfo ReadDoubleMethod = typeof(IReader).GetMethod(nameof(IReader.ReadDouble))!;
        private static readonly MethodInfo ReadStringMethod = typeof(IReader).GetMethod(nameof(IReader.ReadString))!;
        private static readonly MethodInfo ReadBytesMethod = typeof(IReader).GetMethod(nameof(IReader.ReadBytes))!;
        private static readonly MethodInfo ReadEnumMethod = typeof(IReader).GetMethod(nameof(IReader.ReadEnum))!;
        private static readonly MethodInfo ReadUnionIndexMethod = typeof(IReader).GetMethod(nameof(IReader.ReadUnionIndex))!;
        private static readonly MethodInfo ReadFixedMethod = typeof(IReader).GetMethod(nameof(IReader.ReadFixed), new[] { typeof(byte[]) })!;
        private static readonly MethodInfo SkipMethod = typeof(Skipper).GetMethod(nameof(Skipper.Skip), BindingFlags.Instance | BindingFlags.NonPublic)!;
        private static readonly MethodInfo ConvertToLogicalMethod = typeof(LogicalTypeSchema).GetMethod(nameof(LogicalTypeSchema.ConvertToLogicalValue), BindingFlags.Instance | BindingFlags.NonPublic)!;
        private static readonly MethodInfo GetUninitializedObjectMethod = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.GetUninitializedObject))!;
        private static readonly MethodInfo ReadListMethod = Helper(nameof(ReadHelpers.ReadList));
        private static readonly MethodInfo ReadCountIntoMethod = Helper(nameof(ReadHelpers.ReadCountInto));
        private static readonly MethodInfo ReadMapMethod = Helper(nameof(ReadHelpers.ReadMap));
        private static readonly MethodInfo ReadMapKeyedMethod = Helper(nameof(ReadHelpers.ReadMapKeyed));
        private static readonly MethodInfo ReadDictionaryMethod = Helper(nameof(ReadHelpers.ReadDictionary));
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

        internal ReadCompiler(AvroConvertOptions options)
        {
            _namingPolicy = options?.NamingPolicy;
            _missingFieldHandling = options?.MissingFieldHandling ?? AvroMissingFieldHandling.Throw;
            _converters = options?.AvroConverters.Count > 0
                ? options.AvroConverters.ToDictionary(c => c.TypeSchema.RuntimeType, c => (Func<IReader, object>)c.Deserialize)
                : null;
        }

        internal Func<IReader, T> CompileOne<T>(TypeSchema writerSchema, TypeSchema readerSchema)
        {
            var reader = Expression.Parameter(typeof(IReader), "reader");
            var body = Build(writerSchema, readerSchema, typeof(T), reader);
            return Expression.Lambda<Func<IReader, T>>(body, reader).Compile();
        }

        /// <summary>Block-wise reader of top-level container entries into a collection T; null when T is not a collection.</summary>
        internal ManyPlan<T> CompileMany<T>(TypeSchema writerSchema, TypeSchema readerSchema)
        {
            var itemType = CollectionItemType(typeof(T));
            if (itemType == null)
            {
                return null;
            }

            var itemWriter = writerSchema is ArraySchema was ? was.ItemSchema : writerSchema;
            var itemReader = readerSchema is ArraySchema ras ? ras.ItemSchema : readerSchema;
            if (itemReader.IsEmpty())
            {
                itemReader = itemWriter;
            }

            var listType = typeof(List<>).MakeGenericType(itemType);
            var reader = Expression.Parameter(typeof(IReader), "reader");
            var count = Expression.Parameter(typeof(long), "count");
            var accumulator = Expression.Parameter(typeof(object), "accumulator");
            var itemFn = CompileItem(itemWriter, itemReader, itemType);

            var finishBody = AdaptCollection(Expression.Convert(accumulator, listType), itemType, typeof(T));
            if (finishBody == null)
            {
                return null;
            }

            var append = Expression.Call(ReadCountIntoMethod.MakeGenericMethod(itemType), reader, count, Expression.Constant(itemFn), Expression.Convert(accumulator, listType));

            return new ManyPlan<T>
            {
                NewAccumulator = Expression.Lambda<Func<object>>(Expression.Convert(Expression.New(listType), typeof(object))).Compile(),
                Append = Expression.Lambda<Action<IReader, long, object>>(append, reader, count, accumulator).Compile(),
                Finish = Expression.Lambda<Func<object, T>>(finishBody, accumulator).Compile()
            };
        }

        // ------------------------------------------------------------------ core

        private Expression Build(TypeSchema ws, TypeSchema rs, Type target, ParameterExpression reader)
        {
            if (_converters != null && _converters.TryGetValue(target, out var converter))
            {
                return Expression.Convert(Expression.Invoke(Expression.Constant(converter), reader), target);
            }

            if (ws.Type != AvroType.Union && rs is UnionSchema readerUnion)
            {
                rs = FindBranch(readerUnion, ws) ?? rs;
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
                        return Convert(Expression.Call(reader, ReadBytesMethod), target);
                    case AvroType.Logical:
                        return BuildLogical((LogicalTypeSchema)ws, rs, target, reader);
                    case AvroType.Record:
                    case AvroType.Error:
                        if (rs is not RecordSchema readerRecord)
                        {
                            throw new NotSupportedException($"Schema mismatch: reader schema [{rs.Type}] cannot read a record");
                        }
                        return BuildRecord((RecordSchema)ws, readerRecord, target, reader);
                    case AvroType.Enum:
                        return BuildEnum((EnumSchema)ws, target, reader);
                    case AvroType.Fixed:
                        return BuildFixed((FixedSchema)ws, target, reader);
                    case AvroType.Array:
                        return BuildArray((ArraySchema)ws, rs, target, reader);
                    case AvroType.Map:
                        return BuildMap((MapSchema)ws, rs, target, reader);
                    case AvroType.Union:
                        return BuildUnion((UnionSchema)ws, rs, target, reader);
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
            if (underlying == typeof(decimal))
            {
                return Expression.Convert(Expression.Call(typeof(decimal).GetMethod(nameof(decimal.Parse), new[] { typeof(string) })!, read), target);
            }
            if (underlying == typeof(Guid))
            {
                return Expression.Convert(Expression.Call(typeof(Guid).GetMethod(nameof(Guid.Parse), new[] { typeof(string) })!, read), target);
            }
            if (underlying == typeof(DateTimeOffset))
            {
                return Expression.Convert(Expression.Call(typeof(DateTimeOffset).GetMethod(nameof(DateTimeOffset.Parse), new[] { typeof(string) })!, read), target);
            }
            if (target == typeof(Uri))
            {
                return Expression.New(typeof(Uri).GetConstructor(new[] { typeof(string) })!, read);
            }

            throw new NotSupportedException($"No conversion from string to {target}");
        }

        private Expression BuildLogical(LogicalTypeSchema ws, TypeSchema rs, Type target, ParameterExpression reader)
        {
            var baseRs = rs is LogicalTypeSchema logicalRs ? logicalRs.BaseTypeSchema : rs;
            var baseValue = Build(ws.BaseTypeSchema, baseRs, typeof(object), reader);

            var converted = Expression.Call(Expression.Constant(ws), ConvertToLogicalMethod, baseValue, Expression.Constant(ws), Expression.Constant(target));
            return target == typeof(object) ? converted : Expression.Convert(converted, target);
        }

        private Expression BuildEnum(EnumSchema ws, Type target, ParameterExpression reader)
        {
            var index = Expression.Call(reader, ReadEnumMethod);
            var enumType = Nullable.GetUnderlyingType(target) ?? target;
            var cases = new List<SwitchCase>(ws.Symbols.Count);

            for (int i = 0; i < ws.Symbols.Count; i++)
            {
                Expression value;
                if (enumType.IsEnum)
                {
                    try
                    {
                        value = Expression.Convert(Expression.Constant(EnumParser.Parse(enumType, ws.Symbols[i], _namingPolicy), enumType), target);
                    }
                    catch (KeyNotFoundException)
                    {
                        value = Expression.Throw(Expression.Constant(new AvroTypeMismatchException($"Enum symbol '{ws.Symbols[i]}' is not defined on {enumType}")), target);
                    }
                }
                else if (target == typeof(string) || target == typeof(object))
                {
                    value = Expression.Constant(ws.Symbols[i], target);
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
            if (DictionaryArguments(target) is { } kv && ws.ItemSchema is RecordSchema itemRecord
                && itemRecord.TryGetField("Key", out var keyField) && itemRecord.TryGetField("Value", out var valueField))
            {
                var readerRecord = itemRs as RecordSchema ?? itemRecord;
                readerRecord.TryGetField("Key", out var readerKey);
                readerRecord.TryGetField("Value", out var readerValue);

                var keyFn = CompileItem(keyField.TypeSchema, readerKey?.TypeSchema ?? keyField.TypeSchema, kv.Key);
                var valueFn = CompileItem(valueField.TypeSchema, readerValue?.TypeSchema ?? valueField.TypeSchema, kv.Value);
                var dictionary = Expression.Call(ReadDictionaryMethod.MakeGenericMethod(kv.Key, kv.Value), reader, Expression.Constant(keyFn), Expression.Constant(valueFn));
                return Expression.Convert(dictionary, target);
            }

            var itemType = target == typeof(object) ? typeof(object) : CollectionItemType(target) ?? throw new NotSupportedException($"Array cannot be read as {target}");
            var itemFn = CompileItem(ws.ItemSchema, itemRs, itemType);
            var list = Expression.Call(ReadListMethod.MakeGenericMethod(itemType), reader, Expression.Constant(itemFn));
            return AdaptCollection(list, itemType, target) ?? throw new NotSupportedException($"Array cannot be read as {target}");
        }

        private Expression BuildMap(MapSchema ws, TypeSchema rs, Type target, ParameterExpression reader)
        {
            var valueRs = rs is MapSchema mrs ? mrs.ValueSchema : rs;

            if (target == typeof(object))
            {
                var dynamicValueFn = CompileItem(ws.ValueSchema, valueRs, typeof(object));
                return Expression.Convert(Expression.Call(ReadMapMethod.MakeGenericMethod(typeof(object)), reader, Expression.Constant(dynamicValueFn)), typeof(object));
            }

            var kv = DictionaryArguments(target) ?? throw new NotSupportedException($"Map cannot be read as {target}");
            var valueFn = CompileItem(ws.ValueSchema, valueRs, kv.Value);

            if (kv.Key == typeof(string))
            {
                return Expression.Convert(Expression.Call(ReadMapMethod.MakeGenericMethod(kv.Value), reader, Expression.Constant(valueFn)), target);
            }

            // Non-string key type: convert each string key the same way a string value would be converted.
            var keyReader = Expression.Parameter(typeof(IReader), "reader");
            var keyFn = Expression.Lambda(typeof(Func<,>).MakeGenericType(typeof(IReader), kv.Key), BuildString(keyReader, kv.Key), keyReader).Compile();
            var map = Expression.Call(ReadMapKeyedMethod.MakeGenericMethod(kv.Key, kv.Value), reader, Expression.Constant(keyFn), Expression.Constant(valueFn));
            return Expression.Convert(map, target);
        }

        private Expression BuildUnion(UnionSchema ws, TypeSchema rs, Type target, ParameterExpression reader)
        {
            var index = Expression.Call(reader, ReadUnionIndexMethod);
            var cases = new List<SwitchCase>(ws.Schemas.Count);

            for (int i = 0; i < ws.Schemas.Count; i++)
            {
                var branchWs = ws.Schemas[i];
                var branchRs = rs is UnionSchema readerUnion ? FindBranch(readerUnion, branchWs) : rs;

                Expression body;
                if (branchRs == null || (rs is not UnionSchema && !rs.CanRead(branchWs)))
                {
                    body = Expression.Throw(Expression.Constant(new AvroException($"Schema mismatch. Reader: {rs}, writer: {branchWs}")), target);
                }
                else
                {
                    body = Build(branchWs, branchRs, target, reader);
                }

                cases.Add(Expression.SwitchCase(body, Expression.Constant(i)));
            }

            var outOfRange = Expression.Throw(Expression.Call(UnionIndexOutOfRangeMethod, index, Expression.Constant(ws.Schemas.Count)), target);
            return Expression.Switch(index, outOfRange, cases.ToArray());
        }

        // ------------------------------------------------------------------ records

        private Expression BuildRecord(RecordSchema ws, RecordSchema rs, Type target, ParameterExpression reader)
        {
            if (target == typeof(object))
            {
                var clrType = ClrTypeCache.Find(rs);
                if (clrType != null)
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
                || DictionaryArguments(target) != null || CollectionItemType(target) != null)
            {
                throw new NotSupportedException($"Record cannot be read as {target}");
            }

            // Records are compiled into their own delegates (via a holder) so recursive schemas terminate.
            var key = (ws, (TypeSchema)rs, target);
            if (!_recordHolders.TryGetValue(key, out var holderObj))
            {
                holderObj = Activator.CreateInstance(typeof(DelegateHolder<>).MakeGenericType(target))!;
                _recordHolders.Add(key, holderObj);
                var holderField = holderObj.GetType().GetField(nameof(DelegateHolder<object>.Fn))!;
                holderField.SetValue(holderObj, CompileRecordDelegate(ws, rs, target));
            }

            var fn = Expression.Field(Expression.Constant(holderObj), nameof(DelegateHolder<object>.Fn));
            return Expression.Invoke(fn, reader);
        }

        private Delegate CompileRecordDelegate(RecordSchema ws, RecordSchema rs, Type target)
        {
            var reader = Expression.Parameter(typeof(IReader), "reader");
            var instance = Expression.Variable(target, "instance");
            var members = TypeMembers.For(target);
            var body = new List<Expression>
            {
                Expression.Assign(instance, Expression.Convert(Expression.Call(GetUninitializedObjectMethod, Expression.Constant(target)), target))
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

                Expression value = Build(wf.TypeSchema, rf.TypeSchema, member.Type, reader);
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
                if (value == null && !clrNullable)
                {
                    // UseDefault on a non-nullable value type: leave the CLR default rather than assigning null.
                    continue;
                }

                body.Add(Expression.Assign(Expression.MakeMemberAccess(instance, member.Info), Expression.Constant(value, member.Type)));
            }

            body.Add(instance);
            var lambda = Expression.Lambda(typeof(Func<,>).MakeGenericType(typeof(IReader), target), Expression.Block(target, new[] { instance }, body), reader);
            return lambda.Compile();
        }

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
                body.Add(Expression.Call(dictionary, ExpandoAddMethod, Expression.Constant(name), Expression.Constant(value, typeof(object))));
            }

            body.Add(Expression.Convert(dictionary, typeof(object)));
            return Expression.Block(typeof(object), new[] { dictionary }, body);
        }

        /// <summary>
        /// Avro schema resolution for a reader field absent from the writer: the reader's default; otherwise null for
        /// fields that can hold null, or an error governed by MissingFieldHandling.
        /// </summary>
        private object ResolveMissingReaderField(RecordSchema readerSchema, RecordFieldSchema rf, Type memberType, bool clrNullable)
        {
            if (rf.HasDefaultValue)
            {
                return rf.DefaultValue == null ? null : FormatDefaultValue(rf.DefaultValue, memberType);
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
            var reader = Expression.Parameter(typeof(IReader), "reader");
            var body = Build(ws, rs, itemType, reader);
            return Expression.Lambda(typeof(Func<,>).MakeGenericType(typeof(IReader), itemType), body, reader).Compile();
        }

        // ------------------------------------------------------------------ helpers

        private static TypeSchema FindBranch(UnionSchema readerUnion, TypeSchema writerSchema)
        {
            foreach (var candidate in readerUnion.Schemas)
            {
                if (candidate.CanRead(writerSchema))
                {
                    return candidate;
                }
            }

            return null;
        }

        /// <summary>Element type for arrays, generic collections and their common interfaces; null when not a collection.</summary>
        internal static Type CollectionItemType(Type type)
        {
            if (type == typeof(string) || type == typeof(byte[]) || type == typeof(object) || DictionaryArguments(type) != null)
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

        /// <summary>(Key, Value) for generic dictionary types and interfaces; null otherwise.</summary>
        internal static (Type Key, Type Value)? DictionaryArguments(Type type)
        {
            if (!type.IsGenericType)
            {
                return null;
            }

            var args = type.GetGenericArguments();
            if (args.Length != 2)
            {
                return null;
            }

            var readOnly = typeof(IReadOnlyDictionary<,>).MakeGenericType(args);
            var mutable = typeof(IDictionary<,>).MakeGenericType(args);
            return readOnly.IsAssignableFrom(type) || mutable.IsAssignableFrom(type) ? (args[0], args[1]) : null;
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
            public Func<IReader, T> Fn;
#pragma warning restore CS0649
        }
    }
}
