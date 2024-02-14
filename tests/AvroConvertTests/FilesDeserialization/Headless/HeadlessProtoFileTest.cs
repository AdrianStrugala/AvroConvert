using SolTechnology.Avro;
using Xunit;

namespace AvroConvertComponentTests.FilesDeserialization.Headless
{
    public class HeadlessProtoFileTest
    {
        [Fact]
        [Trait("Fix", "https://github.com/AdrianStrugala/AvroConvert/issues/95")]
        public void Component_SerializeHeadlessBiggerObjectAndReadSmaller_NoError()
        {
            //Arrange
            string schemaString =
                "{\r\n  \"name\": \"SampleProtoGeneratedClass\",\r\n  \"namespace\": \"Test\",\r\n  \"type\": \"record\",\r\n  \"fields\": [\r\n    {\r\n      \"name\": \"Type\",\r\n      \"type\": \"string\"\r\n    },\r\n    {\r\n      \"name\": \"Version\",\r\n      \"type\": \"int\"\r\n    },\r\n    {\r\n      \"name\": \"CorrelationId\",\r\n      \"type\": \"string\"\r\n    },\r\n    {\r\n      \"name\": \"OperationId\",\r\n      \"type\": \"string\"\r\n    },\r\n    {\r\n      \"name\": \"MessageId\",\r\n      \"type\": \"string\"\r\n    },\r\n    {\r\n      \"name\": \"Timestamp\",\r\n      \"type\": \"string\"\r\n    }\r\n  ]\r\n}";

            var value = new SampleProtoGeneratedClass
            {
                CorrelationId = "cor",
                MessageId = "mes",
                OperationId = "ope",
                Timestamp = "time",
                Type = "type",
                Version = 2
            };

            //Act
            var serialized = AvroConvert.SerializeHeadless(value, schemaString);
            var deserialized = AvroConvert.DeserializeHeadless<SampleProtoGeneratedClass>(serialized, schemaString);

            //Assert
            Assert.NotNull(serialized);
            Assert.NotNull(deserialized);
            Assert.Equal(value, deserialized);
        }
    }
}
