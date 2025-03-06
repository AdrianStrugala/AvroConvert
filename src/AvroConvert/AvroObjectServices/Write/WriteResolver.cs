#region license
/**Copyright (c) 2020 Adrian Strugała
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* https://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using SolTechnology.Avro.AvroObjectServices.Schemas;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;
using SolTechnology.Avro.Features.Serialize;
using SolTechnology.Avro.Infrastructure.Exceptions;
using SolTechnology.Avro.Policies;


namespace SolTechnology.Avro.AvroObjectServices.Write
{
    internal partial class WriteResolver
    {
        internal delegate void Writer<in T>(T t);

        private static bool _hasCustomConverters;
        private readonly Dictionary<Type, Action<object, IWriter>> _customSerializerMapping;
        private readonly IAvroNamingPolicy _namingPolicy;

        internal WriteResolver(AvroConvertOptions options = null)
        {
            _hasCustomConverters = (options?.AvroConverters.Any()).GetValueOrDefault();
            _customSerializerMapping = options?.AvroConverters.ToDictionary(
                x => x.TypeSchema.RuntimeType,
                y => (Action<object, IWriter>)y.Serialize);
            _namingPolicy = options?.NamingPolicy;
        }

        internal Encoder.WriteItem ResolveWriter(TypeSchema schema)
        {
            if (_hasCustomConverters)
            {
                if (_customSerializerMapping.TryGetValue(schema.RuntimeType, out var serializer))
                {
                    return (v, e) => serializer(v, e);
                }
            }

            switch (schema.Type)
            {
                case AvroType.Null:
                    return ResolveNull;
                case AvroType.Boolean:
                    return ResolveBool;
                case AvroType.Int:
                    return ResolveInt;
                case AvroType.Long:
                    return ResolveLong;
                case AvroType.Float:
                    return (v, e) => Write<float>(v, schema.Type, e.WriteFloat);
                case AvroType.Double:
                    return (v, e) => Write<double>(v, schema.Type, e.WriteDouble);
                case AvroType.String:
                    return ResolveString;
                case AvroType.Bytes:
                    return (v, e) => Write<byte[]>(v, schema.Type, e.WriteBytes);
                case AvroType.Error:
                case AvroType.Logical:
                    {
                        var logicalTypeSchema = (LogicalTypeSchema)schema;
                        switch (logicalTypeSchema.LogicalTypeName)
                        {
                            case LogicalTypeSchema.LogicalTypeEnum.Uuid:
                                return ResolveUuid((UuidSchema)logicalTypeSchema);
                            case LogicalTypeSchema.LogicalTypeEnum.Decimal:
                                return (v, e) => ResolveDecimal((DecimalSchema)logicalTypeSchema, v, e);
                            case LogicalTypeSchema.LogicalTypeEnum.TimestampMilliseconds:
                                return (v, e) => ResolveTimestampMilliseconds((TimestampMillisecondsSchema)logicalTypeSchema, v, e);
                            case LogicalTypeSchema.LogicalTypeEnum.TimestampMicroseconds:
                                return (v, e) => ResolveTimestampMicroseconds((TimestampMicrosecondsSchema)logicalTypeSchema, v, e);
                            case LogicalTypeSchema.LogicalTypeEnum.Duration:
                                return (v, e) => ResolveDuration((DurationSchema)logicalTypeSchema, v, e);
                            case LogicalTypeSchema.LogicalTypeEnum.Date:
                                return ResolveDate();
                            case LogicalTypeSchema.LogicalTypeEnum.TimeMilliseconds:
                                return ResolveTimeMilliseconds((TimeMillisecondsSchema)schema);
                            case LogicalTypeSchema.LogicalTypeEnum.TimeMicrosecond:
                                return ResolveTimeMicroseconds((TimeMicrosecondsSchema)schema);
                        }
                    }
                    return ResolveString;
                case AvroType.Record:
                    if (schema.RuntimeType == typeof(JObject))
                    {
                        return ResolveJson((RecordSchema)schema);
                    }
                    return ResolveRecord((RecordSchema)schema);
                case AvroType.Enum:
                    return ResolveEnum((EnumSchema)schema);
                case AvroType.Fixed:
                    return ResolveFixed((FixedSchema)schema);
                case AvroType.Array:
                    return ResolveArray((ArraySchema)schema);
                case AvroType.Map:
                    return ResolveMap((MapSchema)schema);
                case AvroType.Union:
                    return ResolveUnion((UnionSchema)schema);
                default:
                    return (v, e) =>
                        throw new AvroTypeMismatchException(
                            $"Tried to write against [{schema}] schema, but found [{v.GetType()}] type");
            }
        }

        /// <summary>
        /// A generic method to serialize primitive Avro types.
        /// </summary>
        /// <typeparam name="S">Type of the C# type to be serialized</typeparam>
        /// <param name="value">The value to be serialized</param>
        /// <param name="tag">The schema type tag</param>
        /// <param name="writer">The writer which should be used to write the given type.</param>
        private static void Write<S>(object value, AvroType tag, Writer<S> writer)
        {
            value ??= default(S);

            if (value is not S convertedValue)
            {
                try
                {
                    convertedValue = (S)Convert.ChangeType(value, typeof(S));
                }
                catch
                {
                    throw new AvroTypeMismatchException(
                        $"[{typeof(S)}] required to write against [{tag}] schema but found type: [{value?.GetType()}]");
                }
            }

            writer(convertedValue);
        }
    }
}
