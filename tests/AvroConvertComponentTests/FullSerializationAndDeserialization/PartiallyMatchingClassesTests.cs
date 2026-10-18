using AwesomeAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using AutoFixture.Kernel;
using SolTechnology.Avro;
using SolTechnology.Avro.Policies;
using NUnit.Framework;

namespace AvroConvertComponentTests.FullSerializationAndDeserialization
{
    public class PartiallyMatchingClassesTests
    {
        private readonly Fixture _fixture = new();

        [Test]
        public void SerializeBiggerObjectAndReadSmaller([ValueSource(typeof(Formats), nameof(Formats.Evolution))] Format format)
        {
            //Arrange
            ExtendedBaseTestClass toSerialize = _fixture.Create<ExtendedBaseTestClass>();


            //Act
            var writeSchema = AvroConvert.GenerateSchema(typeof(ExtendedBaseTestClass));
            var readSchema = AvroConvert.GenerateSchema(typeof(BaseTestClass));
            var deserialized = RoundTrip.Evolve<BaseTestClass>(toSerialize, format, writeSchema, readSchema);


            //Assert
            deserialized.Should().NotBeNull();
            deserialized.justSomeProperty.Should().Be(toSerialize.justSomeProperty);
            deserialized.andLongProperty.Should().Be(toSerialize.andLongProperty);
        }


        [Test]
        public void SerializeSmallerClassAndReadBigger_MissingValueTypeFieldWithoutDefault_Throws()
        {
            //Arrange
            ReducedBaseTestClass toSerialize = _fixture.Create<ReducedBaseTestClass>();
            var serialized = AvroConvert.Serialize(toSerialize);


            //Act
            var exception = Recorded.Exception(() => AvroConvert.Deserialize<BaseTestClass>(serialized));


            //Assert
            exception.Should().NotBeNull();
            exception.InnerException!.Message.Should().Contain(nameof(BaseTestClass.andLongProperty));
        }


        [Test]
        public void SerializeSmallerClassAndReadBigger_UseDefaultOption_MissingFieldsGetClrDefault()
        {
            //Arrange
            ReducedBaseTestClass toSerialize = _fixture.Create<ReducedBaseTestClass>();
            var serialized = AvroConvert.Serialize(toSerialize);
            var options = new AvroConvertOptions { MissingFieldHandling = AvroMissingFieldHandling.UseDefault };


            //Act
            var deserialized = AvroConvert.Deserialize<BaseTestClass>(serialized, options);


            //Assert
            deserialized.Should().NotBeNull();
            deserialized.justSomeProperty.Should().Be(toSerialize.justSomeProperty);
            deserialized.andLongProperty.Should().Be(0L);
            deserialized.objectProperty.Should().BeNull();
        }


        [Test]
        public void SerializeSmallerClassAndReadBigger_ReaderDefaults_MissingFieldsGetReaderDefault()
        {
            //Arrange
            ReducedBaseTestClass toSerialize = _fixture.Create<ReducedBaseTestClass>();
            var serialized = AvroConvert.Serialize(toSerialize);


            //Act
            var deserialized = AvroConvert.Deserialize<BaseTestClassWithDefaults>(serialized);


            //Assert
            deserialized.Should().NotBeNull();
            deserialized.justSomeProperty.Should().Be(toSerialize.justSomeProperty);
            deserialized.andLongProperty.Should().Be(42L);
            deserialized.nullableIntProperty.Should().Be(7);
            deserialized.objectProperty.Should().BeNull();
        }


        [Test]
        public void SerializeSmallerClassAndReadBigger([ValueSource(typeof(Formats), nameof(Formats.Evolution))] Format format)
        {
            //Arrange
            ReducedBaseTestClass toSerialize = _fixture.Create<ReducedBaseTestClass>();


            //Act
            // Written with the ReducedBaseTestClass schema, read with the BaseTestClassWithDefaults schema.
            var deserialized = RoundTrip.Evolve<BaseTestClassWithDefaults>(toSerialize, format);


            //Assert
            deserialized.Should().NotBeNull();
            deserialized.justSomeProperty.Should().Be(toSerialize.justSomeProperty);
        }


        [Test]
        public void SerializeHeadlessBiggerObjectUsingReducedSchemaAndReadSmaller([ValueSource(typeof(Formats), nameof(Formats.Evolution))] Format format)
        {
            //Arrange
            ExtendedBaseTestClass toSerialize = _fixture.Create<ExtendedBaseTestClass>();
            string schema = AvroConvert.GenerateSchema(typeof(BaseTestClass));

            //Act
            var deserialized = RoundTrip.Evolve<BaseTestClass>(toSerialize, format, schema, schema);

            //Assert
            deserialized.Should().NotBeNull();
            deserialized.andLongProperty.Should().Be(toSerialize.andLongProperty);
            deserialized.justSomeProperty.Should().Be(toSerialize.justSomeProperty);
        }


        [Test]
        public void SerializeBiggerAvroObjectAndReadSmaller([ValueSource(typeof(Formats), nameof(Formats.Evolution))] Format format)
        {
            //Arrange
            AttributeClass toSerialize = _fixture.Create<AttributeClass>();


            //Act
            // Written with the AttributeClass schema, read with the SmallerAttributeClass schema.
            var deserialized = RoundTrip.Evolve<SmallerAttributeClass>(toSerialize, format);


            //Assert
            deserialized.Should().NotBeNull();
            deserialized.NullableStringProperty.Should().Be(toSerialize.NullableStringProperty);
            deserialized.NullableIntProperty.Should().Be(toSerialize.NullableIntProperty);
        }


        [Test]
        public void SerializeAndDeserializeClassesWithDifferentPropertyCases([ValueSource(typeof(Formats), nameof(Formats.Evolution))] Format format)
        {
            //Arrange
            BaseTestClass toSerialize = _fixture.Create<BaseTestClass>();


            //Act
            // Written with the BaseTestClass schema, read with the DifferentCaseBaseTestClass schema.
            var deserialized = RoundTrip.Evolve<DifferentCaseBaseTestClass>(toSerialize, format);


            //Assert
            deserialized.Should().NotBeNull();
            deserialized.JustSomeProperty.Should().Be(toSerialize.justSomeProperty);
            deserialized.AndLongProperty.Should().Be(toSerialize.andLongProperty);
        }

        private static readonly (Type Value, Type Nullable)[] ValueAndNullableTypes =
        {
            (typeof(short), typeof(short?)),
            (typeof(uint), typeof(uint?)),
            (typeof(ushort), typeof(ushort?)),
            (typeof(int), typeof(int?)),
            (typeof(long), typeof(long?)),
            (typeof(Guid), typeof(Guid?)),
            (typeof(ulong), typeof(ulong?)),
            (typeof(char), typeof(char?)),
            (typeof(byte), typeof(byte?)),
            (typeof(sbyte), typeof(sbyte?)),
            (typeof(bool), typeof(bool?)),
            (typeof(float), typeof(float?)),
            (typeof(double), typeof(double?)),
            (typeof(decimal), typeof(decimal?))
        };

        public static IEnumerable<TestCaseData> PrimitivesAndNullables =>
            from pair in ValueAndNullableTypes
            from format in Formats.All
            select new TestCaseData(pair.Value, pair.Nullable, format);

        [TestCaseSource(nameof(PrimitivesAndNullables))]
        public void Serialize_primitives_read_nullable_correspondings(Type serializationType, Type deserializationType, Format format)
        {
            //Arrange
            var underTest = new SpecimenContext(_fixture).Resolve(serializationType);


            //Act
            var deserialized = RoundTrip.Run(underTest, deserializationType, format);


            //Assert
            deserialized.Should().NotBeNull();
            deserialized.Should().BeEquivalentTo(underTest);
        }
    }
}
