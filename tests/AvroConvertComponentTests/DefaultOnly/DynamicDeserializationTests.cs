using SolTechnology.Avro;
using System;
using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using AutoFixture.Kernel;
using AwesomeAssertions;
using NUnit.Framework;

namespace AvroConvertComponentTests.DefaultOnly
{
    public class DynamicDeserializationTests
    {
        readonly Fixture _fixture = new();

        [TestCase(typeof(int))]
        [TestCase(typeof(long))]
        [TestCase(typeof(string))]
        [TestCase(typeof(decimal))]
        [TestCase(typeof(double))]
        [TestCase(typeof(float))]
        [TestCase(typeof(Uri))]
        [TestCase(typeof(Guid))]
        public void Dynamic_deserialization_works_for_primitives(Type type)
        {
            //Arrange
            var input = new SpecimenContext(_fixture).Resolve(type);

            //Act
            var result = AvroConvert.Serialize(input);

            var deserialized = AvroConvert.Deserialize<dynamic>(result);

            //Assert
            result.Should().NotBeNull();

            // A Uri is written as an Avro string, so without a target type it is read back as that string.
            var expected = input is Uri uri ? uri.ToString() : input;
            ((object)deserialized).Should().BeEquivalentTo(expected);
        }

        [Test]
        public void Dynamic_deserialization_works_for_complex_types()
        {
            //Arrange
            var input = _fixture.Create<User>();

            //Act
            var result = AvroConvert.Serialize(input);

            var deserialized = AvroConvert.Deserialize<dynamic>(result);

            //Assert
            result.Should().NotBeNull();

            ((object)deserialized.favorite_color).Should().Be(input.favorite_color);
            ((object)deserialized.favorite_number).Should().Be(input.favorite_number);
            ((object)deserialized.name).Should().Be(input.name);
        }

        [Test]
        public void Dynamic_deserialization_works_for_collections()
        {
            //Arrange
            var input = _fixture.CreateMany<User>();

            //Act
            var result = AvroConvert.Serialize(input);

            var deserialized = AvroConvert.Deserialize<List<dynamic>>(result);

            //Assert
            result.Should().NotBeNull();

            deserialized.Count.Should().Be(input.Count());
            ((object)deserialized.First().favorite_color).Should().Be(input.First().favorite_color);
            ((object)deserialized.First().favorite_number).Should().Be(input.First().favorite_number);
            ((object)deserialized.First().name).Should().Be(input.First().name);
        }

        [Test]
        public void Dynamic_deserialization_works_for_very_complex_types()
        {
            //Arrange
            var input = _fixture.Create<VeryComplexClass>();

            //Act
            var result = AvroConvert.Serialize(input);

            var deserialized = AvroConvert.Deserialize<dynamic>(result);

            //Assert
            result.Should().NotBeNull();

            ((object)deserialized.ClassesWithArray[0].theArray[0]).Should().Be(input.ClassesWithArray[0].theArray[0]);
            ((object)deserialized.ClassesWithGuid[0].theGuid).Should().Be(input.ClassesWithGuid[0].theGuid);
            ((object)deserialized.floatProperty).Should().Be(input.floatProperty);
            ((object)deserialized.Size).Should().Be(input.Size.Value);
        }
    }
}
