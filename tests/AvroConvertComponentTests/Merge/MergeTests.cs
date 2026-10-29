using System.Collections.Generic;
using System.IO;
using System.Linq;
using AutoFixture;
using AwesomeAssertions;
using SolTechnology.Avro;
using SolTechnology.Avro.Infrastructure.Exceptions;
using NUnit.Framework;
using SolTechnology.Avro.Infrastructure.Attributes;
using OldRow = AvroConvertComponentTests.Merge.Old.Row;

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

        [Test]
        public void Merge_ObjectsWrittenWithAnOlderClass_AreReadWithTheirWriterSchema()
        {
            //Arrange: the files have the fields in another order than the class they are merged into
            var rows = new[] { new OldRow { Name = "a", Id = 1 }, new OldRow { Name = "b", Id = 2 } };
            var files = rows.Select(r => AvroConvert.Serialize(r));


            //Act
            var result = AvroConvert.Merge<Row>(files);


            //Assert
            AvroConvert.Deserialize<List<Row>>(result).Should().BeEquivalentTo(rows);
        }

        [Test]
        public void Merge_ObjectsWrittenWithDifferentSchemas_Throws()
        {
            //Arrange
            var files = new[] { AvroConvert.Serialize(new OldRow { Name = "a", Id = 1 }), AvroConvert.Serialize(new Row { Id = 2, Name = "b" }) };


            //Act
            var act = () => AvroConvert.Merge<Row>(files);


            //Assert
            act.Should().Throw<InvalidAvroObjectException>().WithMessage("*another schema*");
        }

        public class Stamp
        {
            public System.DateTime At { get; set; }
        }

        [Test]
        public void Merge_ObjectsWithDifferentLogicalTypes_Throws()
        {
            //Arrange: timestamp-micros and timestamp-millis have the same parsing canonical form
            var at = new System.DateTime(2024, 5, 6, 7, 8, 9, System.DateTimeKind.Utc);
            var files = new[]
            {
                AvroConvert.Serialize(new Stamp { At = at }),
                AvroConvert.Serialize(new Stamp { At = at }, new AvroConvertOptions { DateTimeRepresentation = AvroTypeRepresentation.TimestampMilliseconds })
            };


            //Act
            var act = () => AvroConvert.Merge<Stamp>(files);


            //Assert
            act.Should().Throw<InvalidAvroObjectException>().WithMessage("*another schema*");
        }
    }
}

namespace AvroConvertComponentTests.Merge
{
    public class Row
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}

namespace AvroConvertComponentTests.Merge.Old
{
    public class Row
    {
        public string Name { get; set; }
        public int Id { get; set; }
    }
}
