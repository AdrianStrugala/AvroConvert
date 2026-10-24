using System.Collections.Generic;
using System.IO;
using System.Linq;
using AutoFixture;
using AwesomeAssertions;
using SolTechnology.Avro;
using SolTechnology.Avro.Infrastructure.Exceptions;
using NUnit.Framework;

namespace AvroConvertComponentTests.Merge
{
    public class MergeTests
    {
        private readonly Fixture _fixture;
        private readonly byte[] _headerOnlyAvroBytes = File.ReadAllBytes("header_only.avro");


        public MergeTests()
        {
            _fixture = new Fixture();
        }

        [Test]
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
            var deserializedUser = deserializedResult.Should().ContainSingle().Subject;
            deserializedUser.Should().BeEquivalentTo(user);
        }


        [Test]
        public void Merge_FileWithHeaderOnly_DoesNotThrowException()
        {
            //Arrange

            //Act
            var result = AvroConvert.Merge<User>(new List<byte[]> { _headerOnlyAvroBytes});


            //Assert
            var deserializedResult = AvroConvert.Deserialize<List<User>>(result);
            deserializedResult.Should().BeEmpty();
        }

        [Test]
        public void Merge_MultipleObject_AllOfThemAreMerged()
        {
            //Arrange
            var users = _fixture.CreateMany<User>();

            var avroObjects = users.Select(AvroConvert.Serialize);


            //Act 
            var result = AvroConvert.Merge<User>(avroObjects);


            //Assert
            var deserializedResult = AvroConvert.Deserialize<List<User>>(result);
            deserializedResult.Should().NotBeNull();
            deserializedResult.Count.Should().Be(users.Count());

            foreach (var user in users)
            {
                var resultUser = deserializedResult.FirstOrDefault(r => r.name == user.name);
                resultUser.Should().NotBeNull();
                resultUser.Should().BeEquivalentTo(user);
            }
        }

        [Test]
        public void Merge_OnOfTheObjectsIsOfDifferentSchema_ExceptionIsThrown()
        {
            var users = _fixture.CreateMany<User>();

            var avroObjects = users.Select(AvroConvert.Serialize).ToArray();

            avroObjects[1] = AvroConvert.Serialize(_fixture.Create<string>());


            //Act 
            var exception = Recorded.Exception(() => AvroConvert.Merge<User>(avroObjects));


            //Assert
            exception.Should().NotBeNull();
            exception.Should().BeOfType<InvalidAvroObjectException>();
            exception.Message.Should().Contain("1");
            exception.Message.Should().Contain(typeof(User).ToString());
        }

        [Test]
        public void Merge_NotAvroObject_ExceptionIsThrown()
        {
            var avroObjects = _fixture.CreateMany<byte[]>();

            //Act 
            var exception = Recorded.Exception(() => AvroConvert.Merge<User>(avroObjects));


            //Assert
            exception.Should().NotBeNull();
            exception.Should().BeOfType<InvalidAvroObjectException>();
        }

        [Test]
        public void Merge_TwoFilesWithMultipleBlocks_EveryItemIsMerged()
        {
            //Arrange
            var content = File.ReadAllBytes("userdata1.avro");


            //Act
            var result = AvroConvert.Merge<kylosample>(new List<byte[]> { content, content });
            var toCheck = AvroConvert.Deserialize<List<kylosample>>(result);


            //Assert
            toCheck.Should().HaveCount(2000);
        }
    }
}
