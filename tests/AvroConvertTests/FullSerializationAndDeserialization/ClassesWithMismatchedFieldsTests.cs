using SolTechnology.Avro;
using Xunit;

namespace AvroConvertComponentTests.FullSerializationAndDeserialization
{
    public class ClassesWithMismatchedFieldsTests
    {
        [Fact]
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
            Assert.NotNull(result);
            Assert.Equal(2, result.Models.Length);
            Assert.Equal("string value", result.Models[0].StringValue);
            Assert.Equal(10, result.Models[0].IntValue);
            Assert.Equal("string value", result.Models[1].StringValue);
            Assert.Equal(10, result.Models[1].IntValue);
            Assert.Equal(456.789m, result.FinalField);
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
