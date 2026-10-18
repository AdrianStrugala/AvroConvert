using AwesomeAssertions;
using System;
using System.IO;
using Newtonsoft.Json;
using SolTechnology.Avro;
using NUnit.Framework;

namespace AvroConvertComponentTests.Avro2Json
{
    public class Avro2JsonTests
    {
        private readonly byte[] _headerOnlyAvroBytes = File.ReadAllBytes("header_only.avro");

        [Test]
        public void Avro2Json_ConvertNull_ProducedDesiredJson()
        {
            //Arrange
            string nullTestObject = null;

            var expectedJson = JsonConvert.SerializeObject(nullTestObject);

            var avroSerialized = AvroConvert.Serialize(nullTestObject);


            //Act
            var resultJson = AvroConvert.Avro2Json(avroSerialized);


            //Assert
            resultJson.Should().Be(expectedJson);
        }

        [Test]
        public void Avro2Json_ConvertFileContainingHeaderOnly_NoExceptionIsThrown()
        {
            //Arrange


            //Act
            var resultJson = AvroConvert.Avro2Json(_headerOnlyAvroBytes);


            //Assert
            resultJson.Should().Be(@"""""");
        }
        
    }
}
