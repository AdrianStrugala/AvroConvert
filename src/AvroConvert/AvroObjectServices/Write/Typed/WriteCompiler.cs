using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Linq;
using SolTechnology.Avro.AvroObjectServices.BuildSchema;
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
    /// Compiles a (schema, T) pair into a strongly typed <c>Action&lt;Writer, T&gt;</c>: member access, conversions,
    /// enum positions and union branches are decided once at compile time for the static type. Values whose static type
    /// is <c>object</c> (or an interface/abstract class) are dispatched at runtime to the plan compiled for their actual
    /// type, which is how polymorphism, ExpandoObject and JSON tokens are handled.
    /// </summary>
    [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
    internal sealed class WriteCompiler
    {
        private static readonly MethodInfo WriteNullMethod = typeof(Writer).GetMethod(nameof(IWriter.WriteNull))!;
        private static readonly MethodInfo WriteBooleanMethod = typeof(Writer).GetMethod(nameof(IWriter.WriteBoolean))!;
        private static readonly MethodInfo WriteIntMethod = typeof(Writer).GetMethod(nameof(IWriter.WriteInt))!;
        private static readonly MethodInfo WriteLongMethod = typeof(Writer).GetMethod(nameof(IWriter.WriteLong))!;
        private static readonly MethodInfo WriteFloatMethod = typeof(Writer).GetMethod(nameof(IWriter.WriteFloat))!;
        private static readonly MethodInfo WriteDoubleMethod = typeof(Writer).GetMethod(nameof(IWriter.WriteDouble))!;
        private static readonly MethodInfo WriteStringMethod = typeof(Writer).GetMethod(nameof(IWriter.WriteString))!;
        private static readonly MethodInfo WriteBytesMethod = typeof(Writer).GetMethod(nameof(IWriter.WriteBytes), new[] { typeof(byte[]) })!;
        private static readonly MethodInfo WriteFixedMethod = typeof(Writer).GetMethod(nameof(IWriter.WriteFixed), new[] { typeof(byte[]) })!;
        private static readonly MethodInfo WriteEnumMethod = typeof(Writer).GetMethod(nameof(IWriter.WriteEnum))!;
        private static readonly MethodInfo WriteUnionIndexMethod = typeof(Writer).GetMethod(nameof(IWriter.WriteUnionIndex))!;
        private static readonly MethodInfo WriteItemCountMethod = typeof(Writer).GetMethod(nameof(IWriter.WriteItemCount))!;
        private static readonly MethodInfo WriteArrayEndMethod = typeof(Writer).GetMethod(nameof(IWriter.WriteArrayEnd))!;
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

        private static MethodInfo LogicalWriter(string name) =>
            typeof(LogicalWriters).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!;
        private static readonly MethodInfo WriteDynamicMethod = typeof(WriteCompiler).GetMethod(nameof(WriteDynamic), BindingFlags.Instance | BindingFlags.NonPublic)!;
        private static readonly MethodInfo EnsureStackMethod = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.EnsureSufficientExecutionStack))!;
        private static readonly MethodInfo KeepAliveMethod = typeof(GC).GetMethod(nameof(GC.KeepAlive))!;
        private static readonly MethodInfo WriteRecordBranchMethod = Helper(nameof(WriteHelpers.WriteRecordBranch));
        private static readonly PropertyInfo DateTimeTicks = typeof(DateTime).GetProperty(nameof(DateTime.Ticks))!;
        private static readonly PropertyInfo DateTimeOffsetDateTime = typeof(DateTimeOffset).GetProperty(nameof(DateTimeOffset.DateTime))!;
        private static readonly PropertyInfo DateTimeOffsetUtcDateTime = typeof(DateTimeOffset).GetProperty(nameof(DateTimeOffset.UtcDateTime))!;
        private static readonly PropertyInfo DateOnlyDayNumber = typeof(DateOnly).GetProperty(nameof(DateOnly.DayNumber))!;

        private static MethodInfo Helper(string name) => typeof(WriteHelpers).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!;

        private readonly AvroConvertOptions _options;
        private readonly AvroNumberHandling _numberHandling;
        private readonly AvroNullValueHandling _nullHandling;
        private readonly Dictionary<Type, Action<object, Writer>> _converters;
        private readonly Dictionary<(TypeSchema, Type), object> _recordHolders = new();
        // Records are inlined into the caller's plan up to this many fields in total; the JIT optimises several small
        // methods better than one very large one, so records past the limit stay separate delegates.
        private const int InlinedFieldsLimit = 100;
        private int _inlinedFields;
        private readonly HashSet<(TypeSchema, Type)> _recordsInProgress = new();

        // Where in the schema the code being compiled writes ("User.name", "User.tags[]"); null for the root value.
        // Only used to make the null-value error point at the member.
        private string _context;

        internal WriteCompiler(AvroConvertOptions options)
        {
            _options = options;
            _numberHandling = options?.NumberHandling ?? AvroNumberHandling.Strict;
            _nullHandling = options?.NullValueHandling ?? AvroNullValueHandling.Throw;
            _converters = options?.AvroConverters.Count > 0
                ? options.AvroConverters.ToDictionary(c => c.TypeSchema.RuntimeType, c => (Action<object, Writer>)c.Serialize)
                : null;
        }

        /// <summary>Compiles a writer that accepts the root as <c>object</c> and casts it once to <paramref name="type"/>.</summary>
        internal Encoder.WriteItem Compile(TypeSchema schema, Type type)
        {
            var value = Expression.Parameter(typeof(object), "value");
            var writer = Expression.Parameter(typeof(Writer), "writer");
            var body = Build(schema, type, Expression.Convert(value, type), writer);
            return Expression.Lambda<Encoder.WriteItem>(body, value, writer).Compile();
        }

        internal Action<Writer, T> CompileTyped<T>(TypeSchema schema)
        {
            var value = Expression.Parameter(typeof(T), "value");
            var writer = Expression.Parameter(typeof(Writer), "writer");
            var body = Build(schema, typeof(T), value, writer);
            return Expression.Lambda<Action<Writer, T>>(body, writer, value).Compile();
        }

        /// <summary>Runtime dispatch for values whose static type carries no information (object, interfaces, JSON tokens).</summary>
        /// <param name="nullMessage">Error for a null value; null when a null value is written (schema allows it or WriteEmpty).</param>
        // Not inlined: the JIT would weigh this body in every plan that calls it, which made compiling a plan several times slower.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void WriteDynamic(TypeSchema schema, string nullMessage, DynamicSite site, object value, Writer writer)
        {
            // How deep this goes depends on the data, a reference cycle has no end, and a stack overflow cannot be caught.
            RuntimeHelpers.EnsureSufficientExecutionStack();

            if (value == null)
            {
                if (nullMessage != null)
                {
                    throw WriteHelpers.NullNotAllowed(nullMessage);
                }

                WriteHelpers.WriteNullValue(schema, writer);
                return;
            }

            var type = value.GetType();
            var first = site.First;
            if (ReferenceEquals(first?.Type, type))
            {
                first.Plan(value, writer);
                return;
            }

            var second = site.Second;
            if (ReferenceEquals(second?.Type, type))
            {
                second.Plan(value, writer);
                return;
            }

            if (type == typeof(object))
            {
                // A bare object has no fields, and its plan would dispatch back here for ever.
                if (RuntimeSchema.IsObjectPlaceholder(schema))
                {
                    return;
                }

                throw new AvroTypeMismatchException($"An instance of [{typeof(object)}] holds no data to write against {Describe(schema)}.");
            }

            var plan = WritePlanCache.Get(schema, type, _options);
            site.Second = first;
            site.First = new DynamicEntry(type, plan);
            plan(value, writer);
        }

        /// <summary>
        /// The plans of the last two runtime types seen at one <see cref="Dynamic"/> call. Values of type <c>object</c> usually hold
        /// one or two types, and looking a plan up in the process-wide cache for every value is what such a write spends most on.
        /// </summary>
        private sealed class DynamicSite
        {
            internal DynamicEntry First;
            internal DynamicEntry Second;
        }

        private sealed record DynamicEntry(Type Type, Encoder.WriteItem Plan);

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
                Expression.Constant(new DynamicSite()),
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
            ReadCompiler.CollectionItemType(type) != null || type.GetDictionaryArguments() != null || type == typeof(IDictionary<string, object>);

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
                    TemporalText.IsTemporal(underlying)
                        ? Expression.Call(TemporalText.FormatMethod(underlying), Expression.Property(value, "Value"))
                        : Expression.Call(Expression.Property(value, "Value"), underlying.GetMethod(nameof(ToString), Type.EmptyTypes)!),
                    OrEmpty(Expression.Constant(null, typeof(string)), Expression.Constant(string.Empty), schema));
            }

            if (TemporalText.IsTemporal(type))
            {
                return Expression.Call(TemporalText.FormatMethod(type), value);
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
                case LogicalTypeSchema.LogicalTypeEnum.Date when type == typeof(DateTime) && schema.BaseTypeSchema.Type == AvroType.Int:
                    return Expression.Call(LogicalWriter(nameof(LogicalWriters.WriteDateFromDateTime)), writer, value);

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
                case LogicalTypeSchema.LogicalTypeEnum.TimeMicrosecond when type == typeof(TimeSpan):
                    return Expression.Call(LogicalWriter(nameof(LogicalWriters.WriteTimeMicrosFromTimeSpan)), writer, value);
                case LogicalTypeSchema.LogicalTypeEnum.TimeMilliseconds when type == typeof(TimeSpan):
                    return Expression.Call(LogicalWriter(nameof(LogicalWriters.WriteTimeMillisFromTimeSpan)), writer, value);

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
            if (RuntimeSchema.IsObjectPlaceholder(schema) && type != typeof(object))
            {
                // Writing against the empty record of `object` would drop every field of the value without a word.
                throw new NotSupportedException(
                    "The schema of a value of type object comes from the data, which is not possible here (e.g. headless data does not carry a schema). " +
                    "Give the schema to both sides: AvroSchema.FromValue(value), then SerializeHeadless(value, schema).");
            }

            if (type == typeof(ExpandoObject) || typeof(IDictionary<string, object>).IsAssignableFrom(type))
            {
                var (names, writers, defaults) = DynamicFieldWriters(schema);
                return Expression.Call(WriteDictionaryRecordMethod, writer, Expression.Convert(value, typeof(IDictionary<string, object>)), Expression.Constant(names), Expression.Constant(writers), Expression.Constant(defaults));
            }

            if (type == typeof(JObject))
            {
                var (names, writers, defaults) = DynamicFieldWriters(schema);
                return Expression.Call(WriteJsonRecordMethod, writer, value, Expression.Constant(names), Expression.Constant(writers), Expression.Constant(defaults));
            }

            if (type.IsPrimitive || type == typeof(string) || type.IsArray || type.IsEnum || ReadCompiler.CollectionItemType(type) != null || type.GetDictionaryArguments() != null)
            {
                throw new NotSupportedException($"[{type}] cannot be written against a record schema");
            }

            var key = ((TypeSchema)schema, type);
            if (!_recordHolders.TryGetValue(key, out var holderObj))
            {
                // Non-recursive records are inlined into the caller's tree; a record that refers back to itself gets its
                // own delegate (via a holder) so the tree terminates.
                if (_recordsInProgress.Contains(key))
                {
                    // A reference back to a record whose body is being built: filled in when that body is complete.
                    holderObj = Activator.CreateInstance(typeof(DelegateHolder<>).MakeGenericType(type))!;
                    _recordHolders.Add(key, holderObj);
                }
                else if (_inlinedFields + schema.Fields.Count <= InlinedFieldsLimit)
                {
                    _inlinedFields += schema.Fields.Count;
                    _recordsInProgress.Add(key);
                    var instance = Expression.Variable(type, "instance");
                    var inline = Expression.Block(typeof(void), new[] { instance },
                        Expression.Assign(instance, value),
                        BuildRecordFields(schema, type, instance, writer));
                    _recordsInProgress.Remove(key);

                    if (!_recordHolders.TryGetValue(key, out holderObj))
                    {
                        return inline;
                    }

                    holderObj.GetType().GetField(nameof(DelegateHolder<object>.Fn))!.SetValue(holderObj, CompileRecordDelegate(schema, type));
                }
                else
                {
                    holderObj = Activator.CreateInstance(typeof(DelegateHolder<>).MakeGenericType(type))!;
                    _recordHolders.Add(key, holderObj);
                    holderObj.GetType().GetField(nameof(DelegateHolder<object>.Fn))!.SetValue(holderObj, CompileRecordDelegate(schema, type));
                }
            }

            var fn = Expression.Field(Expression.Constant(holderObj), nameof(DelegateHolder<object>.Fn));
            return Expression.Invoke(fn, writer, value);
        }

        /// <summary>The keys of a field in a property bag: its name, which reading into a dictionary gives, then its aliases.</summary>
        private static string[] FieldKeys(RecordFieldSchema field) => field.Aliases.Prepend(field.Name).ToArray();

        private (string[][] Names, Action<Writer, object>[] Writers, Action<Writer>[] Defaults) DynamicFieldWriters(RecordSchema schema)
        {
            var names = new string[schema.Fields.Count][];
            var writers = new Action<Writer, object>[schema.Fields.Count];
            var defaults = new Action<Writer>[schema.Fields.Count];
            for (int i = 0; i < names.Length; i++)
            {
                var field = schema.Fields[i];
                names[i] = FieldKeys(field);
                var writer = Expression.Parameter(typeof(Writer), "writer");
                var item = Expression.Parameter(typeof(object), "value");
                var body = InContext($"{schema.Name}.{field.Name}", () => Build(field.TypeSchema, typeof(object), item, writer));
                writers[i] = Expression.Lambda<Action<Writer, object>>(body, writer, item).Compile();

                if (field.HasDefaultValue && field.DefaultValue != null && TryBuildDefault(schema, field, writer) is { } defaultValue)
                {
                    defaults[i] = Expression.Lambda<Action<Writer>>(defaultValue, writer).Compile();
                }
            }

            return (names, writers, defaults);
        }

        private Delegate CompileRecordDelegate(RecordSchema schema, Type type)
        {
            var writer = Expression.Parameter(typeof(Writer), "writer");
            var instance = Expression.Parameter(type, "instance");
            // Recursion goes through these delegates, and a reference cycle has no end. KeepAlive costs nothing and keeps the
            // recursive call out of the tail position: as a tail call it would not grow the stack, so the check would never fire.
            Expression keepInFrame = type.IsValueType ? Expression.Empty() : Expression.Call(KeepAliveMethod, Expression.Convert(instance, typeof(object)));
            var body = Expression.Block(typeof(void), Expression.Call(EnsureStackMethod), BuildRecordFields(schema, type, instance, writer), keepInFrame);
            return Expression.Lambda(typeof(Action<,>).MakeGenericType(typeof(Writer), type), body, writer, instance).Compile();
        }

        private Expression BuildRecordFields(RecordSchema schema, Type type, ParameterExpression instance, ParameterExpression writer)
        {
            var members = TypeMembers.For(type);
            var fields = new List<Expression>(schema.Fields.Count);

            foreach (var field in schema.Fields)
            {
                var member = members.Find(field.Aliases.FirstOrDefault() ?? field.Name);
                if (member is not { CanRead: true })
                {
                    // Member absent on this type, field with a default (e.g. writing with a newer schema version): write the default.
                    if (field.HasDefaultValue && field.DefaultValue != null && TryBuildDefault(schema, field, writer) is { } defaultValue)
                    {
                        fields.Add(defaultValue);
                        continue;
                    }

                    // Otherwise the field is written as "no value".
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

            return body;
        }

        /// <summary>Writer of the default value of <paramref name="field"/>; null when the default cannot be written as its schema.</summary>
        private Expression TryBuildDefault(RecordSchema schema, RecordFieldSchema field, ParameterExpression writer)
        {
            var defaultValue = field.DefaultValue;
            try
            {
                return InContext($"{schema.Name}.{field.Name}",
                    () => Build(field.TypeSchema, defaultValue.GetType(), Expression.Constant(defaultValue, defaultValue.GetType()), writer));
            }
            catch (Exception e) when (e is NotSupportedException or InvalidOperationException or ArgumentException or AvroTypeMismatchException)
            {
                return null;
            }
        }

        // ------------------------------------------------------------------ collections

        /// <summary>T[] and List&lt;T&gt;: one block with the item expression inlined in a loop (no delegate per item).</summary>
        private Expression InlineListLoop(ArraySchema schema, Type type, Type itemType, Expression value, ParameterExpression writer)
        {
            var items = Expression.Variable(type, "items");
            var count = Expression.Variable(typeof(int), "count");
            var i = Expression.Variable(typeof(int), "i");
            var item = Expression.Variable(itemType, "item");
            var end = Expression.Label("end");

            Expression length = type.IsArray ? Expression.ArrayLength(items) : Expression.Property(items, nameof(List<object>.Count));
            Expression at = type.IsArray ? Expression.ArrayIndex(items, i) : Expression.Property(items, "Item", i);
            var writeItem = InContext(ItemContext(), () => Build(schema.ItemSchema, itemType, item, writer));

            return Expression.Block(typeof(void), new[] { items, count, i },
                Expression.Assign(items, value),
                Expression.IfThen(Expression.NotEqual(items, Expression.Constant(null, type)),
                    Expression.Block(
                        Expression.Assign(count, length),
                        Expression.Call(writer, WriteItemCountMethod, Expression.Convert(count, typeof(long))),
                        Expression.Assign(i, Expression.Constant(0)),
                        Expression.Loop(
                            Expression.Block(new[] { item },
                                Expression.IfThen(Expression.GreaterThanOrEqual(i, count), Expression.Break(end)),
                                Expression.Assign(item, at),
                                writeItem,
                                Expression.PreIncrementAssign(i)),
                            end))),
                Expression.Call(writer, WriteArrayEndMethod));
        }
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

            if (type.IsArray || type == typeof(List<>).MakeGenericType(itemType))
            {
                return InlineListLoop(schema, type, itemType, value, writer);
            }

            var itemFn = InContext(ItemContext(), () => CompileItem(schema.ItemSchema, itemType));

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

            var kv = type.GetDictionaryArguments();
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

        private static Action<Writer, JToken> WrapJsonValueWriter(Delegate dynamicValue)
        {
            var write = (Action<Writer, object>)dynamicValue;
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

            var bagBranches = BagRecordBranches(branches, valueType);
            int branchIndex = bagBranches?.Length == 1 ? bagBranches[0].Index : -1;
            if (bagBranches?.Length == 1)
            {
                bagBranches = null;
            }

            for (int i = 0; branchIndex < 0 && bagBranches == null && i < branches.Count; i++)
            {
                // The branch of exactly this type first: a double next to a long branch would lose its fraction, a DateTime next to a date its time.
                if (i != nullIndex && IsExactBranch(branches[i], valueType))
                {
                    branchIndex = i;
                }
            }

            if (branchIndex < 0 && bagBranches == null)
            {
                branchIndex = OwnRecordBranch(branches, valueType);
            }

            for (int i = 0; branchIndex < 0 && bagBranches == null && i < branches.Count; i++)
            {
                if (i != nullIndex && BranchAccepts(branches[i], valueType))
                {
                    branchIndex = i; // first matching branch, like the classic resolver
                }
            }

            if (branchIndex < 0 && bagBranches == null && TemporalText.IsTemporal(valueType))
            {
                for (int i = 0; i < branches.Count; i++)
                {
                    if (branches[i].Type == AvroType.String)
                    {
                        branchIndex = i;
                        break;
                    }
                }
            }

            if (branchIndex < 0 && bagBranches == null)
            {
                // A class with no record branch of its name goes to the first record it can be written as, the same rule as
                // for a record outside a union: the names do not have to match, every field needs a value.
                for (int i = 0; i < branches.Count; i++)
                {
                    if (branches[i] is RecordSchema record && WritesAsRecord(record, valueType))
                    {
                        branchIndex = i;
                        break;
                    }
                }
            }

            Expression nonNullValue = underlying != null ? Expression.Property(value, "Value") : value;
            Expression writeBranch = bagBranches != null
                ? Expression.Call(WriteRecordBranchMethod, writer, Expression.Convert(nonNullValue, typeof(object)), Expression.Constant(bagBranches), Expression.Constant(schema))
                : branchIndex >= 0
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

        /// <summary>
        /// True when <see cref="BuildRecordFields"/> writes <paramref name="type"/> as <paramref name="schema"/> without a missing
        /// value: every field has a readable member, a default, can hold null, or empty values are allowed.
        /// </summary>
        private bool WritesAsRecord(RecordSchema schema, Type type)
        {
            if (!IsRecordLike(type))
            {
                return false;
            }

            var members = TypeMembers.For(type);
            return schema.Fields.All(field =>
                members.Find(field.Aliases.FirstOrDefault() ?? field.Name) is { CanRead: true }
                || field.HasDefaultValue && field.DefaultValue != null
                || WriteHelpers.CanBeNull(field.TypeSchema)
                || _nullHandling == AvroNullValueHandling.WriteEmpty);
        }

        private static bool IsRecordLike(Type type) =>
            !(type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(byte[]) || type.IsArray || type == typeof(object)
              || type.IsAbstract || type.IsInterface || ReadCompiler.CollectionItemType(type) != null || type.GetDictionaryArguments() != null);

        /// <summary>
        /// The record branch of <paramref name="type"/>: of the same full name, of the same name (a file may hold it in another
        /// namespace), or the first one with a field for every member, so that nothing is lost. -1 when there is none.
        /// </summary>
        private int OwnRecordBranch(IReadOnlyList<TypeSchema> branches, Type type)
        {
            if (!IsRecordLike(type) || !branches.OfType<RecordSchema>().Any())
            {
                return -1;
            }

            RecordSchema own;
            try
            {
                own = Schema.Create(type, _options) as RecordSchema;
            }
            catch (Exception)
            {
                // Nothing says the type has a schema of its own: the data goes by the schema it was given.
                own = null;
            }

            int Find(Func<RecordSchema, bool> matches)
            {
                for (int i = 0; i < branches.Count; i++)
                {
                    if (branches[i] is RecordSchema record && matches(record))
                    {
                        return i;
                    }
                }

                return -1;
            }

            var index = own == null ? -1 : Find(record => record.FullName == own.FullName);
            index = index >= 0 || own == null ? index : Find(record => record.Name == own.Name);
            return index >= 0 ? index : Find(record => WritesAsRecord(record, type) && HasFieldForEveryMember(record, type));
        }

        private static bool HasFieldForEveryMember(RecordSchema schema, Type type)
        {
            var members = TypeMembers.For(type);
            var written = schema.Fields.Select(field => members.Find(field.Aliases.FirstOrDefault() ?? field.Name)).ToHashSet();
            return members.All.Where(member => member.CanRead).All(written.Contains);
        }

        /// <summary>
        /// A property bag says which record it is only by its keys, which are known when it is written. Null when the union has no
        /// records for it, or a map takes a dictionary as it is; an ExpandoObject is a record for AvroConvert, so it goes to a record first.
        /// </summary>
        private WriteHelpers.RecordBranch[] BagRecordBranches(IReadOnlyList<TypeSchema> branches, Type type)
        {
            bool isBag = type == typeof(ExpandoObject) || type == typeof(JObject) || typeof(IDictionary<string, object>).IsAssignableFrom(type) && type.IsClass;
            if (!isBag || type != typeof(ExpandoObject) && branches.Any(branch => branch is MapSchema))
            {
                return null;
            }

            var records = new List<WriteHelpers.RecordBranch>();
            for (int i = 0; i < branches.Count; i++)
            {
                if (branches[i] is RecordSchema record)
                {
                    var writer = Expression.Parameter(typeof(Writer), "writer");
                    var item = Expression.Parameter(typeof(object), "value");
                    var write = Expression.Lambda<Action<Writer, object>>(Build(record, typeof(object), item, writer), writer, item).Compile();
                    records.Add(new WriteHelpers.RecordBranch(i, record.Fields.Select(FieldKeys).ToArray(),
                        record.Fields.Select(field => !field.HasDefaultValue && !WriteHelpers.CanBeNull(field.TypeSchema)).ToArray(), write));
                }
            }

            return records.Count == 0 ? null : records.ToArray();
        }

        /// <summary>True for the branch that <paramref name="type"/> is written to without any conversion.</summary>
        private static bool IsExactBranch(TypeSchema branch, Type type) => branch switch
        {
            LogicalTypeSchema logical => logical.LogicalTypeName switch
            {
                LogicalTypeSchema.LogicalTypeEnum.Uuid => type == typeof(Guid),
                LogicalTypeSchema.LogicalTypeEnum.Decimal or LogicalTypeSchema.LogicalTypeEnum.BigDecimal => type == typeof(decimal),
                LogicalTypeSchema.LogicalTypeEnum.Date => type == typeof(DateOnly),
                LogicalTypeSchema.LogicalTypeEnum.TimeMicrosecond or LogicalTypeSchema.LogicalTypeEnum.TimeMilliseconds => type == typeof(TimeOnly),
                LogicalTypeSchema.LogicalTypeEnum.Duration => type == typeof(TimeSpan),
                var name when IsTimestamp(name) => type == typeof(DateTime) || type == typeof(DateTimeOffset),
                _ => false
            },
            MapSchema => type == typeof(JObject) || type.GetDictionaryArguments()?.Key == typeof(string),
            _ => SchemaPromotion.ClrType(branch.Type) == type
        };

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
                case AvroType.String: return type == typeof(string) || type == typeof(Guid) || type == typeof(decimal) || type == typeof(Uri) || type.IsEnum;
                case AvroType.Bytes: return type == typeof(byte[]);
                case AvroType.Enum: return type.IsEnum || type == typeof(string);
                case AvroType.Array:
                    if (type.GetDictionaryArguments() != null)
                    {
                        // A dictionary with non-string keys is an array of {Key, Value} records.
                        return ((ArraySchema)branch).ItemSchema is RecordSchema item && item.TryGetField("Key", out _) && item.TryGetField("Value", out _);
                    }
                    return type != typeof(byte[]) && type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type) && !typeof(IDictionary).IsAssignableFrom(type) && type != typeof(JObject);
                case AvroType.Map: return type.GetDictionaryArguments() != null || typeof(IDictionary).IsAssignableFrom(type) || type == typeof(JObject);
                case AvroType.Record:
                case AvroType.Error:
                    if (type == typeof(ExpandoObject) || type == typeof(JObject) || typeof(IDictionary<string, object>).IsAssignableFrom(type) && type.IsClass)
                        return true;
                    return !type.IsPrimitive && !type.IsEnum && type != typeof(string) && type != typeof(byte[]) && !type.IsArray && type != typeof(object)
                           && ReadCompiler.CollectionItemType(type) == null && type.GetDictionaryArguments() == null
                           && (type.Name.Equals(((RecordSchema)branch).Name) || type.GetAvroName() == ((RecordSchema)branch).Name || type.IsAnonymous() || ((RecordSchema)branch).RuntimeType == type);
                case AvroType.Fixed: return type == typeof(AvroFixed) || type == typeof(byte[]);
                case AvroType.Logical:
                    return ((LogicalTypeSchema)branch).LogicalTypeName switch
                    {
                        LogicalTypeSchema.LogicalTypeEnum.Uuid => type == typeof(Guid) || type == typeof(string),
                        LogicalTypeSchema.LogicalTypeEnum.Decimal or LogicalTypeSchema.LogicalTypeEnum.BigDecimal => type == typeof(decimal) || type.IsPrimitive && type != typeof(bool),
                        LogicalTypeSchema.LogicalTypeEnum.TimestampMicroseconds or LogicalTypeSchema.LogicalTypeEnum.TimestampMilliseconds
                            or LogicalTypeSchema.LogicalTypeEnum.TimestampNanoseconds or LogicalTypeSchema.LogicalTypeEnum.LocalTimestampMilliseconds
                            or LogicalTypeSchema.LogicalTypeEnum.LocalTimestampMicroseconds or LogicalTypeSchema.LogicalTypeEnum.LocalTimestampNanoseconds => type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(long),
                        LogicalTypeSchema.LogicalTypeEnum.Date => type == typeof(DateOnly) || type == typeof(DateTime),
                        LogicalTypeSchema.LogicalTypeEnum.TimeMicrosecond or LogicalTypeSchema.LogicalTypeEnum.TimeMilliseconds => type == typeof(TimeOnly) || type == typeof(TimeSpan),
                        LogicalTypeSchema.LogicalTypeEnum.Duration => type == typeof(TimeSpan),
                        _ => BranchAccepts(((LogicalTypeSchema)branch).BaseTypeSchema, type)
                    };
                default:
                    return false;
            }
        }

        private Delegate CompileItem(TypeSchema schema, Type itemType)
        {
            var writer = Expression.Parameter(typeof(Writer), "writer");
            var item = Expression.Parameter(itemType, "item");
            var body = Build(schema, itemType, item, writer);
            return Expression.Lambda(typeof(Action<,>).MakeGenericType(typeof(Writer), itemType), body, writer, item).Compile();
        }

        private sealed class DelegateHolder<T>
        {
#pragma warning disable CS0649 // assigned through reflection in BuildRecord
            public Action<Writer, T> Fn;
#pragma warning restore CS0649
        }
    }
}
