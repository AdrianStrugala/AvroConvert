using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Runtime.Serialization;
using AwesomeAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SolTechnology.Avro;

namespace AvroConvertComponentTests.FullSerializationAndDeserialization
{
    namespace FirstNamespace
    {
        public class Item
        {
            public int First { get; set; }
        }
    }

    namespace SecondNamespace
    {
        public class Item
        {
            public string Second { get; set; }
        }
    }

    /// <summary>
    /// The API matrix says little when the data is simple: what breaks writing values of type <c>object</c> are records that
    /// resemble each other, names that collide and values nested in values. Every shape here goes through every way of writing
    /// and reading, and has to come back as the JSON written next to it.
    /// </summary>
    public class ObjectValueShapesTests
    {
        public class Small
        {
            public int X { get; set; }
        }

        public class Large
        {
            public int X { get; set; }
            public string Y { get; set; }
        }

        [DataContract(Name = "ContractA")]
        public class ContractSmall
        {
            [DataMember]
            public string Name { get; set; }
        }

        [DataContract(Name = "ContractB")]
        public class ContractLarge
        {
            [DataMember]
            public string Name { get; set; }

            [DataMember]
            public string Extra { get; set; }
        }

        public class WithObject
        {
            public object Value { get; set; }
        }

        public class Outer
        {
            public WithObject Inner { get; set; }
            public object Other { get; set; }
        }

        private static ExpandoObject Expando(params (string Key, object Value)[] fields)
        {
            IDictionary<string, object> result = new ExpandoObject();
            foreach (var (key, value) in fields)
            {
                result[key] = value;
            }

            return (ExpandoObject)result;
        }

        private static Dictionary<string, object> Bag(params (string Key, object Value)[] fields) => fields.ToDictionary(f => f.Key, f => f.Value);

        /// <param name="PlainJson">What comes back through plain JSON, when it differs: its schema is inferred, so objects of an array merge and a whole number next to a fraction is a double.</param>
        public sealed record ObjectShape(string Name, object Value, string Json, string PlainJson = null)
        {
            public override string ToString() => Name;
        }

        private static ObjectShape Shape(string name, object value, string json, string plainJson = null) => new(name, value, json, plainJson);

        private static IEnumerable<ObjectShape> Shapes()
        {
            yield return Shape("records sharing a field", Bag(("a", new Small { X = 1 }), ("b", new Large { X = 2, Y = "s" })),
                @"{""a"":{""X"":1},""b"":{""X"":2,""Y"":""s""}}");
            yield return Shape("larger record first", Bag(("a", new Large { X = 2, Y = "s" }), ("b", new Small { X = 1 })),
                @"{""a"":{""X"":2,""Y"":""s""},""b"":{""X"":1}}");
            yield return Shape("anonymous records sharing a field", Bag(("a", new { X = 1 }), ("b", new { X = 2, Y = "s" })),
                @"{""a"":{""X"":1},""b"":{""X"":2,""Y"":""s""}}");
            yield return Shape("data contract names", Bag(("a", new ContractSmall { Name = "n" }), ("b", new ContractLarge { Name = "m", Extra = "e" })),
                @"{""a"":{""Name"":""n""},""b"":{""Name"":""m"",""Extra"":""e""}}");
            yield return Shape("one name in two namespaces", Bag(("a", new FirstNamespace.Item { First = 1 }), ("b", new SecondNamespace.Item { Second = "s" })),
                @"{""a"":{""First"":1},""b"":{""Second"":""s""}}");
            yield return Shape("record next to a primitive", Bag(("a", new Small { X = 1 }), ("b", "text"), ("c", 2L), ("d", null)),
                @"{""a"":{""X"":1},""b"":""text"",""c"":2,""d"":null}");
            yield return Shape("record next to a dictionary", Bag(("a", new Small { X = 1 }), ("b", Bag(("X", 2L), ("Z", "s")))),
                @"{""a"":{""X"":1},""b"":{""X"":2,""Z"":""s""}}");
            yield return Shape("dictionaries of different shapes", Bag(("a", Bag(("x", "1"))), ("b", Bag(("y", 2L))), ("c", Bag())),
                @"{""a"":{""x"":""1""},""b"":{""y"":2},""c"":{}}");
            yield return Shape("lists of different items", Bag(("a", new List<object> { "x", 1L }), ("b", new List<object> { new Small { X = 1 } })),
                @"{""a"":[""x"",1],""b"":[{""X"":1}]}");
            yield return Shape("list of lists", new WithObject { Value = new List<object> { new List<object> { 1L }, new List<object> { "x" } } },
                @"{""Value"":[[1],[""x""]]}");
            yield return Shape("object in an object", new Outer { Inner = new WithObject { Value = "x" }, Other = new WithObject { Value = 2L } },
                @"{""Inner"":{""Value"":""x""},""Other"":{""Value"":2}}");
            yield return Shape("one class holding different values", Bag(("a", new WithObject { Value = "x" }), ("b", new WithObject { Value = new Small { X = 1 } })),
                @"{""a"":{""Value"":""x""},""b"":{""Value"":{""X"":1}}}");
            yield return Shape("collection of records sharing a field", new List<object> { new Small { X = 1 }, new Large { X = 2, Y = "s" } },
                @"[{""X"":1},{""X"":2,""Y"":""s""}]", @"[{""X"":1,""Y"":null},{""X"":2,""Y"":""s""}]");
            yield return Shape("expando in a dictionary", Bag(("a", Expando(("x", "1"), ("y", 2L)))),
                @"{""a"":{""x"":""1"",""y"":2}}");
            yield return Shape("expando next to a record", Bag(("a", Expando(("Z", "s"))), ("b", new Small { X = 1 })),
                @"{""a"":{""Z"":""s""},""b"":{""X"":1}}");
            yield return Shape("list of different items in an expando", Expando(("a", new List<object> { "x", 1L }), ("b", 2L)),
                @"{""a"":[""x"",1],""b"":2}");
            yield return Shape("whole numbers next to fractions", Bag(("a", new List<object> { 1L, 2.5 })),
                @"{""a"":[1,2.5]}", @"{""a"":[1.0,2.5]}");
            yield return Shape("nulls among the items", Bag(("a", new List<object> { "x", null, 1L })),
                @"{""a"":[""x"",null,1]}");
            yield return Shape("one field name with different fields in two expandos", Expando(("a", Expando(("x", Expando(("p", 1L))))), ("b", Expando(("x", Expando(("q", "s")))))),
                @"{""a"":{""x"":{""p"":1}},""b"":{""x"":{""q"":""s""}}}");
            yield return Shape("dictionary in an expando", Expando(("a", Bag(("x", "1")))),
                @"{""a"":{""x"":""1""}}");
            yield return Shape("record in an expando", Expando(("a", new WithObject { Value = "x" })),
                @"{""a"":{""Value"":""x""}}");
        }

        private static IEnumerable<ObjectShape> SingleValueShapes() => Shapes().Where(shape => shape.Value is not List<object>);

        private static void ShouldBeJson(object actual, string expected)
        {
            var json = JsonConvert.SerializeObject(actual);

            JToken.DeepEquals(JToken.Parse(json), JToken.Parse(expected)).Should().BeTrue($"{expected} was expected, {json} came back");
        }

        private static IEnumerable<object> Entries(object value) => value is List<object> list ? list : new[] { value };

        private static object Single(List<object> entries, object value) => value is List<object> ? entries : entries.Single();

        [Test]
        public void Every_format(
            [ValueSource(nameof(Shapes))] ObjectShape shape,
            [ValueSource(typeof(Formats), nameof(Formats.WithHeader))] Format format)
        {
            ShouldBeJson(shape.Value is List<object> ? RoundTrip.ReadAs<List<object>>(shape.Value, format) : RoundTrip.ReadAs<dynamic>(shape.Value, format), format == Format.PlainJson ? shape.PlainJson ?? shape.Json : shape.Json);
        }

        [Test]
        public void Every_codec(
            [ValueSource(nameof(Shapes))] ObjectShape shape,
            [ValueSource(typeof(Formats), nameof(Formats.Codecs))] CodecType codec)
        {
            var avro = AvroConvert.Serialize(shape.Value, codec);

            ShouldBeJson(shape.Value is List<object> ? AvroConvert.Deserialize<List<object>>(avro) : AvroConvert.Deserialize<dynamic>(avro), shape.Json);
        }

        [Test]
        public void Every_writer_and_reader(
            [ValueSource(nameof(Shapes))] ObjectShape shape,
            [ValueSource(typeof(Formats), nameof(Formats.ContainerWriters))] ContainerWriter writer,
            [ValueSource(typeof(Formats), nameof(Formats.ContainerReaders))] ContainerReader reader)
        {
            var value = shape.Value;

            var avro = RoundTrip.Write(writer, Entries(value));

            ShouldBeJson(Single(RoundTrip.ReadAll<object>(reader, avro), value), shape.Json);
        }

        [Test]
        public void Every_headless_reader(
            [ValueSource(nameof(Shapes))] ObjectShape shape,
            [ValueSource(typeof(Formats), nameof(Formats.HeadlessReaders))] HeadlessReader reader)
        {
            var value = shape.Value;
            var schema = AvroSchema.FromValue(value);

            var avro = AvroConvert.SerializeHeadless(value, schema);

            ShouldBeJson(RoundTrip.ReadHeadless<object>(reader, avro, schema.Json), shape.Json);
        }

        [Test]
        public void Schema_of_a_value_writes_the_value(
            [ValueSource(nameof(Shapes))] ObjectShape shape,
            [ValueSource(typeof(Formats), nameof(Formats.ContainerWriters))] ContainerWriter writer)
        {
            var value = shape.Value;
            var entries = Entries(value);
            var schema = value is List<object> ? AvroSchema.FromAvro(AvroConvert.Serialize(value)) : AvroSchema.FromValue(value);

            var avro = RoundTrip.Write(writer, entries, schema);

            ShouldBeJson(Single(AvroConvert.Deserialize<List<object>>(avro), value), shape.Json);
        }

        [Test]
        public void Reading_into_a_dictionary_gives_the_same_data([ValueSource(nameof(SingleValueShapes))] ObjectShape shape)
        {
            ShouldBeJson(AvroConvert.Deserialize<Dictionary<string, object>>(AvroConvert.Serialize(shape.Value)), shape.Json);
        }

        [Test]
        public void Schema_is_valid_for_other_tools([ValueSource(nameof(Shapes))] ObjectShape shape)
        {
            var schema = AvroSchema.FromAvro(AvroConvert.Serialize(shape.Value)).Json;

            AvroSchema.Parse(schema).Json.Should().Be(schema);
        }
    }
}
