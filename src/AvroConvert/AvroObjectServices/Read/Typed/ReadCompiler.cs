using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Newtonsoft.Json.Linq;
using SolTechnology.Avro.AvroObjectServices.Schemas;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;
using SolTechnology.Avro.AvroObjectServices.Skip;
using SolTechnology.Avro.Infrastructure.Exceptions;
using SolTechnology.Avro.Infrastructure.Extensions;
using SolTechnology.Avro.Infrastructure.Reflection;
using SolTechnology.Avro.Policies;

namespace SolTechnology.Avro.AvroObjectServices.Read.Typed
{
    /// <summary>
    /// Compiles a (writerSchema, readerSchema, T) triple into a strongly typed <c>Func&lt;IReader, T&gt;</c>.
    /// Schema resolution, member lookup and union branch mapping are decided once at compile time; the produced
    /// delegate performs no boxing for primitives and no per-value schema dispatch.
    /// Anything the compiler does not specialise (dynamic targets, JObject, exotic conversions) is delegated to the
    /// classic <see cref="Resolver"/>, so behaviour stays identical for those cases.
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
        private static readonly MethodInfo FallbackResolveMethod = typeof(Resolver).GetMethod(nameof(Resolver.Resolve), BindingFlags.Instance | BindingFlags.NonPublic, new[] { typeof(TypeSchema), typeof(TypeSchema), typeof(IReader), typeof(Type) })!;
        private static readonly MethodInfo ConvertToLogicalMethod = typeof(LogicalTypeSchema).GetMethod(nameof(LogicalTypeSchema.ConvertToLogicalValue), BindingFlags.Instance | BindingFlags.NonPublic)!;
        private static readonly MethodInfo GetUninitializedObjectMethod = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.GetUninitializedObject))!;
        private static readonly MethodInfo ReadListMethod = typeof(ReadHelpers).GetMethod(nameof(ReadHelpers.ReadList), BindingFlags.Static | BindingFlags.NonPublic)!;
        private static readonly MethodInfo ReadCountIntoMethod = typeof(ReadHelpers).GetMethod(nameof(ReadHelpers.ReadCountInto), BindingFlags.Static | BindingFlags.NonPublic)!;
        private static readonly MethodInfo ReadMapMethod = typeof(ReadHelpers).GetMethod(nameof(ReadHelpers.ReadMap), BindingFlags.Static | BindingFlags.NonPublic)!;
        private static readonly MethodInfo UnionIndexOutOfRangeMethod = typeof(ReadHelpers).GetMethod(nameof(ReadHelpers.UnionIndexOutOfRange), BindingFlags.Static | BindingFlags.NonPublic)!;

        private readonly Resolver _fallback;
        private readonly Skipper _skipper = new();
        private readonly AvroConvertOptions _options;
        private readonly Dictionary<Type, Func<IReader, object>> _converters;
        private readonly Dictionary<(TypeSchema, TypeSchema, Type), object> _recordHolders = new();

        internal ReadCompiler(TypeSchema writerSchema, TypeSchema readerSchema, AvroConvertOptions options)
        {
            _options = options;
            _fallback = new Resolver(writerSchema, readerSchema, options);
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
                        return BuildRecord((RecordSchema)ws, rs as RecordSchema, target, reader);
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
                        return Fallback(ws, rs, target, reader);
                }
            }
            catch (NotSupportedException)
            {
                return Fallback(ws, rs, target, reader);
            }
        }

        private Expression Fallback(TypeSchema ws, TypeSchema rs, Type target, ParameterExpression reader)
        {
            var call = Expression.Call(Expression.Constant(_fallback), FallbackResolveMethod,
                Expression.Constant(ws, typeof(TypeSchema)), Expression.Constant(rs, typeof(TypeSchema)), reader, Expression.Constant(target));
            return target == typeof(object) ? call : Expression.Convert(call, target);
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

            throw new NotSupportedException($"No typed conversion from {value.Type} to {target}");
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

            throw new NotSupportedException();
        }

        private Expression BuildLogical(LogicalTypeSchema ws, TypeSchema rs, Type target, ParameterExpression reader)
        {
            var baseRs = rs is LogicalTypeSchema logicalRs ? logicalRs.BaseTypeSchema : rs;
            var baseValue = Build(ws.BaseTypeSchema, baseRs, typeof(object), reader);

            // Same conversion the classic resolver performs; the only boxing left is the base value itself.
            var converted = Expression.Call(Expression.Constant(ws), ConvertToLogicalMethod, baseValue, Expression.Constant(ws), Expression.Constant(target));
            return target == typeof(object) ? converted : Expression.Convert(converted, target);
        }

        private Expression BuildEnum(EnumSchema ws, Type target, ParameterExpression reader)
        {
            var enumType = Nullable.GetUnderlyingType(target) ?? target;
            if (!enumType.IsEnum)
            {
                throw new NotSupportedException();
            }

            var index = Expression.Call(reader, ReadEnumMethod);
            var cases = new List<SwitchCase>(ws.Symbols.Count);
            for (int i = 0; i < ws.Symbols.Count; i++)
            {
                Expression value;
                try
                {
                    value = Expression.Constant(EnumParser.Parse(enumType, ws.Symbols[i], _options?.NamingPolicy), enumType);
                }
                catch (KeyNotFoundException)
                {
                    value = Expression.Throw(Expression.Constant(new AvroTypeMismatchException($"Enum symbol '{ws.Symbols[i]}' is not defined on {enumType}")), enumType);
                }

                cases.Add(Expression.SwitchCase(Expression.Convert(value, target), Expression.Constant(i)));
            }

            var outOfRange = Expression.Throw(Expression.Constant(new AvroTypeMismatchException($"Enum position out of range for {ws.FullName}")), target);
            return Expression.Switch(index, outOfRange, cases.ToArray());
        }

        private static Expression BuildFixed(FixedSchema ws, Type target, ParameterExpression reader)
        {
            if (target != typeof(byte[]) && target != typeof(Guid) && target != typeof(object))
            {
                throw new NotSupportedException();
            }

            var buffer = Expression.Variable(typeof(byte[]), "fixed");
            var fill = Expression.Call(reader, ReadFixedMethod, buffer);
            Expression result = target == typeof(Guid)
                ? Expression.New(typeof(Guid).GetConstructor(new[] { typeof(byte[]) })!, buffer)
                : Expression.Convert(buffer, target);

            return Expression.Block(target, new[] { buffer },
                Expression.Assign(buffer, Expression.NewArrayBounds(typeof(byte), Expression.Constant(ws.Size))),
                fill,
                result);
        }

        private Expression BuildArray(ArraySchema ws, TypeSchema rs, Type target, ParameterExpression reader)
        {
            if (target == typeof(object) || target.IsDictionary())
            {
                throw new NotSupportedException();
            }

            var itemType = CollectionItemType(target) ?? throw new NotSupportedException();
            var itemRs = rs is ArraySchema ras ? ras.ItemSchema : rs;
            if (itemRs.IsEmpty())
            {
                itemRs = ws.ItemSchema;
            }

            var itemFn = CompileItem(ws.ItemSchema, itemRs, itemType);
            var list = Expression.Call(ReadListMethod.MakeGenericMethod(itemType), reader, Expression.Constant(itemFn));
            return AdaptCollection(list, itemType, target) ?? throw new NotSupportedException();
        }

        private Expression BuildMap(MapSchema ws, TypeSchema rs, Type target, ParameterExpression reader)
        {
            if (!target.IsGenericType)
            {
                throw new NotSupportedException();
            }

            var args = target.GetGenericArguments();
            if (args.Length != 2 || args[0] != typeof(string))
            {
                throw new NotSupportedException();
            }

            var valueType = args[1];
            var definition = target.GetGenericTypeDefinition();
            if (definition != typeof(Dictionary<,>) && definition != typeof(IDictionary<,>) && definition != typeof(IReadOnlyDictionary<,>))
            {
                throw new NotSupportedException();
            }

            var valueRs = rs is MapSchema mrs ? mrs.ValueSchema : rs;
            var valueFn = CompileItem(ws.ValueSchema, valueRs, valueType);
            var map = Expression.Call(ReadMapMethod.MakeGenericMethod(valueType), reader, Expression.Constant(valueFn));
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

        private Expression BuildRecord(RecordSchema ws, RecordSchema rs, Type target, ParameterExpression reader)
        {
            if (rs == null || target == typeof(object) || target == typeof(ExpandoObject) || typeof(JToken).IsAssignableFrom(target)
                || target.IsAbstract || target.IsInterface || target.IsPrimitive || target == typeof(string))
            {
                throw new NotSupportedException();
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
                    // Classic behaviour: a null read for a field with a writer default yields that default.
                    object fallbackDefault = null;
                    try { fallbackDefault = _fallback.FormatDefaultValue(wf.DefaultValue, member); }
                    catch (Exception) { /* default not representable as the member type – keep the raw value */ }

                    if (fallbackDefault != null)
                    {
                        value = Expression.Coalesce(value, Expression.Constant(fallbackDefault, member.Type));
                    }
                }

                body.Add(Expression.Assign(Expression.MakeMemberAccess(instance, member.Info), value));
            }

            foreach (var (member, value) in _fallback.ResolveMissingReaderFields(ws, rs, members))
            {
                body.Add(Expression.Assign(Expression.MakeMemberAccess(instance, member.Info), Expression.Constant(value, member.Type)));
            }

            body.Add(instance);
            var lambda = Expression.Lambda(typeof(Func<,>).MakeGenericType(typeof(IReader), target), Expression.Block(target, new[] { instance }, body), reader);
            return lambda.Compile();
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
            if (type == typeof(string) || type == typeof(byte[]) || type == typeof(object))
            {
                return null;
            }

            if (type.IsArray && type.GetArrayRank() == 1)
            {
                return type.GetElementType();
            }

            if (type.IsGenericType)
            {
                var definition = type.GetGenericTypeDefinition();
                if (definition == typeof(List<>) || definition == typeof(IList<>) || definition == typeof(ICollection<>) ||
                    definition == typeof(IEnumerable<>) || definition == typeof(IReadOnlyList<>) || definition == typeof(IReadOnlyCollection<>) ||
                    definition == typeof(HashSet<>) || definition == typeof(ISet<>) || definition == typeof(IReadOnlySet<>) ||
                    definition == typeof(Collection<>) || definition == typeof(ReadOnlyCollection<>))
                {
                    return type.GetGenericArguments()[0];
                }
            }

            return null;
        }

        /// <summary>Turns a <c>List&lt;TItem&gt;</c> expression into the requested collection type; null when unsupported.</summary>
        private static Expression AdaptCollection(Expression list, Type itemType, Type target)
        {
            var listType = typeof(List<>).MakeGenericType(itemType);

            if (target.IsArray)
            {
                return Expression.Call(list, listType.GetMethod(nameof(List<object>.ToArray))!);
            }

            if (target.IsAssignableFrom(listType))
            {
                return target == listType ? list : Expression.Convert(list, target);
            }

            var definition = target.IsGenericType ? target.GetGenericTypeDefinition() : null;
            if (definition == typeof(HashSet<>) || definition == typeof(ISet<>) || definition == typeof(IReadOnlySet<>))
            {
                var hashSetType = typeof(HashSet<>).MakeGenericType(itemType);
                var ctor = hashSetType.GetConstructor(new[] { typeof(IEnumerable<>).MakeGenericType(itemType) })!;
                return Expression.Convert(Expression.New(ctor, list), target);
            }

            if (definition == typeof(Collection<>) || definition == typeof(ReadOnlyCollection<>))
            {
                var ctor = target.GetConstructor(new[] { typeof(IList<>).MakeGenericType(itemType) })!;
                return Expression.New(ctor, list);
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
