#region license
/**Copyright (c) 2021 Adrian Strugala
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

using System;
using System.IO;
using SolTechnology.Avro.AvroObjectServices.BuildSchema;
using SolTechnology.Avro.Features.Deserialize;

namespace SolTechnology.Avro
{
    public static partial class AvroConvert
    {
        /// <summary>
        /// Deserializes Avro object to .NET type
        /// </summary>
        public static T Deserialize<T>(byte[] avroBytes)
        {
            using (var stream = new MemoryStream(avroBytes))
            {
                var decoder = new Decoder();
                var deserialized = decoder.Decode<T>(
                    stream,
                    Schema.Create(typeof(T))
                );
                return deserialized;
            }
        }

        /// <summary>
        /// Deserializes Avro object to .NET type
        /// </summary>
        public static dynamic Deserialize(byte[] avroBytes, Type targetType)
        {
            object result = typeof(AvroConvert)
                            .GetMethod(nameof(Deserialize), new[] { typeof(byte[]) })
                            ?.MakeGenericMethod(targetType)
                            .Invoke(null, new object[] { avroBytes });

            return result;
        }
    }
}
