using System.Collections.Generic;
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
            var avroObject = AvroConvert.Serialize(user);


            //Act
            var result = AvroConvert.Merge<User>(new List<byte[]> { avroObject });

            var ForComaprison = AvroConvert.Serialize(new List<User> { user });


            //Assert
            var deserializedResult = AvroConvert.Deserialize<List<User>>(result);
            var deserializedUser = Assert.Single(deserializedResult);
            Assert.Equal(user, deserializedUser);
        }

        [Fact]
        public void Merge_MultipleObject_AllOfThemAreMerged()
        {
            //Arrange


            //Act
            // var resultJson = AvroConvert.Merge();


            //Assert
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
