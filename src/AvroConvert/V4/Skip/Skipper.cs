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

using SolTechnology.Avro.V4.Exceptions;
using SolTechnology.Avro.V4.Read;
using SolTechnology.Avro.Schema;

namespace SolTechnology.Avro.V4.Skip
{
    internal class Skipper
    {
        internal void Skip(Schema.Schema schema, IReader d)
        {
            switch (schema.Tag)
            {
                case Schema.Schema.Type.Null:
                    d.SkipNull();
                    break;
                case Schema.Schema.Type.Boolean:
                    d.SkipBoolean();
                    break;
                case Schema.Schema.Type.Int:
                    d.SkipInt();
                    break;
                case Schema.Schema.Type.Long:
                    d.SkipLong();
                    break;
                case Schema.Schema.Type.Float:
                    d.SkipFloat();
                    break;
                case Schema.Schema.Type.Double:
                    d.SkipDouble();
                    break;
                case Schema.Schema.Type.String:
                    d.SkipString();
                    break;
                case Schema.Schema.Type.Bytes:
                    d.SkipBytes();
                    break;
                case Schema.Schema.Type.Record:
                    foreach (Field f in (RecordSchema)schema) Skip(f.Schema, d);
                    break;
                case Schema.Schema.Type.Enumeration:
                    d.SkipEnum();
                    break;
                case Schema.Schema.Type.Fixed:
                    d.SkipFixed(((FixedSchema)schema).Size);
                    break;
                case Schema.Schema.Type.Array:
                    {
                        Schema.Schema s = ((ArraySchema)schema).ItemSchema;
                        for (long n = d.ReadArrayStart(); n != 0; n = d.ReadArrayNext())
                        {
                            for (long i = 0; i < n; i++) Skip(s, d);
                        }
                    }
                    break;
                case Schema.Schema.Type.Map:
                    {
                        Schema.Schema s = ((MapSchema)schema).ValueSchema;
                        for (long n = d.ReadMapStart(); n != 0; n = d.ReadMapNext())
                        {
                            for (long i = 0; i < n; i++) { d.SkipString(); Skip(s, d); }
                        }
                    }
                    break;
                case Schema.Schema.Type.Union:
                    Skip(((UnionSchema)schema)[d.ReadUnionIndex()], d);
                    break;
                case Schema.Schema.Type.Error:
                    break;
                default:
                    throw new AvroException("Unknown schema type: " + schema);
            }
        }
    }
}
