using System.Collections.Generic;
using System.IO;
using System.Linq;
using AutoFixture;
using FluentAssertions;
using SolTechnology.Avro;
using Xunit;

namespace AvroConvertComponentTests.DeserializeByLine
{
    public class DeserializeByLineTests
    {
        private readonly Fixture _fixture;
        private readonly byte[] _avroBytes = System.IO.File.ReadAllBytes("example2.avro");

        public DeserializeByLineTests()
        {
            _fixture = new Fixture();
        }

        [Fact]
        public void Deserialize_BlockData_ItIsCorrectlyDeserialized()
        {
            //Arrange
            var expectedResult = new List<User>();
            expectedResult.Add(new User
            {
                name = "Alyssa",
                favorite_number = 256,
                favorite_color = null
            });

            expectedResult.Add(new User
            {
                name = "Ben",
                favorite_number = 7,
                favorite_color = "red"
            });

            //Act
            var result = new List<User>();
            using (var reader = AvroConvert.OpenDeserializer<User>(new MemoryStream(_avroBytes)))
            {
                while (reader.HasNext())
                {
                    var item = reader.ReadNext();

                    result.Add(item);
                }
            }


            //Assert
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void SerializeClassWithList_ThenDeserialize_ListsAreEqual()
        {
            //Arrange
            var someTestClasses = _fixture.CreateMany<ClassWithSimpleList>().ToList();


            //Act
            var serialized = AvroConvert.Serialize(someTestClasses);

            var result = new List<ClassWithSimpleList>();

            using (var reader = AvroConvert.OpenDeserializer<ClassWithSimpleList>(new MemoryStream(serialized)))
            {
                while (reader.HasNext())
                {
                    var item = reader.ReadNext();

                    result.Add(item);
                }
            }


            //Assert
            Assert.NotNull(result);
            Assert.Equal(someTestClasses, result);
        }

        [Fact]
        public void Component_ObjectIsList_ResultIsTheSameAsInput()
        {
            //Arrange
            List<int> list = _fixture.Create<List<int>>();


            //Act
            var serialized = AvroConvert.Serialize(list);

            var result = new List<int>();

            using (var reader = AvroConvert.OpenDeserializer<int>(new MemoryStream(serialized)))
            {
                while (reader.HasNext())
                {
                    var item = reader.ReadNext();

                    result.Add(item);
                }
            }


            //Assert
            Assert.NotNull(result);
            Assert.NotNull(serialized);
            Assert.Equal(list, result);
        }

        [Fact]
        public void Component_ObjectIsArray_ResultIsTheSameAsInput()
        {
            //Arrange
            int[] array = _fixture.Create<int[]>();


            //Act
            var serialized = AvroConvert.Serialize(array);

            var result = new List<int>();

            using (var reader = AvroConvert.OpenDeserializer<int>(new MemoryStream(serialized)))
            {
                while (reader.HasNext())
                {
                    var item = reader.ReadNext();

                    result.Add(item);
                }
            }


            //Assert
            Assert.NotNull(result);
            Assert.NotNull(serialized);
            Assert.Equal(array, result);
        }

        [Fact]
        public void Component_ObjectIsHashSet_ResultIsTheSameAsInput()
        {
            //Arrange
            HashSet<int> hashset = _fixture.Create<HashSet<int>>();


            //Act
            var serialized = AvroConvert.Serialize(hashset);

            var result = new List<int>();

            using (var reader = AvroConvert.OpenDeserializer<int>(new MemoryStream(serialized)))
            {
                while (reader.HasNext())
                {
                    var item = reader.ReadNext();

                    result.Add(item);
                }
            }

            //Assert
            Assert.NotNull(result);
            Assert.NotNull(serialized);
            Assert.Equal(hashset, result);
        }

        [Fact]
        public void Component_HeadlessList_ResultIsTheSameAsInput()
        {
            //Arrange
            List<int> list = _fixture.Create<List<int>>();

            var schema = AvroConvert.GenerateSchema(typeof(List<int>));

            //Act
            var serialized = AvroConvert.SerializeHeadless(list, schema);

            var result = new List<int>();

            using (var reader = AvroConvert.OpenDeserializer<int>(new MemoryStream(serialized)))
            {
                while (reader.HasNext())
                {
                    var item = reader.ReadNext();

                    result.Add(item);
                }
            }


            //Assert
            Assert.NotNull(result);
            Assert.NotNull(serialized);
            Assert.Equal(list, result);
        }



        [Fact]
        public void DeserializeByLine_ReadFromFileStream_ResultIsAsExpected()
        {
            //Arrange
            var expectedResult = new List<User>();
            expectedResult.Add(new User
            {
                name = "Alyssa",
                favorite_number = 256,
                favorite_color = null
            });

            expectedResult.Add(new User
            {
                name = "Ben",
                favorite_number = 7,
                favorite_color = "red"
            });

            var result = new List<User>();


            //Act
            using (var stream = File.OpenRead("example2.avro"))
            {
                using (var reader = AvroConvert.OpenDeserializer<User>(stream))
                {
                    while (reader.HasNext())
                    {
                        var item = reader.ReadNext();

                        result.Add(item);
                    }
                }
            }

            //Assert
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public void DeserializeByLine_FileContainsBlocksOfArrays_ResultCountIsAsExpected()
        {
            //Arrange
            var result = new List<kylosample>();



            //Act
            using (var stream = File.OpenRead("userdata1.avro"))
            {
                using (var reader = AvroConvert.OpenDeserializer<kylosample>(stream))
                {
                    while (reader.HasNext())
                    {
                        var item = reader.ReadNext();

                        result.Add(item);
                    }
                }
            }


            //Assert
            result.Should().HaveCount(1000);
        }
    }
   


}
