using System.Collections.Generic;
using System.Dynamic;
using AwesomeAssertions;
using NUnit.Framework;
using SolTechnology.Avro;
using SolTechnology.Avro.Policies;

namespace AvroConvertComponentTests.FullSerializationAndDeserialization
{
    /// <summary>
    /// A null value against a schema that cannot hold null: the Avro specification has no representation for it,
    /// so it throws by default; <see cref="AvroNullValueHandling.WriteEmpty"/> writes the schema's empty value (3.x behaviour).
    /// </summary>
    public class NullValueHandlingTests
    {
        private static readonly AvroConvertOptions WriteEmpty = new() { NullValueHandling = AvroNullValueHandling.WriteEmpty };

        public enum Color { Red, Green }

        public class Holder<T>
        {
            public T Value { get; set; }
        }

        private static string Schema(string valueType) =>
            "{\"type\":\"record\",\"name\":\"Holder\",\"fields\":[{\"name\":\"Value\",\"type\":" + valueType + "}]}";

        private static IEnumerable<TestCaseData> NonNullableSchemas()
        {
            yield return new TestCaseData(typeof(string), "\"string\"", "").SetName("string");
            yield return new TestCaseData(typeof(int?), "\"int\"", 0).SetName("int");
            yield return new TestCaseData(typeof(long?), "\"long\"", 0L).SetName("long");
            yield return new TestCaseData(typeof(double?), "\"double\"", 0d).SetName("double");
            yield return new TestCaseData(typeof(bool?), "\"boolean\"", false).SetName("boolean");
            yield return new TestCaseData(typeof(byte[]), "\"bytes\"", new byte[0]).SetName("bytes");
            yield return new TestCaseData(typeof(List<int>), "{\"type\":\"array\",\"items\":\"int\"}", new List<int>()).SetName("array");
            yield return new TestCaseData(typeof(Dictionary<string, int>), "{\"type\":\"map\",\"values\":\"int\"}", new Dictionary<string, int>()).SetName("map");
            yield return new TestCaseData(typeof(Color?), "{\"type\":\"enum\",\"name\":\"Color\",\"symbols\":[\"Red\",\"Green\"]}", null).SetName("enum");
            yield return new TestCaseData(typeof(decimal?), "{\"type\":\"bytes\",\"logicalType\":\"decimal\",\"precision\":10,\"scale\":2}", null).SetName("decimal");
        }

        [TestCaseSource(nameof(NonNullableSchemas))]
        public void Null_against_non_nullable_schema_throws_with_field_and_schema_in_message(System.Type type, string valueSchema, object _)
        {
            //Arrange
            var holder = System.Activator.CreateInstance(typeof(Holder<>).MakeGenericType(type));

            //Act
            var exception = Recorded.Exception(() => AvroConvert.SerializeHeadless(holder, Schema(valueSchema)));

            //Assert
            exception.Should().NotBeNull();
            exception.Message.Should().StartWith("Field 'Holder.Value' is null, but its schema")
                .And.Contain("does not allow null")
                .And.Contain("[NullableSchema]")
                .And.Contain("AvroNullValueHandling.WriteEmpty");
        }

        /// <param name="empty">Value read back; null for schemas without an empty value (enum, logical types), which throw as in 3.x.</param>
        [TestCaseSource(nameof(NonNullableSchemas))]
        public void Null_against_non_nullable_schema_with_WriteEmpty_reads_back_the_empty_value(System.Type type, string valueSchema, object empty)
        {
            //Arrange
            var holderType = typeof(Holder<>).MakeGenericType(type);
            var holder = System.Activator.CreateInstance(holderType);
            var schema = Schema(valueSchema);

            //Act
            byte[] bytes = null;
            var exception = Recorded.Exception(() => bytes = AvroConvert.SerializeHeadless(holder, schema, WriteEmpty));

            //Assert
            if (empty == null)
            {
                exception.Should().NotBeNull();
                exception.Message.Should().Contain("but found [null] type");
                return;
            }

            exception.Should().BeNull();
            object value = holderType.GetProperty("Value")!.GetValue(AvroConvert.DeserializeHeadless(bytes, schema, holderType));
            value.Should().BeEquivalentTo(empty);
        }

        [Test]
        public void Null_against_union_with_null_is_written_as_null_in_both_modes()
        {
            //Arrange
            var schema = Schema("[\"null\",\"string\"]");

            //Act
            var strict = AvroConvert.DeserializeHeadless<Holder<string>>(AvroConvert.SerializeHeadless(new Holder<string>(), schema), schema);
            var empty = AvroConvert.DeserializeHeadless<Holder<string>>(AvroConvert.SerializeHeadless(new Holder<string>(), schema, WriteEmpty), schema);

            //Assert
            strict.Value.Should().BeNull();
            empty.Value.Should().BeNull();
        }

        [Test]
        public void Null_nested_record_throws_with_record_name()
        {
            //Arrange
            var schema = Schema("{\"type\":\"record\",\"name\":\"Inner\",\"fields\":[{\"name\":\"A\",\"type\":\"int\"}]}");

            //Act
            var exception = Recorded.Exception(() => AvroConvert.SerializeHeadless(new Holder<Holder<int>>(), schema));

            //Assert
            exception.Message.Should().StartWith("Field 'Holder.Value' is null, but its schema record Inner does not allow null");
        }

        [Test]
        public void Null_element_of_a_collection_points_at_the_element()
        {
            //Arrange
            var holder = new Holder<List<string>> { Value = new List<string> { "a", null } };

            //Act
            var exception = Recorded.Exception(() => AvroConvert.SerializeHeadless(holder, Schema("{\"type\":\"array\",\"items\":\"string\"}")));

            //Assert
            exception.Message.Should().StartWith("Field 'Holder.Value[]' is null");
        }

        [Test]
        public void Null_map_value_points_at_the_value()
        {
            //Arrange
            var holder = new Holder<Dictionary<string, string>> { Value = new Dictionary<string, string> { ["k"] = null } };

            //Act
            var exception = Recorded.Exception(() => AvroConvert.SerializeHeadless(holder, Schema("{\"type\":\"map\",\"values\":\"string\"}")));

            //Assert
            exception.Message.Should().StartWith("Field 'Holder.Value[key]' is null");
        }

        [Test]
        public void Null_root_value_throws_and_WriteEmpty_writes_empty_record()
        {
            //Arrange
            var schema = Schema("\"string\"");

            //Act
            var exception = Recorded.Exception(() => AvroConvert.SerializeHeadless(null, schema));
            var bytes = AvroConvert.SerializeHeadless(null, schema, WriteEmpty);

            //Assert
            exception.Message.Should().StartWith("The value is null, but its schema record Holder does not allow null");
            AvroConvert.DeserializeHeadless<Holder<string>>(bytes, schema).Value.Should().BeEmpty();
        }

        [Test]
        public void Null_item_in_a_serialized_collection_throws()
        {
            //Arrange
            var items = new List<Holder<string>> { new() { Value = "a" }, null };

            //Act
            var exception = Recorded.Exception(() => AvroConvert.Serialize(items));

            //Assert
            exception.Message.Should().StartWith("The value is null, but its schema record Holder");
        }

        [Test]
        public void Field_without_a_member_on_the_type_throws_unless_it_can_be_null()
        {
            //Arrange
            var schema = "{\"type\":\"record\",\"name\":\"Holder\",\"fields\":[" +
                         "{\"name\":\"Value\",\"type\":\"string\"}," +
                         "{\"name\":\"Optional\",\"type\":[\"null\",\"int\"]}," +
                         "{\"name\":\"Required\",\"type\":\"int\"}]}";

            //Act
            var exception = Recorded.Exception(() => AvroConvert.SerializeHeadless(new Holder<string> { Value = "x" }, schema));
            var bytes = AvroConvert.SerializeHeadless(new Holder<string> { Value = "x" }, schema, WriteEmpty);

            //Assert
            exception.Message.Should().StartWith("Field 'Holder.Required' has no readable member in");
            AvroConvert.Avro2Json(bytes, schema).Should().Be("{\"Value\":\"x\",\"Optional\":null,\"Required\":0}");
        }

        [Test]
        public void Null_in_object_member_throws_against_non_nullable_schema()
        {
            //Arrange
            var holder = new Holder<object> { Value = null };

            //Act
            var exception = Recorded.Exception(() => AvroConvert.SerializeHeadless(holder, Schema("\"string\"")));

            //Assert
            exception.Message.Should().StartWith("Field 'Holder.Value' is null");
        }

        [Test]
        public void ExpandoObject_without_a_required_field_throws()
        {
            //Arrange
            dynamic expando = new ExpandoObject();
            expando.Other = 1;

            //Act
            var exception = Recorded.Exception(() => AvroConvert.SerializeHeadless((object)expando, Schema("\"string\"")));

            //Assert
            exception.Message.Should().StartWith("Field 'Holder.Value' is null");
        }

        [Test]
        public void Nullable_reference_type_member_gets_a_union_and_null_round_trips()
        {
            //Arrange
            var user = new User { name = null, favorite_number = null, favorite_color = "green" };

            //Act
            var result = AvroConvert.Deserialize<User>(AvroConvert.Serialize(user));

            //Assert
            AvroConvert.GenerateSchema(typeof(User)).Should().Contain("{\"name\":\"name\",\"type\":[\"null\",\"string\"]}");
            result.name.Should().BeNull();
        }

        [Test]
        public void Field_without_a_member_is_written_with_its_schema_default()
        {
            //Arrange: a newer schema version, as with Kafka "use latest version"
            var schema = """
                {"type":"record","name":"Holder","fields":[
                  {"name":"Value","type":"string"},
                  {"name":"Channel","type":"string","default":"web shop"},
                  {"name":"Priority","type":"int","default":3},
                  {"name":"Color","type":{"type":"enum","name":"Color","symbols":["Red","Green"]},"default":"Green"},
                  {"name":"Score","type":["double","null"],"default":1.5},
                  {"name":"Note","type":["null","string"],"default":null}
                ]}
                """;

            //Act
            var bytes = AvroConvert.SerializeHeadless(new Holder<string> { Value = "x" }, schema);

            //Assert
            AvroConvert.Avro2Json(bytes, schema).Should().Be("{\"Value\":\"x\",\"Channel\":\"web shop\",\"Priority\":3,\"Color\":\"Green\",\"Score\":1.5,\"Note\":null}");
        }
    }
}
