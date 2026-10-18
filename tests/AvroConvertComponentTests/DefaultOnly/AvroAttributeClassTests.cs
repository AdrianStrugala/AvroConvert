using System;
using AutoFixture;
using AwesomeAssertions;
using SolTechnology.Avro;
using NUnit.Framework;

namespace AvroConvertComponentTests.DefaultOnly
{
    public class AvroAttributeClassTests
    {
        private readonly Fixture _fixture = new();

        [Test]
        public void Types_mismatches_are_handled([ValueSource(typeof(Formats), nameof(Formats.ContainerOnly))] Format format)
        {
            //Arrange
            UserWithNonNullableProperties toSerialize = _fixture.Create<UserWithNonNullableProperties>();


            //Act
            var deserialized = RoundTrip.Run<User>(toSerialize, format);


            //Assert
            deserialized.Should().NotBeNull();
            deserialized.favorite_number.Should().Be(toSerialize.favorite_number);
            deserialized.name.Should().Be(toSerialize.name);
            deserialized.favorite_color.Should().Be(toSerialize.favorite_color);
        }

        [Test]
        public void Attribute_class_used_for_serialization_overrides_property_names([ValueSource(typeof(Formats), nameof(Formats.ContainerOnly))] Format format)
        {
            //Arrange
            AttributeClass toSerialize = _fixture.Create<AttributeClass>();


            //Act
            var deserialized = RoundTrip.Run<User>(toSerialize, format);


            //Assert
            deserialized.Should().NotBeNull();
            deserialized.favorite_number.Should().Be(toSerialize.NullableIntProperty);
            deserialized.name.Should().Be(toSerialize.NullableStringProperty);
            deserialized.favorite_color.Should().Be(toSerialize.AndAnotherString);
        }

        [Test]
        public void Attribute_class_used_for_serialization_overrides_fields_names([ValueSource(typeof(Formats), nameof(Formats.ContainerOnly))] Format format)
        {
            //Arrange
            AttributeClassWithoutGetters toSerialize = _fixture.Create<AttributeClassWithoutGetters>();


            //Act
            var deserialized = RoundTrip.Run<User>(toSerialize, format);

            //Assert
            deserialized.Should().NotBeNull();
            deserialized.favorite_number.Should().Be(toSerialize.NullableIntProperty);
            deserialized.name.Should().Be(toSerialize.StringProperty);
            deserialized.favorite_color.Should().Be(toSerialize.AndAnotherString);
        }

        [Test]
        public void AvroTypeAttribute_overwrites_default_type_mapping([ValueSource(typeof(Formats), nameof(Formats.ContainerOnly))] Format format)
        {
            //Arrange
            TypeAttributeClass toSerialize = _fixture.Create<TypeAttributeClass>();


            //Act
            var schema = AvroConvert.GenerateSchema(typeof(TypeAttributeClass));

            var deserialized = RoundTrip.Run<TypeAttributeClass>(toSerialize, format);


            //Assert
            schema.Should().Be("{\"name\":\"TypeAttributeClass\",\"namespace\":\"AvroConvertComponentTests\",\"type\":\"record\",\"fields\":[{\"name\":\"Int\",\"type\":\"double\"},{\"name\":\"DateTime\",\"type\":{\"type\":\"long\",\"logicalType\":\"timestamp-millis\"}}]}");
            deserialized.Should().NotBeNull();
            deserialized.Int.Should().Be(toSerialize.Int);
            toSerialize.DateTime.Should().BeCloseTo(deserialized.DateTime, TimeSpan.FromMilliseconds(1)); //milliseconds precision
        }

        [Test]
        public void AvroTypeAttribute_overwrites_nullable_default_type_mapping([ValueSource(typeof(Formats), nameof(Formats.ContainerOnly))] Format format)
        {
            //Arrange
            NullableTypeAttributeClass toSerialize = _fixture.Create<NullableTypeAttributeClass>();

            //Act
            var schema = AvroConvert.GenerateSchema(typeof(NullableTypeAttributeClass));

            var deserialized = RoundTrip.Run<NullableTypeAttributeClass>(toSerialize, format);


            //Assert
            schema.Should().Be("{\"name\":\"NullableTypeAttributeClass\",\"namespace\":\"AvroConvertComponentTests\",\"type\":\"record\",\"fields\":[{\"name\":\"Int\",\"type\":[\"null\",\"double\"]},{\"name\":\"DateTime\",\"type\":[\"null\",{\"type\":\"long\",\"logicalType\":\"timestamp-millis\"}]}]}");
            deserialized.Should().NotBeNull();
            deserialized.Int.Should().Be(toSerialize.Int);
            toSerialize.DateTime.Should().BeCloseTo(deserialized.DateTime.Value, TimeSpan.FromMilliseconds(1)); //milliseconds precision
        }

        [Test]
        public void AvroTypeAttribute_overwrites_nullable_default_type_mapping_in_array([ValueSource(typeof(Formats), nameof(Formats.ContainerOnly))] Format format)
        {
            //Arrange
            NestedTypeAttributeClass toSerialize = _fixture.Create<NestedTypeAttributeClass>();

            //Act
            var schema = AvroConvert.GenerateSchema(typeof(NestedTypeAttributeClass));

            var deserialized = RoundTrip.Run<NestedTypeAttributeClass>(toSerialize, format);


            //Assert
            schema.Should().Be("{\"name\":\"NestedTypeAttributeClass\",\"namespace\":\"AvroConvertComponentTests\",\"type\":\"record\",\"fields\":[{\"name\":\"Items\",\"type\":{\"type\":\"array\",\"items\":{\"name\":\"NullableTypeAttributeClass\",\"namespace\":\"AvroConvertComponentTests\",\"type\":\"record\",\"fields\":[{\"name\":\"Int\",\"type\":[\"null\",\"double\"]},{\"name\":\"DateTime\",\"type\":[\"null\",{\"type\":\"long\",\"logicalType\":\"timestamp-millis\"}]}]}}}]}");
            deserialized.Should().NotBeNull();

            for (var i = 0; i < toSerialize.Items.Length; i++)
            {
                deserialized.Items[i].Int.Should().Be(toSerialize.Items[i].Int);
                toSerialize.Items[i].DateTime.Should().BeCloseTo(deserialized.Items[i].DateTime.Value, TimeSpan.FromMilliseconds(1)); //milliseconds precision
            }
        }
    }
}
