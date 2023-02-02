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
using System.IO;
using SolTechnology.Avro.AvroObjectServices.BuildSchema;
using SolTechnology.Avro.AvroObjectServices.Read;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;

namespace SolTechnology.Avro
{
    public static partial class AvroConvert
    {
        /// <summary>
        /// Deserializes Avro object, which does not contain header, to .NET type
        /// </summary>
        public static T DeserializeHeadless<T>(byte[] avroBytes, string schema)
        {
            return DeserializeHeadless<T>(avroBytes, Schema.Create(schema), BuildSchema(typeof(T)), 1);
        }

        /// <summary>
        /// Deserializes Avro array object, which does not contain header, to .NET type
        /// </summary>
        public static T DeserializeHeadless<T>(byte[] avroBytes, string schema, int numberOfRows)
        {
            return DeserializeHeadless<T>(avroBytes, Schema.Create(schema), BuildSchema(typeof(T)), numberOfRows);
        }

        /// <summary>
        /// Deserializes Avro array object, which does not contain header, to .NET type
        /// </summary>
        public static T DeserializeHeadless<T>(byte[] avroBytes)
        {
            return DeserializeHeadless<T>(avroBytes, BuildSchema(typeof(T)), BuildSchema(typeof(T)), 1);
        }

        /// <summary>
        /// Deserializes Avro array object, which does not contain header, to .NET type
        /// </summary>
        public static dynamic DeserializeHeadless(byte[] avroBytes, Type targetType)
        {
            object result = typeof(AvroConvert)
                .GetMethod(nameof(DeserializeHeadless), new[] { typeof(byte[]) })
                ?.MakeGenericMethod(targetType)
                .Invoke(null, new object[] { avroBytes });

            return result;
        }

        /// <summary>
        /// Deserializes Avro array object, which does not contain header, to .NET type
        /// </summary>
        public static dynamic DeserializeHeadless(byte[] avroBytes, string schema, Type targetType)
        {
            object result = typeof(AvroConvert)
                .GetMethod(nameof(DeserializeHeadless), new[] { typeof(byte[]), typeof(string) })
                ?.MakeGenericMethod(targetType)
                .Invoke(null, new object[] { avroBytes, schema });

            return result;
        }

        private static T DeserializeHeadless<T>(byte[] avroBytes, TypeSchema writeSchema, TypeSchema readSchema, int numberOfRows)
        {
            var reader = new Reader(new MemoryStream(avroBytes));
            var resolver = new Resolver(writeSchema, readSchema);
            var result = resolver.Resolve<T>(reader, numberOfRows);

            return result;
        }

        private static TypeSchema BuildSchema(Type type)
        {
            var schemaBuilder = new ReflectionSchemaBuilder(new AvroSerializerSettings());
            return schemaBuilder.BuildSchema(type);
        }
    }
}
