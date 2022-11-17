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

using System.IO;
using SolTechnology.Avro.AvroObjectServices.BuildSchema;
using SolTechnology.Avro.AvroObjectServices.Read;

namespace SolTechnology.Avro
{
    public static partial class AvroConvert
    {
        /// <summary>
        /// Deserializes Avro object, which does not contain header, to .NET type
        /// </summary>
        public static T DeserializeHeadless<T>(byte[] avroBytes, string schema)
        {
            return DeserializeHeadless<T>(avroBytes, schema, 1);
        }

        /// <summary>
        /// Deserializes Avro array object, which does not contain header, to .NET type
        /// </summary>
        public static T DeserializeHeadless<T>(byte[] avroBytes, string schema, int numberOfRows)
        {
            var schemaBuilder = new ReflectionSchemaBuilder(new AvroSerializerSettings());
            var readSchema = schemaBuilder.BuildSchema(typeof(T));

            var reader = new Reader(new MemoryStream(avroBytes));
            var resolver = new Resolver(Schema.Create(schema), readSchema);
            var result = resolver.Resolve<T>(reader, numberOfRows);

            return result;
        }
    }
}
