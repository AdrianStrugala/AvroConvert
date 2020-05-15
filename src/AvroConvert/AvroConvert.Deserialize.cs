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

using System;
using System.IO;
using SolTechnology.Avro.Read;

namespace SolTechnology.Avro
{
    public static partial class AvroConvert
    {
        public static T Deserialize<T>(byte[] avroBytes)
        {
            using (var stream = new MemoryStream(avroBytes))
            {
                var decoder = new Decoder();
                var deserialized = decoder.Decode<T>(
                    stream,
                    Schema.Schema.Parse(GenerateSchema(typeof(T)))
                );
                return deserialized;
            }
        }


        public static dynamic Deserialize(byte[] avroBytes, Type targetType)
        {
            object result = typeof(AvroConvert)
                            .GetMethod("Deserialize", new[] { typeof(byte[]) })
                            ?.MakeGenericMethod(targetType)
                            .Invoke(null, new object[] { avroBytes });

            return result;
        }
    }
}
