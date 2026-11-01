using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using AwesomeAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SolTechnology.Avro;

namespace AvroConvertComponentTests.FullSerializationAndDeserialization
{
    /// <summary>
    /// A static type of <c>object</c> says nothing about the data, so the schema of such a value comes from the value. Without
    /// that a string cannot be written at all, and the fields of a class are lost without an error.
    /// </summary>
    public class ObjectValuesTests
    {
        public class Foo
        {
            public string Name { get; set; }
        }

        public class Bar
        {
            public int Size { get; set; }
        }

        public class WithDictionary
        {
            public long Id { get; set; }
            public Dictionary<string, object> Props { get; set; }
        }

        public class WithObject
        {
            public long Id { get; set; }
            public object Value { get; set; }
        }

        public class WithList
        {
            public List<object> Items { get; set; }
        }

        public class WithExpando
        {
            public ExpandoObject Value { get; set; }
        }

        public class Tree
        {
            public object Data { get; set; }
            public List<Tree> Children { get; set; } = new();
        }

        public class Inner
        {
            public object Value { get; set; }
        }

        public class Outer
        {
            public Inner First { get; set; }
            public Inner Second { get; set; }
        }

        public enum Color
        {
            Red,
            Green
        }

        public class Painted
        {
            public Color Color { get; set; }
        }

        public class Holder
        {
            public Foo Foo { get; set; }
        }

        /// <summary>Compares the shape of the data, whatever class the value was read into.</summary>
        private static void ShouldBeJson(object actual, string expected)
        {
            var json = JsonConvert.SerializeObject(actual);

            JToken.DeepEquals(JToken.Parse(json), JToken.Parse(expected)).Should().BeTrue(json);
        }

        private static string SchemaOf(object value) => AvroConvert.GetSchema(AvroConvert.Serialize(value));

        private static void SchemaShouldBe(object value, string expected)
        {
            var schema = SchemaOf(value);

            JToken.DeepEquals(JToken.Parse(schema), JToken.Parse(expected)).Should().BeTrue(schema);
        }

        // ------------------------------------------------------------------ dictionaries

        [Test]
        public void Dictionary_of_strings([ValueSource(typeof(Formats), nameof(Formats.WithHeader))] Format format)
        {
            var value = new Dictionary<string, object> { ["a"] = "x", ["b"] = "y" };

            RoundTrip.Run<Dictionary<string, object>>(value, format).Should().BeEquivalentTo(value);
        }

        [Test]
        public void Dictionary_of_mixed_values([ValueSource(typeof(Formats), nameof(Formats.WithHeader))] Format format)
        {
            var value = new Dictionary<string, object> { ["a"] = "x", ["b"] = 1L, ["c"] = true, ["d"] = 1.5 };

            RoundTrip.Run<Dictionary<string, object>>(value, format).Should().BeEquivalentTo(value);
        }

        [Test]
        public void Dictionary_with_a_null_value([ValueSource(typeof(Formats), nameof(Formats.WithHeader))] Format format)
        {
            var value = new Dictionary<string, object> { ["a"] = "x", ["b"] = null };

            RoundTrip.Run<Dictionary<string, object>>(value, format).Should().BeEquivalentTo(value);
        }

        [Test]
        public void Dictionary_of_nulls([ValueSource(typeof(Formats), nameof(Formats.WithHeader))] Format format)
        {
            var value = new Dictionary<string, object> { ["a"] = null };

            RoundTrip.Run<Dictionary<string, object>>(value, format).Should().BeEquivalentTo(value);
        }

        [Test]
        public void Empty_dictionary([ValueSource(typeof(Formats), nameof(Formats.WithHeader))] Format format)
        {
            RoundTrip.Run<Dictionary<string, object>>(new Dictionary<string, object>(), format).Should().BeEmpty();
        }

        [Test]
        public void Dictionary_holding_a_class_keeps_its_fields([ValueSource(typeof(Formats), nameof(Formats.WithHeader))] Format format)
        {
            var value = new Dictionary<string, object> { ["a"] = new Foo { Name = "n" } };

            ShouldBeJson(RoundTrip.Run<Dictionary<string, object>>(value, format), @"{""a"":{""Name"":""n""}}");
        }

        [Test]
        public void Dictionary_holding_different_classes([ValueSource(typeof(Formats), nameof(Formats.WithHeader))] Format format)
        {
            var value = new Dictionary<string, object> { ["a"] = new Foo { Name = "n" }, ["b"] = new Bar { Size = 2 }, ["c"] = "s" };

            ShouldBeJson(RoundTrip.Run<Dictionary<string, object>>(value, format), @"{""a"":{""Name"":""n""},""b"":{""Size"":2},""c"":""s""}");
        }

        [Test]
        public void Dictionary_in_a_dictionary([ValueSource(typeof(Formats), nameof(Formats.WithHeader))] Format format)
        {
            var value = new Dictionary<string, object>
            {
                ["a"] = new Dictionary<string, object> { ["x"] = "1", ["y"] = 2L },
                ["b"] = new Dictionary<string, object> { ["z"] = true }
            };

            ShouldBeJson(RoundTrip.Run<Dictionary<string, object>>(value, format), @"{""a"":{""x"":""1"",""y"":2},""b"":{""z"":true}}");
        }

        [Test]
        public void List_in_a_dictionary([ValueSource(typeof(Formats), nameof(Formats.WithHeader))] Format format)
        {
            var value = new Dictionary<string, object> { ["a"] = new List<object> { "x", 2L } };

            ShouldBeJson(RoundTrip.Run<Dictionary<string, object>>(value, format), @"{""a"":[""x"",2]}");
        }

        [Test]
        public void Other_dictionary_types()
        {
            IDictionary<string, object> first = new Dictionary<string, object> { ["a"] = "x" };
            var second = new SortedDictionary<string, object> { ["a"] = "x" };

            AvroConvert.Deserialize<Dictionary<string, object>>(AvroConvert.Serialize(first)).Should().BeEquivalentTo(first);
            AvroConvert.Deserialize<Dictionary<string, object>>(AvroConvert.Serialize(second)).Should().BeEquivalentTo(second);
        }

        [Test]
        public void Dictionary_with_non_string_keys()
        {
            var value = new Dictionary<int, object> { [1] = "x", [2] = 3L };

            AvroConvert.Deserialize<Dictionary<int, object>>(AvroConvert.Serialize(value)).Should().BeEquivalentTo(value);
        }

        // ------------------------------------------------------------------ the schema

        [Test]
        public void One_type_of_values_is_a_plain_map()
        {
            SchemaShouldBe(new Dictionary<string, object> { ["a"] = "x" }, @"{""type"":""map"",""values"":""string""}");
        }

        [Test]
        public void Several_types_of_values_are_a_union()
        {
            SchemaShouldBe(new Dictionary<string, object> { ["a"] = "x", ["b"] = 1L }, @"{""type"":""map"",""values"":[""string"",""long""]}");
        }

        [Test]
        public void Null_comes_first_in_the_union()
        {
            SchemaShouldBe(new Dictionary<string, object> { ["a"] = "x", ["b"] = null }, @"{""type"":""map"",""values"":[""null"",""string""]}");
        }

        [Test]
        public void Same_shape_gives_the_same_schema()
        {
            SchemaOf(new Dictionary<string, object> { ["a"] = "x" }).Should().Be(SchemaOf(new Dictionary<string, object> { ["b"] = "y", ["c"] = "z" }));
        }

        [Test]
        public void Schema_of_the_type_is_not_changed()
        {
            AvroConvert.GenerateSchema(typeof(Dictionary<string, object>)).Should().Contain("System");
        }

        // ------------------------------------------------------------------ values that must not change on the way

        [Test]
        public void Whole_numbers_and_fractions_keep_their_values()
        {
            var value = new Dictionary<string, object> { ["a"] = 1L, ["b"] = 1.5, ["c"] = 7, ["d"] = 2.5f };

            var result = AvroConvert.Deserialize<Dictionary<string, object>>(AvroConvert.Serialize(value));

            result.Should().BeEquivalentTo(value);
        }

        [Test]
        public void Dates_and_timestamps_keep_their_values()
        {
            var date = new DateOnly(2026, 10, 30);
            var timestamp = new DateTime(2026, 10, 30, 1, 2, 3, DateTimeKind.Utc);
            var value = new Dictionary<string, object> { ["a"] = date, ["b"] = timestamp };

            var result = AvroConvert.Deserialize<Dictionary<string, object>>(AvroConvert.Serialize(value));

            result["a"].Should().Be(date);
            result["b"].Should().Be(timestamp);
        }

        [Test]
        public void Other_types_of_values()
        {
            var guid = Guid.NewGuid();
            var value = new Dictionary<string, object> { ["a"] = guid, ["b"] = 12.34m, ["c"] = Color.Green };

            var result = AvroConvert.Deserialize<Dictionary<string, object>>(AvroConvert.Serialize(value));

            result["a"].Should().Be(guid);
            result["b"].Should().Be(12.34m);
            result["c"].Should().Be("Green");
        }

        [Test]
        public void Bytes_next_to_text()
        {
            var value = new Dictionary<string, object> { ["a"] = new byte[] { 1, 2 }, ["b"] = "x" };

            var result = AvroConvert.Deserialize<Dictionary<string, object>>(AvroConvert.Serialize(value));

            result["a"].Should().BeEquivalentTo(new byte[] { 1, 2 });
            result["b"].Should().Be("x");
        }

        [Test]
        public void Decimal_next_to_bytes_is_rejected()
        {
            var value = new Dictionary<string, object> { ["a"] = 1.5m, ["b"] = new byte[] { 1 } };

            var act = () => AvroConvert.Serialize(value);

            act.Should().Throw<Exception>().WithMessage("*Serialize(value, schema)*");
        }

        [Test]
        public void Enum_shared_with_a_class_is_defined_once()
        {
            var value = new Dictionary<string, object> { ["a"] = Color.Red, ["b"] = new Painted { Color = Color.Green } };

            var schema = SchemaOf(value);

            AvroSchema.Parse(schema).Should().NotBeNull();
            System.Text.RegularExpressions.Regex.Matches(schema, @"""symbols""").Count.Should().Be(1);
        }

        [Test]
        public void Class_shared_between_values_is_defined_once()
        {
            var value = new Dictionary<string, object> { ["a"] = new Foo { Name = "n" }, ["b"] = new Holder { Foo = new Foo { Name = "m" } } };

            var schema = SchemaOf(value);

            System.Text.RegularExpressions.Regex.Matches(schema, @"""fields""").Count.Should().Be(2);
            ShouldBeJson(AvroConvert.Deserialize<Dictionary<string, object>>(AvroConvert.Serialize(value)), @"{""a"":{""Name"":""n""},""b"":{""Foo"":{""Name"":""m""}}}");
        }

        [Test]
        public void Text_next_to_a_guid_stays_text()
        {
            var guid = Guid.NewGuid();
            var value = new Dictionary<string, object> { ["a"] = "not a guid", ["b"] = guid };

            var result = AvroConvert.Deserialize<Dictionary<string, object>>(AvroConvert.Serialize(value));

            result["a"].Should().Be("not a guid");
            result["b"].ToString().Should().Be(guid.ToString());
        }

        [Test]
        public void Values_avro_cannot_tell_apart_are_rejected()
        {
            var value = new Dictionary<string, object> { ["a"] = 1L, ["b"] = new DateTime(2026, 10, 30, 0, 0, 0, DateTimeKind.Utc) };

            var act = () => AvroConvert.Serialize(value);

            act.Should().Throw<Exception>().WithMessage("*Serialize(value, schema)*");
        }

        // ------------------------------------------------------------------ members

        [Test]
        public void Dictionary_member([ValueSource(typeof(Formats), nameof(Formats.WithHeader))] Format format)
        {
            var value = new WithDictionary { Id = 1, Props = new Dictionary<string, object> { ["a"] = "x", ["b"] = 2L } };

            RoundTrip.Run<WithDictionary>(value, format).Should().BeEquivalentTo(value);
        }

        [Test]
        public void Object_member_with_a_string([ValueSource(typeof(Formats), nameof(Formats.WithHeader))] Format format)
        {
            var value = new WithObject { Id = 1, Value = "x" };

            RoundTrip.Run<WithObject>(value, format).Should().BeEquivalentTo(value);
        }

        [Test]
        public void Object_member_with_a_class_keeps_its_fields([ValueSource(typeof(Formats), nameof(Formats.WithHeader))] Format format)
        {
            var value = new WithObject { Id = 1, Value = new Foo { Name = "n" } };

            ShouldBeJson(RoundTrip.Run<WithObject>(value, format), @"{""Id"":1,""Value"":{""Name"":""n""}}");
        }

        [Test]
        public void Object_member_with_null([ValueSource(typeof(Formats), nameof(Formats.WithHeader))] Format format)
        {
            RoundTrip.Run<WithObject>(new WithObject { Id = 1 }, format).Value.Should().BeNull();
        }

        [Test]
        public void ExpandoObject_member_keeps_its_fields([ValueSource(typeof(Formats), nameof(Formats.WithHeader))] Format format)
        {
            dynamic expando = new ExpandoObject();
            expando.Name = "n";
            var value = new WithExpando { Value = expando };

            ShouldBeJson(RoundTrip.Run<WithExpando>(value, format), @"{""Value"":{""Name"":""n""}}");
        }

        [Test]
        public void List_of_objects_member([ValueSource(typeof(Formats), nameof(Formats.WithHeader))] Format format)
        {
            var value = new WithList { Items = new List<object> { "x", 1L, new Foo { Name = "n" } } };

            ShouldBeJson(RoundTrip.Run<WithList>(value, format), @"{""Items"":[""x"",1,{""Name"":""n""}]}");
        }

        [Test]
        public void One_field_takes_the_types_of_all_instances([ValueSource(typeof(Formats), nameof(Formats.WithHeader))] Format format)
        {
            var value = new List<WithObject> { new() { Id = 1, Value = "x" }, new() { Id = 2, Value = 2L }, new() { Id = 3 } };

            ShouldBeJson(RoundTrip.Run<List<WithObject>>(value, format),
                @"[{""Id"":1,""Value"":""x""},{""Id"":2,""Value"":2},{""Id"":3,""Value"":null}]");
        }

        [Test]
        public void One_class_in_two_places_takes_the_types_of_both()
        {
            var value = new Outer { First = new Inner { Value = "x" }, Second = new Inner { Value = 2L } };

            var result = AvroConvert.Deserialize<Outer>(AvroConvert.Serialize(value));

            result.First.Value.Should().Be("x");
            result.Second.Value.Should().Be(2L);
        }

        [Test]
        public void Recursive_class_with_object_values()
        {
            var value = new Tree { Data = "root", Children = { new Tree { Data = 2L }, new Tree { Data = new Foo { Name = "n" } } } };

            ShouldBeJson(AvroConvert.Deserialize<Tree>(AvroConvert.Serialize(value)),
                @"{""Data"":""root"",""Children"":[{""Data"":2,""Children"":[]},{""Data"":{""Name"":""n""},""Children"":[]}]}");
        }

        // ------------------------------------------------------------------ collections

        [Test]
        public void List_of_objects([ValueSource(typeof(Formats), nameof(Formats.WithHeader))] Format format)
        {
            var value = new List<object> { "a", "b" };

            RoundTrip.Run<List<object>>(value, format).Should().BeEquivalentTo(value);
        }

        [Test]
        public void Array_of_mixed_objects([ValueSource(typeof(Formats), nameof(Formats.WithHeader))] Format format)
        {
            var value = new object[] { "a", 1L, true };

            RoundTrip.Run<object[]>(value, format).Should().BeEquivalentTo(value);
        }

        [Test]
        public void List_of_dictionaries([ValueSource(typeof(Formats), nameof(Formats.WithHeader))] Format format)
        {
            var value = new List<Dictionary<string, object>> { new() { ["a"] = "x" }, new() { ["b"] = 2L } };

            RoundTrip.Run<List<Dictionary<string, object>>>(value, format).Should().BeEquivalentTo(value);
        }

        // ------------------------------------------------------------------ every writer and reader

        private static readonly Dictionary<string, object>[] Dictionaries =
        {
            new() { ["a"] = "x", ["b"] = 1L },
            new() { ["c"] = null }
        };

        [Test]
        public void Dictionaries_through_every_writer_and_reader(
            [ValueSource(typeof(Formats), nameof(Formats.ContainerWriters))] ContainerWriter writer,
            [ValueSource(typeof(Formats), nameof(Formats.ContainerReaders))] ContainerReader reader)
        {
            var avro = RoundTrip.Write(writer, Dictionaries);

            RoundTrip.ReadAll<Dictionary<string, object>>(reader, avro).Should().BeEquivalentTo(Dictionaries);
        }

        private static readonly WithObject[] Objects =
        {
            new() { Id = 1, Value = "x" },
            new() { Id = 2, Value = 2L }
        };

        [Test]
        public void Object_members_through_every_writer_and_reader(
            [ValueSource(typeof(Formats), nameof(Formats.ContainerWriters))] ContainerWriter writer,
            [ValueSource(typeof(Formats), nameof(Formats.ContainerReaders))] ContainerReader reader)
        {
            var avro = RoundTrip.Write(writer, Objects);

            RoundTrip.ReadAll<WithObject>(reader, avro).Should().BeEquivalentTo(Objects);
        }

        [Test]
        public void Every_codec([ValueSource(typeof(Formats), nameof(Formats.Codecs))] CodecType codec)
        {
            var value = new Dictionary<string, object> { ["a"] = "x", ["b"] = 1L };

            AvroConvert.Deserialize<Dictionary<string, object>>(AvroConvert.Serialize(value, codec)).Should().BeEquivalentTo(value);
        }

        [Test]
        public void Files_with_the_same_schema_are_merged()
        {
            var one = AvroConvert.Serialize(new Dictionary<string, object> { ["a"] = "x" });

            var merged = AvroConvert.Merge<Dictionary<string, object>>(new[] { one, one });

            AvroConvert.Deserialize<List<Dictionary<string, object>>>(merged).Should().HaveCount(2);
        }

        // ------------------------------------------------------------------ headless

        [Test]
        public void Headless_data_needs_a_schema_for_a_string()
        {
            var act = () => AvroConvert.SerializeHeadless(new WithObject { Value = "x" }, typeof(WithObject));

            act.Should().Throw<Exception>().WithMessage("*AvroSchema.FromValue*");
        }

        [Test]
        public void Headless_data_does_not_lose_the_fields_of_a_class()
        {
            var act = () => AvroConvert.SerializeHeadless(new WithObject { Value = new Foo { Name = "n" } }, typeof(WithObject));

            act.Should().Throw<Exception>().WithMessage("*AvroSchema.FromValue*");
        }

        [Test]
        public void Dictionary_without_a_schema_is_not_written_headless()
        {
            var act = () => AvroConvert.SerializeHeadless(new Dictionary<string, object> { ["a"] = new Foo { Name = "n" } }, typeof(Dictionary<string, object>));

            act.Should().Throw<Exception>().WithMessage("*AvroSchema.FromValue*");
        }

        [Test]
        public void Headless_with_a_given_schema([ValueSource(typeof(Formats), nameof(Formats.HeadlessReaders))] HeadlessReader reader)
        {
            const string schema = @"{""type"":""map"",""values"":[""string"",""long""]}";
            var value = new Dictionary<string, object> { ["a"] = "x", ["b"] = 1L };

            var avro = AvroConvert.SerializeHeadless(value, schema);

            RoundTrip.ReadHeadless<Dictionary<string, object>>(reader, avro, schema).Should().BeEquivalentTo(value);
        }

        [Test]
        public void Given_schema_wins_over_the_values()
        {
            const string schema = @"{""type"":""map"",""values"":[""null"",""string""]}";
            var given = AvroSchema.Parse(schema);

            var avro = AvroConvert.Serialize(new Dictionary<string, object> { ["a"] = "x" }, given);

            AvroConvert.GetSchema(avro).Should().Contain("null");
        }
    }
}
