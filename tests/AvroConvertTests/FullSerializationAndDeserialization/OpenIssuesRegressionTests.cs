using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Runtime.Serialization;
using FluentAssertions;
using SolTechnology.Avro;
using SolTechnology.Avro.Infrastructure.Attributes;
using Xunit;

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

        [Fact]
        public void Issue159_Generic_types_get_distinct_record_names()
        {
            var schema = AvroConvert.GenerateSchema(typeof(GenericHolder));

            schema.Should().Contain("\"name\":\"Field_String\"").And.Contain("\"name\":\"Field_Int32\"");
        }

        [Theory]
        [MemberData(nameof(TestEngine.Core), MemberType = typeof(TestEngine))]
        public void Issue159_Generic_types_round_trip(Func<object, Type, dynamic> engine)
        {
            var item = new GenericHolder
            {
                Subject = new Field<string> { Value = "math", Confidence = 0.9f },
                Mark = new Field<int> { Value = 5, Confidence = 0.4f }
            };

            var result = (GenericHolder)engine.Invoke(item, typeof(GenericHolder));

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

        [Fact]
        public void Issue174_Union_of_records_read_into_untyped_object_member()
        {
            var bytes = AvroConvert.Serialize(new TypeWithUnion { UnionField = new UnionRecordB { B = "b" } });

            var plain = AvroConvert.Deserialize<TypeWithPlainObject>(bytes);
            dynamic dyn = AvroConvert.Deserialize<dynamic>(bytes);

            plain.UnionField.Should().BeOfType<UnionRecordB>().Which.B.Should().Be("b");
            ((string)dyn.UnionField.B).Should().Be("b");
        }

        // https://github.com/AdrianStrugala/AvroConvert/issues/112
        [Fact]
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

        [Fact]
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

        [Fact]
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
        [Fact]
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

        [Fact]
        public void Issue26_Multidimensional_array_is_rejected_explicitly()
        {
            Action act = () => AvroConvert.Serialize(new Multi { Grid = new[,] { { 1, 2 }, { 3, 4 } } });

            act.Should().Throw<SerializationException>().WithMessage("*Multidimensional array*jagged*");
        }
    }
}
