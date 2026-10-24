using AwesomeAssertions;
using System;
using SolTechnology.Avro;
using SolTechnology.Avro.AvroObjectServices.BuildSchema;
using NUnit.Framework;

namespace AvroConvertUnitTests.BuildSchemaTests
{
    public class ParsingCanonicalFormTests
    {
        [Test]
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
            result.Should().Be(@"{""name"":""a.b.R"",""type"":""record"",""fields"":[{""name"":""i"",""type"":""int""},{""name"":""e"",""type"":{""name"":""a.b.E"",""type"":""enum"",""symbols"":[""A""]}},{""name"":""t"",""type"":""long""},{""name"":""again"",""type"":""a.b.E""}]}");
        }

        [Test]
        public void From_LogicalTypeOnFixed_WritesTheFixedDefinition()
        {
            var result = AvroSchema.Parse(@"{""type"":""fixed"",""size"":12,""name"":""duration"",""logicalType"":""duration""}").CanonicalForm;

            result.Should().Be(@"{""name"":""duration"",""type"":""fixed"",""size"":12}");
        }

        [Test]
        public void Fingerprints_HaveSpecifiedLengths()
        {
            var schema = AvroSchema.Parse(@"""int""");
            schema.GetFingerprint(AvroFingerprintAlgorithm.Crc64Avro).Length.Should().Be(8);
            schema.GetFingerprint(AvroFingerprintAlgorithm.Md5).Length.Should().Be(16);
            schema.GetFingerprint(AvroFingerprintAlgorithm.Sha256).Length.Should().Be(32);
            BitConverter.ToInt64(schema.GetFingerprint(AvroFingerprintAlgorithm.Crc64Avro)).Should().Be(schema.Fingerprint);
            AvroSchema.Parse(@"{""type"":""int"",""doc"":""ignored""}").Fingerprint.Should().Be(schema.Fingerprint);
            AvroSchema.Parse(@"{""type"":""int""}").Should().BeEquivalentTo(schema);
        }
    }
}
