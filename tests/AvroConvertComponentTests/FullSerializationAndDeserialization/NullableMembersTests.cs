#nullable enable
using System.Collections.Generic;
using AwesomeAssertions;
using NUnit.Framework;
using SolTechnology.Avro;

namespace AvroConvertComponentTests.FullSerializationAndDeserialization
{
    /// <summary>Records, generic records and dictionaries in members annotated as nullable (a union with "null").</summary>
    public class NullableMembersTests
    {
        public class Field<T>
        {
            public T? Value { get; set; }
            public float Confidence { get; set; }
        }

        public class WithNullableGeneric
        {
            public Field<string>? Name { get; set; }
        }

        public class WithNullableIntKeyedDictionary
        {
            public Dictionary<int, string>? Values { get; set; }
        }

        public class City
        {
            public string Name { get; set; } = "";
        }

        public class WithNullableCity
        {
            public City? City { get; set; }
        }

        [Test]
        public void Generic_record_in_a_nullable_member_round_trips_with_a_schema_given_as_json()
        {
            var value = new WithNullableGeneric { Name = new Field<string> { Value = "Ann", Confidence = 0.5f } };
            var schema = AvroSchema.Create<WithNullableGeneric>().Json;

            var result = AvroConvert.DeserializeHeadless<WithNullableGeneric>(AvroConvert.SerializeHeadless(value, schema), schema);

            result.Should().BeEquivalentTo(value);
        }

        [Test]
        public void Dictionary_with_int_keys_in_a_nullable_member_round_trips()
        {
            var value = new WithNullableIntKeyedDictionary { Values = new Dictionary<int, string> { [1] = "one", [2] = "two" } };

            var result = AvroConvert.Deserialize<WithNullableIntKeyedDictionary>(AvroConvert.Serialize(value));

            result.Values.Should().BeEquivalentTo(value.Values);
        }

        [Test]
        public void Record_of_another_name_is_read_into_a_nullable_member()
        {
            var anonymous = new { City = new { Name = "Wroclaw" } };

            var result = AvroConvert.Deserialize<WithNullableCity>(AvroConvert.Serialize(anonymous));

            result.City!.Name.Should().Be("Wroclaw");
        }
    }
}
