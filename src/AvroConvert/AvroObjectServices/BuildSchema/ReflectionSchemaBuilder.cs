// Copyright (c) Microsoft Corporation
// All rights reserved.
// 
// Licensed under the Apache License, Version 2.0 (the "License"); you may not
// use this file except in compliance with the License.  You may obtain a copy
// of the License at http://www.apache.org/licenses/LICENSE-2.0
// 
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED
// WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE,
// MERCHANTABLITY OR NON-INFRINGEMENT.
// 
// See the Apache Version 2.0 License for specific language governing
// permissions and limitations under the License.

/** Modifications copyright(C) 2022 Adrian Strugala **/

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using SolTechnology.Avro.AvroObjectServices.Read;
using SolTechnology.Avro.AvroObjectServices.Schemas;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;
using SolTechnology.Avro.Infrastructure.Attributes;
using SolTechnology.Avro.Infrastructure.Extensions;
using SolTechnology.Avro.Policies;
using System.Diagnostics.CodeAnalysis;
using SolTechnology.Avro.Infrastructure;

namespace SolTechnology.Avro.AvroObjectServices.BuildSchema
{
    /// <summary>
    ///     This class creates an avro schema given a c# type.
    /// </summary>

    [RequiresUnreferencedCode(AotMessages.Reflection), RequiresDynamicCode(AotMessages.DynamicCode)]
    internal sealed class ReflectionSchemaBuilder
    {
        private static readonly Dictionary<Type, Func<Type, LogicalTypeSchema>> TypeToAvroLogicalSchemaMap =
            new()
            {
                { typeof(decimal), type => new DecimalSchema(type) },
                { typeof(Guid), type => new UuidSchema(type) },
                { typeof(DateTime), type => new TimestampMicrosecondsSchema(type) },
                { typeof(DateTimeOffset), type => new TimestampMicrosecondsSchema(type) },
                { typeof(DateOnly), type => new DateSchema(type) },
                { typeof(TimeOnly), type => new TimeMicrosecondsSchema(type) },
                { typeof(TimeSpan), type => new DurationSchema(type) },
            };


        private static readonly Dictionary<Type, Func<Type, PrimitiveTypeSchema>> TypeToAvroPrimitiveSchemaMap =
            new()
            {
                { typeof(AvroNull), type => new NullSchema(type) },
                { typeof(char), type => new IntSchema(type) },
                { typeof(byte), type => new IntSchema(type) },
                { typeof(sbyte), type => new IntSchema(type) },
                { typeof(short), type => new IntSchema(type) },
                { typeof(ushort), type => new IntSchema(type) },
                { typeof(uint), type => new IntSchema(type) },
                { typeof(int), type => new IntSchema(type) },
                { typeof(bool), type => new BooleanSchema() },
                { typeof(long), type => new LongSchema(type) },
                { typeof(ulong), type => new LongSchema(type) },
                { typeof(float), type => new FloatSchema() },
                { typeof(double), type => new DoubleSchema() },
                { typeof(string), type => new StringSchema(type) },
                { typeof(Uri), type => new StringSchema(type) },
                { typeof(byte[]), type => new BytesSchema() },
                { typeof(decimal), type => new StringSchema(type) },
                { typeof(DateTime), type => new LongSchema(type) }
            };

        private static readonly Dictionary<AvroTypeRepresentation, Func<Type, TypeSchema>> AvroRepresentationToAvroSchemaMap =
            new()
            {
                { AvroTypeRepresentation.Null, type => new NullSchema(type) },
                { AvroTypeRepresentation.Boolean, type => new BooleanSchema() },
                { AvroTypeRepresentation.Int, type => new IntSchema(type) },
                { AvroTypeRepresentation.Long, type => new LongSchema(type) },
                { AvroTypeRepresentation.Float, type => new FloatSchema(type) },
                { AvroTypeRepresentation.Double, type => new DoubleSchema() },
                { AvroTypeRepresentation.Bytes, type => new BytesSchema() },
                { AvroTypeRepresentation.String, type => new StringSchema(type) },
                { AvroTypeRepresentation.Uuid, type => new UuidSchema(type) },
                { AvroTypeRepresentation.TimestampMilliseconds, type => new TimestampMillisecondsSchema(type) },
                { AvroTypeRepresentation.TimestampMicroseconds, type => new TimestampMicrosecondsSchema(type) },
                { AvroTypeRepresentation.Decimal, type => new DecimalSchema(type) },
                { AvroTypeRepresentation.Duration, type => new DurationSchema(type) },
                { AvroTypeRepresentation.TimeMilliseconds, type => new TimeMillisecondsSchema(type) },
                { AvroTypeRepresentation.TimeMicroseconds, type => new TimeMicrosecondsSchema(type) },
                { AvroTypeRepresentation.Date, type => new DateSchema(type) },
                { AvroTypeRepresentation.TimestampNanoseconds, type => new TimestampSchema(LogicalTypeSchema.LogicalTypeEnum.TimestampNanoseconds, type) },
                { AvroTypeRepresentation.LocalTimestampMilliseconds, type => new TimestampSchema(LogicalTypeSchema.LogicalTypeEnum.LocalTimestampMilliseconds, type) },
                { AvroTypeRepresentation.LocalTimestampMicroseconds, type => new TimestampSchema(LogicalTypeSchema.LogicalTypeEnum.LocalTimestampMicroseconds, type) },
                { AvroTypeRepresentation.LocalTimestampNanoseconds, type => new TimestampSchema(LogicalTypeSchema.LogicalTypeEnum.LocalTimestampNanoseconds, type) },
                { AvroTypeRepresentation.UuidFixed, type => new UuidSchema(type, UuidSchema.CreateFixedBase()) },
                { AvroTypeRepresentation.BigDecimal, type => new BigDecimalSchema(type) },
            };

        private readonly AvroConvertOptions _options;
        private readonly bool _hasCustomConverters;
        private readonly Dictionary<Type, TypeSchema> _customSchemaMapping;
        private readonly AvroContractResolver _resolver;
        private readonly IAvroNamingPolicy _namingPolicy;
        private readonly HashSet<Type> _recordsInProgress = new();
        private readonly NullabilityInfoContext _nullability = new();

        internal ReflectionSchemaBuilder(AvroConvertOptions options = null)
        {
            if (options == null)
            {
                options = new AvroConvertOptions();
            }
            _options = options;
            _hasCustomConverters = options.AvroConverters.Any();
            _customSchemaMapping = options.AvroConverters.ToDictionary(x => x.TypeSchema.RuntimeType, y => y.TypeSchema);
            _resolver = new AvroContractResolver(
                includeOnlyDataContractMembers: options.IncludeOnlyDataContractMembers,
                namingPolicy: options.NamingPolicy);
            _namingPolicy = options.NamingPolicy;
        }

        /// <summary>
        ///     Creates a schema definition.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        ///     New instance of schema definition.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="type"/> parameter is null.</exception>
        internal TypeSchema BuildSchema(Type type)
        {
            return type == null ? new NullSchema() : CreateSchema(false, type, new Dictionary<string, NamedSchema>(), 0, null);
        }

        private TypeSchema CreateSchema(bool forceNullable,
            Type type,
            Dictionary<string, NamedSchema> schemas,
            uint currentDepth,
            AvroTypeRepresentation? representation,
            Type prioritizedType = null,
            MemberInfo memberInfo = null,
            NullabilityInfo nullability = null)
        {
            if (currentDepth == _options.MaxItemsInSchemaTree)
            {
                throw new SerializationException(string.Format(CultureInfo.InvariantCulture, "Maximum depth of object graph reached."));
            }

            if (_hasCustomConverters)
            {
                if (_customSchemaMapping.TryGetValue(type, out var schema))
                {
                    return schema;
                }
            }

            var typeInfo = _resolver.ResolveType(type, memberInfo);
            if (typeInfo == null)
            {
                throw new SerializationException(
                    string.Format(CultureInfo.InvariantCulture, "Unexpected type info returned for type '{0}'.", type));
            }

            // The nullability of the items and values comes from the member: List<string?> has its annotation there, not on the type.
            nullability ??= MemberNullability(memberInfo);

            return typeInfo.Nullable || forceNullable
                ? this.CreateNullableSchema(type, schemas, currentDepth, prioritizedType, memberInfo, representation, nullability)
                : this.CreateNotNullableSchema(type, schemas, currentDepth, memberInfo, representation, nullability);
        }

        private TypeSchema CreateNullableSchema(Type type, Dictionary<string, NamedSchema> schemas, uint currentDepth, Type prioritizedType, MemberInfo info, AvroTypeRepresentation? representation, NullabilityInfo nullability)
        {
            var typeSchemas = new List<TypeSchema> { new NullSchema(type) };
            var notNullableType = type;
            var underlyingType = Nullable.GetUnderlyingType(type);
            if (underlyingType != null)
            {
                notNullableType = underlyingType;
            }

            var notNullableSchema = this.CreateNotNullableSchema(notNullableType, schemas, currentDepth, info, representation, nullability);
            if (notNullableSchema is UnionSchema unionSchema)
            {
                typeSchemas.AddRange(unionSchema.Schemas);
            }
            else
            {
                typeSchemas.Add(notNullableSchema);
            }

            typeSchemas = typeSchemas.OrderBy(x =>
                prioritizedType == null && x.Type.ToString() != "Null"
                || prioritizedType != null && x.Type.ToString() == "Null")
                .ToList();

            return new UnionSchema(typeSchemas, type);
        }

        /// <summary>
        /// Creates the avro schema for the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="schemas">The schemas seen so far.</param>
        /// <param name="currentDepth">The current depth.</param>
        /// <returns>
        /// New instance of schema.
        /// </returns>
        /// <exception cref="System.Runtime.Serialization.SerializationException">Thrown when maximum depth of object graph is reached.</exception>
        private TypeSchema CreateNotNullableSchema(Type type, Dictionary<string, NamedSchema> schemas, uint currentDepth, MemberInfo info, AvroTypeRepresentation? representation, NullabilityInfo nullability = null)
        {
            //Forced type
            if (representation != null)
            {
                return AvroRepresentationToAvroSchemaMap[representation.Value](type);
            }

            if (_options.DateTimeRepresentation is { } dateTimeRepresentation && (type == typeof(DateTime) || type == typeof(DateTimeOffset)))
            {
                return AvroRepresentationToAvroSchemaMap[dateTimeRepresentation](type);
            }

            //Logical
            TypeSchema schema = TryBuildLogicalTypeSchema(type, info);
            if (schema != null)
            {
                return schema;
            }

            //Primitive
            schema = TryBuildPrimitiveTypeSchema(type);
            if (schema != null)
            {
                return schema;
            }

            //Others
            return BuildComplexTypeSchema(type, schemas, currentDepth, info, representation, nullability);
        }

        private static TypeSchema TryBuildPrimitiveTypeSchema(Type type)
        {
            if (!TypeToAvroPrimitiveSchemaMap.ContainsKey(type))
            {
                return null;
            }
            return TypeToAvroPrimitiveSchemaMap[type](type);
        }

        private static TypeSchema TryBuildLogicalTypeSchema(Type type, MemberInfo info = null)
        {
            if (type == typeof(decimal))
            {
                return BuildDecimalTypeSchema(type, info);
            }

            if (!TypeToAvroLogicalSchemaMap.ContainsKey(type))
            {
                return null;
            }

            return TypeToAvroLogicalSchemaMap[type](type);
        }

        private static TypeSchema BuildDecimalTypeSchema(Type type, MemberInfo info)
        {
            var decimalAttribute = info?.GetCustomAttributes(false).OfType<AvroDecimalAttribute>().FirstOrDefault();
            if (decimalAttribute != null)
            {
                return new DecimalSchema(type, decimalAttribute.Precision, decimalAttribute.Scale);
            }

            return new DecimalSchema(type);
        }

        /// <summary>
        ///     Generates the schema for a complex type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="schemas">The schemas.</param>
        /// <param name="currentDepth">The current depth.</param>
        /// <returns>
        ///     New instance of schema.
        /// </returns>
        /// <exception cref="System.Runtime.Serialization.SerializationException">Thrown when <paramref name="type"/> is not supported.</exception>
        private TypeSchema BuildComplexTypeSchema(Type type, Dictionary<string, NamedSchema> schemas, uint currentDepth, MemberInfo info, AvroTypeRepresentation? representation, NullabilityInfo nullability)
        {
            if (type.IsEnum())
            {
                return BuildEnumTypeSchema(type, schemas);
            }

            if (type.IsArray)
            {
                return BuildArrayTypeSchema(type, schemas, currentDepth, representation, Describes(nullability, type) ? nullability.ElementType : null);
            }

            // Shape known only at runtime: same empty record as `object`, so readers fall back to the writer schema.
            if (type == typeof(ExpandoObject))
            {
                return BuildRecordTypeSchema(typeof(object), schemas, currentDepth);
            }

            // Dictionary
            var dictionaryArguments = type.GetDictionaryArguments();

            if (dictionaryArguments is { } kv && (kv.Key == typeof(string) || kv.Key == typeof(Uri)))
            {
                var valueNullability = GenericArgumentNullability(type, nullability, 1, typeof(IDictionary<,>), typeof(IReadOnlyDictionary<,>));
                return new MapSchema(
                    this.CreateNotNullableSchema(kv.Key, schemas, currentDepth + 1, info, representation),
                    this.CreateSchema(IsNullableReference(kv.Value, valueNullability), kv.Value, schemas, currentDepth + 1, representation, nullability: valueNullability),
                    type);
            }

            // Enumerable
            Type enumerableType = type.FindEnumerableType();
            if (enumerableType != null)
            {
                var itemType = enumerableType.GetGenericArguments()[0];
                var itemNullability = GenericArgumentNullability(type, nullability, 0, typeof(IEnumerable<>));
                return new ArraySchema(this.CreateSchema(IsNullableReference(itemType, itemNullability), itemType, schemas, currentDepth + 1, representation, nullability: itemNullability), type);
            }

            //Nullable
            var nullable = Nullable.GetUnderlyingType(type);
            if (nullable != null)
            {
                return new NullableSchema(
                    type,
                    new Dictionary<string, string>(),
                    CreateSchema(false, nullable, schemas, currentDepth + 1, representation));
            }

            // Others
            if (type.IsClass() || type.IsValueType() || type.IsInterface)
            {
                return BuildRecordTypeSchema(type, schemas, currentDepth);
            }

            throw new SerializationException(
                string.Format(CultureInfo.InvariantCulture, "Type '{0}' is not supported.", type));
        }

        /// <summary>
        ///     Builds the enumeration type schema.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="schemas">The schemas.</param>
        /// <returns>Enum schema.</returns>
        private TypeSchema BuildEnumTypeSchema(Type type, Dictionary<string, NamedSchema> schemas)
        {
            if (type.IsFlagEnum())
            {
                return new LongSchema(type);
            }

            if (schemas.TryGetValue(type.ToString(), out var schema))
            {
                return schema;
            }

            var attributes = this.GetNamedEntityAttributesFrom(type);
            var result = new EnumSchema(attributes, type);

            var symbols = Enum.GetNames(type)
                .Select(x => EnumParser.GetEnumName(type, x, _namingPolicy));

            foreach (var symbol in symbols)
            {
                result.AddSymbol(symbol);
            }

            schemas.Add(type.ToString(), result);
            return result;
        }

        private NamedEntityAttributes GetNamedEntityAttributesFrom(Type type)
        {
            TypeSerializationInfo typeInfo = _resolver.ResolveType(type);
            var name = new SchemaName(typeInfo.Name, typeInfo.Namespace);
            var aliases = typeInfo
                .Aliases
                .Select(alias => string.IsNullOrEmpty(name.Namespace) || alias.Contains(".") ? alias : name.Namespace + "." + alias)
                .ToList();
            return new NamedEntityAttributes(name, aliases, typeInfo.Doc);
        }

        /// <summary>
        ///     Generates the array type schema.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="schemas">The schemas.</param>
        /// <param name="currentDepth">The current depth.</param>
        /// <returns>
        ///     A new instance of schema.
        /// </returns>
        private TypeSchema BuildArrayTypeSchema(Type type, Dictionary<string, NamedSchema> schemas, uint currentDepth, AvroTypeRepresentation? representation, NullabilityInfo elementNullability)
        {
            if (type.GetArrayRank() > 1)
            {
                throw new SerializationException(
                    string.Format(CultureInfo.InvariantCulture, "Multidimensional array '{0}' is not supported; use a jagged array (T[][]) instead.", type));
            }

            Type element = type.GetElementType();
            TypeSchema elementSchema = this.CreateSchema(IsNullableReference(element, elementNullability), element, schemas, currentDepth + 1, representation, nullability: elementNullability);
            return new ArraySchema(elementSchema, type);
        }

        private NullabilityInfo MemberNullability(MemberInfo member)
        {
            // In a generic class the info of Page<string>.Items cannot tell List<T> from List<T?>: .NET reports a type parameter
            // as nullable either way. The member of the open definition (Page<>) shows which positions are type parameters, and
            // those are left as they are (IsNullableReference).
            if (member?.DeclaringType is { IsGenericType: true, IsGenericTypeDefinition: false } declaring)
            {
                member = declaring.GetGenericTypeDefinition()
                    .GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                    .FirstOrDefault(candidate => candidate.MetadataToken == member.MetadataToken && candidate.Module == member.Module);
            }

            return member switch
            {
                PropertyInfo property => _nullability.Create(property),
                FieldInfo field => _nullability.Create(field),
                _ => null
            };
        }

        /// <summary>
        /// Whether the info is of this type: the same type, its Nullable&lt;T&gt;, or – read from the open definition of a generic
        /// class – the same generic type or an array. Not for a member whose type differs from the declared one (object, interfaces).
        /// </summary>
        private static bool Describes(NullabilityInfo nullability, Type type)
        {
            if (nullability == null)
            {
                return false;
            }

            var declared = Nullable.GetUnderlyingType(nullability.Type) ?? nullability.Type;
            if (declared == type)
            {
                return true;
            }

            if (!declared.ContainsGenericParameters || declared.IsGenericParameter)
            {
                return false;
            }

            return declared.IsArray
                ? type.IsArray
                : declared.IsGenericType && type.IsGenericType && declared.GetGenericTypeDefinition() == type.GetGenericTypeDefinition();
        }

        /// <summary>
        /// The nullability of the items (or values) of a generic collection: the annotation of the type argument that the
        /// collection interface takes them from (List&lt;string?&gt; → T of IEnumerable&lt;T&gt;, Dictionary&lt;string, string?&gt; →
        /// TValue of IDictionary&lt;TKey, TValue&gt;, Bag&lt;TMeta, TItem&gt; : IEnumerable&lt;TItem&gt; → TItem). None for a type that
        /// only implements the interface with fixed arguments (class Tags : List&lt;string?&gt;), which has no annotation for them.
        /// </summary>
        private static NullabilityInfo GenericArgumentNullability(Type type, NullabilityInfo nullability, int interfaceArgument, params Type[] interfaces)
        {
            // For a nullable struct (ImmutableArray<string?>?) the info is of Nullable<T>, with the type arguments of T.
            if (!Describes(nullability, type) || !type.IsGenericType)
            {
                return null;
            }

            var definition = type.GetGenericTypeDefinition();
            var parameter = definition.GetInterfaces().Prepend(definition)
                .Where(candidate => candidate.IsGenericType && interfaces.Contains(candidate.GetGenericTypeDefinition()))
                .Select(candidate => candidate.GetGenericArguments()[interfaceArgument])
                .FirstOrDefault(argument => argument.IsGenericParameter);

            return parameter != null && parameter.GenericParameterPosition < nullability.GenericTypeArguments.Length
                ? nullability.GenericTypeArguments[parameter.GenericParameterPosition]
                : null;
        }

        /// <summary>
        /// A reference type annotated as nullable (string?, City?) where nullable reference types are enabled. A type parameter
        /// of a generic class (List&lt;T&gt; in Page&lt;T&gt;) is not: its annotation cannot be told from its absence.
        /// </summary>
        private static bool IsNullableReference(Type type, NullabilityInfo nullability) =>
            nullability != null && !type.IsValueType && !nullability.Type.IsGenericParameter && nullability.ReadState == NullabilityState.Nullable;

        /// <summary>
        /// Generates the record type schema.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="schemas">The schemas.</param>
        /// <param name="currentDepth">The current depth.</param>
        /// <returns>
        /// Instance of schema.
        /// </returns>
        private TypeSchema BuildRecordTypeSchema(Type type, Dictionary<string, NamedSchema> schemas, uint currentDepth)
        {
            if (schemas.TryGetValue(type.ToString(), out var schema))
            {
                return schema;
            }

            NamedEntityAttributes attr = GetNamedEntityAttributesFrom(type);

            var record = new RecordSchema(
                attr,
                type);

            var members = _resolver.ResolveMembers(type);

            // KeyValuePair records are named after their fields, so they are registered after them (they never recurse).
            bool isKeyValuePair = record.Name == "KeyValuePair2";
            if (!isKeyValuePair)
            {
                // Registered before its fields: a member of the same type refers back to this record by name.
                schemas.Add(type.ToString(), record);
                _recordsInProgress.Add(type);
            }

            var fields = BuildRecordFields(members, schemas, currentDepth, record);
            _recordsInProgress.Remove(type);

            //Dictionary
            if (isKeyValuePair)
            {
                // A recursive value type may have built (and registered) this KeyValuePair already.
                if (schemas.TryGetValue(type.ToString(), out var registered))
                {
                    return registered;
                }

                attr = new NamedEntityAttributes(
                    new SchemaName(record.Name, $"{fields.First().TypeSchema.Name}_{fields.Last().TypeSchema.Name}"),
                    record.Aliases,
                    record.Doc);
                record = new RecordSchema(attr, type);
                schemas.Add(type.ToString(), record);
            }

            fields.ForEach(f => record.AddField(f));

            return record;
        }

        private bool IsRecursiveReference(Type memberType) =>
            _recordsInProgress.Contains(Nullable.GetUnderlyingType(memberType) ?? memberType);

        private TypeSchema TryBuildUnionSchema(Type memberType, MemberInfo memberInfo, Dictionary<string, NamedSchema> schemas, uint currentDepth, AvroTypeRepresentation? representation, NamedSchema parentSchema = null)
        {

            var attribute = memberInfo.GetCustomAttributes(false).OfType<AvroUnionAttribute>().FirstOrDefault();
            if (attribute == null)
            {
                return null;
            }

            var result = attribute.TypeAlternatives.ToList();
            if (memberType != typeof(object) && !memberType.IsAbstract() && !memberType.IsInterface())
            {
                result.Add(memberType);
            }
            
            // [AvroType] next to [AvroUnion] is the representation of the branch it applies to (e.g. timestamp-millis of a DateTime),
            // not of every branch: a string next to it stays a string.
            var unionSchemas = result.Select(type => type == typeof(byte[]) && TryBuildFixedSchema(type, memberInfo, parentSchema) is { } fixedBranch
                ? fixedBranch // [AvroFixed] next to [AvroUnion] is the byte[] branch
                : CreateNotNullableSchema(type, schemas, currentDepth + 1, memberInfo, TakesRepresentation(type) ? representation : null)).ToList();

            var typeInfo = _resolver.ResolveType(memberType, memberInfo);

            if (typeInfo.Nullable || result.Any(IsRecursiveReference))
            {
                unionSchemas.Insert(0, new NullSchema());
            }

            return new UnionSchema(unionSchemas, memberType);
        }

        /// <summary>
        /// The default of an enum is its symbol, as the specification requires ([DefaultValue(Color.Red)] gives "Red", not 0;
        /// a number is rejected by parsers that check defaults, e.g. Java and Confluent Schema Registry). A flags enum is a long.
        /// </summary>
        private object SchemaDefault(object defaultValue)
        {
            if (defaultValue is Enum value && !value.GetType().IsFlagEnum() && Enum.GetName(value.GetType(), value) is { } name)
            {
                return EnumParser.GetEnumName(value.GetType(), name, _namingPolicy);
            }

            return defaultValue;
        }

        private static bool TakesRepresentation(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
            return type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(DateOnly) || type == typeof(TimeOnly)
                   || type == typeof(TimeSpan) || type == typeof(Guid) || type == typeof(decimal);
        }

        /// <summary>[AvroFixed] on a nullable byte[] is a union of null and the fixed; null goes last when the default is not null.</summary>
        private TypeSchema TryBuildFixedSchema(Type memberType, MemberInfo memberInfo, NamedSchema parentSchema, bool nullable, bool nonNullDefault)
        {
            var fixedSchema = TryBuildFixedSchema(memberType, memberInfo, parentSchema);
            if (fixedSchema == null || !nullable && !_resolver.ResolveType(memberType, memberInfo).Nullable)
            {
                return fixedSchema;
            }

            return new UnionSchema(nonNullDefault
                ? new List<TypeSchema> { fixedSchema, new NullSchema(memberType) }
                : new List<TypeSchema> { new NullSchema(memberType), fixedSchema }, memberType);
        }

        private FixedSchema TryBuildFixedSchema(Type memberType, MemberInfo memberInfo, NamedSchema parentSchema)
        {
            var result = memberInfo.GetCustomAttributes(false).OfType<AvroFixedAttribute>().FirstOrDefault();
            if (result == null)
            {
                return null;
            }

            if (memberType != typeof(byte[]))
            {
                throw new SerializationException(
                    string.Format(CultureInfo.InvariantCulture, "'{0}' can be set only to members of type byte[].", typeof(AvroFixedAttribute)));
            }

            var schemaNamespace = string.IsNullOrEmpty(result.Namespace) && !result.Name.Contains(".") && parentSchema != null
                                      ? parentSchema.Namespace
                                      : result.Namespace;

            return new FixedSchema(
                new NamedEntityAttributes(new SchemaName(result.Name, schemaNamespace), new List<string>(), string.Empty),
                result.Size,
                memberType);
        }

        public List<RecordFieldSchema> BuildRecordFields(
            IEnumerable<MemberSerializationInfo> members,
            Dictionary<string, NamedSchema> schemas,
            uint currentDepth,
            RecordSchema record)
        {
            int index = 0;
            var result = new List<RecordFieldSchema>();
            bool isKeyValuePair = record.Name == "KeyValuePair2";
            foreach (MemberSerializationInfo info in members)
            {
                var property = info.MemberInfo as PropertyInfo;
                var field = info.MemberInfo as FieldInfo;

                Type memberType;
                if (property != null)
                {
                    memberType = property.PropertyType;
                }
                else if (field != null)
                {
                    memberType = field.FieldType;
                }
                else
                {
                    throw new SerializationException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            $"Type member '{info.MemberInfo.GetType().Name}' is not supported."));
                }

                // A member of a record type that is still being built (a recursive reference) must allow null,
                // otherwise no finite value of the type could be written. KeyValuePair records (dictionaries with
                // non-string keys) are left as they are: an empty dictionary ends the recursion, and their name comes from the value type.
                bool nullable = info.Nullable || !isKeyValuePair && IsRecursiveReference(memberType);

                TypeSchema fieldSchema = TryBuildUnionSchema(memberType, info.MemberInfo, schemas, currentDepth, info.AvroTypeRepresentation, record)
                                         ?? TryBuildFixedSchema(memberType, info.MemberInfo, record, nullable, info.DefaultValue != null)
                                         ?? CreateSchema(nullable, memberType, schemas, currentDepth + 1, info.AvroTypeRepresentation, info.DefaultValue?.GetType(), info.MemberInfo);

                var recordField = new RecordFieldSchema(
                    new NamedEntityAttributes(new SchemaName(info.Name), info.Aliases, info.Doc),
                    fieldSchema,
                    info.HasDefaultValue,
                    SchemaDefault(info.DefaultValue),
                    index++);
                result.Add(recordField);
            }

            return result;
        }
    }
}
