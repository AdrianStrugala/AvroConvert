using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using Xunit;

namespace AvroConvertComponentTests.FullSerializationAndDeserialization
{
    public class ConcurrentCollectionClassesTests
    {
        private readonly Fixture _fixture = new();

        [Theory]
        [MemberData(nameof(TestEngine.All), MemberType = typeof(TestEngine))]
        public void Class_with_ConcurrentBag(Func<object, Type, dynamic> engine)
        {
            //Arrange
            ConcurrentBagClass toSerialize = new ConcurrentBagClass
            {
                concurentBagField =  new ConcurrentBag<ComplexClassWithoutGetters>(_fixture.CreateMany<ComplexClassWithoutGetters>())
            };


            //Act
            var deserialized = engine.Invoke(toSerialize, typeof(ConcurrentBagClass));


            //Assert
            Assert.NotNull(deserialized);
            Assert.Equal(toSerialize, deserialized);
        }

        [Theory]
        [MemberData(nameof(TestEngine.DefaultOnly), MemberType = typeof(TestEngine))]
        public void MultiThread_serialization(Func<object, Type, dynamic> engine)
        {
            //Arrange
            VeryComplexClass testClass = _fixture.Create<VeryComplexClass>();


            //Act
            List<Task> listOfTasks = new List<Task>();

            for (var counter = 0; counter < 7; counter++)
            {
                listOfTasks.Add(Task.Run(() =>
                {
                    for (var iMessagesCntr = 0; iMessagesCntr < 100; iMessagesCntr++)
                    {
                        var deserialized = engine.Invoke(testClass, typeof(VeryComplexClass));

                        //Assert
                        Assert.Equal(testClass, deserialized);

                        Thread.Sleep(counter);
                    }
                }));
            }

            Task.WaitAll(listOfTasks.ToArray());
        }
    }
}
