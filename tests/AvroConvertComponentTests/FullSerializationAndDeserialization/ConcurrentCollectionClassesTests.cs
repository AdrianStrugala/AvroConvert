using AwesomeAssertions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using NUnit.Framework;

namespace AvroConvertComponentTests.FullSerializationAndDeserialization
{
    public class ConcurrentCollectionClassesTests
    {
        private readonly Fixture _fixture = new();

        [Test]
        public void Class_with_ConcurrentBag([ValueSource(typeof(Formats), nameof(Formats.All))] Format format)
        {
            //Arrange
            ConcurrentBagClass toSerialize = new ConcurrentBagClass
            {
                concurentBagField =  new ConcurrentBag<ComplexClassWithoutGetters>(_fixture.CreateMany<ComplexClassWithoutGetters>())
            };


            //Act
            var deserialized = RoundTrip.Run<ConcurrentBagClass>(toSerialize, format);


            //Assert
            deserialized.Should().NotBeNull();
            deserialized.Should().BeEquivalentTo(toSerialize);
        }

        [Test]
        public void MultiThread_serialization([ValueSource(typeof(Formats), nameof(Formats.ContainerOnly))] Format format)
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
                        var deserialized = RoundTrip.Run<VeryComplexClass>(testClass, format);

                        //Assert
                        deserialized.Should().BeEquivalentTo(testClass);

                        Thread.Sleep(counter);
                    }
                }));
            }

            Task.WaitAll(listOfTasks.ToArray());
        }
    }
}
