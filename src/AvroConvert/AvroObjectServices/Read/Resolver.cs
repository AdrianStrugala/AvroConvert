#region license
/**Copyright (c) 2020 Adrian Struga�a
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
using SolTechnology.Avro.AvroObjectServices.Schemas;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;
using SolTechnology.Avro.AvroObjectServices.Skip;
using SolTechnology.Avro.Infrastructure.Exceptions;

namespace SolTechnology.Avro.AvroObjectServices.Read
{
    internal partial class Resolver
    {
        private readonly Skipper _skipper;
        private readonly TypeSchema _readerSchema;
        private readonly TypeSchema _writerSchema;

        internal Resolver(TypeSchema writerSchema, TypeSchema readerSchema)
        {
            _readerSchema = readerSchema;
            _writerSchema = writerSchema;

            _skipper = new Skipper();
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
                if (readerSchema.Type == AvroType.Union && writerSchema.Type != AvroType.Union)
                {
                    readerSchema = FindBranch(readerSchema as UnionSchema, writerSchema);
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
                        return ResolveLogical((LogicalTypeSchema)writerSchema, readerSchema, reader, type);
                    case AvroType.Error:
                    case AvroType.Record:
                        return ResolveRecord((RecordSchema)writerSchema, (RecordSchema)readerSchema, reader, type);
                    case AvroType.Enum:
                        return ResolveEnum((EnumSchema)writerSchema, readerSchema, reader, type);
                    case AvroType.Fixed:
                        return ResolveFixed((FixedSchema)writerSchema, readerSchema, reader, type);
                    case AvroType.Array:
                        return ResolveArray(writerSchema, readerSchema, reader, type);
                    case AvroType.Map:
                        return ResolveMap((MapSchema)writerSchema, readerSchema, reader, type);
                    case AvroType.Union:
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
    }
}