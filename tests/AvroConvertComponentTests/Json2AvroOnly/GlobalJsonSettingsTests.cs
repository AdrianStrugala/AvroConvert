using System;
using AwesomeAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using SolTechnology.Avro;

namespace AvroConvertComponentTests.Json2AvroOnly
{
    /// <summary>JsonConvert.DefaultSettings belong to the application: the JSON of Avro data and of schema defaults ignores them.</summary>
    [NonParallelizable] // JsonConvert.DefaultSettings is global
    public class GlobalJsonSettingsTests
    {
        private class Throwing : JsonConverter
        {
            public override bool CanConvert(Type objectType) => true;
            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) => throw new InvalidOperationException("application settings used");
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) => throw new InvalidOperationException("application settings used");
        }

        public class User
        {
            public string Name { get; set; }
            public int Age { get; set; }
        }

        private Func<JsonSerializerSettings> _previous;

        [SetUp]
        public void UseThrowingSettings()
        {
            _previous = JsonConvert.DefaultSettings;
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings { Converters = { new Throwing() } };
        }

        [TearDown]
        public void Restore() => JsonConvert.DefaultSettings = _previous;

        [Test]
        public void Json2Avro_of_a_class()
        {
            var avro = AvroConvert.Json2Avro<User>("{\"Name\":\"x\",\"Age\":3}");

            AvroConvert.Deserialize<User>(avro).Should().BeEquivalentTo(new User { Name = "x", Age = 3 });
        }

        [Test]
        public void Json2Avro_of_a_primitive()
        {
            var avro = AvroConvert.Json2Avro("2137");

            AvroConvert.Avro2Json(avro).Should().Be("2137");
        }

        [Test]
        public void Schema_defaults_of_records_and_maps()
        {
            var schema = AvroSchema.Parse("""
                {"type":"record","name":"Order","fields":[
                  {"name":"address","type":{"type":"record","name":"Address","fields":[{"name":"city","type":"string"}]},"default":{"city":"Wroclaw"}},
                  {"name":"labels","type":{"type":"map","values":"int"},"default":{"a":1}}]}
                """);

            schema.Json.Should().Contain("\"default\":{\"city\":\"Wroclaw\"}");
        }

        [Test]
        public void Avro2Json()
        {
            var avro = AvroConvert.Serialize(new User { Name = "x", Age = 3 });

            AvroConvert.Avro2Json(avro).Should().Be("{\"Name\":\"x\",\"Age\":3}");
        }
    }
}
