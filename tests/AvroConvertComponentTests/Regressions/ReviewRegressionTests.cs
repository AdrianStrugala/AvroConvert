using System;
using System.Collections.Generic;
using System.Linq;
using AwesomeAssertions;
using NUnit.Framework;
using SolTechnology.Avro;
using SolTechnology.Avro.Policies;

namespace AvroConvertComponentTests.Regressions
{
    /// <summary>Regressions found in the 4.0 pre-release review.</summary>
    public class ReviewRegressionTests
    {
        [Test]
        public void Nested_lists_round_trip_in_entries_layout([ValueSource(typeof(Formats), nameof(Formats.ContainerOnly))] Format format)
        {
            var value = new List<List<int>> { new() { 1, 2 }, new() { 3 }, new() };

            var result = RoundTrip.Run<List<List<int>>>(value, format);

            result.Should().BeEquivalentTo(value, o => o.WithStrictOrdering());
        }

        [Test]
        public void Jagged_arrays_round_trip()
        {
            var value = new[] { new[] { "a", "b" }, Array.Empty<string>(), new[] { "c" } };

            var result = AvroConvert.Deserialize<string[][]>(AvroConvert.Serialize(value));

            result.Should().BeEquivalentTo(value, o => o.WithStrictOrdering());
        }

        [Test]
        public void Single_nested_list_is_a_collection_of_one()
        {
            var value = new List<List<int>> { new() { 7, 8, 9 } };

            var result = AvroConvert.Deserialize<List<List<int>>>(AvroConvert.Serialize(value));

            result.Should().BeEquivalentTo(value, o => o.WithStrictOrdering());
        }

        [Test]
        public void Nested_lists_round_trip_in_single_array_layout()
        {
            var options = new AvroConvertOptions { CollectionMode = AvroCollectionMode.SingleArray };
            var value = new List<List<int>> { new() { 1 }, new() { 2, 3 } };

            var result = AvroConvert.Deserialize<List<List<int>>>(AvroConvert.Serialize(value, options), options);

            result.Should().BeEquivalentTo(value, o => o.WithStrictOrdering());
        }

        [Test]
        public void Several_array_entries_are_flattened_into_one_list()
        {
            // Entries that are each a whole array (3.x layout, several of them) read into a flat list.
            var schema = AvroConvert.GenerateSchema(typeof(List<int>));
            var avro = AvroConvert.SerializeHeadless(new List<int> { 1, 2 }, schema)
                .Concat(AvroConvert.SerializeHeadless(new List<int> { 3 }, schema)).ToArray();

            var result = AvroConvert.DeserializeHeadless<List<int>>(avro, schema, 2);

            result.Should().Equal(1, 2, 3);
        }

        [Test]
        public void List_of_object_keeps_the_shape_of_entries_written_from_nested_lists()
        {
            var avro = AvroConvert.Serialize(new List<List<int>> { new() { 1, 2 }, new() { 3 } });

            var result = AvroConvert.Deserialize<List<object>>(avro);

            result.Should().HaveCount(2);
            ((IEnumerable<object>)result[0]).Should().Equal(1, 2);
        }

        [Test]
        public void Nullable_array_entries_are_read_into_a_list()
        {
            const string schema = """["null", {"type": "array", "items": "int"}]""";
            var avro = AvroConvert.SerializeHeadless(new List<int> { 1, 2 }, schema)
                .Concat(AvroConvert.SerializeHeadless(null, schema))
                .Concat(AvroConvert.SerializeHeadless(new List<int> { 3 }, schema)).ToArray();

            AvroConvert.DeserializeHeadless<List<int>>(avro, schema, 3).Should().Equal(1, 2, 3);
            AvroConvert.DeserializeHeadless<List<int>>(AvroConvert.SerializeHeadless(new List<int> { 4 }, schema), schema).Should().Equal(4);
        }

        [Test]
        public void Equal_java_naming_policies_share_the_schema_cache()
        {
            new JavaAvroNamingPolicy("com.acme").Should().Be(new JavaAvroNamingPolicy("com.acme"));
            new JavaAvroNamingPolicy("com.acme").Should().NotBe(new JavaAvroNamingPolicy("org.other"));
        }

        public class Writer { public int Id { get; set; } }

        public class ReaderWithCollectionDefaults
        {
            public int Id { get; set; }
            public List<string> Tags { get; set; }
            public Dictionary<string, int> Counters { get; set; }
            public byte[] Blob { get; set; }
        }

        [Test]
        public void Reader_defaults_for_collections_are_not_shared_between_records()
        {
            const string readerSchema = """
                {"type":"record","name":"ReaderWithCollectionDefaults","fields":[
                  {"name":"Id","type":"int"},
                  {"name":"Tags","type":{"type":"array","items":"string"},"default":["x"]},
                  {"name":"Counters","type":{"type":"map","values":"int"},"default":{"a":1}},
                  {"name":"Blob","type":"bytes","default":"\u0001"}
                ]}
                """;
            var writerSchema = AvroConvert.GenerateSchema(typeof(Writer));
            var avro = AvroConvert.SerializeHeadless(new Writer { Id = 1 }, writerSchema)
                .Concat(AvroConvert.SerializeHeadless(new Writer { Id = 2 }, writerSchema)).ToArray();

            var result = AvroConvert.DeserializeHeadless<List<ReaderWithCollectionDefaults>>(avro, writerSchema, readerSchema, 2);
            result[0].Tags.Add("mutated");
            result[0].Counters["b"] = 2;
            result[0].Blob[0] = 9;

            result[0].Tags.Should().Equal("x", "mutated");
            result[1].Tags.Should().Equal("x");
            result[1].Counters.Should().Equal(new Dictionary<string, int> { ["a"] = 1 });
            result[1].Blob.Should().Equal(1);
            ReferenceEquals(result[0].Tags, result[1].Tags).Should().BeFalse();
        }

        private sealed class PrefixPolicy : IAvroNamingPolicy
        {
            private readonly string _prefix;
            public PrefixPolicy(string prefix) => _prefix = prefix;
            public AvroTypeInfo GetTypeName(Type type) => new(_prefix + type.Name, type.Namespace);
            public string GetMemberName(System.Reflection.MemberInfo member) => _prefix + member.Name;
        }

        public class Plain { public int Id { get; set; } }

        [Test]
        public void Differently_configured_naming_policy_instances_get_their_own_schema()
        {
            var a = new AvroConvertOptions { NamingPolicy = new PrefixPolicy("a_") };
            var b = new AvroConvertOptions { NamingPolicy = new PrefixPolicy("b_") };

            var schemaA = AvroConvert.GenerateSchema(typeof(Plain), a);
            var schemaB = AvroConvert.GenerateSchema(typeof(Plain), b);

            schemaA.Should().Contain("a_Id");
            schemaB.Should().Contain("b_Id").And.NotContain("a_");
            AvroConvert.Deserialize<Plain>(AvroConvert.Serialize(new Plain { Id = 5 }, b), b).Id.Should().Be(5);
        }

        public class WithOffset { public DateTimeOffset At { get; set; } }

        [Test]
        public void DateTimeOffset_keeps_its_instant([ValueSource(typeof(Formats), nameof(Formats.Core))] Format format)
        {
            var value = new WithOffset { At = new DateTimeOffset(2026, 10, 22, 15, 30, 0, TimeSpan.FromHours(2)) };

            var result = RoundTrip.Run<WithOffset>(value, format);

            result.At.Should().Be(value.At);
            result.At.UtcDateTime.Should().Be(new DateTime(2026, 10, 22, 13, 30, 0, DateTimeKind.Utc));
        }
    }
}
