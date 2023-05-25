using System;
using System.IO;
using Newtonsoft.Json;
using SolTechnology.Avro;
using Xunit;

namespace AvroConvertComponentTests.Avro2Json
{
    public class Avro2JsonTests
    {
        private readonly byte[] _headerOnlyAvroBytes = File.ReadAllBytes("header_only.avro");

        [Fact]
        public void Avro2Json_ConvertNull_ProducedDesiredJson()
        {
            //Arrange
            string nullTestObject = null;

            var expectedJson = JsonConvert.SerializeObject(nullTestObject);

            var avroSerialized = AvroConvert.Serialize(nullTestObject);


            //Act
            var resultJson = AvroConvert.Avro2Json(avroSerialized);


            //Assert
            Assert.Equal(expectedJson, resultJson);
        }

        [Fact]
        public void Avro2Json_ConvertFileContainingHeaderOnly_NoExceptionIsThrown()
        {
            //Arrange


            //Act
            var resultJson = AvroConvert.Avro2Json(_headerOnlyAvroBytes);


            //Assert
            Assert.Equal(@"""""", resultJson);
        }
        
    }
}
