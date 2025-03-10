using AvroConvertComponentTests.Infrastructure;
using SolTechnology.Avro;
using Xunit;

namespace AvroConvertComponentTests.GenerateSchema
{
    public class GenericClassTests
    {

        [Fact]
        public void GenerateSchema_ClassContainsFieldWithGenericAttribute_SchemaIsGeneratedCorrectly()
        {
            //Arrange


            //Act
            string schema = AvroConvert.GenerateSchema(typeof(GenericSchema));


            //Assert
            AssertExtensions.JsonEqual(schema, 
              @"{
                      ""name"": ""GenericSchema"",
                      ""namespace"":""AvroConvertComponentTests.GenerateSchema"",
                      ""type"": ""record"",
                      ""fields"": [
                        {
                          ""name"": ""Subject"",
                          ""type"": {
                            ""name"": ""FieldString"",
                            ""type"": ""record"",
                            ""fields"": [
                              { ""name"": ""Value"", ""type"": ""string"" },
                              { ""name"": ""Confidence"", ""type"": ""float"" }
                            ]
                          }
                        },
                        {
                          ""name"": ""Mark"",
                          ""type"": {
                            ""name"": ""FieldInt32"",
                            ""type"": ""record"",
                            ""fields"": [
                              { ""name"": ""Value"", ""type"": ""int"" },
                              { ""name"": ""Confidence"", ""type"": ""float"" }
                            ]
                          }
                        }
                      ]
                    }"
                );
        }

    }

    public class Field<T>
    {
        public T Value { get; set; }

        public float Confidence { get; set; }
    }

    public class GenericSchema
    {
        public Field<string> Subject { get; set; }

        public Field<int> Mark { get; set; }
    }
}
