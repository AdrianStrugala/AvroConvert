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

        public class Prices : Dictionary<string, decimal>
        {
        }

        public class WithPrices
        {
            public Prices Prices { get; set; }
        }

        [Test]
        public void Class_deriving_from_a_dictionary_is_a_map()
        {
            var value = new WithPrices { Prices = new Prices { ["apple"] = 1.5m } };

            AvroSchema.Create<WithPrices>().Json.Should().Contain("{\"name\":\"Prices\",\"type\":{\"type\":\"map\"");
            AvroConvert.Deserialize<WithPrices>(AvroConvert.Serialize(value)).Prices.Should().BeEquivalentTo(value.Prices);
        }

        public class WithOtherDictionaries
        {
            public SortedDictionary<string, int> Sorted { get; set; }
            public System.Collections.ObjectModel.ReadOnlyDictionary<string, int> ReadOnly { get; set; }
        }

        [Test]
        public void Sorted_and_read_only_dictionaries_round_trip()
        {
            var values = new Dictionary<string, int> { ["b"] = 2, ["a"] = 1 };
            var value = new WithOtherDictionaries
            {
                Sorted = new SortedDictionary<string, int>(values),
                ReadOnly = new System.Collections.ObjectModel.ReadOnlyDictionary<string, int>(values)
            };

            var result = AvroConvert.Deserialize<WithOtherDictionaries>(AvroConvert.Serialize(value));

            result.Sorted.Should().BeEquivalentTo(values);
            result.ReadOnly.Should().BeEquivalentTo(values);
        }
    }
}
