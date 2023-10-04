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

using System.Collections.Generic;
using System.IO;
using SolTechnology.Avro.AvroObjectServices.BuildSchema;
using SolTechnology.Avro.Features.Serialize;

namespace SolTechnology.Avro
{
    public static partial class AvroConvert
    {
        /// <summary>
        /// Serializes the given <see cref="IEnumerable{T}"/> into Avro container format (including header with metadata)
        /// </summary>
        public static byte[] SerializeContainer<T>(IEnumerable<T> items)
        {
            return SerializeContainer(items, CodecType.Null);
        }
        
        /// <summary>
        /// Serializes the given <see cref="IEnumerable{T}"/> into Avro container format (including header with metadata)
        /// Choosing <paramref name="codecType"/> reduces output object size
        /// </summary>
        public static byte[] SerializeContainer<T>(IEnumerable<T> items, CodecType codecType)
            where T : notnull
        {
            using (MemoryStream resultStream = new MemoryStream())
            {
                var schema = Schema.Create(typeof(T));

                using (var writer = new Encoder(schema, resultStream, codecType))
                {
                    foreach (var item in items)
                    {
                        writer.Append(item);
                    }
                }

                byte[] result = resultStream.ToArray();
                return result;
            }
        }

        /// <summary>
        /// Serializes the given <see cref="IEnumerable{T}"/> into Avro container format (including header with metadata) and returns the serialized data as a byte array.
        /// </summary>
        /// <param name="items">The items to be serialized into Avro container format.</param>
        /// <param name="options">The Avro conversion options that control the serialization process.</param>
        /// <returns>A byte array containing the serialized Avro data.</returns>
        /// <remarks>
        /// This method takes an <see cref="IEnumerable{T}"/> and serializes it into Avro container format based on the provided Avro conversion options.
        /// The resulting serialized data is returned as a byte array.
        /// </remarks>
        public static byte[] SerializeContainer<T>(IEnumerable<T> items, AvroConvertOptions options)
            where T : notnull
        {
            using (MemoryStream resultStream = new MemoryStream())
            {
                var schema = Schema.Create(typeof(T), options);
                using (var writer = new Encoder(schema, resultStream, options.Codec, options))
                {
                    foreach (var item in items)
                    {
                        writer.Append(item);
                    }
                }
                byte[] result = resultStream.ToArray();
                return result;
            }
        }
    }
}