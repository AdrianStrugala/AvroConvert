using System;
using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using FluentAssertions;
using SolTechnology.Avro;
using SolTechnology.Avro.Infrastructure.Attributes;
using Xunit;

namespace AvroConvertComponentTests.FullSerializationAndDeserialization
{
    public class ClassWithFieldsNotPropertiesTests
    {
        private readonly Fixture _fixture = new();

        [Theory]
        [MemberData(nameof(TestEngine.All), MemberType = typeof(TestEngine))]
        public void Simple_class_with_fields(Func<object, Type, dynamic> engine)
        {
            //Arrange
            ClassWithoutGetters toSerialize = _fixture.Create<ClassWithoutGetters>();


            //Act
            var deserialized = engine.Invoke(toSerialize, typeof(ClassWithoutGetters));


            //Assert
            Assert.NotNull(deserialized);
            Assert.Equal(toSerialize.Count, deserialized.Count);
            Assert.Equal(toSerialize.SomeString, deserialized.SomeString);
        }

        [Theory]
        [MemberData(nameof(TestEngine.All), MemberType = typeof(TestEngine))]
        public void Nested_class_with_fields(Func<object, Type, dynamic> engine)
        {
            //Arrange
            ComplexClassWithoutGetters toSerialize = _fixture.Create<ComplexClassWithoutGetters>();


            //Act
            var deserialized = engine.Invoke(toSerialize, typeof(ComplexClassWithoutGetters));


            //Assert
            Assert.NotNull(deserialized);
            Assert.Equal(toSerialize, deserialized);
        }

        [Theory]
        [MemberData(nameof(TestEngine.All), MemberType = typeof(TestEngine))]
        public void Nested_class_with_fields_and_attributes(Func<object, Type, dynamic> engine)
        {
            //Arrange
            AttributeClassWithoutGetters toSerialize = _fixture.Create<AttributeClassWithoutGetters>();


            //Act
            var deserialized = engine.Invoke(toSerialize, typeof(AttributeClassWithoutGetters));


            //Assert
            Assert.NotNull(deserialized);
            Assert.Equal(toSerialize, deserialized);
        }


        [Theory]
        [MemberData(nameof(TestEngine.All), MemberType = typeof(TestEngine))]
        public void Class_with_read_only_properties(Func<object, Type, dynamic> engine)
        {
            //Arrange
            var toSerialize = _fixture.Create<ClassWithReadOnlyProperties>();


            //Act
            var deserialized = engine.Invoke(toSerialize, typeof(ClassWithReadOnlyProperties));


            //Assert
            Assert.NotNull(deserialized);
            Assert.Equal(toSerialize.ReadOnly, deserialized.ReadOnly);
            Assert.Equal(toSerialize.NullableReadOnly, deserialized.NullableReadOnly);
        }

        [Theory]
        [MemberData(nameof(TestEngine.All), MemberType = typeof(TestEngine))]
        public void Collection_of_classes_with_read_only_properties(Func<object, Type, dynamic> engine)
        {
            //Arrange
            var parent = new Parent()
            {
                Children = new List<Child>
                {
                    new Child()
                    {
                        Toys = new List<Toy>()
                        {
                            new Toy()
                            {
                                Weight = 10, Price = 11, Name = "Toy1"
                            },
                            new Toy()
                            {
                                Weight = 6, Price = 2, Name = "Toy2"
                            },
                            new Toy()
                            {
                                Weight = 500, Price = 15, Name = "Toy3"
                            }
                        },
                    }
                }
            };

            //Act
            var deserialized = (Parent)engine.Invoke(parent, typeof(Parent));

            //Assert
            deserialized.Should().BeEquivalentTo(parent);
        }
    }


    public record Parent
    {
        public List<Child> Children { get; set; } = new();
    }

    public record Child
    {
        public List<Toy> Toys { get; set; } = new();

        public int? TotalPrice => Toys != null && Toys.Any() ? Toys.Sum(v => v.Price) : null;

        public int? TotalWeight => Toys != null && Toys.Any() ? Toys.Sum(v => v.Weight) : null;

        [AvroDecimal(Precision = 28, Scale = 28)]
        public decimal? AveragePricePerWeight => Toys?.Average(t => t.PricePerWeightUnit);
    }

    public record Toy
    {
        public string Name { get; set; }
        public int Weight { get; set; }
        public int Price { get; set; }

        public decimal PricePerWeightUnit => decimal.Round(Price / Weight, 2);
    }
}
