using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public static List<object> Deserialize(byte[] avroBytes, string schema = null)
        {
            var result = new List<object>();

            var reader = Decoder.OpenReader(new MemoryStream(avroBytes), schema);

            List<dynamic> readResult = reader.GetEntries().ToList();

            foreach (var read in readResult)
            {
                result.Add(read);
            }

            return result;
        }

        public static T Deserialize<T>(byte[] avroBytes, string schema = null)
        {
            T result;

            var deserialized = Deserialize(avroBytes, schema ?? GenerateSchema(typeof(T), true));

            result = Mapper.Map<T>(deserialized[0]);

            return result;
        }
    }
}
