using System;
using AutoFixture;
using AwesomeAssertions;
using AwesomeAssertions.Extensions;
using NUnit.Framework;

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

        [Test]
        public void Class_containing_date_time_and_dateTime_properties([ValueSource(typeof(Formats), nameof(Formats.All))] Format format)
        {
            //Arrange
            ClassWithDateTime toSerialize = _fixture.Create<ClassWithDateTime>();


            //Act
            var deserialized = RoundTrip.Run<ClassWithDateTime>(toSerialize, format);

            //Assert
            deserialized.MyDate.Should().Be(toSerialize.MyDate);
            deserialized.MyTime.ToString().Should().Be(toSerialize.MyTime.ToString());
            deserialized.ArriveBy.Should().BeCloseTo(toSerialize.ArriveBy, precision: TimeSpan.FromTicks(10),
                "According to the Avro documentation, the most accurate precision is microseconds");
        }

        [Test]
        public void Class_containing_dateTime_which_has_default_value([ValueSource(typeof(Formats), nameof(Formats.All))] Format format)
        {
            //Arrange
            ClassWithDateTime toSerialize = _fixture.Create<ClassWithDateTime>();
            toSerialize.ArriveBy = new DateTime();


            //Act
            var deserialized = RoundTrip.Run<ClassWithDateTime>(toSerialize, format);


            //Assert
            deserialized.MyDate.Should().Be(toSerialize.MyDate);
            deserialized.MyTime.ToString().Should().Be(toSerialize.MyTime.ToString());
            deserialized.ArriveBy.Should().BeCloseTo(toSerialize.ArriveBy, precision: TimeSpan.FromTicks(10),
                "According to the Avro documentation, the most accurate precision is microseconds");
        }

        [Test]
        public void Class_containing_dateTimeOffset([ValueSource(typeof(Formats), nameof(Formats.All))] Format format)
        {
            //Arrange
            ClassWithDateTimeOffset toSerialize = _fixture.Create<ClassWithDateTimeOffset>();


            //Act
            var deserialized = RoundTrip.Run<ClassWithDateTimeOffset>(toSerialize, format);


            //Assert
            deserialized.Should().NotBeNull();
            // the same instant at microsecond precision; the offset is not stored and reads back as zero
            deserialized.yeah.Should().BeCloseTo(toSerialize.yeah, TimeSpan.FromTicks(9));
            deserialized.yeah.Offset.Should().Be(TimeSpan.Zero);
        }

        [Test]
        public void Nullable_DateTime([ValueSource(typeof(Formats), nameof(Formats.All))] Format format)
        {
            //Arrange
            DateTime? toSerialize = _fixture.Create<DateTime?>();


            //Act
            var deserialized = RoundTrip.Run<DateTime?>(toSerialize, format);


            //Assert
            deserialized.Should().NotBeNull();
            deserialized.Value.Should().BeCloseTo(toSerialize!.Value, precision: TimeSpan.FromTicks(10),
                "According to the Avro documentation, the most accurate precision is microseconds");
        }

        [Test]
        public void Nullable_DateTimeOffset([ValueSource(typeof(Formats), nameof(Formats.All))] Format format)
        {
            //Arrange
            DateTimeOffset? toSerialize = _fixture.Create<DateTimeOffset?>();


            //Act
            var deserialized = RoundTrip.Run<DateTimeOffset?>(toSerialize, format);


            //Assert
            deserialized.Should().NotBeNull();
            deserialized.Value.Should().BeCloseTo(toSerialize.Value, TimeSpan.FromTicks(9));
            deserialized.Value.Offset.Should().Be(TimeSpan.Zero);
        }

        [Test]
        public void TimeSpan_object([ValueSource(typeof(Formats), nameof(Formats.All))] Format format)
        {
            //Arrange
            TimeSpan toSerialize = DateTime.UtcNow.TimeOfDay;


            //Act
            var deserialized = RoundTrip.Run<TimeSpan>(toSerialize, format);


            //Assert
            toSerialize.Should().BeCloseTo(deserialized, TimeSpan.FromMilliseconds(1),
                "Avro Duration supports at lowest milliseconds precision");
        }

        [Test]
        public void Nullable_TimeSpan([ValueSource(typeof(Formats), nameof(Formats.All))] Format format)
        {
            //Arrange
            TimeSpan? toSerialize = _fixture.Create<TimeSpan?>();


            //Act
            var deserialized = RoundTrip.Run<TimeSpan?>(toSerialize, format);


            //Assert
            deserialized.Should().NotBeNull();
            toSerialize!.Value.Should().BeCloseTo(deserialized.Value, TimeSpan.FromMilliseconds(1),
                "Avro Duration supports at lowest milliseconds precision");
        }

        [Test]
        public void Nullable_DateOnly([ValueSource(typeof(Formats), nameof(Formats.All))] Format format)
        {
            //Arrange
            DateOnly? toSerialize = _fixture.Create<DateOnly?>();


            //Act
            var deserialized = RoundTrip.Run<DateOnly?>(toSerialize, format);


            //Assert
            deserialized.Should().NotBeNull();
            toSerialize!.Value.Should().Be(deserialized.Value);
        }

        [Test]
        public void Nullable_TimeOnly([ValueSource(typeof(Formats), nameof(Formats.All))] Format format)
        {
            //Arrange
            TimeOnly? toSerialize = _fixture.Create<TimeOnly?>();


            //Act
            var deserialized = RoundTrip.Run<TimeOnly?>(toSerialize, format);


            //Assert
            deserialized.Should().NotBeNull();
            toSerialize!.Value.ToString().Should().Be(deserialized.Value.ToString());
        }
    }
}
