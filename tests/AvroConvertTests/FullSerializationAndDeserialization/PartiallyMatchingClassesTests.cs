using System;
using AutoFixture;
using SolTechnology.Avro;
using Xunit;

namespace AvroConvertComponentTests.FullSerializationAndDeserialization
{
    public class PartiallyMatchingClassesTests
    {
        private readonly Fixture _fixture = new();

        [Theory]
        [MemberData(nameof(TestEngine.CoreUsingSchema), MemberType = typeof(TestEngine))]
        public void SerializeBiggerObjectAndReadSmaller(Func<object, Type, string, string, dynamic> engine)
        {
            //Arrange
            ExtendedBaseTestClass toSerialize = _fixture.Create<ExtendedBaseTestClass>();


            //Act
            var schema = AvroConvert.GenerateSchema(typeof(ExtendedBaseTestClass));
            var deserialized = engine.Invoke(toSerialize, typeof(BaseTestClass), schema, schema);


            //Assert
            Assert.NotNull(deserialized);
            Assert.Equal(toSerialize.justSomeProperty, deserialized.justSomeProperty);
            Assert.Equal(toSerialize.andLongProperty, deserialized.andLongProperty);
        }


        [Theory]
        [MemberData(nameof(TestEngine.CoreUsingSchema), MemberType = typeof(TestEngine))]
        public void SerializeSmallerClassAndReadBigger(Func<object, Type, string, string, dynamic> engine)
        {
            //Arrange
            ReducedBaseTestClass toSerialize = _fixture.Create<ReducedBaseTestClass>();


            //Act
            var schema = AvroConvert.GenerateSchema(typeof(ReducedBaseTestClass));
            var deserialized = engine.Invoke(toSerialize, typeof(BaseTestClass), schema, schema);


            //Assert
            Assert.NotNull(deserialized);
            Assert.Equal(toSerialize.justSomeProperty, deserialized.justSomeProperty);
        }


        [Theory]
        [MemberData(nameof(TestEngine.CoreUsingSchema), MemberType = typeof(TestEngine))]
        public void SerializeHeadlessBiggerObjectUsingReducedSchemaAndReadSmaller(Func<object, Type, string, string, dynamic> engine)
        {
            //Arrange
            ExtendedBaseTestClass toSerialize = _fixture.Create<ExtendedBaseTestClass>();
            string schema = AvroConvert.GenerateSchema(typeof(BaseTestClass));

            //Act
            var deserialized = engine.Invoke(toSerialize, typeof(BaseTestClass), schema, schema);

            //Assert
            Assert.NotNull(deserialized);
            Assert.Equal(toSerialize.andLongProperty, deserialized.andLongProperty);
            Assert.Equal(toSerialize.justSomeProperty, deserialized.justSomeProperty);
        }


        [Theory]
        [MemberData(nameof(TestEngine.CoreUsingSchema), MemberType = typeof(TestEngine))]
        public void SerializeBiggerAvroObjectAndReadSmaller(Func<object, Type, string, string, dynamic> engine)
        {
            //Arrange
            AttributeClass toSerialize = _fixture.Create<AttributeClass>();


            //Act
            var schema = AvroConvert.GenerateSchema(typeof(AttributeClass));
            var deserialized = engine.Invoke(toSerialize, typeof(SmallerAttributeClass), schema, schema);


            //Assert
            Assert.NotNull(deserialized);
            Assert.Equal(toSerialize.NullableStringProperty, deserialized.NullableStringProperty);
            Assert.Equal(toSerialize.NullableIntProperty, deserialized.NullableIntProperty);
        }


        [Theory]
        [MemberData(nameof(TestEngine.CoreUsingSchema), MemberType = typeof(TestEngine))]
        public void SerializeAndDeserializeClassesWithDifferentPropertyCases(Func<object, Type, string, string, dynamic> engine)
        {
            //Arrange
            BaseTestClass toSerialize = _fixture.Create<BaseTestClass>();


            //Act
            var schema = AvroConvert.GenerateSchema(typeof(BaseTestClass));
            var deserialized = engine.Invoke(toSerialize, typeof(DifferentCaseBaseTestClass), schema, schema);


            //Assert
            Assert.NotNull(deserialized);
            Assert.Equal(toSerialize.justSomeProperty, deserialized.JustSomeProperty);
            Assert.Equal(toSerialize.andLongProperty, deserialized.AndLongProperty);
        }
    }
}
