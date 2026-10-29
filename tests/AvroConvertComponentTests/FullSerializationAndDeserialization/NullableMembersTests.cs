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

        public class Cat
        {
            public string Name { get; set; } = "";
            public int Lives { get; set; }
        }

        public class Dog
        {
            public string Name { get; set; } = "";
            public string Breed { get; set; } = "";
        }

        public class LookalikeDog
        {
            public string Name { get; set; } = "";
            public int Lives { get; set; }
        }

        public class PetWritten
        {
            [SolTechnology.Avro.Infrastructure.Attributes.AvroUnion(typeof(Cat), typeof(Dog), typeof(LookalikeDog))]
            public object? Animal { get; set; }
        }

        public class PetRead
        {
            public Cat? Animal { get; set; }
        }

        public class PetReadWithoutUnion
        {
            public Cat Animal { get; set; } = new();
        }

        [Test]
        public void Record_in_a_union_is_read_like_the_same_record_outside_a_union()
        {
            // Cat: same name. LookalikeDog: another name, its fields resolve into Cat. Dog: Lives is missing and has no default.
            AvroConvert.Deserialize<PetRead>(AvroConvert.Serialize(new PetWritten { Animal = new Cat { Name = "Tom", Lives = 9 } }))
                .Animal!.Lives.Should().Be(9);

            AvroConvert.Deserialize<PetRead>(AvroConvert.Serialize(new PetWritten { Animal = new LookalikeDog { Name = "Rex", Lives = 1 } }))
                .Animal!.Lives.Should().Be(1);
            AvroConvert.Deserialize<PetReadWithoutUnion>(AvroConvert.Serialize(new { Animal = new LookalikeDog { Name = "Rex", Lives = 1 } }))
                .Animal.Lives.Should().Be(1, "outside a union the record is read the same way");

            var dog = () => AvroConvert.Deserialize<PetRead>(AvroConvert.Serialize(new PetWritten { Animal = new Dog { Name = "Rex", Breed = "Beagle" } }));
            dog.Should().Throw<System.Exception>();
            var dogWithoutUnion = () => AvroConvert.Deserialize<PetReadWithoutUnion>(AvroConvert.Serialize(new { Animal = new Dog { Name = "Rex", Breed = "Beagle" } }));
            dogWithoutUnion.Should().Throw<System.Exception>("outside a union the record is rejected the same way");
        }
    }
}
