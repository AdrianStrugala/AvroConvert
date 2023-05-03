using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoFixture;
using FluentAssertions;
using SolTechnology.Avro;
using Xunit;

namespace AvroConvertComponentTests.SerializationAndDeserialization
{
    public class AvroAttributeClassTests
    {
        private readonly Fixture _fixture = new();

        [Theory]
        [MemberData(nameof(TestEngine.All), MemberType = typeof(TestEngine))]
        public void Attribute_values_are_used_instead_of_property_names(Func<object, Type, dynamic> engine)
        {
            //Arrange
            AttributeClass toSerialize = _fixture.Create<AttributeClass>();
            

            //Act
            var deserialized = engine.Invoke(toSerialize, typeof(User));


            //Assert
            Assert.NotNull(deserialized);
            Assert.Equal(toSerialize.NullableIntProperty, deserialized.favorite_number);
            Assert.Equal(toSerialize.NullableStringProperty, deserialized.name);
            Assert.Equal(toSerialize.AndAnotherString, deserialized.favorite_color);
        }

        [Theory]
        [MemberData(nameof(TestEngine.All), MemberType = typeof(TestEngine))]
        public void Attributes_are_used_instead_of_field_names(Func<object, Type, dynamic> engine)
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
        [MemberData(nameof(TestEngine.All), MemberType = typeof(TestEngine))]
        public void Struct_containing_property(Func<object, Type, dynamic> engine)
        {
            //Arrange
            ComplexStruct toSerialize = new ComplexStruct(_fixture.Create<List<int>>());


            //Act
            var deserialized = (ComplexStruct)engine.Invoke(toSerialize, typeof(ComplexStruct));


            //Assert
            deserialized.Should().BeEquivalentTo(toSerialize, opt => opt.ComparingByMembers<ComplexStruct>());
        }

        [Theory]
        [MemberData(nameof(TestEngine.All), MemberType = typeof(TestEngine))]
        public void Default_values_are_replacing_nulls(Func<object, Type, dynamic> engine)
        {
            //Arrange
            DefaultValueClass defaultValueClass = new DefaultValueClass();
            defaultValueClass.andLongBigDefaultedProperty = null;
            defaultValueClass.justSomeProperty = null;
            defaultValueClass.andLongSmallDefaultedProperty = null;

            //Act
            var deserialized = engine.Invoke(defaultValueClass, typeof(DefaultValueClass));

            //Assert
            Assert.NotNull(deserialized);
            Assert.Equal("Let's go", deserialized.justSomeProperty);
            Assert.Equal(9200000000000000007, deserialized.andLongBigDefaultedProperty);
            Assert.Equal(100, deserialized.andLongSmallDefaultedProperty);
            Assert.Null(deserialized.andNullProperty);
        }

        [Theory]
        [MemberData(nameof(TestEngine.All), MemberType = typeof(TestEngine))]
        public void Properties_marked_as_ignored_are_not_serialized(Func<object, Type, dynamic> engine)
        {
            //Arrange
            AttributeClass toSerialize = _fixture.Create<AttributeClass>();

            //Act
            var deserialized = engine.Invoke(toSerialize, typeof(AttributeClass));

            //Assert
            Assert.NotNull(deserialized);
            Assert.Null(deserialized.IgnoredProperty);
        }

        [Fact(Skip = "Scenario not supported")]
        public void Component_PrivatePropertyIsMarkedAsDataMember_ItIsSerialized()
        {
            //Arrange
            long expectedValue = 2137;

            AttributeClass toSerialize = _fixture.Create<AttributeClass>();
            typeof(AttributeClass)
                .GetFields(BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault(f => f.Name.Contains("privateProperty"))?
                .SetValue(toSerialize, expectedValue);


            //Act
            var result = AvroConvert.Serialize(toSerialize);

            var deserialized = AvroConvert.Deserialize<AttributeClass>(result);


            //Assert
            Assert.NotNull(result);
            Assert.NotNull(deserialized);

            var resultValue = typeof(AttributeClass)
                .GetFields(BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault(f => f.Name.Contains("privateProperty"))?
                .GetValue(deserialized);

            Assert.Equal(expectedValue, resultValue);
        }
    }
}
