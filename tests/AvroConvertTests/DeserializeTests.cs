using SolTechnology.Avro;
using SolTechnology.Avro.Exceptions;

namespace AvroConvertTests
{
    using System.Collections.Generic;
    using Xunit;

    public class DeserializeTests
    {
        private readonly byte[] _avroBytes = System.IO.File.ReadAllBytes("example2.avro");


        [Fact]
        public void Deserialize_CustomSchema_OnlyValuesFromCustomSchemaAreReturned()
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
            var result = AvroConvert.Deserialize<List<UserNameClass>>(_avroBytes);


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
    }
}
