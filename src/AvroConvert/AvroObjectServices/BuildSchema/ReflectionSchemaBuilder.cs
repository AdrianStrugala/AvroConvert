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
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using SolTechnology.Avro.AvroObjectServices.Schemas;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;
using SolTechnology.Avro.Infrastructure.Attributes;
using SolTechnology.Avro.Infrastructure.Extensions;

namespace SolTechnology.Avro.AvroObjectServices.BuildSchema
{
    /// <summary>
    ///     This class creates an avro schema given a c# type.
    /// </summary>

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
            };

        private readonly AvroConvertOptions _options;
        private readonly bool _hasCustomConverters;
        private readonly Dictionary<Type, TypeSchema> _customSchemaMapping;
        private readonly AvroContractResolver _resolver;


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
                includeOnlyDataContractMembers: options.IncludeOnlyDataContractMembers);
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
            return type == null ? new NullSchema() : CreateSchema(false, type, new Dictionary<string, NamedSchema>(), 0);
        }

        private TypeSchema CreateSchema(bool forceNullable,
            Type type,
            Dictionary<string, NamedSchema> schemas,
            uint currentDepth,
            Type prioritizedType = null,
            MemberInfo memberInfo = null)
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

            return typeInfo.Nullable || forceNullable
                ? this.CreateNullableSchema(type, schemas, currentDepth, prioritizedType, memberInfo)
                : this.CreateNotNullableSchema(type, schemas, currentDepth, memberInfo);
        }

        private TypeSchema CreateNullableSchema(Type type, Dictionary<string, NamedSchema> schemas, uint currentDepth, Type prioritizedType, MemberInfo info)
        {
            var typeSchemas = new List<TypeSchema> { new NullSchema(type) };
            var notNullableType = type;
            var underlyingType = Nullable.GetUnderlyingType(type);
            if (underlyingType != null)
            {
                notNullableType = underlyingType;
            }

            var notNullableSchema = this.CreateNotNullableSchema(notNullableType, schemas, currentDepth, info);
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
        private TypeSchema CreateNotNullableSchema(Type type, Dictionary<string, NamedSchema> schemas, uint currentDepth, MemberInfo info)
        {
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
            return BuildComplexTypeSchema(type, schemas, currentDepth, info);
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
        private TypeSchema BuildComplexTypeSchema(Type type, Dictionary<string, NamedSchema> schemas, uint currentDepth, MemberInfo info)
        {
            if (type.IsEnum())
            {
                return BuildEnumTypeSchema(type, schemas);
            }

            if (type.IsArray)
            {
                return BuildArrayTypeSchema(type, schemas, currentDepth);
            }

            // Dictionary
            Type dictionaryType = type
                .GetAllInterfaces()
                .SingleOrDefault(t => t.IsGenericType() && t.GetGenericTypeDefinition() == typeof(IDictionary<,>));

            if (dictionaryType != null
                && (dictionaryType.GetGenericArguments()[0] == typeof(string)
                 || dictionaryType.GetGenericArguments()[0] == typeof(Uri)))
            {
                return new MapSchema(
                    this.CreateNotNullableSchema(dictionaryType.GetGenericArguments()[0], schemas, currentDepth + 1, info),
                    this.CreateSchema(false, dictionaryType.GetGenericArguments()[1], schemas, currentDepth + 1),
                    type);
            }

            // Enumerable
            Type enumerableType = type.FindEnumerableType();
            if (enumerableType != null)
            {
                var itemType = enumerableType.GetGenericArguments()[0];
                return new ArraySchema(this.CreateSchema(false, itemType, schemas, currentDepth + 1), type);
            }

            //Nullable
            var nullable = Nullable.GetUnderlyingType(type);
            if (nullable != null)
            {
                return new NullableSchema(
                    type,
                    new Dictionary<string, string>(),
                    CreateSchema(false, nullable, schemas, currentDepth + 1));
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
        private TypeSchema BuildArrayTypeSchema(Type type, Dictionary<string, NamedSchema> schemas, uint currentDepth)
        {
            Type element = type.GetElementType();
            TypeSchema elementSchema = this.CreateSchema(false, element, schemas, currentDepth + 1);
            return new ArraySchema(elementSchema, type);
        }

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

            var attr = GetNamedEntityAttributesFrom(type);

            var record = new RecordSchema(
                attr,
                type);
            schemas.Add(type.ToString(), record);

            var members = _resolver.ResolveMembers(type);
            AddRecordFields(members, schemas, currentDepth, record);

            //Dictionary
            if (record.Name == "KeyValuePair2")
            {
                record.Namespace == $"{record.Fields.First().Name}_{record.Fields.Last().Name}";
            }

            return record;
        }

        private TypeSchema TryBuildUnionSchema(Type memberType, MemberInfo memberInfo, Dictionary<string, NamedSchema> schemas, uint currentDepth)
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


            return new UnionSchema(result.Select(type => CreateNotNullableSchema(type, schemas, currentDepth + 1, memberInfo)).ToList(), memberType);
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

        public void AddRecordFields(
            IEnumerable<MemberSerializationInfo> members,
            Dictionary<string, NamedSchema> schemas,
            uint currentDepth,
            RecordSchema record)
        {
            int index = 0;
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

                TypeSchema fieldSchema;

                if (info.AvroTypeRepresentation.HasValue)
                {
                    fieldSchema = AvroRepresentationToAvroSchemaMap[info.AvroTypeRepresentation.Value].Invoke(memberType);
                }
                else
                {
                    fieldSchema = TryBuildUnionSchema(memberType, info.MemberInfo, schemas, currentDepth)
                                  ?? TryBuildFixedSchema(memberType, info.MemberInfo, record)
                                  ?? CreateSchema(info.Nullable, memberType, schemas, currentDepth + 1, info.DefaultValue?.GetType(), info.MemberInfo);
                }

                var recordField = new RecordFieldSchema(
                    new NamedEntityAttributes(new SchemaName(info.Name), info.Aliases, info.Doc),
                    fieldSchema,
                    info.HasDefaultValue,
                    info.DefaultValue,
                    index++);
                record.AddField(recordField);
            }
        }
    }
}
