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

        public class Account
        {
            public string UserName { get; set; }
            public System.Collections.Generic.Dictionary<int, string> Ids { get; set; }
        }

        [Test]
        [NonParallelizable] // JsonConvert.DefaultSettings is global
        public void Avro2Json_IgnoresTheGlobalJsonSettings_NamesAreTheSchemaNames()
        {
            var avro = AvroConvert.Serialize(new Account { UserName = "x", Ids = new() { [5] = "five" } });
            var previous = JsonConvert.DefaultSettings;
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver(),
                TypeNameHandling = TypeNameHandling.Objects
            };
            try
            {
                AvroConvert.Avro2Json(avro).Should().Be("{\"UserName\":\"x\",\"Ids\":{\"5\":\"five\"}}");
            }
            finally
            {
                JsonConvert.DefaultSettings = previous;
            }
        }

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
