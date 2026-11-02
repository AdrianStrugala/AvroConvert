using AwesomeAssertions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AutoFixture;
using SolTechnology.Avro;
using NUnit.Framework;

namespace AvroConvertComponentTests
{
    public class GetSchemaTests
    {
        private readonly byte[] _avroBytes;
        private readonly string _example2schema;

        public GetSchemaTests()
        {
            _avroBytes = File.ReadAllBytes("example2.avro");

            _example2schema =
                "{\"type\": \"record\", \"name\": \"User\", \"namespace\": \"example.avro\", \"fields\": [{\"type\": \"string\", \"name\": \"name\"}, {\"type\": [\"int\", \"null\"], \"name\": \"favorite_number\"}, {\"type\": [\"string\", \"null\"], \"name\": \"favorite_color\"}]}";
        }

        [Test]
        public void GetSchemaAsString_ValidBytes_SchemaIsReturned()
        {
            //Arrange

            //Act
            string schema = AvroConvert.GetSchema(_avroBytes);

            //Assert
            schema.Should().Be(_example2schema);
        }

        [Test]
        public void GetSchemaAsString_ValidStream_SchemaIsReturned()
        {
            //Arrange

            //Act
            string schema = AvroConvert.GetSchema(new MemoryStream(_avroBytes));

            //Assert
            schema.Should().Be(_example2schema);
        }

        [Test]
        public void GetSchemaAsString_ValidFile_SchemaIsReturned()
        {
            //Arrange

            //Act
            string schema = AvroConvert.GetSchema("example.avro");

            //Assert
            schema.Should().NotBeNull();
        }

        [Test]
        public void Read_only_file_can_be_read()
        {
            var path = Path.GetTempFileName();
            File.WriteAllBytes(path, _avroBytes);
            File.SetAttributes(path, FileAttributes.ReadOnly);
            try
            {
                AvroConvert.GetSchema(path).Should().Be(_example2schema);
            }
            finally
            {
                File.SetAttributes(path, FileAttributes.Normal);
                File.Delete(path);
            }
        }

        [Test]
        public void File_open_for_reading_elsewhere_can_be_read()
        {
            var path = Path.GetTempFileName();
            File.WriteAllBytes(path, _avroBytes);
            try
            {
                using var other = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);

                AvroConvert.GetSchema(path).Should().Be(_example2schema);
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}
