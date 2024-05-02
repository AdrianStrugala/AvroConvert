using System;
using AutoFixture;
using FluentAssertions;
using FluentAssertions.Extensions;
using Xunit;

namespace AvroConvertComponentTests.FullSerializationAndDeserialization
{
    public class SpecificClassesTests
    {
        private readonly Fixture _fixture = new();

        [Theory]
        [MemberData(nameof(TestEngine.All), MemberType = typeof(TestEngine))]
        public void NetRecord(Func<object, Type, dynamic> engine)
        {
            //Arrange
            TestRecord testRecord = new TestRecord();
            testRecord.Name = _fixture.Create<string>();

            //Act
            var deserialized = engine.Invoke(testRecord, typeof(TestRecord));

            //Assert
            Assert.NotNull(deserialized);
            Assert.Equal(testRecord.Name, deserialized.Name);
        }

        [Theory]
        [MemberData(nameof(TestEngine.All), MemberType = typeof(TestEngine))]
        public void AvroLogicalTypes(Func<object, Type, dynamic> engine)
        {
            //Arrange
            var testObject = _fixture.Create<LogicalTypesClass>();

            //Act
            var deserialized = (LogicalTypesClass)engine.Invoke(testObject, typeof(LogicalTypesClass));

            //Assert
            Assert.NotNull(deserialized);
            Assert.Equal(testObject.One, deserialized.One);
            Assert.Equal(testObject.Two, deserialized.Two);

            Assert.Equal(testObject.Three.Milliseconds, deserialized.Three.Milliseconds);
            Assert.Equal(testObject.Three.Seconds, deserialized.Three.Seconds);
            Assert.Equal(testObject.Three.Minutes, deserialized.Three.Minutes);
            Assert.Equal(testObject.Three.Hours, deserialized.Three.Hours);
            Assert.Equal(testObject.Three.Days, deserialized.Three.Days);

            deserialized.Four!.Value.Should().BeCloseTo(testObject.Four!.Value, precision: TimeSpan.FromTicks(10),
                "According to the Avro documentation, the most accurate precision is microseconds");

            Assert.Equal(testObject.Five.Microsecond(), deserialized.Five.Microsecond());
            Assert.Equal(testObject.Five.Millisecond, deserialized.Five.Millisecond);
            Assert.Equal(testObject.Five.Second, deserialized.Five.Second);
            Assert.Equal(testObject.Five.Minute, deserialized.Five.Minute);
            Assert.Equal(testObject.Five.Hour, deserialized.Five.Hour);
            Assert.Equal(testObject.Five.Day, deserialized.Five.Day);
            Assert.Equal(testObject.Five.Month, deserialized.Five.Month);
            Assert.Equal(testObject.Five.Year, deserialized.Five.Year);

        }

        [Theory]
        [MemberData(nameof(TestEngine.All), MemberType = typeof(TestEngine))]
        public void ClassInheritingFromBaseClass(Func<object, Type, dynamic> engine)
        {
            //Arrange
            InheritingClass testObject = _fixture.Create<InheritingClass>();

            //Act
            var deserialized = (InheritingClass)engine.Invoke(testObject, typeof(InheritingClass));

            //Assert
            deserialized.Should().BeEquivalentTo(testObject);
        }

        [Theory]
        [MemberData(nameof(TestEngine.All), MemberType = typeof(TestEngine))]
        public void ClassInheritingFromInterface(Func<object, Type, dynamic> engine)
        {
            //Arrange
            InheritingClassFromInterface testObject = _fixture.Create<InheritingClassFromInterface>();

            //Act
            var deserialized = (InheritingClassFromInterface)engine.Invoke(testObject, typeof(InheritingClassFromInterface));

            //Assert
            deserialized.Should().BeEquivalentTo(testObject);
        }

        [Theory]
        [MemberData(nameof(TestEngine.All), MemberType = typeof(TestEngine))]
        public void ClassWithDuplicateTypeProperties(Func<object, Type, dynamic> engine)
        {
            //Arrange
            ClassWithDuplicateTypes testObject = _fixture.Create<ClassWithDuplicateTypes>();

            //Act
            var deserialized = (ClassWithDuplicateTypes)engine.Invoke(testObject, typeof(ClassWithDuplicateTypes));

            //Assert
            deserialized.Should().BeEquivalentTo(testObject);
        }
    }
}
