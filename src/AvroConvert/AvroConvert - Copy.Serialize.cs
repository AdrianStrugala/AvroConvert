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
using DUPA.File;
using DUPA.Generic;
using DUPA.IO;
using DUPA.Reflect;
using DUPA.Specific;
using SolTechnology.Avro.Codec;
using SolTechnology.Avro.Write;
using Encoder = SolTechnology.Avro.Write.Encoder;

namespace SolTechnology.Avro
{
    public static partial class AvroConvert
    {
        public static byte[] Serialize1(object obj, CodecType codecType = CodecType.Null)
        {
            MemoryStream resultStream = new MemoryStream();

            string schema = GenerateSchema(obj.GetType());
//            var dchema = DUPA.Schema.Schema.Parse(schema);

            using (var writer = new Encoder(Schema.Schema.Parse(schema), resultStream, codecType))
            {
                writer.Append(obj);
            }
            //
            //            var result = resultStream.ToArray();
            //            return result;
            //

            //            using (var x = DataFileWriter.OpenWriter(new GenericWriter(DUPA.Schema.Schema.Parse(schema)), resultStream, DUPA.File.Codec.CreateCodecFromString("Null")))
            //            {
            //                x.Append(obj);
            //            }

//            var decoder = new BinaryEncoder(resultStream);

//            var x = new SpecificDatumWriter(dchema);
//            x.Write(obj, decoder);

            //            var avroWriter = new ReflectWriter(dchema, null);

            //                avroWriter.Write(obj, new BinaryEncoder(resultStream));


            return resultStream.ToArray();
        }
    }
}
