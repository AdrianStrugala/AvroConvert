using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using AutoFixture;
using Avro;
using Avro.Generic;
using Avro.IO;
using SolTechnology.PerformanceBenchmark.AvroConvertToUpdate;

namespace SolTechnology.PerformanceBenchmark
{
    public static class IntegrationTest
    {
        public static void Invoke()
        {
            //Arrange
            var fixture = new Fixture();
            Dataset[] dataset = fixture.CreateMany<Dataset>(2).ToArray();

            var schema = AvroConvert.GenerateSchema(typeof(Dataset));
            Schema apacheSchema = Schema.Parse(AvroConvert.GenerateSchema(typeof(Dataset)));


            //AvroConvert to Apache
            var avroConvertSerialized = AvroConvert.SerializeHeadless(dataset, schema);

            List<Dataset> apacheDeserialized = new List<Dataset>();
            using (var ms = new MemoryStream(avroConvertSerialized))
            {
                var apacheReader = new GenericDatumReader<GenericRecord>(apacheSchema, apacheSchema);
                var decoder = new BinaryDecoder(ms);
                foreach (var x in dataset)
                {
                    apacheDeserialized.Add(ApacheAvroHelpers.Decreate<Dataset>(apacheReader.Read(null, decoder)));
                }
            }

            Contract.Assert(dataset == apacheDeserialized.ToArray());


            //Apache to AvroConvert
            MemoryStream apacheAvroSerializeStream = new MemoryStream();
            var encoder = new BinaryEncoder(apacheAvroSerializeStream);
            var apacheWriter = new GenericDatumWriter<GenericRecord>(apacheSchema);
            foreach (var x in dataset)
            {
                apacheWriter.Write(ApacheAvroHelpers.Create(dataset, apacheSchema), encoder);
            }

            var apacheSerialized = apacheAvroSerializeStream.ToArray();

            var acroConvertDeserialized = AvroConvert.DeserializeHeadless<Dataset[]>(apacheSerialized, schema);

            Contract.Assert(dataset == acroConvertDeserialized);

        }
    }
}
