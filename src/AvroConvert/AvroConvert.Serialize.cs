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
using SolTechnology.Avro.BuildSchema;
using SolTechnology.Avro.FileHeader.Codec;
using SolTechnology.Avro.Write;

namespace SolTechnology.Avro
{
    public static partial class AvroConvert
    {
        /// <summary>
        /// Serializes given object to AVRO format (including header with metadata)
        /// </summary>
        public static byte[] Serialize(object obj)
        {
            return Serialize(obj, CodecType.Null);
        }

        /// <summary>
        /// Serializes given object to AVRO format (including header with metadata)
        /// Choosing <paramref name="codecType"/> reduces output object size
        /// </summary>
        public static byte[] Serialize(object obj, CodecType codecType)
        {
            using (MemoryStream resultStream = new MemoryStream())
            {
                var schema = BuildSchema.Schema.Create(obj);
                using (var writer = new Encoder(schema, resultStream, codecType))
                {
                    writer.Append(obj);
                }
                var result = resultStream.ToArray();
                return result;
            }
        }
    }
}