namespace AvroConvertTests
{
    using AvroConvert;
    using Newtonsoft.Json.Linq;
    using System.IO;
    using Xunit;

    public class GetSchemaTests
    {
        private readonly byte[] _avroBytes;
        private readonly string _example2schema;

        public GetSchemaTests()
        {
            _avroBytes = System.IO.File.ReadAllBytes("example2.avro");

            _example2schema =
                "{\"type\": \"record\", \"name\": \"User\", \"namespace\": \"example.avro\", \"fields\": [{\"type\": \"string\", \"name\": \"name\"}, {\"type\": [\"int\", \"null\"], \"name\": \"favorite_number\"}, {\"type\": [\"string\", \"null\"], \"name\": \"favorite_color\"}]}";
        }

        [Fact]
        public void GetSchemaAsString_ValidBytes_SchemaIsReturned()
        {
            //Arrange

            //Act
            string schema = AvroConvert.GetSchemaAsString(_avroBytes);

            //Assert
            Assert.Equal(_example2schema, schema);
        }

        [Fact]
        public void GetSchemaAsString_ValidStream_SchemaIsReturned()
        {
            //Arrange

            //Act
            string schema = AvroConvert.GetSchemaAsString(new MemoryStream(_avroBytes));

            //Assert
            Assert.Equal(_example2schema, schema);
        }

        [Fact]
        public void GetSchemaAsString_ValidFile_SchemaIsReturned()
        {
            //Arrange

            //Act
            string schema = AvroConvert.GetSchemaAsString("example.avro");

            //Assert
            Assert.NotNull(schema);
        }

        [Fact]
        public void GetSchemaAsJObject_ValidBytes_SchemaIsReturned()
        {
            //Arrange

            //Act
            JObject schema = AvroConvert.GetSchemaAsJObject(_avroBytes);

            //Assert
            Assert.Equal(JObject.Parse(_example2schema), schema);
        }

        [Fact]
        public void GetSchemaAsJObject_ValidStream_SchemaIsReturned()
        {
            //Arrange

            //Act
            JObject schema = AvroConvert.GetSchemaAsJObject(new MemoryStream(_avroBytes));

            //Assert
            Assert.Equal(JObject.Parse(_example2schema), schema);
        }

        [Fact]
        public void GetSchemaAsJObject_ValidFile_SchemaIsReturned()
        {
            //Arrange

            //Act
            JObject schema = AvroConvert.GetSchemaAsJObject("example.avro");

            //Assert
            Assert.NotNull(schema);
        }
    }
}
