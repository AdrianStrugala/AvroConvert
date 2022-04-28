using AutoFixture;
using NodaTime;
using SolTechnology.Avro;
using Xunit;

namespace AvroConvertComponentTests.Headless
{
    public class HeadlessComponentTests
    {
        private readonly Fixture _fixture;

        public HeadlessComponentTests()
        {
            _fixture = new Fixture();
        }

        [Fact]
        public void Component_SerializeHeadlessBiggerObjectAndReadSmaller_NoError()
        {
            //Arrange
            ExtendedBaseTestClass toSerialize = _fixture.Create<ExtendedBaseTestClass>();
            string schema = AvroConvert.GenerateSchema(typeof(BaseTestClass));

            //Act
            var result = AvroConvert.SerializeHeadless(toSerialize, schema);

            var deserialized = AvroConvert.DeserializeHeadless<BaseTestClass>(result, schema);

            //Assert
            Assert.NotNull(result);
            Assert.NotNull(deserialized);
            Assert.Equal(toSerialize.andLongProperty, deserialized.andLongProperty);
            Assert.Equal(toSerialize.justSomeProperty, deserialized.justSomeProperty);
        }

        [Fact]
        public void Component_DeserializeWithMissingFields_NoError()
        {
            //Arrange
            BaseTestClass toSerialize = _fixture.Create<BaseTestClass>();
            string schema = AvroConvert.GenerateSchema(typeof(BaseTestClass));

            //Act
            var result = AvroConvert.SerializeHeadless(toSerialize, schema);

            var deserialized = AvroConvert.DeserializeHeadless<ReducedBaseTestClass>(result, schema);

            //Assert
            Assert.NotNull(result);
            Assert.NotNull(deserialized);
            Assert.Equal(toSerialize.justSomeProperty, deserialized.justSomeProperty);
        }

        [Fact]
        public void Component_AvroAttributeClass_ResultIsEqualToInput()
        {
            //Arrange
            AttributeClass toSerialize = new AttributeClass
            {
                AndAnotherString = "anotherString",
                NullableIntProperty = 1,
                NullableIntPropertyWithDefaultValue = null,
                NullableStringProperty = "nullableString"
            };
            string schema = AvroConvert.GenerateSchema(typeof(AttributeClass));


            //Act
            var result = AvroConvert.SerializeHeadless(toSerialize, schema);

            var deserialized = AvroConvert.DeserializeHeadless<AttributeClass>(result, schema);


            //Assert
            Assert.NotNull(result);
            Assert.NotNull(deserialized);
            Assert.Equal(toSerialize.NullableIntProperty, deserialized.NullableIntProperty);
            Assert.Equal(toSerialize.AndAnotherString, deserialized.AndAnotherString);
            Assert.Equal(toSerialize.NullableStringProperty, deserialized.NullableStringProperty);
            Assert.Equal(2137, deserialized.NullableIntPropertyWithDefaultValue);
        }

        [Fact]
        public void Component_NodaTime_ResultIsEqualToInput()
        {
            //Arrange
            Error appError = new Error
            {
//AppName = "MyApplication",
 //               Id = 123,
                Date = new StructWithPrivateFields(2137),
                Dupa = SystemClock.Instance.GetCurrentInstant()
            };

            string avroSchema = AvroConvert.GenerateSchema(typeof(Error));

            var x = appError.Dupa;

            //Act
            var result = AvroConvert.SerializeHeadless(appError, avroSchema);

            var deserialized = AvroConvert.DeserializeHeadless<Error>(result, avroSchema);


            //Assert
            Assert.NotNull(result);
            Assert.NotNull(deserialized);
            Assert.Equal(appError, deserialized);
        }
    }

    public class Error
    {
  //      public int Id { get; set; }
  //      public string AppName { get; set; }
        public StructWithPrivateFields Date { get; set; }
        public Instant Dupa { get; set; }
    }
}
