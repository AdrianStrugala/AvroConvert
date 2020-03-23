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
using SolTechnology.PerformanceBenchmark.AvroConvertToUpdate.Codec;
using SolTechnology.PerformanceBenchmark.AvroConvertToUpdate.Write;

namespace SolTechnology.PerformanceBenchmark.AvroConvertToUpdate
{
    public static partial class AvroConvert
    {
        public static byte[] Serialize(object obj, CodecType codecType = CodecType.Null)
        {
            MemoryStream resultStream = new MemoryStream();

            string schema = GenerateSchema(obj.GetType());
            using (var writer = new Encoder(Schema.Schema.Parse(schema), resultStream, codecType))
            {
                writer.Append(obj);
            }

            var result = resultStream.ToArray();
            return result;
        }

        public static byte[] SerializeHeadless(object obj, string schema, CodecType codecType = CodecType.Null)
        {
            MemoryStream resultStream = new MemoryStream();
            var encoder = new Writer(resultStream);

            var writer = Resolver.ResolveWriter(Schema.Schema.Parse(schema));
            writer(obj, encoder);


            var result = resultStream.ToArray();
            return result;
        }
    }
}
