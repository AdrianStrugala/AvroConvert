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

using SolTechnology.Avro.AvroObjectServices.Read;
using SolTechnology.Avro.AvroObjectServices.Schemas;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;
using SolTechnology.Avro.Infrastructure.Exceptions;

namespace SolTechnology.Avro.AvroObjectServices.Skip
{
    internal class Skipper
    {
        internal void Skip(TypeSchema schema, IReader d)
        {
            switch (schema.Type)
            {
                case AvroType.Null:
                    d.SkipNull();
                    break;
                case AvroType.Boolean:
                    d.SkipBoolean();
                    break;
                case AvroType.Int:
                    d.SkipInt();
                    break;
                case AvroType.Long:
                    d.SkipLong();
                    break;
                case AvroType.Float:
                    d.SkipFloat();
                    break;
                case AvroType.Double:
                    d.SkipDouble();
                    break;
                case AvroType.String:
                    d.SkipString();
                    break;
                case AvroType.Bytes:
                    d.SkipBytes();
                    break;
                case AvroType.Record:
                    foreach (var field in ((RecordSchema)schema).Fields)
                    {
                        Skip(field.TypeSchema, d);
                    }
                    break;
                case AvroType.Enum:
                    d.SkipEnum();
                    break;
                case AvroType.Fixed:
                    d.SkipFixed(((FixedSchema)schema).Size);
                    break;
                case AvroType.Array:
                    {
                        TypeSchema s = ((ArraySchema)schema).ItemSchema;
                        for (long n = d.ReadArrayStart(); n != 0; n = d.ReadArrayNext())
                        {
                            for (long i = 0; i < n; i++) Skip(s, d);
                        }
                    }
                    break;
                case AvroType.Map:
                    {
                        TypeSchema s = ((MapSchema)schema).ValueSchema;
                        for (long n = d.ReadMapStart(); n != 0; n = d.ReadMapNext())
                        {
                            for (long i = 0; i < n; i++) { d.SkipString(); Skip(s, d); }
                        }
                    }
                    break;
                case AvroType.Union:
                    Skip(((UnionSchema)schema).Schemas[d.ReadUnionIndex()], d);
                    break;
                case AvroType.Logical:
                    Skip(((LogicalTypeSchema) schema).BaseTypeSchema, d);
                    break;
                case AvroType.Error:
                    break;
                default:
                    throw new AvroException("Unknown schema type: " + schema);
            }
        }
    }
}
