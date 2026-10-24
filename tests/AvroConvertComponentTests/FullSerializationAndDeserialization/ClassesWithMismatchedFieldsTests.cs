using AwesomeAssertions;
using SolTechnology.Avro;
using NUnit.Framework;

namespace AvroConvertComponentTests.FullSerializationAndDeserialization
{
    public class ClassesWithMismatchedFieldsTests
    {
        [Test]
        public void Write_schema_with_additional_field_in_array_can_be_read()
        {
            //Arrange
            var write = new WriteModel
            {
                Models = new[]
                {
                    new WriteModelWithAdditionalField
                    {
                        StringValue = "string value",
                        DecimalValue = 123.456m,
                        IntValue = 10
                    },
                    new WriteModelWithAdditionalField
                    {
                        StringValue = "string value",
                        DecimalValue = 123.456m,
                        IntValue = 10
                    }
                },
                FinalField = 456.789m
            };

            var writeSchema = AvroConvert.GenerateSchema(typeof(WriteModel));
            var readSchema = AvroConvert.GenerateSchema(typeof(ReadModel));

            //Act
            var serialized = AvroConvert.SerializeHeadless(write, writeSchema);
            var result = AvroConvert.DeserializeHeadless(serialized, writeSchema, readSchema, typeof(ReadModel)) as ReadModel;

            //Assert
            result.Should().NotBeNull();
            result.Models.Length.Should().Be(2);
            result.Models[0].StringValue.Should().Be("string value");
            result.Models[0].IntValue.Should().Be(10);
            result.Models[1].StringValue.Should().Be("string value");
            result.Models[1].IntValue.Should().Be(10);
            result.FinalField.Should().Be(456.789m);
        }

        public class WriteModel
        {
            public WriteModelWithAdditionalField[] Models { get; set; }

            public decimal FinalField { get; set; }
        }

        public class WriteModelWithAdditionalField
        {
            public string StringValue { get; set; }

            public decimal DecimalValue { get; set; }

            public int IntValue { get; set; }
        }

        public class ReadModel
        {
            public ReadModelWithMissingField[] Models { get; set; }

            public decimal FinalField { get; set; }
        }

        public class ReadModelWithMissingField
        {
            public string StringValue { get; set; }

            public int IntValue { get; set; }
        }
    }
}
