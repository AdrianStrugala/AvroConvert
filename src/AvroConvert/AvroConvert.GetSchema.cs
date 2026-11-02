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
using SolTechnology.Avro.Features.GetSchema;

namespace SolTechnology.Avro
{
    public static partial class AvroConvert
    {
        /// <summary>
        /// Extracts data schema from given Avro object
        /// </summary>
        public static string GetSchema(byte[] avroBytes)
        {
            using (var stream = new MemoryStream(avroBytes))
            {
                var headerDecoder = new HeaderDecoder();
                var schema = headerDecoder.GetSchema(stream);

                return schema;
            }
        }


        /// <summary>
        /// Extracts data schema from given Avro stream
        /// </summary>
        public static string GetSchema(Stream avroStream)
        {
            var headerDecoder = new HeaderDecoder();
            var schema = headerDecoder.GetSchema(avroStream);

            return schema;
        }

        /// <summary>
        /// Reads the key-value metadata of the header of given Avro object container, <c>avro.schema</c> and <c>avro.codec</c> included
        /// </summary>
        public static IReadOnlyDictionary<string, byte[]> GetMetadata(byte[] avroBytes)
        {
            using (var stream = new MemoryStream(avroBytes))
            {
                return GetMetadata(stream);
            }
        }

        /// <summary>
        /// Reads the key-value metadata of the header of given Avro stream, <c>avro.schema</c> and <c>avro.codec</c> included
        /// </summary>
        public static IReadOnlyDictionary<string, byte[]> GetMetadata(Stream avroStream)
        {
            return new HeaderDecoder().GetMetadata(avroStream);
        }
    }
}
