using System;
using AutoFixture;
using FluentAssertions;
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

            Assert.NotNull(testObject.Four);
            Assert.NotNull(deserialized.Four);
            Assert.Equal(testObject.Four.Value.Millisecond, deserialized.Four.Value.Millisecond);
            Assert.Equal(testObject.Four.Value.Second, deserialized.Four.Value.Second);
            Assert.Equal(testObject.Four.Value.Minute, deserialized.Four.Value.Minute);
            Assert.Equal(testObject.Four.Value.Hour, deserialized.Four.Value.Hour);
            Assert.Equal(testObject.Four.Value.Day, deserialized.Four.Value.Day);
            Assert.Equal(testObject.Four.Value.Month, deserialized.Four.Value.Month);
            Assert.Equal(testObject.Four.Value.Year, deserialized.Four.Value.Year);

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
    }
}
