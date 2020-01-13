using System.Collections.Generic;
using AvroConvertTests.RealExample2;
using SolTechnology.Avro;
using Xunit;

namespace AvroConvertTests.UserDataTests
{
    public class Tests
    {
        private readonly string userData1 = "UserDataTests/userdata1.avro";

        [Fact]
        public void Deserialize_CustomSchema_OnlyValuesFromCustomSchemaAreReturned()
        {
            //Arrange

            //Act
            var result = AvroConvert.Deserialize<List<ModelItem>>(System.IO.File.ReadAllBytes(userData1));

            var xd = result;

            //Assert
            //            Assert.Equal(expectedResult, result);
        }
    }
}
