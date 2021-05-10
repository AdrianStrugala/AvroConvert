using System;
using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using SolTechnology.Avro;
using Xunit;

namespace AvroConvertComponentTests.Merge
{
    public class MergeTests
    {
        private readonly Fixture _fixture;

        public MergeTests()
        {
            _fixture = new AutoFixture.Fixture();
        }

        [Fact]
        public void Merge_SingleObject_ItIsConvertedToEnumerableWithSingleItem()
        {
            //Arrange
            var user = _fixture.Create<User>();

            user.favorite_color = "red";
            user.favorite_number = 2137;
            user.name = "Ash";

            var avroObject = AvroConvert.Serialize(user);


            //Act
            var result = AvroConvert.Merge<User>(new List<byte[]> { avroObject });


            //Assert
            var deserializedResult = AvroConvert.Deserialize<List<User>>(result);
            var deserializedUser = Assert.Single(deserializedResult);
            Assert.Equal(user, deserializedUser);
        }

        [Fact]
        public void Merge_MultipleObject_AllOfThemAreMerged()
        {
            var users = _fixture.CreateMany<User>();

            var avroObjects = users.Select(AvroConvert.Serialize);


            //Act
            var result = AvroConvert.Merge<User>(avroObjects);


            //Assert
            var deserializedResult = AvroConvert.Deserialize<List<User>>(result);
            Assert.NotNull(deserializedResult);
            Assert.Equal(users.Count(), deserializedResult.Count);

            foreach (var user in users)
            {
                var resultUser = deserializedResult.FirstOrDefault(r => r.name == user.name);
                Assert.NotNull(resultUser);
                Assert.Equal(user, resultUser);
            }
        }

        [Fact]
        public void Merge_OnOfTheObjectsIsOfDifferentSchema_ExceptionIsThrown()
        {
            //Arrange


            //Act
            // var resultJson = AvroConvert.Merge();


            //Assert
        }

        [Fact]
        public void Merge_NotAvroObject_ExceptionIsThrown()
        {
            //Arrange


            //Act
            // var resultJson = AvroConvert.Merge();


            //Assert
        }
    }
}
