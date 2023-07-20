using System;
using AutoFixture;
using FluentAssertions;
using FluentAssertions.Extensions;
using Xunit;

namespace AvroConvertComponentTests.FullSerializationAndDeserialization
{
    public class DateTimeTests
    {
        private readonly Fixture _fixture;

        public DateTimeTests()
        {
            _fixture = new Fixture();
            _fixture.Customize<DateOnly>(composer => composer.FromFactory<DateTime>(DateOnly.FromDateTime));
            _fixture.Customize<TimeOnly>(composer => composer.FromFactory<DateTime>(TimeOnly.FromDateTime));
        }

        [Theory]
        [MemberData(nameof(TestEngine.All), MemberType = typeof(TestEngine))]
        public void Class_containing_date_time_and_dateTime_properties(Func<object, Type, dynamic> engine)
        {
            //Arrange
            ClassWithDateTime toSerialize = _fixture.Create<ClassWithDateTime>();


            //Act
            var deserialized = (ClassWithDateTime)engine.Invoke(toSerialize, typeof(ClassWithDateTime));

            //Assert
            deserialized.MyDate.Should().Be(toSerialize.MyDate);
            deserialized.MyTime.ToString().Should().Be(toSerialize.MyTime.ToString());
            deserialized.ArriveBy.Should().BeCloseTo(toSerialize.ArriveBy, precision: TimeSpan.FromTicks(10),
                "According to the Avro documentation, the most accurate precision is microseconds");
        }

        [Theory]
        [MemberData(nameof(TestEngine.All), MemberType = typeof(TestEngine))]
        public void Class_containing_dateTime_which_has_default_value(Func<object, Type, dynamic> engine)
        {
            //Arrange
            ClassWithDateTime toSerialize = _fixture.Create<ClassWithDateTime>();
            toSerialize.ArriveBy = new DateTime();


            //Act
            var deserialized = (ClassWithDateTime)engine.Invoke(toSerialize, typeof(ClassWithDateTime));


            //Assert
            deserialized.MyDate.Should().Be(toSerialize.MyDate);
            deserialized.MyTime.ToString().Should().Be(toSerialize.MyTime.ToString());
            deserialized.ArriveBy.Should().BeCloseTo(toSerialize.ArriveBy, precision: TimeSpan.FromTicks(10),
                "According to the Avro documentation, the most accurate precision is microseconds");
        }

        [Theory]
        [MemberData(nameof(TestEngine.All), MemberType = typeof(TestEngine))]
        public void Class_containing_dateTimeOffset(Func<object, Type, dynamic> engine)
        {
            //Arrange
            ClassWithDateTimeOffset toSerialize = _fixture.Create<ClassWithDateTimeOffset>();


            //Act
            var deserialized = (ClassWithDateTimeOffset)engine.Invoke(toSerialize, typeof(ClassWithDateTimeOffset));


            //Assert
            Assert.NotNull(deserialized);
            //the timezone is dropped
            Assert.Equal(toSerialize.yeah.Microsecond(), deserialized.yeah.Microsecond());
            Assert.Equal(toSerialize.yeah.Millisecond, deserialized.yeah.Millisecond);
            Assert.Equal(toSerialize.yeah.Second, deserialized.yeah.Second);
            Assert.Equal(toSerialize.yeah.Minute, deserialized.yeah.Minute);
            Assert.Equal(toSerialize.yeah.Hour, deserialized.yeah.Hour);
            Assert.Equal(toSerialize.yeah.Day, deserialized.yeah.Day);
            Assert.Equal(toSerialize.yeah.Month, deserialized.yeah.Month);
            Assert.Equal(toSerialize.yeah.Year, deserialized.yeah.Year);
        }

        [Theory]
        [MemberData(nameof(TestEngine.All), MemberType = typeof(TestEngine))]
        public void Nullable_DateTime(Func<object, Type, dynamic> engine)
        {
            //Arrange
            DateTime? toSerialize = _fixture.Create<DateTime?>();


            //Act
            var deserialized = (DateTime?)engine.Invoke(toSerialize, typeof(DateTime?));


            //Assert
            Assert.NotNull(deserialized);
            deserialized.Value.Should().BeCloseTo(toSerialize!.Value, precision: TimeSpan.FromTicks(10),
                "According to the Avro documentation, the most accurate precision is microseconds");
        }

        [Theory]
        [MemberData(nameof(TestEngine.All), MemberType = typeof(TestEngine))]
        public void Nullable_DateTimeOffset(Func<object, Type, dynamic> engine)
        {
            //Arrange
            DateTimeOffset? toSerialize = _fixture.Create<DateTimeOffset?>();


            //Act
            var deserialized = (DateTimeOffset?)engine.Invoke(toSerialize, typeof(DateTimeOffset?));


            //Assert
            Assert.NotNull(deserialized);
            Assert.Equal(toSerialize.Value.Microsecond(), deserialized.Value.Microsecond());
            Assert.Equal(toSerialize.Value.Millisecond, deserialized.Value.Millisecond);
            Assert.Equal(toSerialize.Value.Second, deserialized.Value.Second);
            Assert.Equal(toSerialize.Value.Minute, deserialized.Value.Minute);
            Assert.Equal(toSerialize.Value.Hour, deserialized.Value.Hour);
            Assert.Equal(toSerialize.Value.Day, deserialized.Value.Day);
            Assert.Equal(toSerialize.Value.Month, deserialized.Value.Month);
            Assert.Equal(toSerialize.Value.Year, deserialized.Value.Year);
        }

        [Theory]
        [MemberData(nameof(TestEngine.All), MemberType = typeof(TestEngine))]
        public void TimeSpan_object(Func<object, Type, dynamic> engine)
        {
            //Arrange
            TimeSpan toSerialize = DateTime.UtcNow.TimeOfDay;


            //Act
            var deserialized = engine.Invoke(toSerialize, typeof(TimeSpan));


            //Assert
            toSerialize.Should().BeCloseTo(deserialized, TimeSpan.FromMilliseconds(1),
                "Avro Duration supports at lowest milliseconds precision");
        }

        [Theory]
        [MemberData(nameof(TestEngine.All), MemberType = typeof(TestEngine))]
        public void Nullable_TimeSpan(Func<object, Type, dynamic> engine)
        {
            //Arrange
            TimeSpan? toSerialize = _fixture.Create<TimeSpan?>();


            //Act
            var deserialized = (TimeSpan?)engine.Invoke(toSerialize, typeof(TimeSpan?));


            //Assert
            Assert.NotNull(deserialized);
            toSerialize!.Value.Should().BeCloseTo(deserialized.Value, TimeSpan.FromMilliseconds(1),
                "Avro Duration supports at lowest milliseconds precision");
        }

        [Theory]
        [MemberData(nameof(TestEngine.All), MemberType = typeof(TestEngine))]
        public void Nullable_DateOnly(Func<object, Type, dynamic> engine)
        {
            //Arrange
            DateOnly? toSerialize = _fixture.Create<DateOnly?>();


            //Act
            var deserialized = (DateOnly?)engine.Invoke(toSerialize, typeof(DateOnly?));


            //Assert
            Assert.NotNull(deserialized);
            toSerialize!.Value.Should().Be(deserialized.Value);
        }

        [Theory]
        [MemberData(nameof(TestEngine.All), MemberType = typeof(TestEngine))]
        public void Nullable_TimeOnly(Func<object, Type, dynamic> engine)
        {
            //Arrange
            TimeOnly? toSerialize = _fixture.Create<TimeOnly?>();


            //Act
            var deserialized = (TimeOnly?)engine.Invoke(toSerialize, typeof(TimeOnly?));


            //Assert
            Assert.NotNull(deserialized);
            toSerialize!.Value.ToString().Should().Be(deserialized.Value.ToString());
        }
    }
}
