using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Runtime.Serialization;
using AwesomeAssertions;
using SolTechnology.Avro;
using SolTechnology.Avro.Infrastructure.Attributes;
using SolTechnology.Avro.Infrastructure.Exceptions;
using NUnit.Framework;

namespace AvroConvertComponentTests.FullSerializationAndDeserialization
{
    public class OpenIssuesRegressionTests
    {
        // https://github.com/AdrianStrugala/AvroConvert/issues/159
        public class Field<T>
        {
            public T Value { get; set; }
            public float Confidence { get; set; }
        }

        public class GenericHolder
        {
            public Field<string> Subject { get; set; }
            public Field<int> Mark { get; set; }
        }

        [Test]
        public void Issue159_Generic_types_get_distinct_record_names()
        {
            var schema = AvroConvert.GenerateSchema(typeof(GenericHolder));

            schema.Should().Contain("\"name\":\"Field_String\"").And.Contain("\"name\":\"Field_Int32\"");
        }

        [Test]
        public void Issue159_Generic_types_round_trip([ValueSource(typeof(Formats), nameof(Formats.Core))] Format format)
        {
            var item = new GenericHolder
            {
                Subject = new Field<string> { Value = "math", Confidence = 0.9f },
                Mark = new Field<int> { Value = 5, Confidence = 0.4f }
            };

            var result = RoundTrip.Run<GenericHolder>(item, format);

            result.Should().BeEquivalentTo(item);
        }

        // https://github.com/AdrianStrugala/AvroConvert/issues/174
        public class UnionRecordA { public int A { get; set; } }
        public class UnionRecordB { public string B { get; set; } }

        public class TypeWithUnion
        {
            [AvroUnion(typeof(UnionRecordA), typeof(UnionRecordB))]
            public object UnionField { get; set; }
        }

        public class TypeWithPlainObject
        {
            public object UnionField { get; set; }
        }

        [Test]
        public void Issue174_Union_of_records_read_into_untyped_object_member()
        {
            var bytes = AvroConvert.Serialize(new TypeWithUnion { UnionField = new UnionRecordB { B = "b" } });

            var plain = AvroConvert.Deserialize<TypeWithPlainObject>(bytes);
            dynamic dyn = AvroConvert.Deserialize<dynamic>(bytes);

            plain.UnionField.Should().BeOfType<UnionRecordB>().Which.B.Should().Be("b");
            ((string)dyn.UnionField.B).Should().Be("b");
        }

        [Test]
        public void Issue174_AvroUnion_alternatives_win_over_same_named_classes_in_other_namespaces()
        {
            // Registry-style schema: namespace matches neither this test class nor AvroGen.ObjA / AvroGen.ObjB
            var schema = "{\"type\":\"record\",\"name\":\"TypeWithUnionAvro\",\"namespace\":\"com.example.events\",\"fields\":[" +
                         "{\"name\":\"TopLevelField\",\"type\":\"string\"}," +
                         "{\"name\":\"UnionField\",\"type\":[{\"type\":\"record\",\"name\":\"ObjA\",\"fields\":[{\"name\":\"FieldA\",\"type\":\"string\"}]}," +
                         "{\"type\":\"record\",\"name\":\"ObjB\",\"fields\":[{\"name\":\"FieldB\",\"type\":\"int\"}]}]}]}";
            var item = new AvroConvertComponentTests.TypeWithUnionAvro { TopLevelField = "x", UnionField = new AvroConvertComponentTests.ObjB { FieldB = 7 } };
            var bytes = AvroConvert.SerializeHeadless(item, schema);

            var typed = AvroConvert.DeserializeHeadless<AvroConvertComponentTests.TypeWithUnionAvro>(bytes, schema);
            var generated = AvroConvert.DeserializeHeadless<AvroGen.TypeWithUnionAvro>(bytes, schema);
            dynamic dyn = AvroConvert.DeserializeHeadless<dynamic>(bytes, schema);

            typed.UnionField.Should().BeOfType<AvroConvertComponentTests.ObjB>().Which.FieldB.Should().Be(7);
            generated.UnionField.Should().BeOfType<AvroGen.ObjB>().Which.FieldB.Should().Be(7);
            ((object)dyn.UnionField).Should().BeOfType<ExpandoObject>();
            ((int)dyn.UnionField.FieldB).Should().Be(7);
        }

        // https://github.com/AdrianStrugala/AvroConvert/issues/112
        [Test]
        public void Issue112_ExpandoObject_default_flow()
        {
            dynamic item = new ExpandoObject();
            item.Name = "n";
            item.Age = 3;
            item.Nested = new ExpandoObject();
            item.Nested.X = 1.5;
            item.Tags = new List<object> { "a", "b" };
            item.Missing = null;

            var bytes = AvroConvert.Serialize((object)item);
            dynamic back = AvroConvert.Deserialize<dynamic>(bytes);
            var expando = AvroConvert.Deserialize<ExpandoObject>(bytes);

            ((string)back.Name).Should().Be("n");
            ((int)back.Age).Should().Be(3);
            ((double)back.Nested.X).Should().Be(1.5);
            ((IEnumerable<object>)back.Tags).Should().Equal("a", "b");
            ((object)back.Missing).Should().BeNull();
            ((IDictionary<string, object>)expando)["Age"].Should().Be(3);
        }

        [Test]
        public void Issue112_ExpandoObject_headless_and_json_flows()
        {
            dynamic item = new ExpandoObject();
            item.Name = "n";
            item.Age = 3;
            var schema = AvroConvert.GetSchema(AvroConvert.Serialize((object)item));

            var headless = AvroConvert.SerializeHeadless((object)item, schema);
            dynamic back = AvroConvert.DeserializeHeadless<dynamic>(headless, schema);
            var expando = AvroConvert.DeserializeHeadless<ExpandoObject>(headless, schema);
            var json = AvroConvert.Avro2Json(AvroConvert.Serialize((object)item));

            ((int)back.Age).Should().Be(3);
            ((IDictionary<string, object>)expando)["Name"].Should().Be("n");
            json.Should().Be("{\"Name\":\"n\",\"Age\":3}");
        }

        [Test]
        public void Issue112_ExpandoObject_collection_merges_shapes_and_codecs_apply()
        {
            dynamic first = new ExpandoObject();
            first.Name = "n";
            first.Age = 3;
            dynamic second = new ExpandoObject();
            second.Name = null;
            second.Age = 4;
            second.Extra = true;

            var bytes = AvroConvert.Serialize(new List<ExpandoObject> { first, second }, CodecType.Deflate);
            var schema = AvroConvert.GetSchema(bytes);
            var back = AvroConvert.Deserialize<List<dynamic>>(bytes);

            schema.Should().Contain("{\"name\":\"Name\",\"type\":[\"null\",\"string\"]}")
                  .And.Contain("{\"name\":\"Extra\",\"type\":[\"null\",\"boolean\"]}");
            back.Should().HaveCount(2);
            ((object)back[0].Extra).Should().BeNull();
            ((bool)back[1].Extra).Should().BeTrue();
            ((object)back[1].Name).Should().BeNull();
        }

        // https://github.com/AdrianStrugala/AvroConvert/issues/107
        [Test]
        public void Issue107_Headless_deserialization_to_dynamic_with_explicit_schema()
        {
            var schema = AvroConvert.GenerateSchema(typeof(UnionRecordA));
            var bytes = AvroConvert.SerializeHeadless(new UnionRecordA { A = 7 }, schema);

            dynamic dyn = AvroConvert.DeserializeHeadless<dynamic>(bytes, schema);
            var expando = AvroConvert.DeserializeHeadless<ExpandoObject>(bytes, schema);

            ((int)dyn.A).Should().Be(7);
            ((IDictionary<string, object>)expando)["A"].Should().Be(7);
        }

        // https://github.com/AdrianStrugala/AvroConvert/issues/26
        public class Multi { public int[,] Grid { get; set; } }

        [Test]
        public void Issue26_Multidimensional_array_is_rejected_explicitly()
        {
            Action act = () => AvroConvert.Serialize(new Multi { Grid = new[,] { { 1, 2 }, { 3, 4 } } });

            act.Should().Throw<SerializationException>().WithMessage("*Multidimensional array*jagged*");
        }

        // https://github.com/AdrianStrugala/AvroConvert/issues/69
        public class Row
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        private const string RowSchema =
            "{\"type\":\"record\",\"name\":\"Row\",\"fields\":[{\"name\":\"Id\",\"type\":\"int\"},{\"name\":\"Name\",\"type\":\"string\"}]}";

        [Test]
        public void Issue69_Headless_rows_written_one_after_another_are_read_into_a_list()
        {
            var rows = new[] { new Row { Id = 1, Name = "a" }, new Row { Id = 2, Name = "b" }, new Row { Id = 3, Name = "c" } };
            var bytes = rows.SelectMany(r => AvroConvert.SerializeHeadless(r, RowSchema)).ToArray();

            AvroConvert.DeserializeHeadless<List<Row>>(bytes, RowSchema).Should().BeEquivalentTo(rows);
            AvroConvert.DeserializeHeadless<Row[]>(bytes.AsSpan(), RowSchema).Should().BeEquivalentTo(rows);
            AvroConvert.DeserializeHeadless<List<Row>>(bytes, AvroSchema.Parse(RowSchema)).Should().BeEquivalentTo(rows);
        }

        [Test]
        public void Issue69_Single_headless_row_and_empty_data()
        {
            var row = new Row { Id = 1, Name = "a" };
            var bytes = AvroConvert.SerializeHeadless(row, RowSchema);

            AvroConvert.DeserializeHeadless<Row>(bytes, RowSchema).Should().BeEquivalentTo(row);
            AvroConvert.DeserializeHeadless<List<Row>>(bytes, RowSchema).Should().BeEquivalentTo(new[] { row });
            AvroConvert.DeserializeHeadless<List<Row>>(Array.Empty<byte>(), RowSchema).Should().BeEmpty();
        }

        [Test]
        public void Issue69_Explicit_row_count_and_array_writer_schema_still_work()
        {
            var rows = new List<Row> { new() { Id = 1, Name = "a" }, new() { Id = 2, Name = "b" } };
            var concatenated = rows.SelectMany(r => AvroConvert.SerializeHeadless(r, RowSchema)).ToArray();

            AvroConvert.DeserializeHeadless<List<Row>>(concatenated, RowSchema, 1).Should().BeEquivalentTo(rows.Take(1));
            AvroConvert.DeserializeHeadless<List<Row>>(AvroConvert.SerializeHeadless(rows, typeof(List<Row>))).Should().BeEquivalentTo(rows);
        }

        [Test]
        public void Issue69_Rows_that_take_no_bytes_cannot_be_counted()
        {
            const string emptyRecord = "{\"type\":\"record\",\"name\":\"Empty\",\"fields\":[]}";

            Action act = () => AvroConvert.DeserializeHeadless<List<object>>(new byte[] { 1, 2 }, emptyRecord);

            act.Should().Throw<InvalidAvroObjectException>().WithMessage("*numberOfRows*");
        }
    }
}
