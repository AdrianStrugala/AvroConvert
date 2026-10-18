using System;
using System.Collections.Generic;
using System.IO;
using AwesomeAssertions;
using SolTechnology.Avro;
using SolTechnology.Avro.Infrastructure.Exceptions;
using NUnit.Framework;

namespace AvroConvertComponentTests.FilesDeserialization.Default
{
    public class DeserializeFilesTests
    {
        private readonly byte[] _avroBytes = File.ReadAllBytes("example2.avro");
        private readonly byte[] _schemaOnlyAvroBytes = File.ReadAllBytes("header_only.avro");


        [Test]
        public void Deserialize_FileContainsNoAvroData_NoExceptionIsThrown([ValueSource(typeof(Formats), nameof(Formats.Readers))] Format format)
        {
            //Arrange


            //Act
            var result = RoundTrip.Read<List<UserNameClass>>(_schemaOnlyAvroBytes, format);


            //Assert
            result.Should().BeNullOrEmpty();
        }


        [Test]
        [Property("Fix", "https://github.com/AdrianStrugala/AvroConvert/issues/152")]
        public void Deserialize_FileWithMultipleBlocks_EveryItemIsRead([ValueSource(typeof(Formats), nameof(Formats.Readers))] Format format)
        {
            //Arrange


            //Act
            var result = RoundTrip.Read<List<kylosample>>(File.ReadAllBytes("userdata1.avro"), format);


            //Assert
            result.Should().HaveCount(1000);
        }


        [Test]
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
            result.Should().BeEquivalentTo(expectedResult, o => o.WithStrictOrdering());
        }

        [Test]
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
            var result = (List<UserNameClass>)AvroConvert.Deserialize(_avroBytes, typeof(List<UserNameClass>));


            //Assert
            result.Should().BeEquivalentTo(expectedResult);
        }

        [Test]
        public void Deserialize_InvalidFile_InvalidAvroObjectExceptionIsThrown()
        {
            //Arrange
            byte[] invalidBytes = new byte[2137];


            //Act
            var result = Recorded.Exception(() => AvroConvert.Deserialize<int>(invalidBytes));


            //Assert
            result.Should().BeOfType<InvalidAvroObjectException>();
        }

        [Test]
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
            result.Should().BeEquivalentTo(expectedResult, o => o.WithStrictOrdering());
        }
    }
}
