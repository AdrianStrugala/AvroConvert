using AwesomeAssertions;
using SolTechnology.Avro;
using SolTechnology.Avro.AvroObjectServices.BuildSchema;
using System;
using NUnit.Framework;
using Newtonsoft.Json;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;

namespace AvroConvertUnitTests
{
    public class DeserializeLogicalDateTests
    {
        [Test]
        public void GivenDateTimeProperty_WhenUsingSchemaWithTimeAsTimestampMicroseconds_ThenShouldWork()
        {
            //Arrange
            var toSerialize = new ClassWithDateTime { ArriveBy = DateTime.UtcNow };

            //Act
            var schema = Schema.Create(toSerialize);

            // Change schema logical type from timestamp-millis to timestamp-micros (a bit hacky)
            var microsecondsSchema = schema.ToString().Replace(LogicalTypeSchema.LogicalTypeEnum.TimestampMilliseconds, LogicalTypeSchema.LogicalTypeEnum.TimestampMicroseconds);

            var result = AvroConvert.SerializeHeadless(toSerialize, microsecondsSchema);

            var avro2Json = AvroConvert.Avro2Json(result, microsecondsSchema);
            var deserialized = JsonConvert.DeserializeObject<ClassWithDateTime>(avro2Json);

            //Assert
            result.Should().NotBeNull();
            deserialized.Should().NotBeNull();
            deserialized.ArriveBy.Millisecond.Should().Be(toSerialize.ArriveBy.Millisecond);
            deserialized.ArriveBy.Second.Should().Be(toSerialize.ArriveBy.Second);
            deserialized.ArriveBy.Minute.Should().Be(toSerialize.ArriveBy.Minute);
            deserialized.ArriveBy.Hour.Should().Be(toSerialize.ArriveBy.Hour);
            deserialized.ArriveBy.Day.Should().Be(toSerialize.ArriveBy.Day);
            deserialized.ArriveBy.Month.Should().Be(toSerialize.ArriveBy.Month);
            deserialized.ArriveBy.Year.Should().Be(toSerialize.ArriveBy.Year);
        }

        public class ClassWithDateTime
        {
            public DateTime ArriveBy { get; set; }
        }
    }
}
