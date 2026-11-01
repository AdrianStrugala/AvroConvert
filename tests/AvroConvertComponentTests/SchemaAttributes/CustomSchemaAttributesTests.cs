using System.Collections.Generic;
using System.IO;
using AwesomeAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SolTechnology.Avro;

namespace AvroConvertComponentTests.SchemaAttributes
{
    /// <summary>
    /// Attributes the Avro specification does not define are metadata of the schema: they do not change the data, and tools
    /// outside of Avro depend on them (Apache Iceberg's field-id, Java's avro.java.string).
    /// </summary>
    public class CustomSchemaAttributesTests
    {
        private const string Manifest = @"{""type"":""record"",""name"":""manifest_entry"",""owner"":""iceberg"",""fields"":[" +
            @"{""name"":""status"",""type"":""int"",""field-id"":0}," +
            @"{""name"":""tags"",""type"":{""type"":""array"",""items"":""string"",""element-id"":9},""field-id"":1}," +
            @"{""name"":""props"",""type"":{""type"":""map"",""values"":""long"",""key-id"":10,""value-id"":11},""field-id"":2}," +
            @"{""name"":""sizes"",""type"":{""type"":""array"",""logicalType"":""map"",""items"":{""type"":""record"",""name"":""k12_v13"",""fields"":[" +
                @"{""name"":""key"",""type"":""int"",""field-id"":12},{""name"":""value"",""type"":""long"",""field-id"":13}]}},""field-id"":3}]}";

        [TestCase(@"{""type"":""record"",""name"":""R"",""fields"":[{""name"":""a"",""type"":""int"",""field-id"":8}]}", TestName = "Record field")]
        [TestCase(@"{""type"":""record"",""name"":""R"",""owner"":""me"",""fields"":[]}", TestName = "Record")]
        [TestCase(@"{""type"":""array"",""items"":""int"",""element-id"":9}", TestName = "Array")]
        [TestCase(@"{""type"":""map"",""values"":""int"",""key-id"":10,""value-id"":11}", TestName = "Map")]
        [TestCase(@"{""type"":""array"",""logicalType"":""map"",""items"":""int""}", TestName = "Logical type unknown to Avro")]
        [TestCase(@"{""type"":""string"",""avro.java.string"":""String""}", TestName = "Primitive")]
        [TestCase(@"{""type"":""long"",""logicalType"":""timestamp-micros"",""adjust-to-utc"":true}", TestName = "Logical type")]
        [TestCase(@"{""type"":""bytes"",""logicalType"":""decimal"",""precision"":9,""scale"":2,""owner"":""me""}", TestName = "Decimal")]
        [TestCase(@"{""type"":""fixed"",""name"":""F"",""size"":4,""owner"":""me""}", TestName = "Fixed")]
        [TestCase(@"{""type"":""fixed"",""name"":""D"",""size"":12,""logicalType"":""duration"",""owner"":""me""}", TestName = "Logical type on fixed")]
        [TestCase(@"{""type"":""enum"",""name"":""E"",""symbols"":[""A""],""owner"":""me""}", TestName = "Enum")]
        [TestCase(@"{""type"":""enum"",""name"":""E"",""aliases"":[""Old""],""symbols"":[""A""]}", TestName = "Enum aliases")]
        [TestCase(@"{""type"":""array"",""items"":""int"",""owner"":{""name"":""me"",""ids"":[1,2.5,true,null,""x""]}}", TestName = "Any JSON value")]
        public void Schema_text_keeps_its_attributes(string json)
        {
            var result = AvroSchema.Parse(json).Json;

            JToken.DeepEquals(JToken.Parse(result), JToken.Parse(json)).Should().BeTrue(result);
        }

        [Test]
        public void Identifier_stays_a_json_number()
        {
            var result = JObject.Parse(AvroSchema.Parse(Manifest).Json);

            result.SelectToken("fields[0].field-id").Type.Should().Be(JTokenType.Integer);
        }

        [Test]
        public void Attributes_do_not_change_the_identity_of_the_schema()
        {
            const string plain = @"{""type"":""record"",""name"":""R"",""fields"":[{""name"":""a"",""type"":""int""}]}";
            const string annotated = @"{""type"":""record"",""name"":""R"",""owner"":""me"",""fields"":[{""name"":""a"",""type"":""int"",""field-id"":8}]}";

            var schema = AvroSchema.Parse(annotated);

            schema.CanonicalForm.Should().Be(AvroSchema.Parse(plain).CanonicalForm);
            schema.Should().Be(AvroSchema.Parse(plain));
            schema.Fingerprint.Should().Be(AvroSchema.Parse(plain).Fingerprint);
        }

        [Test]
        public void Schema_of_a_dotnet_type_has_no_additional_attributes()
        {
            var json = JObject.Parse(AvroSchema.Create<Plain>().Json);

            json.Should().NotContainKey("logicalType");
            json.Properties().Should().HaveCount(4);
        }

        public class Plain
        {
            public int Id { get; set; }
        }

        public class Entry
        {
            public int status { get; set; }
            public List<string> tags { get; set; }
            public Dictionary<string, long> props { get; set; }
            public Dictionary<int, long> sizes { get; set; }
        }

        private static readonly Entry Value = new()
        {
            status = 1,
            tags = new List<string> { "a" },
            props = new Dictionary<string, long> { ["k"] = 2 },
            sizes = new Dictionary<int, long>()
        };

        private static string HeaderSchema(byte[] avro) => AvroConvert.GetSchema(new MemoryStream(avro));

        [Test]
        public void Data_written_with_an_annotated_schema_is_read_back(
            [ValueSource(typeof(Formats), nameof(Formats.ContainerWriters))] ContainerWriter writer,
            [ValueSource(typeof(Formats), nameof(Formats.ContainerReaders))] ContainerReader reader)
        {
            var schema = AvroSchema.Parse(Manifest);

            var avro = RoundTrip.Write(writer, new[] { Value }, schema);

            JToken.DeepEquals(JToken.Parse(HeaderSchema(avro)), JToken.Parse(Manifest)).Should().BeTrue(HeaderSchema(avro));
            RoundTrip.ReadAll<Entry>(reader, avro).Should().BeEquivalentTo(new[] { Value });
        }

        [Test]
        public void Attributes_travel_with_the_data_through_every_codec([ValueSource(typeof(Formats), nameof(Formats.Codecs))] CodecType codec)
        {
            var options = new AvroConvertOptions { Codec = codec };

            var avro = AvroConvert.Serialize(Value, AvroSchema.Parse(Manifest), options);

            JToken.DeepEquals(JToken.Parse(HeaderSchema(avro)), JToken.Parse(Manifest)).Should().BeTrue(HeaderSchema(avro));
            AvroConvert.Deserialize<Entry>(avro).Should().BeEquivalentTo(Value);
        }

        [Test]
        public void Headless_data_written_with_an_annotated_schema_is_read_back([ValueSource(typeof(Formats), nameof(Formats.HeadlessReaders))] HeadlessReader reader)
        {
            var avro = AvroConvert.SerializeHeadless(Value, Manifest);

            RoundTrip.ReadHeadless<Entry>(reader, avro, Manifest).Should().BeEquivalentTo(Value);
        }

        [Test]
        public void Attributes_stay_in_the_schema_of_a_json_encoded_file()
        {
            var avro = AvroConvert.JsonEncoding2Avro(AvroConvert.Avro2JsonEncoding(AvroConvert.SerializeHeadless(Value, Manifest), Manifest), Manifest);

            JToken.DeepEquals(JToken.Parse(HeaderSchema(avro)), JToken.Parse(Manifest)).Should().BeTrue(HeaderSchema(avro));
        }
    }
}
