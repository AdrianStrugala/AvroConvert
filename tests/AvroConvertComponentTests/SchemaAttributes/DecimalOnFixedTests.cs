#nullable enable
using System;
using AwesomeAssertions;
using NUnit.Framework;
using SolTechnology.Avro;
using SolTechnology.Avro.Infrastructure.Attributes;

namespace AvroConvertComponentTests.SchemaAttributes
{
    /// <summary>[AvroFixed] on a decimal: a decimal on a fixed, as other tools write it (Java, Apache.Avro, Confluent).</summary>
    public class DecimalOnFixedTests
    {
        private const string Schema = @"{""type"":""record"",""name"":""Payment"",""namespace"":""shop"",""fields"":[
            {""name"":""amount"",""type"":{""type"":""fixed"",""name"":""Money"",""size"":16,""logicalType"":""decimal"",""precision"":20,""scale"":2}},
            {""name"":""refund"",""type"":[""null"",""Money""],""default"":null}]}";

        [System.Runtime.Serialization.DataContract(Name = "Payment", Namespace = "shop")]
        public class Payment
        {
            [AvroFixed(16, "Money", "shop")]
            [AvroDecimal(Precision = 20, Scale = 2)]
            [System.Runtime.Serialization.DataMember]
            public decimal amount { get; set; }

            [AvroFixed(16, "Money", "shop")]
            [AvroDecimal(Precision = 20, Scale = 2)]
            [System.Runtime.Serialization.DataMember]
            public decimal? refund { get; set; }
        }

        [Test]
        public void Schema_of_the_class_is_the_schema_of_other_tools()
        {
            var fromClass = AvroSchema.Create<Payment>();

            fromClass.CanonicalForm.Should().Be(AvroSchema.Parse(Schema).CanonicalForm);
            fromClass.Json.Should().Contain("\"logicalType\":\"decimal\"").And.Contain("\"precision\":20").And.Contain("\"scale\":2");
        }

        [Test]
        public void Values_round_trip([ValueSource(typeof(Formats), nameof(Formats.Core))] Format format)
        {
            foreach (var value in new[]
                     {
                         new Payment { amount = 12.34m, refund = null },
                         new Payment { amount = -0.01m, refund = -99999999.99m },
                         new Payment { amount = 0m, refund = 123456789012345678.90m }
                     })
            {
                RoundTrip.Run<Payment>(value, format).Should().BeEquivalentTo(value);
            }
        }

        [Test]
        public void Bytes_are_those_of_the_schema()
        {
            // Headless data has no schema to resolve with: the class must write what the schema of other tools reads.
            var value = new Payment { amount = -12.34m, refund = 5m };

            var fromClass = AvroConvert.SerializeHeadless(value, AvroSchema.Create<Payment>());
            var fromSchema = AvroConvert.SerializeHeadless(value, AvroSchema.Parse(Schema));

            fromClass.Should().Equal(fromSchema);
            fromClass.Should().HaveCount(16 + 1 + 16, "two fixed of 16 bytes and the union index");
        }

        public class TooSmall
        {
            [AvroFixed(2, "Small")]
            [AvroDecimal(Precision = 4, Scale = 0)]
            public decimal value { get; set; }
        }

        [Test]
        public void Value_that_does_not_fit_the_fixed_names_the_fixed()
        {
            var exception = Recorded.Exception(() => AvroConvert.Serialize(new TooSmall { value = 1_000_000m }));

            exception.Message.Should().Contain("needs 3 bytes").And.Contain("SchemaAttributes.Small] of 2 bytes");
        }

        public class OnString
        {
            [AvroFixed(2, "Small")]
            public string value { get; set; } = "";
        }

        [Test]
        public void AvroFixed_on_another_type_says_where_it_can_be()
        {
            Recorded.Exception(() => AvroSchema.Create<OnString>()).Message.Should().Contain("byte[] or decimal");
        }
    }
}
