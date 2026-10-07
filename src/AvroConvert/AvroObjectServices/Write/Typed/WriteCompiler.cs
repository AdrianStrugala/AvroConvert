using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Newtonsoft.Json.Linq;
using SolTechnology.Avro.AvroObjectServices.Read;
using SolTechnology.Avro.AvroObjectServices.Schemas;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;
using SolTechnology.Avro.Features.Serialize;
using SolTechnology.Avro.Infrastructure.Extensions;
using SolTechnology.Avro.Infrastructure.Reflection;

namespace SolTechnology.Avro.AvroObjectServices.Write.Typed
{
    /// <summary>
    /// Compiles a (schema, T) pair into a strongly typed <c>Action&lt;IWriter, T&gt;</c>: member access, numeric
    /// conversions, enum positions and nullable-union branches are decided once at compile time, so the produced
    /// delegate boxes nothing for primitives. Anything not specialised (dynamic/ExpandoObject/JObject values,
    /// multi-branch unions on object-typed members, decimal/duration/time logical types) is delegated to the classic
    /// <see cref="WriteResolver"/> so behaviour stays identical for those cases.
    /// </summary>
    internal sealed class WriteCompiler
    {
        private static readonly MethodInfo WriteNullMethod = typeof(IWriter).GetMethod(nameof(IWriter.WriteNull))!;
        private static readonly MethodInfo WriteBooleanMethod = typeof(IWriter).GetMethod(nameof(IWriter.WriteBoolean))!;
        private static readonly MethodInfo WriteIntMethod = typeof(IWriter).GetMethod(nameof(IWriter.WriteInt))!;
        private static readonly MethodInfo WriteLongMethod = typeof(IWriter).GetMethod(nameof(IWriter.WriteLong))!;
        private static readonly MethodInfo WriteFloatMethod = typeof(IWriter).GetMethod(nameof(IWriter.WriteFloat))!;
        private static readonly MethodInfo WriteDoubleMethod = typeof(IWriter).GetMethod(nameof(IWriter.WriteDouble))!;
        private static readonly MethodInfo WriteStringMethod = typeof(IWriter).GetMethod(nameof(IWriter.WriteString))!;
        private static readonly MethodInfo WriteBytesMethod = typeof(IWriter).GetMethod(nameof(IWriter.WriteBytes), new[] { typeof(byte[]) })!;
        private static readonly MethodInfo WriteEnumMethod = typeof(IWriter).GetMethod(nameof(IWriter.WriteEnum))!;
        private static readonly MethodInfo WriteUnionIndexMethod = typeof(IWriter).GetMethod(nameof(IWriter.WriteUnionIndex))!;
        private static readonly MethodInfo WriteArrayMethod = typeof(WriteHelpers).GetMethod(nameof(WriteHelpers.WriteArray), BindingFlags.Static | BindingFlags.NonPublic)!;
        private static readonly MethodInfo WriteListMethod = typeof(WriteHelpers).GetMethod(nameof(WriteHelpers.WriteList), BindingFlags.Static | BindingFlags.NonPublic)!;
        private static readonly MethodInfo WriteEnumerableMethod = typeof(WriteHelpers).GetMethod(nameof(WriteHelpers.WriteEnumerable), BindingFlags.Static | BindingFlags.NonPublic)!;
        private static readonly MethodInfo WriteMapMethod = typeof(WriteHelpers).GetMethod(nameof(WriteHelpers.WriteMap), BindingFlags.Static | BindingFlags.NonPublic)!;
        private static readonly PropertyInfo DateTimeTicks = typeof(DateTime).GetProperty(nameof(DateTime.Ticks))!;
        private static readonly PropertyInfo DateTimeOffsetDateTime = typeof(DateTimeOffset).GetProperty(nameof(DateTimeOffset.DateTime))!;
        private static readonly PropertyInfo DateOnlyDayNumber = typeof(DateOnly).GetProperty(nameof(DateOnly.DayNumber))!;
        private static readonly MethodInfo GuidToString = typeof(Guid).GetMethod(nameof(Guid.ToString), Type.EmptyTypes)!;

        private readonly WriteResolver _fallback;
        private readonly AvroConvertOptions _options;
        private readonly Dictionary<Type, Action<object, IWriter>> _converters;
        private readonly Dictionary<(TypeSchema, Type), object> _recordHolders = new();

        internal WriteCompiler(AvroConvertOptions options)
        {
            _options = options;
            _fallback = new WriteResolver(options);
            _converters = options?.AvroConverters.Count > 0
                ? options.AvroConverters.ToDictionary(c => c.TypeSchema.RuntimeType, c => (Action<object, IWriter>)c.Serialize)
                : null;
        }

        /// <summary>Compiles a writer that accepts the root as <c>object</c> and casts it once to <paramref name="type"/>.</summary>
        internal Encoder.WriteItem Compile(TypeSchema schema, Type type)
        {
            var value = Expression.Parameter(typeof(object), "value");
            var writer = Expression.Parameter(typeof(IWriter), "writer");
            var body = Build(schema, type, Expression.Convert(value, type), writer);
            return Expression.Lambda<Encoder.WriteItem>(body, value, writer).Compile();
        }

        // ------------------------------------------------------------------ core

        private Expression Build(TypeSchema schema, Type type, Expression value, ParameterExpression writer)
        {
            if (_converters != null && schema.RuntimeType != null && _converters.TryGetValue(schema.RuntimeType, out var converter))
            {
                return Expression.Invoke(Expression.Constant(converter), Expression.Convert(value, typeof(object)), writer);
            }

            if (type == typeof(object) || type == typeof(ExpandoObject) || typeof(JToken).IsAssignableFrom(type) || type.IsInterface && schema.Type == AvroType.Record)
            {
                return Fallback(schema, value, writer);
            }

            try
            {
                switch (schema.Type)
                {
                    case AvroType.Null:
                        return Expression.Call(writer, WriteNullMethod);
                    case AvroType.Boolean:
                        return Expression.Call(writer, WriteBooleanMethod, Convert(value, typeof(bool)));
                    case AvroType.Int:
                        return Expression.Call(writer, WriteIntMethod, Convert(value, typeof(int)));
                    case AvroType.Long:
                        return Expression.Call(writer, WriteLongMethod, Convert(value, typeof(long)));
                    case AvroType.Float:
                        return Expression.Call(writer, WriteFloatMethod, Convert(value, typeof(float)));
                    case AvroType.Double:
                        return Expression.Call(writer, WriteDoubleMethod, Convert(value, typeof(double)));
                    case AvroType.String:
                        return BuildString(type, value, writer);
                    case AvroType.Bytes:
                        if (type != typeof(byte[])) throw new NotSupportedException();
                        return Expression.Call(writer, WriteBytesMethod, value);
                    case AvroType.Logical:
                        return BuildLogical((LogicalTypeSchema)schema, type, value, writer);
                    case AvroType.Record:
                    case AvroType.Error:
                        return BuildRecord((RecordSchema)schema, type, value, writer);
                    case AvroType.Enum:
                        return BuildEnum((EnumSchema)schema, type, value, writer);
                    case AvroType.Array:
                        return BuildArray((ArraySchema)schema, type, value, writer);
                    case AvroType.Map:
                        return BuildMap((MapSchema)schema, type, value, writer);
                    case AvroType.Union:
                        return BuildUnion((UnionSchema)schema, type, value, writer);
                    default:
                        throw new NotSupportedException();
                }
            }
            catch (NotSupportedException)
            {
                return Fallback(schema, value, writer);
            }
        }

        private Expression Fallback(TypeSchema schema, Expression value, ParameterExpression writer)
        {
            var writeItem = _fallback.ResolveWriter(schema);
            return Expression.Invoke(Expression.Constant(writeItem), Expression.Convert(value, typeof(object)), writer);
        }

        /// <summary>Widening / enum-to-integer conversion of a non-nullable primitive; throws NotSupported otherwise.</summary>
        private static Expression Convert(Expression value, Type target)
        {
            var type = value.Type;
            if (type == target)
            {
                return value;
            }

            if (Nullable.GetUnderlyingType(type) != null)
            {
                throw new NotSupportedException("Nullable values are written through a union");
            }

            if (target == typeof(bool))
            {
                throw new NotSupportedException();
            }

            if (type.IsEnum || (type.IsPrimitive && type != typeof(bool)) || type == typeof(decimal))
            {
                return Expression.Convert(value, target);
            }

            throw new NotSupportedException($"No typed conversion from {type} to {target}");
        }

        private static Expression BuildString(Type type, Expression value, ParameterExpression writer)
        {
            if (type == typeof(string))
            {
                // Classic behaviour: a null string is written as an empty string.
                return Expression.Call(writer, WriteStringMethod, Expression.Coalesce(value, Expression.Constant(string.Empty)));
            }

            if (type == typeof(Guid) || type == typeof(decimal) || type == typeof(DateTimeOffset) || type == typeof(Uri) || type.IsEnum)
            {
                var toString = type.GetMethod(nameof(ToString), Type.EmptyTypes)!;
                return Expression.Call(writer, WriteStringMethod, Expression.Call(value, toString));
            }

            throw new NotSupportedException();
        }

        private Expression BuildLogical(LogicalTypeSchema schema, Type type, Expression value, ParameterExpression writer)
        {
            switch (schema.LogicalTypeName)
            {
                case LogicalTypeSchema.LogicalTypeEnum.Uuid when type == typeof(Guid) && schema.BaseTypeSchema.Type == AvroType.String:
                    return Expression.Call(writer, WriteStringMethod, Expression.Call(value, GuidToString));

                case LogicalTypeSchema.LogicalTypeEnum.TimestampMicroseconds when schema.BaseTypeSchema.Type == AvroType.Long && (type == typeof(DateTime) || type == typeof(DateTimeOffset)):
                    return Expression.Call(writer, WriteLongMethod, Expression.Divide(TicksSinceEpoch(type, value), Expression.Constant(10L)));

                case LogicalTypeSchema.LogicalTypeEnum.TimestampMilliseconds when schema.BaseTypeSchema.Type == AvroType.Long && (type == typeof(DateTime) || type == typeof(DateTimeOffset)):
                    return Expression.Call(writer, WriteLongMethod, Expression.Divide(TicksSinceEpoch(type, value), Expression.Constant(TimeSpan.TicksPerMillisecond)));

                case LogicalTypeSchema.LogicalTypeEnum.Date when type == typeof(DateOnly) && schema.BaseTypeSchema.Type == AvroType.Int:
                    return Expression.Call(writer, WriteIntMethod,
                        Expression.Subtract(Expression.Property(value, DateOnlyDayNumber), Expression.Constant(DateTimeExtensions.UnixEpochDate.DayNumber)));

                default:
                    throw new NotSupportedException();
            }
        }

        private static Expression TicksSinceEpoch(Type type, Expression value)
        {
            var dateTime = type == typeof(DateTimeOffset) ? Expression.Property(value, DateTimeOffsetDateTime) : value;
            return Expression.Subtract(Expression.Property(dateTime, DateTimeTicks), Expression.Constant(DateTimeExtensions.UnixEpochDateTime.Ticks));
        }

        private Expression BuildEnum(EnumSchema schema, Type type, Expression value, ParameterExpression writer)
        {
            if (!type.IsEnum)
            {
                throw new NotSupportedException();
            }

            var underlying = Enum.GetUnderlyingType(type);
            var cases = new List<SwitchCase>();
            foreach (var enumValue in Enum.GetValues(type))
            {
                var symbol = EnumParser.GetEnumName(type, enumValue.ToString(), _options?.NamingPolicy);
                var position = schema.GetSymbolPosition(symbol);
                if (position < 0)
                {
                    // Not part of the schema: let the classic resolver raise its usual error at runtime.
                    continue;
                }

                cases.Add(Expression.SwitchCase(
                    Expression.Call(writer, WriteEnumMethod, Expression.Constant(position)),
                    Expression.Constant(System.Convert.ChangeType(enumValue, underlying), underlying)));
            }

            if (cases.Count == 0)
            {
                throw new NotSupportedException();
            }

            // Duplicate enum values map to the first symbol; unknown values take the fallback path.
            var distinct = cases.GroupBy(c => c.TestValues[0].ToString()).Select(g => g.First()).ToArray();
            return Expression.Switch(typeof(void), Expression.Convert(value, underlying), Fallback(schema, value, writer), null, distinct);
        }

        private Expression BuildRecord(RecordSchema schema, Type type, Expression value, ParameterExpression writer)
        {
            if (type.IsPrimitive || type == typeof(string) || type.IsArray || type.IsAbstract || type.IsInterface)
            {
                throw new NotSupportedException();
            }

            var key = (( TypeSchema)schema, type);
            if (!_recordHolders.TryGetValue(key, out var holderObj))
            {
                holderObj = Activator.CreateInstance(typeof(DelegateHolder<>).MakeGenericType(type))!;
                _recordHolders.Add(key, holderObj);
                holderObj.GetType().GetField(nameof(DelegateHolder<object>.Fn))!.SetValue(holderObj, CompileRecordDelegate(schema, type));
            }

            var fn = Expression.Field(Expression.Constant(holderObj), nameof(DelegateHolder<object>.Fn));
            return Expression.Invoke(fn, writer, value);
        }

        private Delegate CompileRecordDelegate(RecordSchema schema, Type type)
        {
            var writer = Expression.Parameter(typeof(IWriter), "writer");
            var instance = Expression.Parameter(type, "instance");
            var members = TypeMembers.For(type);
            var fields = new List<Expression>(schema.Fields.Count);

            foreach (var field in schema.Fields)
            {
                var member = members.Find(field.Aliases.FirstOrDefault() ?? field.Name);
                if (member is not { CanRead: true })
                {
                    // Member absent on this type: classic resolver writes the field's representation of null.
                    fields.Add(Fallback(field.TypeSchema, Expression.Constant(null, typeof(object)), writer));
                    continue;
                }

                var memberValue = Expression.MakeMemberAccess(instance, member.Info);
                fields.Add(Build(field.TypeSchema, member.Type, memberValue, writer));
            }

            Expression body = fields.Count == 0 ? Expression.Empty() : Expression.Block(typeof(void), fields);
            if (!type.IsValueType)
            {
                body = Expression.IfThenElse(
                    Expression.ReferenceEqual(instance, Expression.Constant(null, type)),
                    Expression.Call(writer, WriteNullMethod),
                    body);
            }

            return Expression.Lambda(typeof(Action<,>).MakeGenericType(typeof(IWriter), type), body, writer, instance).Compile();
        }

        private Expression BuildArray(ArraySchema schema, Type type, Expression value, ParameterExpression writer)
        {
            var itemType = Read.Typed.ReadCompiler.CollectionItemType(type) ?? throw new NotSupportedException();
            var itemFn = CompileItem(schema.ItemSchema, itemType);

            if (type.IsArray)
            {
                return Expression.Call(WriteArrayMethod.MakeGenericMethod(itemType), writer, value, Expression.Constant(itemFn));
            }

            var readOnlyList = typeof(IReadOnlyList<>).MakeGenericType(itemType);
            if (readOnlyList.IsAssignableFrom(type))
            {
                return Expression.Call(WriteListMethod.MakeGenericMethod(itemType), writer, Expression.Convert(value, readOnlyList), Expression.Constant(itemFn));
            }

            var enumerable = typeof(IEnumerable<>).MakeGenericType(itemType);
            return Expression.Call(WriteEnumerableMethod.MakeGenericMethod(itemType), writer, Expression.Convert(value, enumerable), Expression.Constant(itemFn));
        }

        private Expression BuildMap(MapSchema schema, Type type, Expression value, ParameterExpression writer)
        {
            var (valueType, collectionInterface) = DictionaryShape(type) ?? throw new NotSupportedException();
            var valueFn = CompileItem(schema.ValueSchema, valueType);
            var entries = typeof(IEnumerable<>).MakeGenericType(typeof(KeyValuePair<,>).MakeGenericType(typeof(string), valueType));

            // Null dictionaries only reach here outside a union; classic resolver would fail, we write an empty map.
            var count = Expression.Condition(
                Expression.ReferenceEqual(value, Expression.Constant(null, type)),
                Expression.Constant(0),
                Expression.Property(Expression.Convert(value, collectionInterface), collectionInterface.GetProperty("Count")!));
            var safeEntries = Expression.Coalesce(Expression.Convert(value, entries),
                Expression.Constant(Array.CreateInstance(typeof(KeyValuePair<,>).MakeGenericType(typeof(string), valueType), 0), entries));

            return Expression.Call(WriteMapMethod.MakeGenericMethod(valueType), writer, safeEntries, count, Expression.Constant(valueFn));
        }

        private Expression BuildUnion(UnionSchema schema, Type type, Expression value, ParameterExpression writer)
        {
            var branches = schema.Schemas;
            int nullIndex = -1;
            for (int i = 0; i < branches.Count; i++)
            {
                if (branches[i].Type == AvroType.Null) nullIndex = i;
            }

            var underlying = Nullable.GetUnderlyingType(type);
            var valueType = underlying ?? type;
            bool canBeNull = underlying != null || !type.IsValueType;

            var candidates = Enumerable.Range(0, branches.Count)
                .Where(i => i != nullIndex && BranchAccepts(branches[i], valueType))
                .ToList();

            if (candidates.Count != 1 || (canBeNull && nullIndex < 0))
            {
                throw new NotSupportedException("Union branch cannot be decided statically");
            }

            int branchIndex = candidates[0];
            Expression nonNullValue = underlying != null ? Expression.Property(value, "Value") : value;
            Expression writeBranch = Expression.Block(
                Expression.Call(writer, WriteUnionIndexMethod, Expression.Constant(branchIndex)),
                Build(branches[branchIndex], valueType, nonNullValue, writer));

            if (!canBeNull)
            {
                return writeBranch;
            }

            Expression isNull = underlying != null
                ? Expression.Not(Expression.Property(value, "HasValue"))
                : Expression.ReferenceEqual(value, Expression.Constant(null, type));

            return Expression.IfThenElse(isNull, Expression.Call(writer, WriteUnionIndexMethod, Expression.Constant(nullIndex)), writeBranch);
        }

        /// <summary>Static counterpart of WriteResolver.UnionBranchMatches for a known CLR type.</summary>
        private static bool BranchAccepts(TypeSchema branch, Type type)
        {
            switch (branch.Type)
            {
                case AvroType.Boolean: return type == typeof(bool);
                case AvroType.Int: return type == typeof(int) || type == typeof(short) || type == typeof(ushort) || type == typeof(byte) || type == typeof(sbyte) || type == typeof(char) || type == typeof(uint);
                case AvroType.Long: return type == typeof(long) || type == typeof(ulong) || type == typeof(int) || type == typeof(uint);
                case AvroType.Float: return type == typeof(float);
                case AvroType.Double: return type == typeof(double) || type == typeof(float);
                case AvroType.String: return type == typeof(string) || type == typeof(Guid) || type == typeof(decimal) || type == typeof(DateTimeOffset) || type == typeof(Uri) || type.IsEnum;
                case AvroType.Bytes: return type == typeof(byte[]);
                case AvroType.Enum: return type.IsEnum;
                case AvroType.Array: return type != typeof(byte[]) && Read.Typed.ReadCompiler.CollectionItemType(type) != null;
                case AvroType.Map: return DictionaryShape(type) != null;
                case AvroType.Record:
                case AvroType.Error:
                    return !type.IsPrimitive && !type.IsEnum && type != typeof(string) && type != typeof(byte[]) && !type.IsArray && !type.IsInterface && !type.IsAbstract
                           && type != typeof(object) && Read.Typed.ReadCompiler.CollectionItemType(type) == null && DictionaryShape(type) == null
                           && (type.Name.Equals(((RecordSchema)branch).Name) || type.IsAnonymous() || ((RecordSchema)branch).RuntimeType == type);
                case AvroType.Logical:
                    return ((LogicalTypeSchema)branch).LogicalTypeName switch
                    {
                        LogicalTypeSchema.LogicalTypeEnum.Uuid => type == typeof(Guid),
                        LogicalTypeSchema.LogicalTypeEnum.Decimal => type == typeof(decimal),
                        LogicalTypeSchema.LogicalTypeEnum.TimestampMicroseconds or LogicalTypeSchema.LogicalTypeEnum.TimestampMilliseconds => type == typeof(DateTime) || type == typeof(DateTimeOffset),
                        LogicalTypeSchema.LogicalTypeEnum.Date => type == typeof(DateOnly) || type == typeof(DateTime),
                        LogicalTypeSchema.LogicalTypeEnum.TimeMicrosecond or LogicalTypeSchema.LogicalTypeEnum.TimeMilliseconds => type == typeof(TimeOnly) || type == typeof(TimeSpan),
                        LogicalTypeSchema.LogicalTypeEnum.Duration => type == typeof(TimeSpan),
                        _ => false
                    };
                default:
                    return false;
            }
        }

        private static (Type ValueType, Type CollectionInterface)? DictionaryShape(Type type)
        {
            if (!type.IsGenericType)
            {
                return null;
            }

            var definition = type.GetGenericTypeDefinition();
            var args = type.GetGenericArguments();
            if (args.Length != 2 || args[0] != typeof(string))
            {
                return null;
            }

            var pair = typeof(KeyValuePair<,>).MakeGenericType(typeof(string), args[1]);
            if (definition == typeof(IReadOnlyDictionary<,>))
            {
                return (args[1], typeof(IReadOnlyCollection<>).MakeGenericType(pair));
            }

            if (definition == typeof(Dictionary<,>) || definition == typeof(IDictionary<,>) || definition == typeof(SortedDictionary<,>))
            {
                return (args[1], typeof(ICollection<>).MakeGenericType(pair));
            }

            return null;
        }

        private Delegate CompileItem(TypeSchema schema, Type itemType)
        {
            var writer = Expression.Parameter(typeof(IWriter), "writer");
            var item = Expression.Parameter(itemType, "item");
            var body = Build(schema, itemType, item, writer);
            return Expression.Lambda(typeof(Action<,>).MakeGenericType(typeof(IWriter), itemType), body, writer, item).Compile();
        }

        private sealed class DelegateHolder<T>
        {
#pragma warning disable CS0649 // assigned through reflection in BuildRecord
            public Action<IWriter, T> Fn;
#pragma warning restore CS0649
        }
    }
}
