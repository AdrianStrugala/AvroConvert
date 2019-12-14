using System;
using System.IO;
using AutoMapper;
using SolTechnology.Avro.Models;
using SolTechnology.Avro.Read;
using SolTechnology.Avro.Read.AutoMapperConverters;

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
            var reader = Decoder.OpenReader(
                new MemoryStream(avroBytes),
                GenerateSchema(typeof(T), true)
                );

            return Mapper.Map<T>(reader.Read());
        }
    }
}
