using System;
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


        [Theory]
        [MemberData(nameof(TestEngine.DeserializeOnly), MemberType = typeof(TestEngine))]
        public void Deserialize_FileContainsNoAvroData_NoExceptionIsThrown(Func<byte[], Type, dynamic> engine)
        {
            //Arrange


            //Act
            var result = engine.Invoke(_schemaOnlyAvroBytes, typeof(List<UserNameClass>));


            //Assert
            Assert.Equal(null, result);
        }


        [Theory]
        [MemberData(nameof(TestEngine.DeserializeOnly), MemberType = typeof(TestEngine))]
        [Trait("Fix", "https://github.com/AdrianStrugala/AvroConvert/issues/152")]
        public void Deserialize_FileWithMultipleBlocks_EveryItemIsRead(Func<byte[], Type, dynamic> engine)
        {
            //Arrange


            //Act
            var result = (List<kylosample>)engine.Invoke(File.ReadAllBytes("userdata1.avro"), typeof(List<kylosample>));


            //Assert
            result.Should().HaveCount(1000);
        }


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
        public void Deserialization_with_ReadOnlySpan_Works()
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

            var readOnlySpan = new ReadOnlySpan<byte>(_avroBytes);

            //Act
            var result = AvroConvert.Deserialize<List<User>>(readOnlySpan);


            //Assert
            Assert.Equal(expectedResult, result);
        }
    }
}
