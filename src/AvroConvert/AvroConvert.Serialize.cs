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

using System.IO;
using SolTechnology.Avro.AvroObjectServices.BuildSchema;
using SolTechnology.Avro.Features.Serialize;

namespace SolTechnology.Avro
{
    public static partial class AvroConvert
    {
        /// <summary>
        /// Serializes given object into Avro format (including header with metadata)
        /// </summary>
        public static byte[] Serialize(object obj)
        {
            return Serialize(obj, CodecType.Null);
        }
        

        /// <summary>
        /// Serializes given object into Avro format (including header with metadata)
        /// Choosing <paramref name="codecType"/> reduces output object size
        /// </summary>
        public static byte[] Serialize(object obj, CodecType codecType)
        {
            using (MemoryStream resultStream = new MemoryStream())
            {
                var schema = Schema.Create(obj);
                using (var writer = new Encoder(schema, resultStream, codecType))
                {
                    writer.Append(obj);
                }
                byte[] result = resultStream.ToArray();
                return result;
            }
        }
        
        public static byte[] Serialize(object obj, string schema)
        {
            using MemoryStream resultStream = new MemoryStream();
            var schemaObject = Schema.Parse(schema);
            using (var writer = new Encoder(schemaObject, resultStream, CodecType.Null))
            {
                writer.Append(obj);
            }
            byte[] result = resultStream.ToArray();
            return result;
        }

        /// <summary>
        /// Serializes given object into Avro format (including header with metadata) and returns the serialized data as a byte array.
        /// </summary>
        /// <param name="obj">The object to be serialized into Avro format.</param>
        /// <param name="options">The Avro conversion options that control the serialization process.</param>
        /// <returns>A byte array containing the serialized Avro data.</returns>
        /// <remarks>
        /// This method takes an object and serializes it into Avro format based on the provided Avro conversion options.
        /// The resulting serialized data is returned as a byte array.
        /// </remarks>
        public static byte[] Serialize(object obj, AvroConvertOptions options)
        {
            using (MemoryStream resultStream = new MemoryStream())
            {
                var schema = Schema.Create(obj, options);
                using (var writer = new Encoder(schema, resultStream, options.Codec, options))
                {
                    writer.Append(obj);
                }
                byte[] result = resultStream.ToArray();
                return result;
            }
        }
    }
}