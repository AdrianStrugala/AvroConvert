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
using AutoMapper;
using SolTechnology.Avro.Models;
using SolTechnology.Avro.Read.AutoMapperConverters;
using V2.IO;
using V2.Specific;

namespace SolTechnology.Avro
{
    public static partial class AvroConvert
    {
        static AvroConvert()
        {
            Mapper.Initialize(cfg =>
                              {
                                  cfg.CreateMap<long, DateTime>().ConvertUsing(new DateTimeConverter());
                                  cfg.CreateMap<Fixed, Guid>().ConvertUsing(new GuidConverter());
                              });
        }

        public static T Deserialize<T>(byte[] avroBytes)
        {

            using (var ms = new MemoryStream(avroBytes))
            {


                //            CreateReader = (stream, schema) => DataFileReader<T>.OpenReader(stream, schema,
                //                                                                            (ws, rs) => new SpecificDatumReader<T>(ws, rs)),
                //            CreateWriter = (stream, schema, codec) =>
                //                               DataFileWriter<T>.OpenWriter(new SpecificDatumWriter<T>(schema), stream, codec)


                //            var reader = Decoder.OpenReader(
                //                new MemoryStream(avroBytes),
                //                GenerateSchema(typeof(T), true)
                //                );

                var schema = V2.Schema.Schema.Parse(GenerateSchema(typeof(T), true));

                var decoder = new BinaryDecoder(ms);

                //                var reader3 = DataFileReader<T>.OpenReader(ms, schema);


                var reader2 = new SpecificDatumReader<T>(schema, schema);
                var read = reader2.Read(decoder);
                //                var read = reader3.Next();

                // var read = reader.Read();

                //                var avroReader = new ReflectReader<T>(schema, schema);

                //                using (var stream = new MemoryStream(avroBytes))
                //                {
                //                   var deserialized = avroReader.Read(new BinaryDecoder(stream));
                //
                //                   return Mapper.Map<T>(deserialized);
                //                }

                return read;
            }
        }
    }
}
