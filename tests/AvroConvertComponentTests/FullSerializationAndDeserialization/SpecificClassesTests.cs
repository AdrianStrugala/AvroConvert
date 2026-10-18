using System;
using AutoFixture;
using AwesomeAssertions;
using AwesomeAssertions.Extensions;
using NUnit.Framework;

namespace AvroConvertComponentTests.FullSerializationAndDeserialization
{
    public class SpecificClassesTests
    {
        private readonly Fixture _fixture = new();

        [Test]
        public void NetRecord([ValueSource(typeof(Formats), nameof(Formats.All))] Format format)
        {
            //Arrange
            TestRecord testRecord = new TestRecord();
            testRecord.Name = _fixture.Create<string>();

            //Act
            var deserialized = RoundTrip.Run<TestRecord>(testRecord, format);

            //Assert
            deserialized.Should().NotBeNull();
            deserialized.Name.Should().Be(testRecord.Name);
        }

        [Test]
        public void AvroLogicalTypes([ValueSource(typeof(Formats), nameof(Formats.All))] Format format)
        {
            //Arrange
            var testObject = _fixture.Create<LogicalTypesClass>();

            //Act
            var deserialized = RoundTrip.Run<LogicalTypesClass>(testObject, format);

            //Assert
            deserialized.Should().NotBeNull();
            deserialized.One.Should().Be(testObject.One);
            deserialized.Two.Should().Be(testObject.Two);

            deserialized.Three.Milliseconds.Should().Be(testObject.Three.Milliseconds);
            deserialized.Three.Seconds.Should().Be(testObject.Three.Seconds);
            deserialized.Three.Minutes.Should().Be(testObject.Three.Minutes);
            deserialized.Three.Hours.Should().Be(testObject.Three.Hours);
            deserialized.Three.Days.Should().Be(testObject.Three.Days);

            deserialized.Four!.Value.Should().BeCloseTo(testObject.Four!.Value, precision: TimeSpan.FromTicks(10),
                "According to the Avro documentation, the most accurate precision is microseconds");

            deserialized.Five.Microsecond().Should().Be(testObject.Five.Microsecond());
            deserialized.Five.Millisecond.Should().Be(testObject.Five.Millisecond);
            deserialized.Five.Second.Should().Be(testObject.Five.Second);
            deserialized.Five.Minute.Should().Be(testObject.Five.Minute);
            deserialized.Five.Hour.Should().Be(testObject.Five.Hour);
            deserialized.Five.Day.Should().Be(testObject.Five.Day);
            deserialized.Five.Month.Should().Be(testObject.Five.Month);
            deserialized.Five.Year.Should().Be(testObject.Five.Year);

        }

        [Test]
        public void ClassInheritingFromBaseClass([ValueSource(typeof(Formats), nameof(Formats.All))] Format format)
        {
            //Arrange
            InheritingClass testObject = _fixture.Create<InheritingClass>();

            //Act
            var deserialized = RoundTrip.Run<InheritingClass>(testObject, format);

            //Assert
            deserialized.Should().BeEquivalentTo(testObject);
        }

        [Test]
        public void ClassInheritingFromInterface([ValueSource(typeof(Formats), nameof(Formats.All))] Format format)
        {
            //Arrange
            InheritingClassFromInterface testObject = _fixture.Create<InheritingClassFromInterface>();

            //Act
            var deserialized = RoundTrip.Run<InheritingClassFromInterface>(testObject, format);

            //Assert
            deserialized.Should().BeEquivalentTo(testObject);
        }

        [Test]
        public void ClassWithDuplicateTypeProperties([ValueSource(typeof(Formats), nameof(Formats.All))] Format format)
        {
            //Arrange
            ClassWithDuplicateTypes testObject = _fixture.Create<ClassWithDuplicateTypes>();

            //Act
            var deserialized = RoundTrip.Run<ClassWithDuplicateTypes>(testObject, format);

            //Assert
            deserialized.Should().BeEquivalentTo(testObject);
        }
    }
}
