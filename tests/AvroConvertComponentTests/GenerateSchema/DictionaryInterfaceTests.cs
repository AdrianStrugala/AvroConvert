using System.Collections.Generic;
using AwesomeAssertions;
using NUnit.Framework;
using SolTechnology.Avro;

namespace AvroConvertComponentTests.GenerateSchema
{
    public class DictionaryInterfaceTests
    {
        public class WithIDictionary
        {
            public IDictionary<string, int> Values { get; set; }
        }

        public class WithIReadOnlyDictionary
        {
            public IReadOnlyDictionary<string, int> Values { get; set; }
        }

        [Test]
        public void IDictionary_member_is_a_map()
        {
            var schema = AvroSchema.Create<WithIDictionary>().Json;

            schema.Should().Contain("{\"name\":\"Values\",\"type\":{\"type\":\"map\",\"values\":\"int\"}}");
        }

        [Test]
        public void IReadOnlyDictionary_member_is_a_map()
        {
            var schema = AvroSchema.Create<WithIReadOnlyDictionary>().Json;

            schema.Should().Contain("{\"name\":\"Values\",\"type\":{\"type\":\"map\",\"values\":\"int\"}}");
        }

        [Test]
        public void Dictionary_interfaces_round_trip()
        {
            var values = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 };

            AvroConvert.Deserialize<WithIDictionary>(AvroConvert.Serialize(new WithIDictionary { Values = values }))
                .Values.Should().BeEquivalentTo(values);
            AvroConvert.Deserialize<WithIReadOnlyDictionary>(AvroConvert.Serialize(new WithIReadOnlyDictionary { Values = values }))
                .Values.Should().BeEquivalentTo(values);
        }
    }
}
