using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using AwesomeAssertions;
using NUnit.Framework;
using SolTechnology.Avro;
using SolTechnology.Avro.Policies;

namespace AvroConvertComponentTests.SchemaAttributes
{
    /// <summary>
    /// <see cref="AvroSchema.FromValue"/>: headless data does not carry a schema, so for values of type <c>object</c> it has to
    /// be taken from the value and handed to both sides.
    /// </summary>
    public class SchemaFromValueTests
    {
        public class Row
        {
            public long id { get; set; }
            public string name { get; set; }
        }

        public class WithObject
        {
            public long Id { get; set; }
            public object Value { get; set; }
        }

        public class Foo
        {
            public string Name { get; set; }
        }

        private static readonly Row Value = new() { id = 7, name = "abc" };

        private static IEnumerable<object> Values()
        {
            yield return new Dictionary<string, object> { ["a"] = "x", ["b"] = 1L, ["c"] = null };
            yield return new Dictionary<string, object> { ["a"] = new Foo { Name = "n" } };
            yield return new WithObject { Id = 1, Value = "x" };
            yield return new WithObject { Id = 1, Value = new Foo { Name = "n" } };
            yield return new List<object> { "a", 1L };
            yield return Value;
        }

        private static string Json(object value) => Newtonsoft.Json.JsonConvert.SerializeObject(value);

        [Test]
        public void Any_value_goes_headless(
            [ValueSource(nameof(Values))] object value,
            [ValueSource(typeof(Formats), nameof(Formats.HeadlessReaders))] HeadlessReader reader)
        {
            var schema = AvroSchema.FromValue(value);

            var avro = AvroConvert.SerializeHeadless(value, schema);

            Json(RoundTrip.ReadHeadless<object>(reader, avro, schema.Json)).Should().Be(Json(value));
        }

        [Test]
        public void Class_with_object_values_reads_into_a_dictionary([ValueSource(typeof(Formats), nameof(Formats.HeadlessReaders))] HeadlessReader reader)
        {
            var value = new WithObject { Id = 1, Value = new Foo { Name = "n" } };
            var schema = AvroSchema.FromValue(value);

            var avro = AvroConvert.SerializeHeadless(value, schema);

            Json(RoundTrip.ReadHeadless<Dictionary<string, object>>(reader, avro, schema.Json)).Should().Be(Json(value));
        }

        [Test]
        public void ExpandoObject_goes_headless()
        {
            dynamic value = new ExpandoObject();
            value.a = "x";
            value.b = 1L;
            var schema = AvroSchema.FromValue((object)value);

            var avro = AvroConvert.SerializeHeadless((object)value, schema);

            Json(AvroConvert.DeserializeHeadless<Dictionary<string, object>>(avro, schema)).Should().Be(@"{""a"":""x"",""b"":1}");
        }

        [Test]
        public void Plain_class_gives_the_schema_of_the_class()
        {
            AvroSchema.FromValue(Value).Should().Be(AvroSchema.Create<Row>());
        }

        [Test]
        public void Single_value_gives_the_schema_of_its_file()
        {
            var value = new Dictionary<string, object> { ["a"] = "x", ["b"] = 1L };

            AvroSchema.FromValue(value).Should().Be(AvroSchema.FromAvro(AvroConvert.Serialize(value)));
        }

        [Test]
        public void Collection_gives_the_schema_of_the_collection()
        {
            var value = new List<Row> { Value };

            var schema = AvroSchema.FromValue(value);

            AvroConvert.DeserializeHeadless<List<Row>>(AvroConvert.SerializeHeadless(value, schema), schema).Should().BeEquivalentTo(value);
        }

        [Test]
        public void Options_are_used()
        {
            var options = new AvroConvertOptions { NamingPolicy = new JavaAvroNamingPolicy("com.acme") };

            AvroSchema.FromValue(Value, options).Json.Should().Contain("com.acme");
        }

        [Test]
        public void Schema_of_one_value_writes_a_file_of_many()
        {
            var schema = AvroSchema.FromValue(new Dictionary<string, object> { ["a"] = "x", ["b"] = 1L });

            var avro = AvroConvert.Serialize(new[] { new Dictionary<string, object> { ["c"] = 2L } }, schema);

            AvroConvert.Deserialize<List<Dictionary<string, object>>>(avro).Single()["c"].Should().Be(2L);
        }

        [Test]
        public void Null_is_rejected()
        {
            var act = () => AvroSchema.FromValue(null);

            act.Should().Throw<ArgumentNullException>();
        }

        [Test]
        public void Values_avro_cannot_tell_apart_are_rejected()
        {
            var value = new Dictionary<string, object> { ["a"] = 1L, ["b"] = new DateTime(2026, 10, 30, 0, 0, 0, DateTimeKind.Utc) };

            var act = () => AvroSchema.FromValue(value);

            act.Should().Throw<ArgumentException>().WithMessage("*Serialize(value, schema)*");
        }

        [Test]
        public void Reading_object_values_without_a_schema_says_what_is_missing()
        {
            var avro = AvroConvert.SerializeHeadless(new Dictionary<string, object> { ["a"] = "x" }, @"{""type"":""map"",""values"":""string""}");

            var dictionary = () => AvroConvert.DeserializeHeadless<Dictionary<string, object>>(avro);
            var dynamicValue = () => AvroConvert.DeserializeHeadless<dynamic>(avro);
            var member = () => AvroConvert.DeserializeHeadless<WithObject>(avro);

            dictionary.Should().Throw<Exception>().WithMessage("*DeserializeHeadless<T>(bytes, schema)*");
            dynamicValue.Should().Throw<Exception>().WithMessage("*DeserializeHeadless<T>(bytes, schema)*");
            member.Should().Throw<Exception>().WithMessage("*DeserializeHeadless<T>(bytes, schema)*");
        }

        [Test]
        public void Reading_a_class_without_a_schema_still_works()
        {
            AvroConvert.DeserializeHeadless<Row>(AvroConvert.SerializeHeadless(Value, typeof(Row))).Should().BeEquivalentTo(Value);
        }

        [Test]
        public void Error_of_writing_points_at_FromValue()
        {
            var act = () => AvroConvert.SerializeHeadless(new Dictionary<string, object> { ["a"] = "x" }, typeof(Dictionary<string, object>));

            act.Should().Throw<Exception>().WithMessage("*AvroSchema.FromValue*");
        }
    }
}
