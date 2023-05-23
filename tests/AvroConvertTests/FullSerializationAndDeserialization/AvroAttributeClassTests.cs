using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoFixture;
using FluentAssertions;
using SolTechnology.Avro;
using Xunit;

namespace AvroConvertComponentTests.FullSerializationAndDeserialization
{
    public class AvroAttributeClassTests
    {
        private readonly Fixture _fixture = new();

        [Theory]
        [MemberData(nameof(TestEngine.CoreUsingSchema), MemberType = typeof(TestEngine))]
        public void Attribute_class_used_for_deserialization_overrides_property_names(Func<object, Type, string, string, dynamic> engine)
        {
            //Arrange
            User toSerialize = _fixture.Create<User>();
            

            //Act
            var writeSchema = AvroConvert.GenerateSchema(typeof(User));
            var readSchema = AvroConvert.GenerateSchema(typeof(AttributeClass));
            var deserialized = (AttributeClass)engine.Invoke(toSerialize, typeof(AttributeClass), writeSchema, readSchema);


            //Assert
            Assert.NotNull(deserialized);
            Assert.Equal(deserialized.NullableIntProperty, toSerialize.favorite_number);
            Assert.Equal(deserialized.NullableStringProperty, toSerialize.name);
            Assert.Equal(deserialized.AndAnotherString, toSerialize.favorite_color);
        }

        [Theory]
        [MemberData(nameof(TestEngine.CoreUsingSchema), MemberType = typeof(TestEngine))]
        public void Attribute_class_used_for_deserialization_overrides_fields_names(Func<object, Type, string, string, dynamic> engine)
        {
            //Arrange
            User toSerialize = _fixture.Create<User>();


            //Act
            var writeSchema = AvroConvert.GenerateSchema(typeof(User));
            var readSchema = AvroConvert.GenerateSchema(typeof(AttributeClassWithoutGetters));
            var deserialized = engine.Invoke(toSerialize, typeof(AttributeClassWithoutGetters), writeSchema, readSchema);


            //Assert
            Assert.NotNull(deserialized);
            Assert.Equal(deserialized.NullableIntProperty, toSerialize.favorite_number);
            Assert.Equal(deserialized.StringProperty, toSerialize.name);
            Assert.Equal(deserialized.AndAnotherString, toSerialize.favorite_color);
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
