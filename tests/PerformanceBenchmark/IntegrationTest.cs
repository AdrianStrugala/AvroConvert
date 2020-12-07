using System.Diagnostics.Contracts;
using System.IO;
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
            Dataset dataset = fixture.Create<Dataset>();

            var schema = AvroConvert.GenerateSchema(typeof(Dataset));
            Schema apacheSchema = Schema.Parse(schema);


            //AvroConvert to Apache
            var avroConvertSerialized = AvroConvert.SerializeHeadless(dataset, schema);

            Dataset apacheDeserialized;
            using (var ms = new MemoryStream(avroConvertSerialized))
            {
                var apacheReader = new GenericDatumReader<GenericRecord>(apacheSchema, apacheSchema);
                var decoder = new BinaryDecoder(ms);
                apacheDeserialized = (ApacheAvroHelpers.Decreate<Dataset>(apacheReader.Read(null, decoder)));
            }

            Contract.Assert(dataset == apacheDeserialized);


            //Apache to AvroConvert
            MemoryStream apacheAvroSerializeStream = new MemoryStream();
            var encoder = new BinaryEncoder(apacheAvroSerializeStream);
            var apacheWriter = new GenericDatumWriter<GenericRecord>(apacheSchema);
            apacheWriter.Write(ApacheAvroHelpers.Create(dataset, apacheSchema), encoder);

            var apacheSerialized = apacheAvroSerializeStream.ToArray();

            var avroConvertDeserialized = AvroConvert.DeserializeHeadless<Dataset>(apacheSerialized);

            Contract.Assert(dataset == avroConvertDeserialized);
        }
    }
}
