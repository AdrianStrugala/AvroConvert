using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using AwesomeAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SolTechnology.Avro;
using SolTechnology.Avro.Infrastructure.Attributes;

namespace AvroConvertComponentTests.FullSerializationAndDeserialization
{
    public class ObjectValueRegressionTests
    {
        public class Node
        {
            public object Next { get; set; }
        }

        public class TypedNode
        {
            public int Value { get; set; }
            public TypedNode Next { get; set; }
        }

        public class WithObject
        {
            public object Value { get; set; }
        }

        public class WithDictionary
        {
            public int Id { get; set; }
            public Dictionary<string, object> Props { get; set; }
        }

        public class Base
        {
            public object Value { get; set; }
        }

        public class Derived : Base
        {
            public int Extra { get; set; }
        }

        public class UnionHolder
        {
            [AvroUnion(typeof(Guid), typeof(string))]
            public object Text { get; set; }

            [AvroUnion(typeof(long), typeof(DateTime))]
            public object Number { get; set; }
        }

        public class FirstOpen
        {
            public object O { get; set; }
        }

        public class SecondOpen
        {
            public object O { get; set; }
            public int Extra { get; set; }
        }

        public class OpenUnion
        {
            [AvroUnion(typeof(FirstOpen), typeof(SecondOpen))]
            public object V { get; set; }
        }

        public class Short
        {
            public int X { get; set; }
        }

        public class Long
        {
            public int X { get; set; }
            public string Y { get; set; }
        }

        // ------------------------------------------------------------------ cycles and bare objects

        [Test]
        public void Cycle_through_an_object_member_is_an_exception()
        {
            var node = new Node();
            node.Next = node;

            Recorded.Exception(() => AvroConvert.Serialize(node)).Should().NotBeNull();
        }

        [Test]
        public void Cycle_through_a_typed_member_is_an_exception()
        {
            var node = new TypedNode();
            node.Next = node;

            Recorded.Exception(() => AvroConvert.Serialize(node)).Should().NotBeNull();
        }

        [Test]
        public void Dictionary_holding_itself_is_an_exception()
        {
            var dictionary = new Dictionary<string, object>();
            dictionary["self"] = dictionary;

            Recorded.Exception(() => AvroConvert.Serialize(dictionary)).Should().NotBeNull();
        }

        [Test]
        public void List_holding_itself_is_an_exception()
        {
            var list = new List<object>();
            list.Add(list);

            Recorded.Exception(() => AvroConvert.Serialize(list)).Should().NotBeNull();
        }

        [Test]
        public void Value_with_no_data_writes_nothing()
        {
            var avro = AvroConvert.Serialize(new Dictionary<string, object> { ["a"] = new object() });

            AvroConvert.Deserialize<Dictionary<string, object>>(avro).Keys.Should().Equal("a");
        }

        [Test]
        public void Value_with_no_data_next_to_others_is_an_exception()
        {
            var act = () => AvroConvert.Serialize(new Dictionary<string, object> { ["a"] = new object(), ["b"] = "x" });

            act.Should().Throw<Exception>().WithMessage("*System.Object*");
        }

        // ------------------------------------------------------------------ unions

        [TestCase(@"[""null"",""long"",{""type"":""long"",""logicalType"":""timestamp-millis""}]")]
        [TestCase(@"[""string"",{""type"":""string"",""logicalType"":""uuid""}]")]
        [TestCase(@"[{""type"":""int"",""logicalType"":""date""},{""type"":""long"",""logicalType"":""timestamp-micros""}]")]
        [TestCase(@"[{""type"":""long"",""logicalType"":""timestamp-millis""},{""type"":""long"",""logicalType"":""timestamp-micros""}]")]
        public void Union_of_logical_types_is_parsed(string schema)
        {
            AvroSchema.Parse(schema).Json.Should().Be(schema);
        }

        [TestCase(@"[""string"",""string""]")]
        [TestCase(@"[{""type"":""long"",""logicalType"":""timestamp-millis""},{""type"":""long"",""logicalType"":""timestamp-millis""}]")]
        public void Union_with_a_repeated_branch_is_rejected(string schema)
        {
            Recorded.Exception(() => AvroSchema.Parse(schema)).Should().NotBeNull();
        }

        [Test]
        public void Union_of_a_logical_type_and_its_base_type_is_read_back()
        {
            var guid = Guid.NewGuid();
            var timestamp = new DateTime(2026, 10, 30, 1, 2, 3, DateTimeKind.Utc);

            var result = AvroConvert.Deserialize<UnionHolder>(AvroConvert.Serialize(new UnionHolder { Text = guid, Number = timestamp }));

            result.Text.Should().Be(guid);
            result.Number.Should().Be(timestamp);
        }

        [Test]
        public void Avro2Json_reads_the_branch_of_a_union_of_logical_types()
        {
            var value = new Dictionary<string, object> { ["a"] = new DateOnly(2026, 10, 30), ["b"] = new DateTime(2026, 10, 30, 1, 2, 3, DateTimeKind.Utc) };

            var json = JObject.Parse(AvroConvert.Avro2Json(AvroConvert.Serialize(value)));

            json.Value<string>("a").Should().Contain("2026");
            json["b"].ToObject<DateTime>().Hour.Should().Be(1);
        }

        [Test]
        public void Records_of_one_name_in_two_namespaces_are_parsed()
        {
            const string schema = @"[{""name"":""Item"",""namespace"":""a"",""type"":""record"",""fields"":[]},{""name"":""Item"",""namespace"":""b"",""type"":""record"",""fields"":[]}]";

            AvroSchema.Parse(schema).Json.Should().Be(schema);
        }

        [Test]
        public void Union_of_open_records_completes_the_branch_of_the_value()
        {
            var value = new OpenUnion { V = new SecondOpen { O = "x", Extra = 1 } };

            var result = AvroConvert.Deserialize<OpenUnion>(AvroConvert.Serialize(value));

            result.V.Should().BeOfType<SecondOpen>().Which.O.Should().Be("x");
        }

        [Test]
        public void Class_takes_the_record_with_all_its_members_from_a_given_union()
        {
            const string schema = @"[{""type"":""record"",""name"":""A"",""fields"":[{""name"":""X"",""type"":""int""}]},{""type"":""record"",""name"":""B"",""fields"":[{""name"":""X"",""type"":""int""},{""name"":""Y"",""type"":""string""}]}]";

            var avro = AvroConvert.SerializeHeadless(new Long { X = 1, Y = "s" }, schema);

            AvroConvert.DeserializeHeadless<Dictionary<string, object>>(avro, schema)["Y"].Should().Be("s");
        }

        private const string RecordsWithDefault = @"[{""type"":""record"",""name"":""A"",""fields"":[{""name"":""a"",""type"":""int"",""default"":1}]},{""type"":""record"",""name"":""B"",""fields"":[{""name"":""b"",""type"":""int""}]}]";

        [Test]
        public void Dictionary_takes_the_record_with_its_keys_from_a_given_union()
        {
            var avro = AvroConvert.SerializeHeadless(new Dictionary<string, object> { ["b"] = 2 }, RecordsWithDefault);

            AvroConvert.DeserializeHeadless<Dictionary<string, object>>(avro, RecordsWithDefault).Should().BeEquivalentTo(new Dictionary<string, object> { ["b"] = 2 });
        }

        [Test]
        public void ExpandoObject_takes_the_record_with_its_keys_from_a_given_union()
        {
            dynamic value = new ExpandoObject();
            value.b = 2;

            var avro = AvroConvert.SerializeHeadless((object)value, RecordsWithDefault);

            AvroConvert.DeserializeHeadless<Dictionary<string, object>>(avro, RecordsWithDefault).Should().BeEquivalentTo(new Dictionary<string, object> { ["b"] = 2 });
        }

        [Test]
        public void Json_object_takes_the_record_with_its_keys_from_a_given_union()
        {
            var avro = AvroConvert.SerializeHeadless(JObject.Parse(@"{""b"":2}"), RecordsWithDefault);

            AvroConvert.DeserializeHeadless<Dictionary<string, object>>(avro, RecordsWithDefault).Should().BeEquivalentTo(new Dictionary<string, object> { ["b"] = 2 });
        }

        [Test]
        public void Dictionary_fitting_no_record_of_a_given_union_is_rejected()
        {
            var act = () => AvroConvert.SerializeHeadless(new Dictionary<string, object> { ["c"] = 2 }, @"[{""type"":""record"",""name"":""B"",""fields"":[{""name"":""b"",""type"":""int""}]},{""type"":""record"",""name"":""C"",""fields"":[{""name"":""d"",""type"":""int""}]}]");

            act.Should().Throw<Exception>();
        }

        // ------------------------------------------------------------------ field names

        private const string Renamed = @"{""type"":""record"",""name"":""R"",""fields"":[{""name"":""n"",""aliases"":[""m""],""type"":""int"",""default"":7}]}";

        [Test]
        public void Dictionary_is_matched_by_the_name_of_a_field()
        {
            var read = new Dictionary<string, object> { ["n"] = 42 };

            var avro = AvroConvert.Serialize(read, AvroSchema.Parse(Renamed));

            AvroConvert.Deserialize<Dictionary<string, object>>(avro)["n"].Should().Be(42);
        }

        [Test]
        public void Dictionary_is_matched_by_an_alias_of_a_field_too()
        {
            var avro = AvroConvert.Serialize(new Dictionary<string, object> { ["m"] = 42 }, AvroSchema.Parse(Renamed));

            AvroConvert.Deserialize<Dictionary<string, object>>(avro)["n"].Should().Be(42);
        }

        [Test]
        public void Json_object_is_matched_by_the_name_of_a_field()
        {
            var avro = AvroConvert.Serialize(JObject.Parse(@"{""n"":42}"), AvroSchema.Parse(Renamed));

            AvroConvert.Deserialize<Dictionary<string, object>>(avro)["n"].Should().Be(42);
        }

        // ------------------------------------------------------------------ headless without a schema

        [Test]
        public void Class_with_an_empty_dictionary_is_read_without_a_schema()
        {
            var value = new WithDictionary { Id = 5, Props = new Dictionary<string, object>() };

            var avro = AvroConvert.SerializeHeadless(value, typeof(WithDictionary));

            AvroConvert.DeserializeHeadless<WithDictionary>(avro).Id.Should().Be(5);
        }

        [Test]
        public void Data_left_after_reading_without_a_schema_is_an_error()
        {
            var avro = AvroConvert.SerializeHeadless(new Dictionary<string, object> { ["a"] = "" }, @"{""type"":""map"",""values"":""string""}");

            var act = () => AvroConvert.DeserializeHeadless<Dictionary<string, object>>(avro);

            act.Should().Throw<Exception>().WithMessage("*DeserializeHeadless<T>(bytes, schema)*");
        }

        // ------------------------------------------------------------------ streaming

        [Test]
        public void Empty_sequence_keeps_the_schema_of_the_type([ValueSource(typeof(Formats), nameof(Formats.ContainerWriters))] ContainerWriter writer)
        {
            var avro = RoundTrip.Write(writer, Array.Empty<WithObject>());

            AvroSchema.FromAvro(avro).Json.Should().Contain(nameof(WithObject));
        }

        [Test]
        public void Derived_items_are_written_as_the_declared_type([ValueSource(typeof(Formats), nameof(Formats.ContainerWriters))] ContainerWriter writer)
        {
            var items = new[] { new Base { Value = "x" }, new Derived { Value = 2L, Extra = 1 } };

            var avro = RoundTrip.Write<Base>(writer, items);

            AvroConvert.Deserialize<List<Base>>(avro).Select(i => i.Value).Should().Equal("x", 2L);
        }

        [Test]
        public void Items_of_different_types_are_entries_of_one_union([ValueSource(typeof(Formats), nameof(Formats.ContainerWriters))] ContainerWriter writer)
        {
            var avro = RoundTrip.Write<object>(writer, new object[] { "a", 1L, null });

            AvroConvert.Deserialize<List<object>>(avro).Should().Equal("a", 1L, null);
        }

        [Test]
        public void ExpandoObject_is_a_record_whatever_writes_it()
        {
            dynamic value = new ExpandoObject();
            value.X = 1;
            var items = new List<object> { value };
            using var stream = new MemoryStream();

            AvroConvert.Serialize<object>(items, stream);
            var bytes = AvroConvert.Serialize(items);

            AvroConvert.GetSchema(bytes).Should().Be(AvroConvert.GetSchema(stream.ToArray()));
            AvroConvert.Deserialize<List<Short>>(bytes).Single().X.Should().Be(1);
        }

        [Test]
        public void ExpandoObjects_of_one_place_are_merged()
        {
            dynamic first = new ExpandoObject();
            first.x = 1;
            dynamic second = new ExpandoObject();
            second.y = "s";
            var value = new Dictionary<string, object> { ["a"] = first, ["b"] = second };

            var result = AvroConvert.Deserialize<Dictionary<string, object>>(AvroConvert.Serialize(value));

            ((IDictionary<string, object>)result["a"])["x"].Should().Be(1);
            ((IDictionary<string, object>)result["b"])["y"].Should().Be("s");
        }

        [Test]
        public void ExpandoObjects_of_two_places_keep_their_own_fields()
        {
            dynamic first = new ExpandoObject();
            first.x = 1;
            dynamic second = new ExpandoObject();
            second.y = "s";
            var value = new List<object> { new Dictionary<string, object> { ["a"] = first }, new WithObject { Value = second } };

            var avro = AvroConvert.Serialize(value);

            AvroConvert.Deserialize<List<object>>(avro).Should().HaveCount(2);
            AvroConvert.GetSchema(avro).Should().Contain(@"""x""").And.Contain(@"""y""");
        }

        // ------------------------------------------------------------------ enumerating once

        [Test]
        public void Lazy_sequence_is_enumerated_once()
        {
            var calls = 0;
            var items = new List<object> { "a", "b" }.Where(_ => { calls++; return true; });

            var avro = AvroConvert.Serialize(items);

            calls.Should().Be(2);
            AvroConvert.Deserialize<List<object>>(avro).Should().Equal("a", "b");
        }

        [Test]
        public void Lazy_sequence_of_classes_with_object_values_is_enumerated_once()
        {
            var calls = 0;
            var items = new List<WithObject> { new() { Value = "a" }, new() { Value = 2L } }.Where(_ => { calls++; return true; });

            AvroConvert.Serialize(items);

            calls.Should().Be(2);
        }

        [Test]
        public void Json_with_one_key_holding_different_objects_keeps_their_fields()
        {
            const string json = @"{""a"":{""x"":{""p"":1}},""b"":{""x"":{""q"":""s""}}}";

            var result = AvroConvert.Avro2Json(AvroConvert.Json2Avro(json));

            JToken.DeepEquals(JToken.Parse(result), JToken.Parse(json)).Should().BeTrue(result);
        }

        [Test]
        public void Json_with_one_key_holding_equal_objects_has_one_record()
        {
            var schema = AvroSchema.InferFromJson(@"{""a"":{""x"":{""p"":1}},""b"":{""x"":{""p"":2}}}").Json;

            System.Text.RegularExpressions.Regex.Matches(schema, @"""name"":""x""").Count.Should().BeGreaterThan(0);
            System.Text.RegularExpressions.Regex.Matches(schema, @"""name"":""p""").Count.Should().Be(1);
        }

        // ------------------------------------------------------------------ attributes as they are

        [TestCase(@"""2026-10-30T10:00:00+02:00""", TestName = "Text that looks like a date")]
        [TestCase(@"""1.10""", TestName = "Text that looks like a number")]
        [TestCase(@"2147483648", TestName = "Large number")]
        public void Attribute_stays_as_it_was_written(string value)
        {
            var schema = $@"{{""type"":""array"",""items"":""int"",""owner"":{value}}}";

            AvroSchema.Parse(schema).Json.Should().Be(schema);
        }

        [Test]
        public void Default_that_looks_like_a_date_stays_text()
        {
            const string schema = @"{""name"":""R"",""type"":""record"",""fields"":[{""name"":""a"",""type"":""string"",""default"":""2026-10-30T10:00:00+02:00""}]}";

            AvroSchema.Parse(schema).Json.Should().Be(schema);
        }

        // ------------------------------------------------------------------ one name, one definition

        public class Envelope
        {
            public object Before { get; set; }
            public object After { get; set; }
        }

        public class Order
        {
            public enum Status { New, Paid }

            public Status State { get; set; }
            public Shipment Ship { get; set; }
        }

        public class Shipment
        {
            public enum Status { Pending, Sent, Delivered }

            public Status State { get; set; }
        }

        [Test]
        public void ExpandoObject_holding_itself_is_an_exception()
        {
            dynamic expando = new ExpandoObject();
            expando.Self = expando;

            Recorded.Exception(() => AvroConvert.Serialize((ExpandoObject)expando)).Should().NotBeNull();
        }

        [Test]
        public void ExpandoObject_holding_itself_in_an_object_member_is_an_exception()
        {
            dynamic expando = new ExpandoObject();
            expando.Self = expando;

            Recorded.Exception(() => AvroConvert.Serialize(new WithObject { Value = expando })).Should().NotBeNull();
        }

        [Test]
        public void ExpandoObjects_with_one_key_in_two_object_members_keep_their_fields()
        {
            dynamic before = new ExpandoObject();
            before.address = new ExpandoObject();
            before.address.street = "a";
            dynamic after = new ExpandoObject();
            after.address = new ExpandoObject();
            after.address.street = "b";
            after.address.city = "c";

            var avro = AvroConvert.Serialize(new Envelope { Before = before, After = after });

            var result = AvroConvert.Deserialize<Dictionary<string, object>>(avro);
            ((IDictionary<string, object>)((IDictionary<string, object>)result["After"])["address"]).Should().ContainKey("city").WhoseValue.Should().Be("c");
            ((IDictionary<string, object>)((IDictionary<string, object>)result["Before"])["address"]).Should().ContainKey("street").WhoseValue.Should().Be("a");
        }

        [Test]
        public void ExpandoObjects_with_different_nested_objects_keep_all_fields()
        {
            dynamic first = new ExpandoObject();
            first.address = new ExpandoObject();
            first.address.street = "a";
            dynamic second = new ExpandoObject();
            second.address = new ExpandoObject();
            second.address.city = "c";

            var avro = AvroConvert.Serialize(new List<ExpandoObject> { first, second });

            var result = AvroConvert.Deserialize<List<Dictionary<string, object>>>(avro);
            ((IDictionary<string, object>)result[0]["address"])["street"].Should().Be("a");
            ((IDictionary<string, object>)result[1]["address"])["city"].Should().Be("c");
        }

        [Test]
        public void Json_array_with_different_nested_objects_keeps_all_fields()
        {
            const string json = @"[{""a"":{""x"":1}},{""a"":{""y"":""s""}}]";

            var result = AvroConvert.Avro2Json(AvroConvert.Json2Avro(json));

            JToken.Parse(result).SelectToken("[1].a.y")!.Value<string>().Should().Be("s");
        }

        [Test]
        public void Two_types_of_one_name_with_different_definitions_are_an_exception()
        {
            var order = new Order { State = Order.Status.Paid, Ship = new Shipment { State = Shipment.Status.Delivered } };

            var act = () => AvroConvert.Serialize(order);

            act.Should().Throw<System.Runtime.Serialization.SerializationException>().WithMessage("*Status*");
        }
    }
}
