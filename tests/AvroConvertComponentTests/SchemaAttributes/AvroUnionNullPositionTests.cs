#nullable enable
using System.ComponentModel;
using AwesomeAssertions;
using NUnit.Framework;
using SolTechnology.Avro;
using SolTechnology.Avro.Infrastructure.Attributes;

namespace AvroConvertComponentTests.SchemaAttributes
{
    /// <summary>
    /// null in [AvroUnion] is the null branch at the position it has there. The order of the branches is part of the data
    /// (the branch index is written), so a class gives back ["int", "null"] of a schema only when it says where null goes.
    /// </summary>
    public class AvroUnionNullPositionTests
    {
        public class NullLast
        {
            [AvroUnion(typeof(int), null)]
            public int? Number { get; set; }

            [AvroUnion(typeof(string), null)]
            public string? Name { get; set; }
        }

        public class NullFirst
        {
            [AvroUnion(null, typeof(int))]
            [DefaultValue(5)]
            public int? Number { get; set; }
        }

        public class Branch
        {
            public int Value { get; set; }
        }

        public class NullBetween
        {
            [AvroUnion(typeof(string), null, typeof(Branch))]
            public object? Value { get; set; }
        }

        [Test]
        public void Null_takes_the_position_it_has_in_the_attribute()
        {
            AvroSchema.Create<NullLast>().Json.Should()
                .Contain("{\"name\":\"Number\",\"type\":[\"int\",\"null\"]}")
                .And.Contain("{\"name\":\"Name\",\"type\":[\"string\",\"null\"]}");
            AvroSchema.Create<NullFirst>().Json.Should().Contain("{\"name\":\"Number\",\"type\":[\"null\",\"int\"],\"default\":5}",
                "without the attribute [DefaultValue(5)] int? puts null last");
            AvroSchema.Create<NullBetween>().Json.Should().Contain("\"type\":[\"string\",\"null\",{\"name\":\"Branch\"");
        }

        [Test]
        public void Values_and_nulls_round_trip([ValueSource(typeof(Formats), nameof(Formats.Core))] Format format)
        {
            RoundTrip.Run<NullLast>(new NullLast { Number = 7, Name = null }, format).Should().BeEquivalentTo(new NullLast { Number = 7, Name = null });
            RoundTrip.Run<NullLast>(new NullLast { Number = null, Name = "x" }, format).Should().BeEquivalentTo(new NullLast { Number = null, Name = "x" });
            RoundTrip.Run<NullBetween>(new NullBetween { Value = new Branch { Value = 3 } }, format).Value.Should().BeEquivalentTo(new Branch { Value = 3 });
            RoundTrip.Run<NullBetween>(new NullBetween { Value = null }, format).Value.Should().BeNull();
        }

        [Test]
        public void Headless_data_of_the_schema_is_the_data_of_the_class()
        {
            // Headless data has no writer schema to resolve with: the class must give the same branch indexes as the schema.
            const string schema = @"{""type"":""record"",""name"":""NullLast"",""fields"":[" +
                                  @"{""name"":""Number"",""type"":[""int"",""null""]},{""name"":""Name"",""type"":[""string"",""null""]}]}";
            var value = new NullLast { Number = 7, Name = null };

            var fromSchema = AvroConvert.SerializeHeadless(value, AvroSchema.Parse(schema));
            var fromClass = AvroConvert.SerializeHeadless(value, AvroSchema.Create<NullLast>());

            fromClass.Should().Equal(fromSchema);
            AvroConvert.DeserializeHeadless<NullLast>(fromSchema, AvroSchema.Create<NullLast>()).Should().BeEquivalentTo(value);
        }
    }
}
