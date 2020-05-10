using Newtonsoft.Json;
using SolTechnology.Avro;
using Xunit;

namespace AvroConvertTests.AvroToJson
{
    public class Avro2JsonTests
    {
        [Fact]
        public void Avro2Json_ConvertComplexType_ProducedDesiredJson()
        {
            //Arrange
            var user = new User();
            user.favorite_color = "blue";
            user.favorite_number = 2137;
            user.name = "red";

            var expectedJson = JsonConvert.SerializeObject(user);

            var avroSerialized = AvroConvert.Serialize(user);


            //Act
            var resultJson = AvroConvert.Avro2Json(avroSerialized);


            //Assert
            Assert.Equal(expectedJson, resultJson);
        }

        [Fact]
        public void Avro2Json_ConvertString_ProducedDesiredJson()
        {
            //Arrange
            var @string = "I am the serialization string";

            var expectedJson = JsonConvert.SerializeObject(@string);

            var avroSerialized = AvroConvert.Serialize(@string);


            //Act
            var resultJson = AvroConvert.Avro2Json(avroSerialized);


            //Assert
            Assert.Equal(expectedJson, resultJson);
        }

        [Fact]
        public void Avro2Json_ConvertInt_ProducedDesiredJson()
        {
            //Arrange
            var @int = 2137;

            var expectedJson = JsonConvert.SerializeObject(@int);

            var avroSerialized = AvroConvert.Serialize(@int);


            //Act
            var resultJson = AvroConvert.Avro2Json(avroSerialized);


            //Assert
            Assert.Equal(expectedJson, resultJson);
        }

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
        public void Avro2Json_ConvertDouble_ProducedDesiredJson()
        {
            //Arrange
            var doubleTestObject = 21.34;

            var expectedJson = JsonConvert.SerializeObject(doubleTestObject);

            var avroSerialized = AvroConvert.Serialize(doubleTestObject);


            //Act
            var resultJson = AvroConvert.Avro2Json(avroSerialized);


            //Assert
            Assert.Equal(expectedJson, resultJson);
        }

        [Fact]
        public void Avro2Json_ConvertArray_ProducedDesiredJson()
        {
            //Arrange
            var arrayTestObject = new int[] { 2, 1, 3, 7, 453, 1, 6, };

            var expectedJson = JsonConvert.SerializeObject(arrayTestObject);

            var avroSerialized = AvroConvert.Serialize(arrayTestObject);


            //Act
            var resultJson = AvroConvert.Avro2Json(avroSerialized);


            //Assert
            Assert.Equal(expectedJson, resultJson);
        }
    }
}
