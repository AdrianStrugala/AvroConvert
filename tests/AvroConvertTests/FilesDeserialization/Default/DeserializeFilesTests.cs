using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using SolTechnology.Avro;
using SolTechnology.Avro.Infrastructure.Exceptions;
using Xunit;

namespace AvroConvertComponentTests.FilesDeserialization.Default
{
    public class DeserializeFilesTests
    {
        private readonly byte[] _avroBytes = File.ReadAllBytes("example2.avro");
        private readonly byte[] _schemaOnlyAvroBytes = File.ReadAllBytes("header_only.avro");


        [Fact]
        public void Deserialize_CustomSchema_OnlyValuesFromCustomSchemaAreReturned()
        {
            //Arrange
            var expectedResult = new List<User>();
            expectedResult.Add(new User
            {
                name = "Alyssa",
                favorite_number = 256,
                favorite_color = null
            });

            expectedResult.Add(new User
            {
                name = "Ben",
                favorite_number = 7,
                favorite_color = "red"
            });

            //Act
            var result = AvroConvert.Deserialize<List<User>>(_avroBytes);


            //Assert
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void Deserialize_NonGenericMethod_OnlyValuesFromCustomSchemaAreReturned()
        {
            //Arrange
            var expectedResult = new List<UserNameClass>();
            expectedResult.Add(new UserNameClass
            {
                name = "Alyssa"
            });

            expectedResult.Add(new UserNameClass
            {
                name = "Ben"
            });

            //Act
            var result = AvroConvert.Deserialize(_avroBytes, typeof(List<UserNameClass>));


            //Assert
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void Deserialize_FileContainsNoAvroData_NoExceptionIsThrown()
        {
            //Arrange

            //Act
            var result = AvroConvert.Deserialize(_schemaOnlyAvroBytes, typeof(List<UserNameClass>));


            //Assert
            Assert.Equal(null, result);
        }

        [Fact]
        public void Deserialize_InvalidFile_InvalidAvroObjectExceptionIsThrown()
        {
            //Arrange
            byte[] invalidBytes = new byte[2137];


            //Act
            var result = Record.Exception(() => AvroConvert.Deserialize<int>(invalidBytes));


            //Assert
            Assert.IsType<InvalidAvroObjectException>(result);
        }

        [Fact]
        public void Deserialize_FileWithMultipleBlocks_EveryItemIsRead()
        {
            //Arrange


            //Act
            var result = AvroConvert.Deserialize<List<kylosample>>(File.ReadAllBytes("userdata1.avro"));


            //Assert
            result.Should().HaveCount(1000);
        }
    }
}
