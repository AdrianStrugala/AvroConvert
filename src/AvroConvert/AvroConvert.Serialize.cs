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
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;
using SolTechnology.Avro.Features.Serialize;

namespace SolTechnology.Avro
{
    public static partial class AvroConvert
    {
        private static byte[] SerializeInternal(object obj, TypeSchema schema, CodecType codecType, AvroConvertOptions options = null)
        {
            using MemoryStream resultStream = new MemoryStream();
            using (var writer = options != null 
                       ? new Encoder(schema, resultStream, options.Codec, options)
                       : new Encoder(schema, resultStream, codecType))
            {
                writer.Append(obj);
            }
            return resultStream.ToArray();
        }

        /// <summary>
        /// Serializes the given object into Avro format (including header with metadata) using a default codec.
        /// </summary>
        /// <param name="obj">The object to serialize.</param>
        /// <returns>A byte array containing the serialized Avro data.</returns>
        public static byte[] Serialize(object obj)
        {
            return Serialize(obj, CodecType.Null);
        }
        
        /// <summary>
        /// Serializes the given object into Avro format (including header with metadata) using the specified codec type.
        /// </summary>
        /// <param name="obj">The object to serialize.</param>
        /// <param name="codecType">The codec type to be used for serialization. Choosing a specific codec may reduce the output size.</param>
        /// <returns>A byte array containing the serialized Avro data.</returns>
        public static byte[] Serialize(object obj, CodecType codecType)
        {
            var schema = Schema.Create(obj);
            return SerializeInternal(obj, schema, codecType);
        }
        

        /// <summary>
        /// Serializes the given object into Avro format (including header with metadata) using the specified Avro conversion options.
        /// </summary>
        /// <param name="obj">The object to serialize.</param>
        /// <param name="options">The Avro conversion options that control the serialization process, including codec and other settings.</param>
        /// <returns>A byte array containing the serialized Avro data.</returns>
        public static byte[] Serialize(object obj, AvroConvertOptions options)
        {
            var schema = Schema.Create(obj, options);
            return SerializeInternal(obj, schema, options.Codec, options);
        }
    }
}
