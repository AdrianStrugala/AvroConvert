namespace AvroConvertTests
{
    using Xunit;

    public class SerializeTests
    {
        private readonly byte[] _avroBytes;
        private readonly string _example2schema;

        public SerializeTests()
        {
            _avroBytes = System.IO.File.ReadAllBytes("example2.avro");

            _example2schema =
                "{\"type\": \"record\", \"name\": \"User\", \"namespace\": \"example.avro\", \"fields\": [{\"type\": \"string\", \"name\": \"name\"}, {\"type\": [\"int\", \"null\"], \"name\": \"favorite_number\"}, {\"type\": [\"string\", \"null\"], \"name\": \"favorite_color\"}]}";
        }

        [Fact]
        public void Serialize_idk_idk()
        {
            //Arrange

            //Act


            //Assert
        }

    }
}
