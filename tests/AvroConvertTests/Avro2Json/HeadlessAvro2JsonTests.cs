using Newtonsoft.Json;
using SolTechnology.Avro;
using Xunit;

namespace AvroConvertComponentTests.AvroToJson
{
    public class HeadlessAvro2JsonTests
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

            var schema = AvroConvert.GenerateSchema(user.GetType());
            var avroSerialized = AvroConvert.SerializeHeadless(user, schema);


            //Act
            var resultJson = AvroConvert.Avro2Json(avroSerialized, schema);


            //Assert
            Assert.Equal(expectedJson, resultJson);
        }

        [Fact]
        public void Avro2Json_ConvertString_ProducedDesiredJson()
        {
            //Arrange
            var @string = "I am the serialization string";

            var expectedJson = JsonConvert.SerializeObject(@string);

            var schema = AvroConvert.GenerateSchema(@string.GetType());
            var avroSerialized = AvroConvert.SerializeHeadless(@string, schema);


            //Act
            var resultJson = AvroConvert.Avro2Json(avroSerialized, schema);


            //Assert
            Assert.Equal(expectedJson, resultJson);
        }

        [Fact]
        public void Avro2Json_ConvertInt_ProducedDesiredJson()
        {
            //Arrange
            var @int = 2137;

            var expectedJson = JsonConvert.SerializeObject(@int);

            var schema = AvroConvert.GenerateSchema(@int.GetType());
            var avroSerialized = AvroConvert.SerializeHeadless(@int, schema);


            //Act
            var resultJson = AvroConvert.Avro2Json(avroSerialized, schema);


            //Assert
            Assert.Equal(expectedJson, resultJson);
        }

        [Fact]
        public void Avro2Json_ConvertNull_ProducedDesiredJson()
        {
            //Arrange
            string nullTestObject = null;

            var expectedJson = JsonConvert.SerializeObject(nullTestObject);

            var schema = AvroConvert.GenerateSchema(nullTestObject?.GetType());
            var avroSerialized = AvroConvert.SerializeHeadless(nullTestObject, schema);


            //Act
            var resultJson = AvroConvert.Avro2Json(avroSerialized, schema);


            //Assert
            Assert.Equal(expectedJson, resultJson);
        }

        [Fact]
        public void Avro2Json_ConvertDouble_ProducedDesiredJson()
        {
            //Arrange
            var doubleTestObject = 21.34;

            var expectedJson = JsonConvert.SerializeObject(doubleTestObject);

            var schema = AvroConvert.GenerateSchema(doubleTestObject.GetType());
            var avroSerialized = AvroConvert.SerializeHeadless(doubleTestObject, schema);


            //Act
            var resultJson = AvroConvert.Avro2Json(avroSerialized, schema);


            //Assert
            Assert.Equal(expectedJson, resultJson);
        }

        [Fact]
        public void Avro2Json_ConvertArray_ProducedDesiredJson()
        {
            //Arrange
            var arrayTestObject = new int[] { 2, 1, 3, 7, 453, 1, 6, };

            var expectedJson = JsonConvert.SerializeObject(arrayTestObject);

            var schema = AvroConvert.GenerateSchema(arrayTestObject.GetType());
            var avroSerialized = AvroConvert.SerializeHeadless(arrayTestObject, schema);


            //Act
            var resultJson = AvroConvert.Avro2Json(avroSerialized, schema);


            //Assert
            Assert.Equal(expectedJson, resultJson);
        }
    }
}
