using System.Collections.Generic;
using Newtonsoft.Json;
using Xunit;

namespace AvroConvertComponentTests.Avro2Json
{
    public class JsonTests
    {
        [Fact]
        public void Json_SerializeObjectAndDictionaryWithItemsNamedLikeProperties_TheyAreEqual()
        {
            //Arrange
            var user = new User();
            user.favorite_color = "blue";
            user.favorite_number = 2137;
            user.name = "red";

            var userDictionary = new Dictionary<string, object>();
            userDictionary.Add("name", "red");
            userDictionary.Add("favorite_number", 2137);
            userDictionary.Add("favorite_color", "blue");


            //Act
            var json1 = JsonConvert.SerializeObject(user);
            var json2 = JsonConvert.SerializeObject(userDictionary);


            //Assert
            Assert.Equal(json1, json2);
        }
    }
}
