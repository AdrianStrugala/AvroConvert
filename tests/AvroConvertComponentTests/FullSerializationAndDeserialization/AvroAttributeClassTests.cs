using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoFixture;
using AwesomeAssertions;
using SolTechnology.Avro;
using NUnit.Framework;
using System.Runtime.Serialization;

namespace AvroConvertComponentTests.FullSerializationAndDeserialization
{
    public class AvroAttributeClassTests
    {
        private readonly Fixture _fixture = new();

        [Test]
        public void Attribute_class_used_for_deserialization_overrides_property_names([ValueSource(typeof(Formats), nameof(Formats.Evolution))] Format format)
        {
            //Arrange
            User toSerialize = _fixture.Create<User>();
            

            //Act
            var writeSchema = AvroConvert.GenerateSchema(typeof(User));
            var readSchema = AvroConvert.GenerateSchema(typeof(AttributeClass));
            var deserialized = RoundTrip.Evolve<AttributeClass>(toSerialize, format, writeSchema, readSchema);


            //Assert
            deserialized.Should().NotBeNull();
            toSerialize.favorite_number.Should().Be(deserialized.NullableIntProperty);
            toSerialize.name.Should().Be(deserialized.NullableStringProperty);
            toSerialize.favorite_color.Should().Be(deserialized.AndAnotherString);
        }

        [Test]
        public void Attribute_class_used_for_deserialization_overrides_fields_names([ValueSource(typeof(Formats), nameof(Formats.Evolution))] Format format)
        {
            //Arrange
            User toSerialize = _fixture.Create<User>();


            //Act
            var writeSchema = AvroConvert.GenerateSchema(typeof(User));
            var readSchema = AvroConvert.GenerateSchema(typeof(AttributeClassWithoutGetters));
            var deserialized = RoundTrip.Evolve<AttributeClassWithoutGetters>(toSerialize, format, writeSchema, readSchema);


            //Assert
            deserialized.Should().NotBeNull();
            toSerialize.favorite_number.Should().Be(deserialized.NullableIntProperty);
            toSerialize.name.Should().Be(deserialized.StringProperty);
            toSerialize.favorite_color.Should().Be(deserialized.AndAnotherString);
        }

        [Test]
        public void Struct_containing_property([ValueSource(typeof(Formats), nameof(Formats.All))] Format format)
        {
            //Arrange
            ComplexStruct toSerialize = new ComplexStruct(_fixture.Create<List<int>>());


            //Act
            var deserialized = RoundTrip.Run<ComplexStruct>(toSerialize, format);


            //Assert
            deserialized.Should().BeEquivalentTo(toSerialize, opt => opt.ComparingByMembers<ComplexStruct>());
        }

        [Test]
        public void Default_values_are_replacing_nulls([ValueSource(typeof(Formats), nameof(Formats.All))] Format format)
        {
            //Arrange
            DefaultValueClass defaultValueClass = new DefaultValueClass();
            defaultValueClass.andLongBigDefaultedProperty = null;
            defaultValueClass.justSomeProperty = null;
            defaultValueClass.andLongSmallDefaultedProperty = null;

            //Act
            var deserialized = RoundTrip.Run<DefaultValueClass>(defaultValueClass, format);

            //Assert
            deserialized.Should().NotBeNull();
            deserialized.justSomeProperty.Should().Be("Let's go");
            deserialized.andLongBigDefaultedProperty.Should().Be(9200000000000000007);
            deserialized.andLongSmallDefaultedProperty.Should().Be(100);
            deserialized.andNullProperty.Should().BeNull();
        }

        [Test]
        public void Properties_marked_as_ignored_are_not_serialized([ValueSource(typeof(Formats), nameof(Formats.All))] Format format)
        {
            //Arrange
            AttributeClass toSerialize = _fixture.Create<AttributeClass>();

            //Act
            var deserialized = RoundTrip.Run<AttributeClass>(toSerialize, format);

            //Assert
            deserialized.Should().NotBeNull();
            deserialized.IgnoredProperty.Should().BeNull();
        }

        [DataContract]
        public class ClassWithPrivateDataMembers
        {
            [DataMember] public string Name { get; set; }
            [DataMember] private long privateProperty { get; set; }
            [DataMember] private int _privateField;

            public long ExposedProperty { get => privateProperty; set => privateProperty = value; }
            public int ExposedField { get => _privateField; set => _privateField = value; }
        }

        [Test]
        public void Component_PrivateMembersMarkedAsDataMember_AreSerialized([ValueSource(typeof(Formats), nameof(Formats.All))] Format format)
        {
            //Arrange (issue #51)
            var toSerialize = new ClassWithPrivateDataMembers { Name = "x", ExposedProperty = 2137, ExposedField = 7 };


            //Act
            var deserialized = RoundTrip.Run<ClassWithPrivateDataMembers>(toSerialize, format);


            //Assert
            deserialized.Name.Should().Be("x");
            deserialized.ExposedProperty.Should().Be(2137);
            deserialized.ExposedField.Should().Be(7);
            AvroConvert.GenerateSchema(typeof(ClassWithPrivateDataMembers), includeOnlyDataContractMembers: true)
                .Should().Contain(@"{""name"":""_privateField"",""type"":""int""}")
                .And.Contain(@"{""name"":""privateProperty"",""type"":""long""}");
        }
    }
}
