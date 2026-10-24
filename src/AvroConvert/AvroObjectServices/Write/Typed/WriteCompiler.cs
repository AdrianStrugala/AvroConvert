using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Newtonsoft.Json.Linq;
using SolTechnology.Avro.AvroObjectServices.Read;
using SolTechnology.Avro.AvroObjectServices.Read.Typed;
using SolTechnology.Avro.AvroObjectServices.Schemas;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;
using SolTechnology.Avro.AvroObjectServices.Schemas.AvroTypes;
using SolTechnology.Avro.Features.Serialize;
using SolTechnology.Avro.Infrastructure.Exceptions;
using SolTechnology.Avro.Infrastructure.Extensions;
using SolTechnology.Avro.Infrastructure.Reflection;
using SolTechnology.Avro.Policies;
using System.Diagnostics.CodeAnalysis;
using SolTechnology.Avro.Infrastructure;

namespace SolTechnology.Avro.AvroObjectServices.Write.Typed
{
    /// <summary>
    /// Compiles a (schema, T) pair into a strongly typed <c>Action&lt;IWriter, T&gt;</c>: member access, conversions,
    /// enum positions and union branches are decided once at compile time for the static type. Values whose static type
    /// is <c>object</c> (or an interface/abstract class) are dispatched at runtime to the plan compiled for their actual
    /// type, which is how polymorphism, ExpandoObject and JSON tokens are handled.
    /// </summary>
    [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
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
        private static readonly MethodInfo WriteFixedMethod = typeof(IWriter).GetMethod(nameof(IWriter.WriteFixed), new[] { typeof(byte[]) })!;
        private static readonly MethodInfo WriteEnumMethod = typeof(IWriter).GetMethod(nameof(IWriter.WriteEnum))!;
        private static readonly MethodInfo WriteUnionIndexMethod = typeof(IWriter).GetMethod(nameof(IWriter.WriteUnionIndex))!;
        private static readonly MethodInfo WriteArrayMethod = Helper(nameof(WriteHelpers.WriteArray));
        private static readonly MethodInfo WriteListMethod = Helper(nameof(WriteHelpers.WriteList));
        private static readonly MethodInfo WriteEnumerableMethod = Helper(nameof(WriteHelpers.WriteEnumerable));
        private static readonly MethodInfo WriteUntypedEnumerableMethod = Helper(nameof(WriteHelpers.WriteUntypedEnumerable));
        private static readonly MethodInfo WriteMapMethod = Helper(nameof(WriteHelpers.WriteMap));
        private static readonly MethodInfo WriteUntypedMapMethod = Helper(nameof(WriteHelpers.WriteUntypedMap));
        private static readonly MethodInfo WriteDictionaryRecordMethod = Helper(nameof(WriteHelpers.WriteDictionaryRecord));
        private static readonly MethodInfo WriteJsonRecordMethod = Helper(nameof(WriteHelpers.WriteJsonRecord));
        private static readonly MethodInfo UnwrapJsonMethod = Helper(nameof(WriteHelpers.UnwrapJson));
        private static readonly MethodInfo WriteEnumSymbolMethod = Helper(nameof(WriteHelpers.WriteEnumSymbol));
        private static readonly MethodInfo NoUnionBranchMethod = Helper(nameof(WriteHelpers.NoUnionBranch));
        private static readonly MethodInfo TypeMismatchMethod = Helper(nameof(WriteHelpers.TypeMismatch));
        private static readonly MethodInfo WriteNullValueMethod = Helper(nameof(WriteHelpers.WriteNullValue));
        private static readonly MethodInfo NullNotAllowedMethod = Helper(nameof(WriteHelpers.NullNotAllowed));
        private static readonly MethodInfo WriteDecimalMethod = typeof(LogicalWriters).GetMethod(nameof(LogicalWriters.WriteDecimal), BindingFlags.Static | BindingFlags.NonPublic)!;
        private static readonly MethodInfo WriteDurationMethod = typeof(LogicalWriters).GetMethod(nameof(LogicalWriters.WriteDuration), BindingFlags.Static | BindingFlags.NonPublic)!;
        private static readonly MethodInfo WriteTimeMicrosMethod = typeof(LogicalWriters).GetMethod(nameof(LogicalWriters.WriteTimeMicros), BindingFlags.Static | BindingFlags.NonPublic)!;
        private static readonly MethodInfo WriteTimeMillisMethod = typeof(LogicalWriters).GetMethod(nameof(LogicalWriters.WriteTimeMillis), BindingFlags.Static | BindingFlags.NonPublic)!;
        private static readonly MethodInfo WriteBigDecimalMethod = typeof(LogicalWriters).GetMethod(nameof(LogicalWriters.WriteBigDecimal), BindingFlags.Static | BindingFlags.NonPublic)!;
        private static readonly MethodInfo WriteUuidFixedMethod = typeof(LogicalWriters).GetMethod(nameof(LogicalWriters.WriteUuidFixed), BindingFlags.Static | BindingFlags.NonPublic)!;
        private static readonly MethodInfo WriteTimestampMethod = typeof(LogicalWriters).GetMethod(nameof(LogicalWriters.WriteTimestamp), BindingFlags.Static | BindingFlags.NonPublic)!;
        private static readonly MethodInfo GuidParseMethod = typeof(Guid).GetMethod(nameof(Guid.Parse), new[] { typeof(string) })!;
        private static readonly MethodInfo WriteAvroFixedMethod = typeof(LogicalWriters).GetMethod(nameof(LogicalWriters.WriteFixed), BindingFlags.Static | BindingFlags.NonPublic)!;
        private static readonly MethodInfo WriteDynamicMethod = typeof(WriteCompiler).GetMethod(nameof(WriteDynamic), BindingFlags.Instance | BindingFlags.NonPublic)!;
        private static readonly PropertyInfo DateTimeTicks = typeof(DateTime).GetProperty(nameof(DateTime.Ticks))!;
        private static readonly PropertyInfo DateTimeOffsetDateTime = typeof(DateTimeOffset).GetProperty(nameof(DateTimeOffset.DateTime))!;
        private static readonly PropertyInfo DateTimeOffsetUtcDateTime = typeof(DateTimeOffset).GetProperty(nameof(DateTimeOffset.UtcDateTime))!;
        private static readonly PropertyInfo DateOnlyDayNumber = typeof(DateOnly).GetProperty(nameof(DateOnly.DayNumber))!;

        private static MethodInfo Helper(string name) => typeof(WriteHelpers).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!;

        private readonly AvroConvertOptions _options;
        private readonly AvroNumberHandling _numberHandling;
        private readonly AvroNullValueHandling _nullHandling;
        private readonly Dictionary<Type, Action<object, IWriter>> _converters;
        private readonly Dictionary<(TypeSchema, Type), object> _recordHolders = new();

        // Where in the schema the code being compiled writes ("User.name", "User.tags[]"); null for the root value.
        // Only used to make the null-value error point at the member.
        private string _context;

        internal WriteCompiler(AvroConvertOptions options)
        {
            _options = options;
            _numberHandling = options?.NumberHandling ?? AvroNumberHandling.Strict;
            _nullHandling = options?.NullValueHandling ?? AvroNullValueHandling.Throw;
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

        internal Action<IWriter, T> CompileTyped<T>(TypeSchema schema)
        {
            var value = Expression.Parameter(typeof(T), "value");
            var writer = Expression.Parameter(typeof(IWriter), "writer");
            var body = Build(schema, typeof(T), value, writer);
            return Expression.Lambda<Action<IWriter, T>>(body, writer, value).Compile();
        }

        /// <summary>Runtime dispatch for values whose static type carries no information (object, interfaces, JSON tokens).</summary>
        /// <param name="nullMessage">Error for a null value; null when a null value is written (schema allows it or WriteEmpty).</param>
        private void WriteDynamic(TypeSchema schema, string nullMessage, object value, IWriter writer)
        {
            if (value == null)
            {
                if (nullMessage != null)
                {
                    throw WriteHelpers.NullNotAllowed(nullMessage);
                }

                WriteHelpers.WriteNullValue(schema, writer);
                return;
            }

            WritePlanCache.Get(schema, value.GetType(), _options)(value, writer);
        }

        // ------------------------------------------------------------------ core

        private Expression Build(TypeSchema schema, Type type, Expression value, ParameterExpression writer)
        {
            if (_converters != null && schema.RuntimeType != null && _converters.TryGetValue(schema.RuntimeType, out var converter))
            {
                return Expression.Invoke(Expression.Constant(converter), Expression.Convert(value, typeof(object)), writer);
            }

            if (type == typeof(object) || type.IsInterface && !IsKnownCollectionInterface(type) || type.IsAbstract && !type.IsSealed && schema.Type != AvroType.Array)
            {
                return Dynamic(schema, value, writer);
            }

            if (typeof(JToken).IsAssignableFrom(type) && type != typeof(JObject))
            {
                // JValue / JArray / generic JToken: unwrap to CLR primitives / object[] and dispatch by runtime type.
                return Dynamic(schema, Expression.Call(UnwrapJsonMethod, Expression.Convert(value, typeof(JToken))), writer);
            }

            try
            {
                switch (schema.Type)
                {
                    case AvroType.Null:
                        return Expression.Call(writer, WriteNullMethod);
                    case AvroType.Boolean:
                        return Expression.Call(writer, WriteBooleanMethod, Convert(value, typeof(bool), schema));
                    case AvroType.Int:
                        return Expression.Call(writer, WriteIntMethod, Convert(value, typeof(int), schema));
                    case AvroType.Long:
                        return Expression.Call(writer, WriteLongMethod, Convert(value, typeof(long), schema));
                    case AvroType.Float:
                        return Expression.Call(writer, WriteFloatMethod, Convert(value, typeof(float), schema));
                    case AvroType.Double:
                        return Expression.Call(writer, WriteDoubleMethod, Convert(value, typeof(double), schema));
                    case AvroType.String:
                        return Expression.Call(writer, WriteStringMethod, AsString(type, value, schema));
                    case AvroType.Bytes:
                        if (type != typeof(byte[])) throw new NotSupportedException($"[byte[]] required to write against [Bytes] schema but found [{type}]");
                        return Expression.Call(writer, WriteBytesMethod, OrEmpty(value, Expression.Constant(Array.Empty<byte>()), schema));
                    case AvroType.Logical:
                        return BuildLogical((LogicalTypeSchema)schema, type, value, writer);
                    case AvroType.Record:
                    case AvroType.Error:
                        return BuildRecord((RecordSchema)schema, type, value, writer);
                    case AvroType.Enum:
                        return BuildEnum((EnumSchema)schema, type, value, writer);
                    case AvroType.Fixed:
                        return BuildFixed((FixedSchema)schema, type, value, writer);
                    case AvroType.Array:
                        return BuildArray((ArraySchema)schema, type, value, writer);
                    case AvroType.Map:
                        return BuildMap((MapSchema)schema, type, value, writer);
                    case AvroType.Union:
                        return BuildUnion((UnionSchema)schema, type, value, writer);
                    default:
                        throw new NotSupportedException($"Unknown schema type {schema.Type}");
                }
            }
            catch (NotSupportedException e)
            {
                // Compile-time decision: this value cannot be written, so the plan fails exactly where the data is.
                return Expression.Throw(Expression.Constant(new AvroTypeMismatchException(
                    $"Unable to serialize [{type}] against [{schema.Name}] of schema [{schema.Type}]. {e.Message}")));
            }
        }

        private Expression Dynamic(TypeSchema schema, Expression value, ParameterExpression writer) =>
            Expression.Call(Expression.Constant(this), WriteDynamicMethod,
                Expression.Constant(schema, typeof(TypeSchema)),
                Expression.Constant(ThrowsOnNull(schema) ? NullMessage(schema) : null, typeof(string)),
                Expression.Convert(value, typeof(object)), writer);

        private static Expression WriteEmptyValue(TypeSchema schema, ParameterExpression writer) =>
            Expression.Call(WriteNullValueMethod, Expression.Constant(schema, typeof(TypeSchema)), writer);

        // ------------------------------------------------------------------ null values

        private bool ThrowsOnNull(TypeSchema schema) =>
            _nullHandling == AvroNullValueHandling.Throw && !WriteHelpers.CanBeNull(schema);

        /// <summary>
        /// What a null value turns into: the null branch of the schema if it has one, otherwise an error
        /// (<see cref="AvroNullValueHandling.Throw"/>) or the schema's empty value (<see cref="AvroNullValueHandling.WriteEmpty"/>).
        /// </summary>
        private Expression NullValue(TypeSchema schema, ParameterExpression writer, string message = null) =>
            ThrowsOnNull(schema)
                ? Expression.Throw(Expression.Call(NullNotAllowedMethod, Expression.Constant(message ?? NullMessage(schema))))
                : WriteEmptyValue(schema, writer);

        /// <summary><paramref name="value"/>, or for null the empty value <paramref name="empty"/> / an error, typed as the value.</summary>
        private Expression OrEmpty(Expression value, Expression empty, TypeSchema schema) =>
            Expression.Coalesce(value, ThrowsOnNull(schema)
                ? Expression.Throw(Expression.Call(NullNotAllowedMethod, Expression.Constant(NullMessage(schema))), empty.Type)
                : empty);

        /// <summary>For reference types under <see cref="AvroNullValueHandling.Throw"/>: checks null before <paramref name="write"/>.</summary>
        private Expression GuardNull(Type type, Expression value, TypeSchema schema, ParameterExpression writer, Expression write) =>
            type.IsValueType || !ThrowsOnNull(schema)
                ? write
                : Expression.IfThenElse(Expression.ReferenceEqual(value, Expression.Constant(null, type)), NullValue(schema, writer), write);

        private string NullMessage(TypeSchema schema) => WriteHelpers.NullMessage(_context, schema);

        private static string Describe(TypeSchema schema) => WriteHelpers.Describe(schema);

        private string ItemContext() => $"{_context ?? "value"}[]";

        private string ValueContext() => $"{_context ?? "value"}[key]";

        /// <summary>Runs <paramref name="build"/> with <see cref="_context"/> set to <paramref name="context"/>.</summary>
        private T InContext<T>(string context, Func<T> build)
        {
            var previous = _context;
            _context = context;
            try
            {
                return build();
            }
            finally
            {
                _context = previous;
            }
        }

        private static bool IsKnownCollectionInterface(Type type) =>
            ReadCompiler.CollectionItemType(type) != null || ReadCompiler.DictionaryArguments(type) != null || type == typeof(IDictionary<string, object>);

        /// <summary>Numeric / enum / nullable conversion of a primitive; throws NotSupported otherwise.</summary>
        private Expression Convert(Expression value, Type target, TypeSchema schema)
        {
            var type = value.Type;
            if (type == target)
            {
                return value;
            }

            var underlying = Nullable.GetUnderlyingType(type);
            if (underlying != null)
            {
                // Outside a union a missing value is an error, or the primitive's default under WriteEmpty.
                return Convert(OrEmpty(value, Expression.Default(underlying), schema), target, schema);
            }

            if (target == typeof(bool))
            {
                throw new NotSupportedException($"[bool] required to write against [Boolean] schema but found [{type}]");
            }

            if (type.IsEnum || (type.IsPrimitive && type != typeof(bool)) || type == typeof(decimal))
            {
                return Expression.Convert(value, target);
            }

            throw new NotSupportedException($"[{target}] required but found [{type}]");
        }

        private Expression AsString(Type type, Expression value, TypeSchema schema)
        {
            if (type == typeof(string))
            {
                return OrEmpty(value, Expression.Constant(string.Empty), schema);
            }

            var underlying = Nullable.GetUnderlyingType(type);
            if (underlying != null)
            {
                return Expression.Condition(
                    Expression.Property(value, "HasValue"),
                    Expression.Call(Expression.Property(value, "Value"), underlying.GetMethod(nameof(ToString), Type.EmptyTypes)!),
                    OrEmpty(Expression.Constant(null, typeof(string)), Expression.Constant(string.Empty), schema));
            }

            var toString = Expression.Call(value, type.GetMethod(nameof(ToString), Type.EmptyTypes)!);
            return type.IsValueType
                ? toString
                : Expression.Condition(Expression.ReferenceEqual(value, Expression.Constant(null, type)),
                    OrEmpty(Expression.Constant(null, typeof(string)), Expression.Constant(string.Empty), schema), toString);
        }

        private Expression BuildLogical(LogicalTypeSchema schema, Type type, Expression value, ParameterExpression writer)
        {
            var underlying = Nullable.GetUnderlyingType(type);
            if (underlying != null)
            {
                return Expression.IfThenElse(
                    Expression.Property(value, "HasValue"),
                    BuildLogical(schema, underlying, Expression.Property(value, "Value"), writer),
                    NullValue(schema, writer));
            }

            switch (schema.LogicalTypeName)
            {
                case LogicalTypeSchema.LogicalTypeEnum.Uuid when schema.BaseTypeSchema.Type == AvroType.Fixed && type == typeof(Guid):
                    return Expression.Call(WriteUuidFixedMethod, writer, value);
                case LogicalTypeSchema.LogicalTypeEnum.Uuid when schema.BaseTypeSchema.Type == AvroType.Fixed && type == typeof(string):
                    return GuardNull(type, value, schema, writer, Expression.Call(WriteUuidFixedMethod, writer, Expression.Call(GuidParseMethod, value)));
                case LogicalTypeSchema.LogicalTypeEnum.Uuid when type == typeof(Guid):
                    return Expression.Call(writer, WriteStringMethod, Expression.Call(value, typeof(Guid).GetMethod(nameof(Guid.ToString), Type.EmptyTypes)!));
                case LogicalTypeSchema.LogicalTypeEnum.Uuid when type == typeof(string):
                    return Expression.Call(writer, WriteStringMethod, AsString(type, value, schema));

                case var _ when IsTimestamp(schema.LogicalTypeName) && type == typeof(long):
                    // A long member holds the raw count since the epoch and is written as is.
                    return Expression.Call(writer, WriteLongMethod, value);

                case LogicalTypeSchema.LogicalTypeEnum.TimestampMicroseconds when schema.BaseTypeSchema.Type == AvroType.Long && (type == typeof(DateTime) || type == typeof(DateTimeOffset)):
                    return Expression.Call(writer, WriteLongMethod, Expression.Divide(TicksSinceEpoch(type, value, local: false), Expression.Constant(10L)));
                case LogicalTypeSchema.LogicalTypeEnum.TimestampMilliseconds when schema.BaseTypeSchema.Type == AvroType.Long && (type == typeof(DateTime) || type == typeof(DateTimeOffset)):
                    return Expression.Call(writer, WriteLongMethod, Expression.Divide(TicksSinceEpoch(type, value, local: false), Expression.Constant(TimeSpan.TicksPerMillisecond)));

                case LogicalTypeSchema.LogicalTypeEnum.Date when type == typeof(DateOnly) && schema.BaseTypeSchema.Type == AvroType.Int:
                    return Expression.Call(writer, WriteIntMethod,
                        Expression.Subtract(Expression.Property(value, DateOnlyDayNumber), Expression.Constant(DateTimeExtensions.UnixEpochDate.DayNumber)));

                case var _ when schema is TimestampSchema timestampSchema && (type == typeof(DateTime) || type == typeof(DateTimeOffset)):
                    return Expression.Call(WriteTimestampMethod, writer, TicksSinceEpoch(type, value, timestampSchema.IsLocal), Expression.Constant(timestampSchema));

                case LogicalTypeSchema.LogicalTypeEnum.BigDecimal when type == typeof(decimal) || type.IsPrimitive && type != typeof(bool):
                    return Expression.Call(WriteBigDecimalMethod, writer, Convert(value, typeof(decimal), schema));

                case LogicalTypeSchema.LogicalTypeEnum.Decimal when type == typeof(decimal) || type.IsPrimitive && type != typeof(bool):
                    return Expression.Call(WriteDecimalMethod, writer, Convert(value, typeof(decimal), schema), Expression.Constant(schema), Expression.Constant(_numberHandling));

                case LogicalTypeSchema.LogicalTypeEnum.Duration when type == typeof(TimeSpan):
                    return Expression.Call(WriteDurationMethod, writer, value, Expression.Constant(schema));

                case LogicalTypeSchema.LogicalTypeEnum.TimeMicrosecond when type == typeof(TimeOnly):
                    return Expression.Call(WriteTimeMicrosMethod, writer, value);
                case LogicalTypeSchema.LogicalTypeEnum.TimeMilliseconds when type == typeof(TimeOnly):
                    return Expression.Call(WriteTimeMillisMethod, writer, value);

                case LogicalTypeSchema.LogicalTypeEnum.Uuid:
                case LogicalTypeSchema.LogicalTypeEnum.TimestampMicroseconds:
                case LogicalTypeSchema.LogicalTypeEnum.TimestampMilliseconds:
                case LogicalTypeSchema.LogicalTypeEnum.TimestampNanoseconds:
                case LogicalTypeSchema.LogicalTypeEnum.LocalTimestampMilliseconds:
                case LogicalTypeSchema.LogicalTypeEnum.LocalTimestampMicroseconds:
                case LogicalTypeSchema.LogicalTypeEnum.LocalTimestampNanoseconds:
                case LogicalTypeSchema.LogicalTypeEnum.Date:
                case LogicalTypeSchema.LogicalTypeEnum.Decimal:
                case LogicalTypeSchema.LogicalTypeEnum.BigDecimal:
                case LogicalTypeSchema.LogicalTypeEnum.Duration:
                case LogicalTypeSchema.LogicalTypeEnum.TimeMicrosecond:
                case LogicalTypeSchema.LogicalTypeEnum.TimeMilliseconds:
                    throw new NotSupportedException($"[{type}] cannot be written against logical type [{schema.LogicalTypeName}]");

                default:
                    // Unknown logical type: write the underlying type.
                    return Build(schema.BaseTypeSchema, type, value, writer);
            }
        }

        private static bool IsTimestamp(string logicalTypeName) =>
            logicalTypeName is LogicalTypeSchema.LogicalTypeEnum.TimestampMilliseconds or LogicalTypeSchema.LogicalTypeEnum.TimestampMicroseconds
                || TimestampSchema.Handles(logicalTypeName);

        // A DateTimeOffset denotes an instant: timestamp-* logical types take its UTC value (it reads back with offset zero
        // as the same instant); local-timestamp-* types keep the wall-clock time. A DateTime is written by its ticks as is.
        private static Expression TicksSinceEpoch(Type type, Expression value, bool local)
        {
            var dateTime = type == typeof(DateTimeOffset) ? Expression.Property(value, local ? DateTimeOffsetDateTime : DateTimeOffsetUtcDateTime) : value;
            return Expression.Subtract(Expression.Property(dateTime, DateTimeTicks), Expression.Constant(DateTimeExtensions.UnixEpochDateTime.Ticks));
        }

        private Expression BuildEnum(EnumSchema schema, Type type, Expression value, ParameterExpression writer)
        {
            if (type == typeof(string))
            {
                return Expression.IfThenElse(
                    Expression.ReferenceEqual(value, Expression.Constant(null, type)),
                    NullValue(schema, writer),
                    Expression.Call(WriteEnumSymbolMethod, writer, value, Expression.Constant(schema)));
            }

            var underlyingNullable = Nullable.GetUnderlyingType(type);
            if (underlyingNullable != null)
            {
                return Expression.IfThenElse(
                    Expression.Property(value, "HasValue"),
                    BuildEnum(schema, underlyingNullable, Expression.Property(value, "Value"), writer),
                    NullValue(schema, writer));
            }

            if (!type.IsEnum)
            {
                throw new NotSupportedException($"[Enum] or [string] required to write against [Enum] schema but found [{type}]");
            }

            var underlying = Enum.GetUnderlyingType(type);
            var seen = new HashSet<object>();
            var cases = new List<SwitchCase>();
            foreach (var enumValue in Enum.GetValues(type))
            {
                var key = System.Convert.ChangeType(enumValue, underlying);
                if (!seen.Add(key))
                {
                    continue; // duplicate enum values map to the first symbol
                }

                var symbol = EnumParser.GetEnumName(type, enumValue.ToString(), _options?.NamingPolicy);
                var position = schema.GetSymbolPosition(symbol);
                Expression body = position >= 0
                    ? Expression.Call(writer, WriteEnumMethod, Expression.Constant(position))
                    : Expression.Throw(Expression.Constant(new AvroTypeException($"[Enum] Provided value [{symbol}] is not of the enum [{schema.Name}] members")));

                cases.Add(Expression.SwitchCase(body, Expression.Constant(key, underlying)));
            }

            var unknown = Expression.Throw(Expression.Call(TypeMismatchMethod, Expression.Constant(schema, typeof(TypeSchema)), Expression.Convert(value, typeof(object))));
            return cases.Count == 0
                ? unknown
                : Expression.Switch(typeof(void), Expression.Convert(value, underlying), unknown, null, cases);
        }

        private Expression BuildFixed(FixedSchema schema, Type type, Expression value, ParameterExpression writer)
        {
            if (type == typeof(AvroFixed))
            {
                return GuardNull(type, value, schema, writer, Expression.Call(WriteAvroFixedMethod, writer, value, Expression.Constant(schema)));
            }

            if (type == typeof(byte[]))
            {
                return GuardNull(type, value, schema, writer, Expression.Call(writer, WriteFixedMethod, value));
            }

            if (type == typeof(Guid) && schema.Size == 16)
            {
                return Expression.Call(writer, WriteFixedMethod, Expression.Call(value, typeof(Guid).GetMethod(nameof(Guid.ToByteArray), Type.EmptyTypes)!));
            }

            throw new NotSupportedException($"[AvroFixed] or [byte[]] required to write against [Fixed] schema but found [{type}]");
        }

        // ------------------------------------------------------------------ records

        private Expression BuildRecord(RecordSchema schema, Type type, Expression value, ParameterExpression writer) =>
            GuardNull(type, value, schema, writer, BuildRecordBody(schema, type, value, writer));

        private Expression BuildRecordBody(RecordSchema schema, Type type, Expression value, ParameterExpression writer)
        {
            if (type == typeof(ExpandoObject) || typeof(IDictionary<string, object>).IsAssignableFrom(type))
            {
                var (names, writers) = DynamicFieldWriters(schema);
                return Expression.Call(WriteDictionaryRecordMethod, writer, Expression.Convert(value, typeof(IDictionary<string, object>)), Expression.Constant(names), Expression.Constant(writers));
            }

            if (type == typeof(JObject))
            {
                var (names, writers) = DynamicFieldWriters(schema);
                return Expression.Call(WriteJsonRecordMethod, writer, value, Expression.Constant(names), Expression.Constant(writers));
            }

            if (type.IsPrimitive || type == typeof(string) || type.IsArray || type.IsEnum || ReadCompiler.CollectionItemType(type) != null || ReadCompiler.DictionaryArguments(type) != null)
            {
                throw new NotSupportedException($"[{type}] cannot be written against a record schema");
            }

            var key = ((TypeSchema)schema, type);
            if (!_recordHolders.TryGetValue(key, out var holderObj))
            {
                holderObj = Activator.CreateInstance(typeof(DelegateHolder<>).MakeGenericType(type))!;
                _recordHolders.Add(key, holderObj);
                holderObj.GetType().GetField(nameof(DelegateHolder<object>.Fn))!.SetValue(holderObj, CompileRecordDelegate(schema, type));
            }

            var fn = Expression.Field(Expression.Constant(holderObj), nameof(DelegateHolder<object>.Fn));
            return Expression.Invoke(fn, writer, value);
        }

        private (string[] Names, Action<IWriter, object>[] Writers) DynamicFieldWriters(RecordSchema schema)
        {
            var names = new string[schema.Fields.Count];
            var writers = new Action<IWriter, object>[schema.Fields.Count];
            for (int i = 0; i < names.Length; i++)
            {
                var field = schema.Fields[i];
                names[i] = field.Aliases.FirstOrDefault() ?? field.Name;
                var writer = Expression.Parameter(typeof(IWriter), "writer");
                var item = Expression.Parameter(typeof(object), "value");
                var body = InContext($"{schema.Name}.{field.Name}", () => Build(field.TypeSchema, typeof(object), item, writer));
                writers[i] = Expression.Lambda<Action<IWriter, object>>(body, writer, item).Compile();
            }

            return (names, writers);
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
                    // Member absent on this type: the field is written as "no value".
                    fields.Add(NullValue(field.TypeSchema, writer,
                        $"Field '{schema.Name}.{field.Name}' has no readable member in [{type}], and its schema {Describe(field.TypeSchema)} does not allow null. " +
                        "Add the member, make the field a union with \"null\", " +
                        "or set AvroConvertOptions.NullValueHandling = AvroNullValueHandling.WriteEmpty to write an empty value instead (AvroConvert 3.x behaviour)."));
                    continue;
                }

                fields.Add(InContext($"{schema.Name}.{field.Name}",
                    () => Build(field.TypeSchema, member.Type, Expression.MakeMemberAccess(instance, member.Info), writer)));
            }

            Expression body = fields.Count == 0 ? Expression.Empty() : Expression.Block(typeof(void), fields);
            if (!type.IsValueType)
            {
                body = Expression.IfThenElse(
                    Expression.ReferenceEqual(instance, Expression.Constant(null, type)),
                    NullValue(schema, writer),
                    body);
            }

            return Expression.Lambda(typeof(Action<,>).MakeGenericType(typeof(IWriter), type), body, writer, instance).Compile();
        }

        // ------------------------------------------------------------------ collections

        private Expression BuildArray(ArraySchema schema, Type type, Expression value, ParameterExpression writer) =>
            GuardNull(type, value, schema, writer, BuildArrayBody(schema, type, value, writer));

        private Expression BuildArrayBody(ArraySchema schema, Type type, Expression value, ParameterExpression writer)
        {
            var itemType = ReadCompiler.CollectionItemType(type);
            if (itemType == null)
            {
                if (typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string) && type != typeof(byte[]))
                {
                    var dynamicItem = InContext(ItemContext(), () => CompileItem(schema.ItemSchema, typeof(object)));
                    return Expression.Call(WriteUntypedEnumerableMethod, writer, Expression.Convert(value, typeof(IEnumerable)), Expression.Constant(dynamicItem));
                }

                throw new NotSupportedException($"[IEnumerable] required to write against [Array] schema but found [{type}]");
            }

            var itemFn = InContext(ItemContext(), () => CompileItem(schema.ItemSchema, itemType));

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

        private Expression BuildMap(MapSchema schema, Type type, Expression value, ParameterExpression writer) =>
            GuardNull(type, value, schema, writer, BuildMapBody(schema, type, value, writer));

        private Expression BuildMapBody(MapSchema schema, Type type, Expression value, ParameterExpression writer)
        {
            if (type == typeof(JObject))
            {
                var dynamicValue = InContext(ValueContext(), () => CompileItem(schema.ValueSchema, typeof(object)));
                var jsonEntries = typeof(IEnumerable<KeyValuePair<string, JToken>>);
                return Expression.Call(WriteMapMethod.MakeGenericMethod(typeof(string), typeof(JToken)), writer,
                    Expression.Convert(value, jsonEntries),
                    Expression.Property(value, nameof(JObject.Count)),
                    Expression.Constant(WrapJsonValueWriter(dynamicValue)));
            }

            var kv = ReadCompiler.DictionaryArguments(type);
            if (kv == null)
            {
                if (typeof(IDictionary).IsAssignableFrom(type))
                {
                    var dynamicValue = InContext(ValueContext(), () => CompileItem(schema.ValueSchema, typeof(object)));
                    return Expression.Call(WriteUntypedMapMethod, writer, Expression.Convert(value, typeof(IDictionary)), Expression.Constant(dynamicValue));
                }

                throw new NotSupportedException($"[IDictionary] required to write against [Map] schema but found [{type}]");
            }

            var (keyType, valueType) = kv.Value;
            var valueFn = InContext(ValueContext(), () => CompileItem(schema.ValueSchema, valueType));
            var pair = typeof(KeyValuePair<,>).MakeGenericType(keyType, valueType);
            var entries = typeof(IEnumerable<>).MakeGenericType(pair);
            var countInterface = typeof(IReadOnlyDictionary<,>).MakeGenericType(keyType, valueType).IsAssignableFrom(type) && !typeof(IDictionary<,>).MakeGenericType(keyType, valueType).IsAssignableFrom(type)
                ? typeof(IReadOnlyCollection<>).MakeGenericType(pair)
                : typeof(ICollection<>).MakeGenericType(pair);

            // Under WriteEmpty a null dictionary outside a union is written as an empty map (Throw is handled by GuardNull).
            var isNull = Expression.ReferenceEqual(value, Expression.Constant(null, type));
            var count = Expression.Condition(isNull, Expression.Constant(0), Expression.Property(Expression.Convert(value, countInterface), countInterface.GetProperty("Count")!));
            var safeEntries = Expression.Coalesce(Expression.Convert(value, entries), Expression.Constant(Array.CreateInstance(pair, 0), entries));

            return Expression.Call(WriteMapMethod.MakeGenericMethod(keyType, valueType), writer, safeEntries, count, Expression.Constant(valueFn));
        }

        private static Action<IWriter, JToken> WrapJsonValueWriter(Delegate dynamicValue)
        {
            var write = (Action<IWriter, object>)dynamicValue;
            return (writer, token) => write(writer, WriteHelpers.UnwrapJson(token));
        }

        // ------------------------------------------------------------------ unions

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

            int branchIndex = -1;
            for (int i = 0; i < branches.Count; i++)
            {
                if (i != nullIndex && BranchAccepts(branches[i], valueType))
                {
                    branchIndex = i; // first matching branch, like the classic resolver
                    break;
                }
            }

            Expression nonNullValue = underlying != null ? Expression.Property(value, "Value") : value;
            Expression writeBranch = branchIndex >= 0
                ? Expression.Block(
                    Expression.Call(writer, WriteUnionIndexMethod, Expression.Constant(branchIndex)),
                    Build(branches[branchIndex], valueType, nonNullValue, writer))
                : Expression.Throw(Expression.Call(NoUnionBranchMethod, Expression.Convert(value, typeof(object)), Expression.Constant(schema)));

            if (!canBeNull)
            {
                return writeBranch;
            }

            Expression isNull = underlying != null
                ? Expression.Not(Expression.Property(value, "HasValue"))
                : Expression.ReferenceEqual(value, Expression.Constant(null, type));

            Expression writeNull = nullIndex >= 0
                ? Expression.Call(writer, WriteUnionIndexMethod, Expression.Constant(nullIndex))
                : Expression.Throw(Expression.Call(NoUnionBranchMethod, Expression.Constant(null, typeof(object)), Expression.Constant(schema)));

            return Expression.IfThenElse(isNull, writeNull, writeBranch);
        }

        /// <summary>Static branch matching for a known CLR type (mirrors the classic runtime matcher).</summary>
        private static bool BranchAccepts(TypeSchema branch, Type type)
        {
            switch (branch.Type)
            {
                case AvroType.Boolean: return type == typeof(bool);
                case AvroType.Int:
                case AvroType.Long:
                case AvroType.Float:
                case AvroType.Double:
                    // Any numeric CLR type converts to any numeric Avro type (e.g. int? marked [AvroType(Double)]).
                    return type.IsEnum || (type.IsPrimitive && type != typeof(bool) && type != typeof(IntPtr) && type != typeof(UIntPtr)) || type == typeof(decimal);
                case AvroType.String: return type == typeof(string) || type == typeof(Guid) || type == typeof(decimal) || type == typeof(DateTimeOffset) || type == typeof(Uri) || type.IsEnum;
                case AvroType.Bytes: return type == typeof(byte[]);
                case AvroType.Enum: return type.IsEnum || type == typeof(string);
                case AvroType.Array: return type != typeof(byte[]) && type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type) && !typeof(IDictionary).IsAssignableFrom(type) && ReadCompiler.DictionaryArguments(type) == null && type != typeof(JObject);
                case AvroType.Map: return ReadCompiler.DictionaryArguments(type) != null || typeof(IDictionary).IsAssignableFrom(type) || type == typeof(JObject);
                case AvroType.Record:
                case AvroType.Error:
                    if (type == typeof(ExpandoObject) || type == typeof(JObject) || typeof(IDictionary<string, object>).IsAssignableFrom(type) && type.IsClass)
                        return true;
                    return !type.IsPrimitive && !type.IsEnum && type != typeof(string) && type != typeof(byte[]) && !type.IsArray && type != typeof(object)
                           && ReadCompiler.CollectionItemType(type) == null && ReadCompiler.DictionaryArguments(type) == null
                           && (type.Name.Equals(((RecordSchema)branch).Name) || type.IsAnonymous() || ((RecordSchema)branch).RuntimeType == type);
                case AvroType.Fixed: return type == typeof(AvroFixed) || type == typeof(byte[]);
                case AvroType.Logical:
                    return ((LogicalTypeSchema)branch).LogicalTypeName switch
                    {
                        LogicalTypeSchema.LogicalTypeEnum.Uuid => type == typeof(Guid) || type == typeof(string),
                        LogicalTypeSchema.LogicalTypeEnum.Decimal or LogicalTypeSchema.LogicalTypeEnum.BigDecimal => type == typeof(decimal) || type.IsPrimitive && type != typeof(bool),
                        LogicalTypeSchema.LogicalTypeEnum.TimestampMicroseconds or LogicalTypeSchema.LogicalTypeEnum.TimestampMilliseconds
                            or LogicalTypeSchema.LogicalTypeEnum.TimestampNanoseconds or LogicalTypeSchema.LogicalTypeEnum.LocalTimestampMilliseconds
                            or LogicalTypeSchema.LogicalTypeEnum.LocalTimestampMicroseconds or LogicalTypeSchema.LogicalTypeEnum.LocalTimestampNanoseconds => type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(long),
                        LogicalTypeSchema.LogicalTypeEnum.Date => type == typeof(DateOnly),
                        LogicalTypeSchema.LogicalTypeEnum.TimeMicrosecond or LogicalTypeSchema.LogicalTypeEnum.TimeMilliseconds => type == typeof(TimeOnly),
                        LogicalTypeSchema.LogicalTypeEnum.Duration => type == typeof(TimeSpan),
                        _ => BranchAccepts(((LogicalTypeSchema)branch).BaseTypeSchema, type)
                    };
                default:
                    return false;
            }
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
