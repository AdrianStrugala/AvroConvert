#region license
/**Copyright (c) 2022 Adrian Strugala
*
* Licensed under the CC BY-NC-SA 3.0 License(the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* https://creativecommons.org/licenses/by-nc-sa/3.0/
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*
* You are free to use or modify the code for personal usage.
* For commercial usage purchase the product at
*
* https://xabe.net/product/avroconvert/
*/
#endregion

using System.IO;
using Newtonsoft.Json;
using SolTechnology.Avro.AvroObjectServices.BuildSchema;
using SolTechnology.Avro.Features.AvroToJson;

namespace SolTechnology.Avro
{
    public static partial class AvroConvert
    {
        /// <summary>
        /// Converts Avro object directly to JSON format
        /// </summary>
        public static string Avro2Json(byte[] avro)
        {
            using (var stream = new MemoryStream(avro))
            {
                var decoder = new Decoder();
                var deserialized = decoder.Decode(stream, null);
                var json = JsonConvert.SerializeObject(deserialized);

                return json;
            }
        }


        /// <summary>
        /// Converts Avro object compatible with given <paramref name="avroSchema"/> directly to JSON format
        /// </summary>
        public static string Avro2Json(byte[] avro, string avroSchema)
        {
            using (var stream = new MemoryStream(avro))
            {
                var decoder = new Decoder();
                var deserialized = decoder.Decode(stream, Schema.Parse(avroSchema));
                var json = JsonConvert.SerializeObject(deserialized);

                return json;
            }
        }
    }
}
