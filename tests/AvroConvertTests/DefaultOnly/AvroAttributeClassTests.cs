using System;
using AutoFixture;
using FluentAssertions;
using SolTechnology.Avro;
using Xunit;

namespace AvroConvertComponentTests.DefaultOnly
{
    public class AvroAttributeClassTests
    {
        private readonly Fixture _fixture = new();

        [Theory]
        [MemberData(nameof(TestEngine.DefaultOnly), MemberType = typeof(TestEngine))]
        public void Types_mismatches_are_handled(Func<object, Type, dynamic> engine)
        {
            //Arrange
            UserWithNonNullableProperties toSerialize = _fixture.Create<UserWithNonNullableProperties>();


            //Act
            var deserialized = engine.Invoke(toSerialize, typeof(User));


            //Assert
            Assert.NotNull(deserialized);
            Assert.Equal(toSerialize.favorite_number, deserialized.favorite_number);
            Assert.Equal(toSerialize.name, deserialized.name);
            Assert.Equal(toSerialize.favorite_color, deserialized.favorite_color);
        }

        [Theory]
        [MemberData(nameof(TestEngine.DefaultOnly), MemberType = typeof(TestEngine))]
        public void Attribute_class_used_for_serialization_overrides_property_names(Func<object, Type, dynamic> engine)
        {
            //Arrange
            AttributeClass toSerialize = _fixture.Create<AttributeClass>();


            //Act
            var deserialized = (User)engine.Invoke(toSerialize, typeof(User));


            //Assert
            Assert.NotNull(deserialized);
            Assert.Equal(toSerialize.NullableIntProperty, deserialized.favorite_number);
            Assert.Equal(toSerialize.NullableStringProperty, deserialized.name);
            Assert.Equal(toSerialize.AndAnotherString, deserialized.favorite_color);
        }

        [Theory]
        [MemberData(nameof(TestEngine.DefaultOnly), MemberType = typeof(TestEngine))]
        public void Attribute_class_used_for_serialization_overrides_fields_names(Func<object, Type, dynamic> engine)
        {
            //Arrange
            AttributeClassWithoutGetters toSerialize = _fixture.Create<AttributeClassWithoutGetters>();


            //Act
            var deserialized = engine.Invoke(toSerialize, typeof(User));

            //Assert
            Assert.NotNull(deserialized);
            Assert.Equal(toSerialize.NullableIntProperty, deserialized.favorite_number);
            Assert.Equal(toSerialize.StringProperty, deserialized.name);
            Assert.Equal(toSerialize.AndAnotherString, deserialized.favorite_color);
        }

        [Theory]
        [MemberData(nameof(TestEngine.DefaultOnly), MemberType = typeof(TestEngine))]
        public void AvroTypeAttribute_overwrites_default_type_mapping(Func<object, Type, dynamic> engine)
        {
            //Arrange
            TypeAttributeClass toSerialize = _fixture.Create<TypeAttributeClass>();


            //Act
            var schema = AvroConvert.GenerateSchema(typeof(TypeAttributeClass));

            var deserialized = (TypeAttributeClass)engine.Invoke(toSerialize, typeof(TypeAttributeClass));


            //Assert
            Assert.Equal("{\"name\":\"TypeAttributeClass\",\"namespace\":\"AvroConvertComponentTests\",\"type\":\"record\",\"fields\":[{\"name\":\"Int\",\"type\":\"double\"},{\"name\":\"DateTime\",\"type\":{\"type\":\"long\",\"logicalType\":\"timestamp-millis\"}}]}", schema);
            Assert.NotNull(deserialized);
            Assert.Equal(toSerialize.Int, deserialized.Int);
            toSerialize.DateTime.Should().BeCloseTo(deserialized.DateTime, TimeSpan.FromMilliseconds(1)); //milliseconds precision
        }
    }
}
