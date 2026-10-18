using System;
using SolTechnology.Avro;
using SolTechnology.Avro.AvroObjectServices.BuildSchema;
using Xunit;

namespace AvroConvertUnitTests.BuildSchemaTests
{
    public class ParsingCanonicalFormTests
    {
        [Fact]
        public void From_StripsAttributesUsesFullNamesAndOrdersProperties()
        {
            //Arrange
            var json = @"{""type"":""record"",""namespace"":""a.b"",""name"":""R"",""doc"":""d"",""aliases"":[""X""],""fields"":[
                {""type"":{""type"":""int""},""name"":""i"",""default"":1,""order"":""ignore""},
                {""name"":""e"",""type"":{""symbols"":[""A""],""type"":""enum"",""name"":""E"",""default"":""A""}},
                {""name"":""t"",""type"":{""type"":""long"",""logicalType"":""timestamp-millis""}},
                {""name"":""again"",""type"":""E""}]}";


            //Act
            var result = ParsingCanonicalForm.From(Schema.Parse(json));


            //Assert
            Assert.Equal(
                @"{""name"":""a.b.R"",""type"":""record"",""fields"":[{""name"":""i"",""type"":""int""},{""name"":""e"",""type"":{""name"":""a.b.E"",""type"":""enum"",""symbols"":[""A""]}},{""name"":""t"",""type"":""long""},{""name"":""again"",""type"":""a.b.E""}]}",
                result);
        }

        [Fact]
        public void From_LogicalTypeOnFixed_WritesTheFixedDefinition()
        {
            var result = AvroSchema.Parse(@"{""type"":""fixed"",""size"":12,""name"":""duration"",""logicalType"":""duration""}").CanonicalForm;

            Assert.Equal(@"{""name"":""duration"",""type"":""fixed"",""size"":12}", result);
        }

        [Fact]
        public void Fingerprints_HaveSpecifiedLengths()
        {
            var schema = AvroSchema.Parse(@"""int""");
            Assert.Equal(8, schema.GetFingerprint(AvroFingerprintAlgorithm.Crc64Avro).Length);
            Assert.Equal(16, schema.GetFingerprint(AvroFingerprintAlgorithm.Md5).Length);
            Assert.Equal(32, schema.GetFingerprint(AvroFingerprintAlgorithm.Sha256).Length);
            Assert.Equal(schema.Fingerprint, BitConverter.ToInt64(schema.GetFingerprint(AvroFingerprintAlgorithm.Crc64Avro)));
            Assert.Equal(schema.Fingerprint, AvroSchema.Parse(@"{""type"":""int"",""doc"":""ignored""}").Fingerprint);
            Assert.Equal(schema, AvroSchema.Parse(@"{""type"":""int""}"));
        }
    }
}
