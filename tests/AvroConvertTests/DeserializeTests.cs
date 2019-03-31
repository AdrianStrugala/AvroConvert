namespace AvroConvertTests
{
    using AvroConvert;
    using Xunit;

    public class DeserializeTests
    {
        private readonly byte[] _avroBytes;
        private readonly string _example2schema;

        public DeserializeTests()
        {
            _avroBytes = System.IO.File.ReadAllBytes("example2.avro");

            _example2schema =
                "{\"type\": \"record\", \"name\": \"User\", \"namespace\": \"example.avro\", \"fields\": [{\"type\": \"string\", \"name\": \"name\"}, {\"type\": [\"int\", \"null\"], \"name\": \"favorite_number\"}, {\"type\": [\"string\", \"null\"], \"name\": \"favorite_color\"}]}";
        }

        [Fact]
        public void Deserialize_ValidBytes_SetOfPropertiesAreReturned()
        {
            //Arrange

            //Act
            var result = AvroConvert.Deserialize(_avroBytes);

            var xd = result;

            //Assert
            //   Assert.Equal(_example2schema, schema);
        }

    }
}
