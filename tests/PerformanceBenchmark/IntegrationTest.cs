using System.Collections.Generic;
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
            var fixture = new AutoFixture.Fixture();
            Dataset dataset = fixture.Create<Dataset>();
            dataset.min_position = 1;
            dataset.isActive = true;
            dataset.MoreString = "StringForTestPurpose";

            var schema = AvroConvert.GenerateSchema(typeof(Dataset));
            Schema apacheSchema = Schema.Parse(AvroConvert.GenerateSchema(typeof(Dataset)));


            //AvroConvert to Apache
            var avroConvertSerialized = AvroConvert.SerializeHeadless(dataset, schema);

            Dataset apacheDeserialized = new Dataset();
            using (var ms = new MemoryStream(avroConvertSerialized))
            {
                var apacheReader = new GenericDatumReader<GenericRecord>(apacheSchema, apacheSchema);
                var decoder = new BinaryDecoder(ms);
                apacheDeserialized = (ApacheAvroHelpers.Decreate<Dataset>(apacheReader.Read(null, decoder)));
            }

            Contract.Assert(dataset.min_position == apacheDeserialized.min_position);
            Contract.Assert(dataset.isActive == apacheDeserialized.isActive);
            Contract.Assert(dataset.MoreString == apacheDeserialized.MoreString);


            //Apache to AvroConvert
            MemoryStream apacheAvroSerializeStream = new MemoryStream();
            var encoder = new BinaryEncoder(apacheAvroSerializeStream);
            var apacheWriter = new GenericDatumWriter<GenericRecord>(apacheSchema);
            apacheWriter.Write(ApacheAvroHelpers.Create(dataset, apacheSchema), encoder);
            var apacheSerialized = apacheAvroSerializeStream.ToArray();

            var acroConvertDeserialized = AvroConvert.DeserializeHeadless<Dataset>(apacheSerialized, schema);

            Contract.Assert(dataset.min_position == acroConvertDeserialized.min_position);
            Contract.Assert(dataset.isActive == acroConvertDeserialized.isActive);
            Contract.Assert(dataset.MoreString == acroConvertDeserialized.MoreString);

        }
    }
}
