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

using SolTechnology.PerformanceBenchmark.AvroConvertToUpdate.Exceptions;
using SolTechnology.PerformanceBenchmark.AvroConvertToUpdate.Schema;
using SolTechnology.PerformanceBenchmark.AvroConvertToUpdate.Write.Resolvers;

namespace SolTechnology.PerformanceBenchmark.AvroConvertToUpdate.Write
{
    internal static class Resolver
    {
        internal delegate void Writer<in T>(T t);

        private static readonly Array Array;
        private static readonly Map Map;
        private static readonly Null Null;
        private static readonly String String;
        private static readonly Record Record;
        private static readonly Enum Enum;
        private static readonly Fixed Fixed;
        private static readonly Union Union;
        private static readonly Long Long;

        static Resolver()
        {
            Array = new Array();
            Null = new Null();
            Map = new Map();
            String = new String();
            Record = new Record();
            Enum = new Enum();
            Fixed = new Fixed();
            Union = new Union();
            Long = new Long();
        }

        internal static Encoder.WriteItem ResolveWriter(Schema.Schema schema)
        {
            switch (schema.Tag)
            {
                case Schema.Schema.Type.Null:
                    return Null.Resolve;
                case Schema.Schema.Type.Boolean:
                    return (v, e) => Write<bool>(v, schema.Tag, e.WriteBoolean);
                case Schema.Schema.Type.Int:
                    return (v, e) => Write<int>(v, schema.Tag, e.WriteInt);
                case Schema.Schema.Type.Long:
                    return Long.Resolve;
                case Schema.Schema.Type.Float:
                    return (v, e) => Write<float>(v, schema.Tag, e.WriteFloat);
                case Schema.Schema.Type.Double:
                    return (v, e) => Write<double>(v, schema.Tag, e.WriteDouble);
                case Schema.Schema.Type.String:
                    return String.Resolve;
                case Schema.Schema.Type.Bytes:
                    return (v, e) => Write<byte[]>(v, schema.Tag, e.WriteBytes);
                case Schema.Schema.Type.Error:
                case Schema.Schema.Type.Record:
                    return Record.Resolve((RecordSchema)schema);
                case Schema.Schema.Type.Enumeration:
                    return Enum.Resolve((EnumSchema)schema);
                case Schema.Schema.Type.Fixed:
                    return Fixed.Resolve((FixedSchema)schema);
                case Schema.Schema.Type.Array:
                    return Array.Resolve((ArraySchema)schema);
                case Schema.Schema.Type.Map:
                    return Map.Resolve((MapSchema)schema);
                case Schema.Schema.Type.Union:
                    return Union.Resolve((UnionSchema)schema);
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
        private static void Write<S>(object value, Schema.Schema.Type tag, Writer<S> writer)
        {
            if (value == null)
            {
                value = default(S);
            }

            if (!(value is S))
                throw new AvroTypeMismatchException(
                    $"[{typeof(S)}] required to write against [{tag.ToString()}] schema but found " + value.GetType());

            writer((S)value);
        }
    }
}
