using System;
using System.Collections.Generic;
using System.Linq;
using AwesomeAssertions;
using SolTechnology.Avro;
using NUnit.Framework;

namespace AvroConvertComponentTests.JsonEncoding
{
    public class JsonEncodingTests
    {
        private const string OptionalFieldSchema =
            @"{""type"":""record"",""name"":""Rec"",""fields"":[{""name"":""MyField"",""type"":[""null"",""string""]}]}";

        public class Rec
        {
            public string MyField { get; set; }
        }

        public class OptionalNumber
        {
            public int? Value { get; set; }
        }

        [Test]
        public void Avro2JsonEncoding_UnionValue_IsWrappedInBranchName()
        {
            //Arrange (issue #156)
            var avro = AvroConvert.SerializeHeadless(new Rec { MyField = "MyValue" }, OptionalFieldSchema);


            //Act
            var json = AvroConvert.Avro2JsonEncoding(avro, OptionalFieldSchema);


            //Assert
            json.Should().Be(@"{""MyField"":{""string"":""MyValue""}}");
            AvroConvert.Avro2Json(avro, OptionalFieldSchema).Should().Be(@"{""MyField"":""MyValue""}");
        }

        [Test]
        public void Avro2JsonEncoding_NullUnion_IsJsonNull()
        {
            //Arrange
            var avro = AvroConvert.SerializeHeadless(new Rec { MyField = null }, OptionalFieldSchema);


            //Act
            var json = AvroConvert.Avro2JsonEncoding(avro, OptionalFieldSchema);


            //Assert
            json.Should().Be(@"{""MyField"":null}");
        }

        [Test]
        public void Avro2JsonEncoding_SpecialValues_FollowSpecification()
        {
            //Arrange
            const string schema = @"{""type"":""record"",""name"":""S"",""namespace"":""ns"",""fields"":[
                {""name"":""b"",""type"":""bytes""},
                {""name"":""f"",""type"":{""type"":""fixed"",""name"":""Two"",""size"":2}},
                {""name"":""t"",""type"":{""type"":""long"",""logicalType"":""timestamp-millis""}},
                {""name"":""d"",""type"":""double""},
                {""name"":""e"",""type"":{""type"":""enum"",""name"":""E"",""symbols"":[""A"",""B""]}},
                {""name"":""u"",""type"":[""null"",""E"",{""type"":""array"",""items"":""int""}]},
                {""name"":""m"",""type"":{""type"":""map"",""values"":""long""}}]}";
            var json = @"{""b"":""\u0000Aéÿ"",""f"":""\u0001\u0002"",""t"":1500,""d"":""NaN"",""e"":""B"",""u"":{""array"":[1,2]},""m"":{""k"":5}}";


            //Act
            var avro = AvroConvert.JsonEncoding2Avro(json, schema);
            var result = AvroConvert.Avro2JsonEncoding(avro);


            //Assert
            result.Should().Be(json);
            var plain = AvroConvert.Avro2Json(avro);
            plain.Should().Contain(@"""t"":""1970-01-01T00:00:01.5Z""");
        }

        [Test]
        public void Avro2JsonEncoding_ContainerWithManyEntries_WritesOneValuePerLine()
        {
            //Arrange
            var items = new List<OptionalNumber> { new() { Value = 1 }, new() { Value = null }, new() { Value = 3 } };
            var avro = AvroConvert.Serialize(items);


            //Act
            var json = AvroConvert.Avro2JsonEncoding(avro);


            //Assert
            json.Split('\n').Should().Equal(
                @"{""Value"":{""int"":1}}",
                @"{""Value"":null}",
                @"{""Value"":{""int"":3}}");
        }

        [Test]
        public void JsonEncoding2Avro_NewLineSeparatedValues_BecomeContainerEntries()
        {
            //Arrange
            var json = "{\"MyField\":{\"string\":\"a\"}}\n{\"MyField\":null}\n";


            //Act
            var avro = AvroConvert.JsonEncoding2Avro(json, OptionalFieldSchema, CodecType.Deflate);


            //Assert
            AvroConvert.Deserialize<List<Rec>>(avro).Select(r => r.MyField).Should().Equal("a", null);
        }

        [Test]
        public void JsonEncoding_HeadlessValues_RoundTrip()
        {
            //Arrange
            var schema = @"""long""";
            var avro = AvroConvert.JsonEncoding2Avro("1 2 3", schema);


            //Act
            var json = AvroConvert.Avro2JsonEncoding(avro);


            //Assert
            json.Should().Be("1\n2\n3");
            AvroConvert.Avro2JsonEncoding(new byte[] { 2, 4, 6 }, schema).Should().Be("1\n2\n3");
        }

        [Test]
        public void JsonEncoding2Avro_MissingFieldWithDefault_UsesDefault()
        {
            //Arrange
            var schema = @"{""type"":""record"",""name"":""D"",""fields"":[
                {""name"":""a"",""type"":""int""},
                {""name"":""b"",""type"":""string"",""default"":""x""},
                {""name"":""c"",""type"":[""null"",""int""],""default"":null}]}";


            //Act
            var avro = AvroConvert.JsonEncoding2Avro(@"{""a"":1,""ignored"":true}", schema);


            //Assert
            AvroConvert.Avro2JsonEncoding(avro).Should().Be(@"{""a"":1,""b"":""x"",""c"":null}");
        }

        [Test]
        public void JsonEncoding2Avro_NamedBranchByShortName_IsAccepted()
        {
            //Arrange
            var schema = @"{""type"":""record"",""name"":""Outer"",""namespace"":""ns"",""fields"":[
                {""name"":""v"",""type"":[""null"",{""type"":""record"",""name"":""Inner"",""fields"":[{""name"":""x"",""type"":""int""}]}]}]}";


            //Act
            var fullName = AvroConvert.Avro2JsonEncoding(AvroConvert.JsonEncoding2Avro(@"{""v"":{""ns.Inner"":{""x"":1}}}", schema));
            var shortName = AvroConvert.Avro2JsonEncoding(AvroConvert.JsonEncoding2Avro(@"{""v"":{""Inner"":{""x"":1}}}", schema));


            //Assert
            fullName.Should().Be(@"{""v"":{""ns.Inner"":{""x"":1}}}");
            shortName.Should().Be(fullName);
        }

        [TestCase(@"{""MyField"":""plain""}", "$.MyField")]
        [TestCase(@"{""MyField"":{""int"":1}}", "'int' is not a branch")]
        [TestCase(@"{}", "missing and has no default")]
        [TestCase(@"{""MyField"":{""string"":1}}", "expected String")]
        public void JsonEncoding2Avro_InvalidJson_ThrowsWithPath(string json, string expectedMessagePart)
        {
            //Act
            var act = () => AvroConvert.JsonEncoding2Avro(json, OptionalFieldSchema);


            //Assert
            act.Should().Throw<Exception>().Which.Message.Should().Contain(expectedMessagePart);
        }

        [Test]
        public void Avro2JsonEncoding_HeadlessWithoutSchema_Throws()
        {
            var act = () => AvroConvert.Avro2JsonEncoding(new byte[] { 2 });

            act.Should().Throw<Exception>().WithMessage("*schema*");
        }
    }
}
