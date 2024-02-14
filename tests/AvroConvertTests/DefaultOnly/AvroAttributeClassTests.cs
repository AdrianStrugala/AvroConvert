using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using AutoFixture;
using FluentAssertions;
using SolTechnology.Avro;
using SolTechnology.Avro.AvroObjectServices.BuildSchema;
using SolTechnology.Avro.Infrastructure.Attributes;
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
    }
}
