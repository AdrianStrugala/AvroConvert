using SolTechnology.Avro;
using SolTechnology.Avro.AvroObjectServices.BuildSchema;
using System;
using Xunit;
using Newtonsoft.Json;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;

namespace AvroConvertUnitTests
{
    public class DeserializeLogicalDateTests
    {
        [Fact]
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
            Assert.NotNull(result);
            Assert.NotNull(deserialized);
            Assert.Equal(toSerialize.ArriveBy.Millisecond, deserialized.ArriveBy.Millisecond);
            Assert.Equal(toSerialize.ArriveBy.Second, deserialized.ArriveBy.Second);
            Assert.Equal(toSerialize.ArriveBy.Minute, deserialized.ArriveBy.Minute);
            Assert.Equal(toSerialize.ArriveBy.Hour, deserialized.ArriveBy.Hour);
            Assert.Equal(toSerialize.ArriveBy.Day, deserialized.ArriveBy.Day);
            Assert.Equal(toSerialize.ArriveBy.Month, deserialized.ArriveBy.Month);
            Assert.Equal(toSerialize.ArriveBy.Year, deserialized.ArriveBy.Year);
        }

        public class ClassWithDateTime
        {
            public DateTime ArriveBy { get; set; }
        }
    }
}
