#region license
/**Copyright (c) 2020 Adrian Struga³a
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
using SolTechnology.Avro.BuildSchema.SchemaModels;
using SolTechnology.Avro.BuildSchema.SchemaModels.Abstract;
using SolTechnology.Avro.Exceptions;
using SolTechnology.Avro.Skip;

namespace SolTechnology.Avro.Read
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
            IReader d,
            Type type)
        {
            try
            {
                if (readerSchema.Type == Schema.Schema.Type.Union && writerSchema.Type != Schema.Schema.Type.Union)
                {
                    readerSchema = FindBranch(readerSchema as UnionSchema, writerSchema);
                }

                switch (writerSchema.Type)
                {
                    case Schema.Schema.Type.Null:
                        return null;
                    case Schema.Schema.Type.Boolean:
                        return d.ReadBoolean();
                    case Schema.Schema.Type.Int:
                        return d.ReadInt();
                    case Schema.Schema.Type.Long:
                        return ResolveLong(type, d);
                    case Schema.Schema.Type.Float:
                        return d.ReadFloat();
                    case Schema.Schema.Type.Double:
                        return d.ReadDouble();
                    case Schema.Schema.Type.String:
                        return ResolveString(type, d);
                    case Schema.Schema.Type.Bytes:
                        return d.ReadBytes();
                    case Schema.Schema.Type.Error:
                    case Schema.Schema.Type.Record:
                        return ResolveRecord((RecordSchema)writerSchema, (RecordSchema)readerSchema, d, type);
                    case Schema.Schema.Type.Enum:
                        return ResolveEnum((EnumSchema)writerSchema, readerSchema, d, type);
                    case Schema.Schema.Type.Fixed:
                        return ResolveFixed((FixedSchema)writerSchema, readerSchema, d, type);
                    case Schema.Schema.Type.Array:
                        return ResolveArray(writerSchema, readerSchema, d, type);
                    case Schema.Schema.Type.Map:
                        return ResolveMap((MapSchema)writerSchema, readerSchema, d, type);
                    case Schema.Schema.Type.Union:
                        return ResolveUnion((UnionSchema)writerSchema, readerSchema, d, type);
                    default:
                        throw new AvroException("Unknown schema type: " + writerSchema);
                }
            }
            catch (Exception e)
            {
                throw new AvroTypeMismatchException($"Unable to deserialize [{writerSchema.Name}] of schema [{writerSchema.Type}] to the target type [{type}]", e);
            }
        }
    }
}