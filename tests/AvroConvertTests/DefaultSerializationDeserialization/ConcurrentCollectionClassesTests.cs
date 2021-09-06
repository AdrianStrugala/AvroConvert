using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using SolTechnology.Avro;
using Xunit;

namespace AvroConvertComponentTests.DefaultSerializationDeserialization
{
    public class ConcurrentCollectionClassesTests
    {
        private readonly Fixture _fixture;

        public ConcurrentCollectionClassesTests()
        {
            _fixture = new Fixture();
        }

        [Fact]
        public void Component_SerializeConcurrentBagClass_ResultIsTheSameAsInput()
        {
            //Arrange
            ConcurrentBagClass toSerialize = _fixture.Create<ConcurrentBagClass>();


            //Act
            var result = AvroConvert.Serialize(toSerialize);

            var deserialized = AvroConvert.Deserialize<ConcurrentBagClass>(result);


            //Assert
            Assert.NotNull(result);
            Assert.NotNull(deserialized);
            Assert.Equal(toSerialize, deserialized);
        }

        [Fact]
        public void Serialize_MultiThreadSerialization_NoExceptionIsThrown()
        {
            //Arrange
            VeryComplexClass testClass = _fixture.Create<VeryComplexClass>();


            //Act
            List<Task> listOfTasks = new List<Task>();

            for (var counter = 0; counter < 10; counter++)
            {
                listOfTasks.Add(Task.Run(() =>
                {
                    for (var iMessagesCntr = 0; iMessagesCntr < 100; iMessagesCntr++)
                    {
                        var result = AvroConvert.Serialize(testClass);
                        var deserialized = AvroConvert.Deserialize<VeryComplexClass>(result);

                        //Assert
                        Assert.Equal(testClass, deserialized);

                        Thread.Sleep(counter
);
                    }
                }));
            }

            Task.WaitAll(listOfTasks.ToArray());
        }
    }
}
