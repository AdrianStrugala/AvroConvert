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
        public void Nulls_stay_null_although_the_fields_have_defaults([ValueSource(typeof(Formats), nameof(Formats.All))] Format format)
        {
            // Avro specification, "default": only used when reading instances that lack the field.
            //Arrange
            DefaultValueClass defaultValueClass = new DefaultValueClass();
            defaultValueClass.andLongBigDefaultedProperty = null;
            defaultValueClass.justSomeProperty = null;
            defaultValueClass.andLongSmallDefaultedProperty = null;

            //Act
            var deserialized = RoundTrip.Run<DefaultValueClass>(defaultValueClass, format);

            //Assert
            deserialized.Should().NotBeNull();
            deserialized.justSomeProperty.Should().BeNull();
            deserialized.andLongBigDefaultedProperty.Should().BeNull();
            deserialized.andLongSmallDefaultedProperty.Should().BeNull();
            deserialized.andNullProperty.Should().BeNull();
        }

        [Test]
        public void Nulls_stay_null_in_Avro2Json_and_dictionaries_although_the_fields_have_defaults()
        {
            //Arrange
            var avro = AvroConvert.Serialize(new DefaultValueClass { justSomeProperty = null, andLongSmallDefaultedProperty = null, andLongBigDefaultedProperty = 5 });

            //Act
            var json = AvroConvert.Avro2Json(avro);
            var dictionary = AvroConvert.Deserialize<Dictionary<string, object>>(avro);

            //Assert
            json.Should().Be("{\"justSomeProperty\":null,\"andLongProperty\":null,\"andLongBigDefaultedProperty\":5,\"andLongSmallDefaultedProperty\":null,\"andNullProperty\":null}");
            dictionary["justSomeProperty"].Should().BeNull();
            dictionary["andLongSmallDefaultedProperty"].Should().BeNull();
            dictionary["andLongBigDefaultedProperty"].Should().Be(5L);
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
