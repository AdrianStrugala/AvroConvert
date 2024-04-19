using System;
using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using AutoFixture.Kernel;
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
            var writeSchema = AvroConvert.GenerateSchema(typeof(ExtendedBaseTestClass));
            var readSchema = AvroConvert.GenerateSchema(typeof(BaseTestClass));
            var deserialized = engine.Invoke(toSerialize, typeof(BaseTestClass), writeSchema, readSchema);


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

        public static IEnumerable<object[]> PrimitivesAndNullables
        {
            get
            {
                foreach (var c in TestEngine.All().ToList())
                {
                    yield return new[] { typeof(short), typeof(short?), c.FirstOrDefault() };
                    yield return new[] { typeof(uint), typeof(uint?), c.FirstOrDefault() };
                    yield return new[] { typeof(ushort), typeof(ushort?), c.FirstOrDefault() };
                    yield return new[] { typeof(int), typeof(int?), c.FirstOrDefault() };
                    yield return new[] { typeof(long), typeof(long?), c.FirstOrDefault() };
                    yield return new[] { typeof(Guid), typeof(Guid?), c.FirstOrDefault() };
                    yield return new[] { typeof(ulong), typeof(ulong?), c.FirstOrDefault() };
                    yield return new[] { typeof(char), typeof(char?), c.FirstOrDefault() };
                    yield return new[] { typeof(byte), typeof(byte?), c.FirstOrDefault() };
                    yield return new[] { typeof(sbyte), typeof(sbyte?), c.FirstOrDefault() };
                    yield return new[] { typeof(bool), typeof(bool?), c.FirstOrDefault() };
                    yield return new[] { typeof(float), typeof(float?), c.FirstOrDefault() };
                    yield return new[] { typeof(double), typeof(double?), c.FirstOrDefault() };
                    yield return new[] { typeof(decimal), typeof(decimal?), c.FirstOrDefault() };
                }
            }
        }

        [Theory]
        [MemberData(nameof(PrimitivesAndNullables))]
        public void Serialize_primitives_read_nullable_correspondings(
            Type serializationType, Type deserializationType,
            Func<object, Type, dynamic> engine)
        {
            //Arrange
            var underTest = new SpecimenContext(_fixture).Resolve(serializationType);


            //Act
            var deserialized = engine.Invoke(underTest, deserializationType);


            //Assert
            Assert.NotNull(deserialized);
            Assert.Equal(underTest, deserialized);
        }
    }
}
