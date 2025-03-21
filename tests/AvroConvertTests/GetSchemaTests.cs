﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using AutoFixture;
using SolTechnology.Avro;
using Xunit;

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

        [Fact]
        public void GetSchemaAsString_ValidBytes_SchemaIsReturned()
        {
            //Arrange

            //Act
            string schema = AvroConvert.GetSchema(_avroBytes);

            //Assert
            Assert.Equal(_example2schema, schema);
        }

        [Fact]
        public void GetSchemaAsString_ValidStream_SchemaIsReturned()
        {
            //Arrange

            //Act
            string schema = AvroConvert.GetSchema(new MemoryStream(_avroBytes));

            //Assert
            Assert.Equal(_example2schema, schema);
        }

        [Fact]
        public void GetSchemaAsString_ValidFile_SchemaIsReturned()
        {
            //Arrange

            //Act
            string schema = AvroConvert.GetSchema("example.avro");

            //Assert
            Assert.NotNull(schema);
        }
    }
}
