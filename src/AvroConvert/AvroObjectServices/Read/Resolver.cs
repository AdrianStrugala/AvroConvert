#region license
/**Copyright (c) 2020 Adrian Strugala
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
using SolTechnology.Avro.AvroObjectServices.Schemas;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;
using SolTechnology.Avro.AvroObjectServices.Skip;
using SolTechnology.Avro.AvroObjectServices.Write;
using SolTechnology.Avro.Infrastructure.Exceptions;

namespace SolTechnology.Avro.AvroObjectServices.Read
{
    internal partial class Resolver
    {
        private readonly Skipper _skipper;
        private readonly TypeSchema _readerSchema;
        private readonly AvroConvertOptions _options;
        private readonly TypeSchema _writerSchema;
        private readonly bool _hasCustomConverters;
        private readonly Dictionary<Type, Func<Type, IReader, object>> _customDeserializerMapping;

        internal Resolver(TypeSchema writerSchema, TypeSchema readerSchema, AvroConvertOptions options = null)
        {
            _readerSchema = readerSchema;
            _writerSchema = writerSchema;

            _skipper = new Skipper();

            _hasCustomConverters = (options?.AvroConverters.Any()).GetValueOrDefault();
            _customDeserializerMapping = options?.AvroConverters.ToDictionary(
                x => x.TypeSchema.RuntimeType,
                y => (Func<Type, IReader, object>)y.Deserialize);
        }

        internal T Resolve<T>(IReader reader, long itemsCount = 0)
        {
            if (itemsCount > 1)
            {
                return (T)ResolveArray(
                        _writerSchema,
                        _readerSchema,
                        reader, typeof(T), itemsCount);
            }

            var result = Resolve(_writerSchema, _readerSchema, reader, typeof(T));
            return (T)result;
        }

        internal object Resolve(
            TypeSchema writerSchema,
            TypeSchema readerSchema,
            IReader reader,
            Type type)
        {
            try
            {
                if (_hasCustomConverters)
                {
                    if (_customDeserializerMapping.TryGetValue(type, out var deserializer))
                    {
                        return deserializer(type, reader);
                    }
                }

                switch (writerSchema.Type)
                {
                    case AvroType.Null:
                        return null;
                    case AvroType.Boolean:
                        return reader.ReadBoolean();
                    case AvroType.Int:
                        return ResolveInt(type, reader);
                    case AvroType.Long:
                        return ResolveLong(type, reader);
                    case AvroType.Float:
                        return ResolveFloat(type, reader);
                    case AvroType.Double:
                        return ResolveDouble(type, reader);
                    case AvroType.String:
                        return ResolveString(type, reader);
                    case AvroType.Bytes:
                        return reader.ReadBytes();
                    case AvroType.Logical:
                        readerSchema = FindBranchReaderSchema(writerSchema, readerSchema);
                        return ResolveLogical((LogicalTypeSchema)writerSchema, readerSchema, reader, type);
                    case AvroType.Error:
                    case AvroType.Record:
                        readerSchema = FindBranchReaderSchema(writerSchema, readerSchema);
                        return ResolveRecord((RecordSchema)writerSchema, (RecordSchema)readerSchema, reader, type);
                    case AvroType.Enum:
                        readerSchema = FindBranchReaderSchema(writerSchema, readerSchema);
                        return ResolveEnum((EnumSchema)writerSchema, readerSchema, reader, type);
                    case AvroType.Fixed:
                        readerSchema = FindBranchReaderSchema(writerSchema, readerSchema);
                        return ResolveFixed((FixedSchema)writerSchema, readerSchema, reader, type);
                    case AvroType.Array:
                        readerSchema = FindBranchReaderSchema(writerSchema, readerSchema);
                        return ResolveArray(writerSchema, readerSchema, reader, type);
                    case AvroType.Map:
                        readerSchema = FindBranchReaderSchema(writerSchema, readerSchema);
                        return ResolveMap((MapSchema)writerSchema, readerSchema, reader, type);
                    case AvroType.Union:
                        readerSchema = FindBranchReaderSchema(writerSchema, readerSchema);
                        return ResolveUnion((UnionSchema)writerSchema, readerSchema, reader, type);
                    default:
                        throw new AvroException("Unknown schema type: " + writerSchema);
                }
            }
            catch (Exception e)
            {
                throw new AvroTypeMismatchException($"Unable to deserialize [{writerSchema.Name}] of schema [{writerSchema.Type}] to the target type [{type}]. Inner exception:", e);
            }
        }

        private TypeSchema FindBranchReaderSchema(TypeSchema writerSchema, TypeSchema readerSchema)
        {
            if (readerSchema.Type == AvroType.Union && writerSchema.Type != AvroType.Union)
            {
                readerSchema = FindBranch(readerSchema as UnionSchema, writerSchema);
            }

            return readerSchema;
        }
    }
}
