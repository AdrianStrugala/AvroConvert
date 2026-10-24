using AwesomeAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using NUnit.Framework;

namespace AvroConvertComponentTests.FullSerializationAndDeserialization
{
    public class EnumTests
    {
        private readonly Fixture _fixture = new();

        [Test]
        public void Class_with_enum_property([ValueSource(typeof(Formats), nameof(Formats.All))] Format format)
        {
            //Arrange
            ClassWithEnum toSerialize = _fixture.Create<ClassWithEnum>();


            //Act
            var deserialized = RoundTrip.Run<ClassWithEnum>(toSerialize, format);


            //Assert
            deserialized.Should().NotBeNull();
            deserialized.EnumProp.Should().Be(toSerialize.EnumProp);
            deserialized.Should().BeEquivalentTo(toSerialize);
        }

        [Test]
        public void Enum_object([ValueSource(typeof(Formats), nameof(Formats.All))] Format format)
        {
            //Arrange
            TestEnum toSerialize = _fixture.Create<TestEnum>();


            //Act
            var deserialized = RoundTrip.Run<TestEnum>(toSerialize, format);


            //Assert
            deserialized.Should().Be(toSerialize);
        }
        
        [Test]
        public void Enum_with_flags_object([ValueSource(typeof(Formats), nameof(Formats.All))] Format format)
        {
            foreach (var toSerialize in Enum.GetValues<TestEnumWithFlags>())
            {
                //Act
                var deserialized = RoundTrip.Run<TestEnumWithFlags>(toSerialize, format);

                //Assert
                deserialized.Should().Be(toSerialize);
            }
        }

        [Test]
        public void Nullable_enum([ValueSource(typeof(Formats), nameof(Formats.All))] Format format)
        {
            //Arrange
            TestEnum? toSerialize = _fixture.Create<TestEnum?>();


            //Act
            var deserialized = RoundTrip.Run<TestEnum?>(toSerialize, format);


            //Assert
            deserialized.Should().Be(toSerialize);
        }

        [Test]
        public void List_of_enums([ValueSource(typeof(Formats), nameof(Formats.All))] Format format)
        {
            //Arrange
            List<TestEnum> toSerialize = _fixture.CreateMany<TestEnum>(20).ToList();


            //Act
            var deserialized = RoundTrip.Run<List<TestEnum>>(toSerialize, format);


            //Assert
            deserialized.Should().NotBeNull();
            deserialized.Should().Equal(toSerialize);
        }

        [Test]
        public void Dictionary_enum_key_and_value([ValueSource(typeof(Formats), nameof(Formats.Core))] Format format)
        {
            //Arrange
            Dictionary<TestEnum, TestEnum> toSerialize = _fixture.Create<Dictionary<TestEnum, TestEnum>>();


            //Act
            var deserialized = RoundTrip.Run<Dictionary<TestEnum, TestEnum>>(toSerialize, format);


            //Assert
            deserialized.Should().NotBeNull();
            deserialized.Should().BeEquivalentTo(toSerialize);
        }

        [Test]
        public void Class_with_enums_with_default_values([ValueSource(typeof(Formats), nameof(Formats.All))] Format format)
        {
            //Arrange
            ClassWithEnum toSerialize = _fixture.Create<ClassWithEnum>();
            toSerialize.EnumProp = null;
            toSerialize.SecondEnumProp = null;


            //Act
            var deserialized = RoundTrip.Run<ClassWithEnum>(toSerialize, format);


            //Assert
            deserialized.Should().NotBeNull();
            deserialized.EnumProp.Should().Be(TestEnum.be);
            deserialized.SecondEnumProp.Should().Be(TestEnum.ca);
        }

        [Test]
        public void Class_with_enum_with_member_value_attributes([ValueSource(typeof(Formats), nameof(Formats.All))] Format format)
        {
            //Arrange
            ClassWithEnumDefiningMembers toSerialize = _fixture.Create<ClassWithEnumDefiningMembers>();
            toSerialize.EnumProp = TestEnumWithMembers.Positive;
            toSerialize.EnumPropWithStringDefault = null;

            //Act
            var deserialized = RoundTrip.Run<ClassWithEnumDefiningMembers>(toSerialize, format);

            //Assert
            deserialized.Should().NotBeNull();
            deserialized.EnumProp.Should().Be(TestEnumWithMembers.Positive);
            deserialized.EnumPropWithDefault.Should().Be(TestEnumWithMembers.Negative);
            deserialized.EnumPropWithStringDefault.Should().Be(TestEnumWithMembers.Negative);
        }

        [Test]
        public void Enum_with_duplicate_values([ValueSource(typeof(Formats), nameof(Formats.All))] Format format)
        {
            var itemsToTest =
            new [] {
                TestDuplicateEnum.Test,
                TestDuplicateEnum.Exam, //Compilation already sets these values to the primary duplicate value.
                TestDuplicateEnum.Error,
                TestDuplicateEnum.Fail,
                TestDuplicateEnum.TestLabel,
                TestDuplicateEnum.TestLabel2
            };
            
            foreach (var toSerialize in itemsToTest)
            {
                //Act
                var deserialized = RoundTrip.Run<TestDuplicateEnum>(toSerialize, format);

                //Assert
                ((int)deserialized).Should().Be((int)toSerialize);
            }
        }

        [Test]
        public void Class_with_enum_with_duplicate_value([ValueSource(typeof(Formats), nameof(Formats.All))] Format format)
        {
            //Arrange
            ClassWithDuplicateEnums toSerialize = _fixture.Create<ClassWithDuplicateEnums>();

            //Act
            var deserialized = RoundTrip.Run<ClassWithDuplicateEnums>(toSerialize, format);

            //Assert
            deserialized.Should().NotBeNull();
            deserialized.Should().BeEquivalentTo(toSerialize);
        }
    }
}
