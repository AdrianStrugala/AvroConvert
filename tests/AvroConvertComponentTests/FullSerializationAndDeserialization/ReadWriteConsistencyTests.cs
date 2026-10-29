#nullable enable
using System;
using System.Runtime.Serialization;
using AwesomeAssertions;
using NUnit.Framework;
using SolTechnology.Avro;

namespace AvroConvertComponentTests.FullSerializationAndDeserialization
{
    /// <summary>Data that schema resolution allows is read and written; the same rule inside and outside a union.</summary>
    public class ReadWriteConsistencyTests
    {
        [DataContract(Name = "User")]
        public class UserV1
        {
            [DataMember] public string Name { get; set; } = "";
        }

        [DataContract(Name = "User")]
        public class UserV2
        {
            [DataMember(Name = "FullName")] public string Name { get; set; } = "";
        }

        [Test]
        public void Field_renamed_with_an_alias_reads_the_old_data()
        {
            var result = AvroConvert.Deserialize<UserV2>(AvroConvert.Serialize(new UserV1 { Name = "Ann" }));

            result.Name.Should().Be("Ann");
        }

        public class Animal
        {
            public string Name { get; set; } = "";
            public int Lives { get; set; }
        }

        public class AnimalHolder
        {
            public Animal? Pet { get; set; }
        }

        [Test]
        public void Union_branch_matched_by_alias_wins_over_a_record_that_only_resolves()
        {
            const string writer = @"{""type"":""record"",""name"":""Holder"",""fields"":[{""name"":""Pet"",""type"":[""null"",
                {""type"":""record"",""name"":""Cat"",""fields"":[{""name"":""Name"",""type"":""string""},{""name"":""Lives"",""type"":""int""}]}]}]}";
            const string reader = @"{""type"":""record"",""name"":""Holder"",""fields"":[{""name"":""Pet"",""type"":[""null"",
                {""type"":""record"",""name"":""Dog"",""fields"":[{""name"":""Name"",""type"":""string""}]},
                {""type"":""record"",""name"":""Kitty"",""aliases"":[""Cat""],""fields"":[{""name"":""Name"",""type"":""string""},{""name"":""Lives"",""type"":""int""}]}]}]}";
            var data = AvroConvert.SerializeHeadless(new AnimalHolder { Pet = new Animal { Name = "Tom", Lives = 9 } }, writer);

            var result = AvroConvert.DeserializeHeadless<AnimalHolder>(data, AvroSchema.Parse(writer), AvroSchema.Parse(reader));

            result.Pet!.Lives.Should().Be(9);
        }

        public class Owner
        {
            public string Name { get; set; } = "";
            public int Age { get; set; }
        }

        public class OwnedPet
        {
            public string Name { get; set; } = "";
            public Owner Owner { get; set; } = new();
        }

        public class OwnedPetHolder
        {
            public OwnedPet? Pet { get; set; }
        }

        [Test]
        public void Union_record_is_chosen_by_nested_records_too()
        {
            // A's Owner needs Age, which the writer does not have; B's Owner does not.
            const string writer = @"{""type"":""record"",""name"":""Holder"",""fields"":[{""name"":""Pet"",""type"":[""null"",
                {""type"":""record"",""name"":""W"",""fields"":[{""name"":""Name"",""type"":""string""},
                  {""name"":""Owner"",""type"":{""type"":""record"",""name"":""OW"",""fields"":[{""name"":""Name"",""type"":""string""}]}}]}]}]}";
            const string reader = @"{""type"":""record"",""name"":""Holder"",""fields"":[{""name"":""Pet"",""type"":[""null"",
                {""type"":""record"",""name"":""A"",""fields"":[{""name"":""Name"",""type"":""string""},
                  {""name"":""Owner"",""type"":{""type"":""record"",""name"":""O1"",""fields"":[{""name"":""Name"",""type"":""string""},{""name"":""Age"",""type"":""int""}]}}]},
                {""type"":""record"",""name"":""B"",""fields"":[{""name"":""Name"",""type"":""string""},
                  {""name"":""Owner"",""type"":{""type"":""record"",""name"":""O2"",""fields"":[{""name"":""Name"",""type"":""string""}]}}]}]}]}";
            var data = AvroConvert.SerializeHeadless(new { Pet = new { Name = "Rex", Owner = new { Name = "Ann" } } }, writer);

            var result = AvroConvert.DeserializeHeadless<OwnedPetHolder>(data, AvroSchema.Parse(writer), AvroSchema.Parse(reader));

            result.Pet!.Owner.Name.Should().Be("Ann");
        }

        [Test]
        public void Union_record_with_a_default_that_cannot_be_read_is_not_chosen()
        {
            const string writer = @"{""type"":""record"",""name"":""Holder"",""fields"":[{""name"":""Pet"",""type"":[""null"",
                {""type"":""record"",""name"":""W"",""fields"":[{""name"":""Name"",""type"":""string""}]}]}]}";
            const string reader = @"{""type"":""record"",""name"":""Holder"",""fields"":[{""name"":""Pet"",""type"":[""null"",
                {""type"":""record"",""name"":""A"",""fields"":[{""name"":""Name"",""type"":""string""},{""name"":""Lives"",""type"":""string"",""default"":""x""}]},
                {""type"":""record"",""name"":""B"",""fields"":[{""name"":""Name"",""type"":""string""}]}]}]}";

            foreach (var pet in new[] { new { Name = "Rex" }, null })
            {
                var data = AvroConvert.SerializeHeadless(new { Pet = pet }, writer);
                var result = AvroConvert.DeserializeHeadless<AnimalHolder>(data, AvroSchema.Parse(writer), AvroSchema.Parse(reader));
                result.Pet?.Name.Should().Be(pet?.Name);
            }
        }

        public class UserModel
        {
            public string Name { get; set; } = "";
        }

        public class UserModelHolder
        {
            public UserModel? User { get; set; }
        }

        [Test]
        public void Class_is_written_into_a_union_record_of_another_name()
        {
            const string schema = @"{""type"":""record"",""name"":""Holder"",""fields"":[{""name"":""User"",""type"":[""null"",
                {""type"":""record"",""name"":""User"",""namespace"":""com.acme"",""fields"":[{""name"":""Name"",""type"":""string""}]}]}]}";

            var data = AvroConvert.SerializeHeadless(new UserModelHolder { User = new UserModel { Name = "Ann" } }, schema);

            AvroConvert.DeserializeHeadless<UserModelHolder>(data, schema).User!.Name.Should().Be("Ann");
        }

        public struct Point
        {
            public int X { get; set; }
            public int Y { get; set; }
        }

        public class WithPoint
        {
            public Point? P { get; set; }
        }

        [Test]
        public void Nullable_struct_record_round_trips()
        {
            var result = AvroConvert.Deserialize<WithPoint>(AvroConvert.Serialize(new WithPoint { P = new Point { X = 3, Y = 4 } }));

            result.P.Should().Be(new Point { X = 3, Y = 4 });
        }

        public class Stamp
        {
            public DateTime At { get; set; }
        }

        [Test]
        public void Long_is_read_as_the_timestamp_of_the_reader_schema()
        {
            const string writer = @"{""type"":""record"",""name"":""Stamp"",""fields"":[{""name"":""At"",""type"":""long""}]}";
            const string reader = @"{""type"":""record"",""name"":""Stamp"",""fields"":[{""name"":""At"",""type"":{""type"":""long"",""logicalType"":""timestamp-micros""}}]}";
            var at = new DateTime(2026, 10, 29, 12, 0, 0, DateTimeKind.Utc);
            var micros = (at - DateTime.UnixEpoch).Ticks / 10;

            var result = AvroConvert.DeserializeHeadless<Stamp>(AvroConvert.SerializeHeadless(new { At = micros }, writer), AvroSchema.Parse(writer), AvroSchema.Parse(reader));

            AvroSchema.CheckCompatibility(AvroSchema.Parse(writer), AvroSchema.Parse(reader), AvroCompatibilityMode.Backward).IsCompatible.Should().BeTrue();
            result.At.Should().Be(at);
        }
    }
}
