using System;
using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using AwesomeAssertions;
using SolTechnology.Avro;
using SolTechnology.Avro.Infrastructure.Attributes;
using NUnit.Framework;

namespace AvroConvertComponentTests.FullSerializationAndDeserialization
{
    public class ClassWithFieldsNotPropertiesTests
    {
        private readonly Fixture _fixture = new();

        [Test]
        public void Simple_class_with_fields([ValueSource(typeof(Formats), nameof(Formats.All))] Format format)
        {
            //Arrange
            ClassWithoutGetters toSerialize = _fixture.Create<ClassWithoutGetters>();


            //Act
            var deserialized = RoundTrip.Run<ClassWithoutGetters>(toSerialize, format);


            //Assert
            deserialized.Should().NotBeNull();
            deserialized.Count.Should().Be(toSerialize.Count);
            deserialized.SomeString.Should().Be(toSerialize.SomeString);
        }

        [Test]
        public void Nested_class_with_fields([ValueSource(typeof(Formats), nameof(Formats.All))] Format format)
        {
            //Arrange
            ComplexClassWithoutGetters toSerialize = _fixture.Create<ComplexClassWithoutGetters>();


            //Act
            var deserialized = RoundTrip.Run<ComplexClassWithoutGetters>(toSerialize, format);


            //Assert
            deserialized.Should().NotBeNull();
            deserialized.Should().BeEquivalentTo(toSerialize);
        }

        [Test]
        public void Nested_class_with_fields_and_attributes([ValueSource(typeof(Formats), nameof(Formats.All))] Format format)
        {
            //Arrange
            AttributeClassWithoutGetters toSerialize = _fixture.Create<AttributeClassWithoutGetters>();


            //Act
            var deserialized = RoundTrip.Run<AttributeClassWithoutGetters>(toSerialize, format);


            //Assert
            deserialized.Should().NotBeNull();
            deserialized.Should().BeEquivalentTo(toSerialize);
        }


        [Test]
        public void Class_with_read_only_properties([ValueSource(typeof(Formats), nameof(Formats.All))] Format format)
        {
            //Arrange
            var toSerialize = _fixture.Create<ClassWithReadOnlyProperties>();


            //Act
            var deserialized = RoundTrip.Run<ClassWithReadOnlyProperties>(toSerialize, format);


            //Assert
            deserialized.Should().NotBeNull();
            deserialized.ReadOnly.Should().Be(toSerialize.ReadOnly);
            deserialized.NullableReadOnly.Should().Be(toSerialize.NullableReadOnly);
        }

        [Test]
        public void Collection_of_classes_with_read_only_properties([ValueSource(typeof(Formats), nameof(Formats.All))] Format format)
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
            var deserialized = RoundTrip.Run<Parent>(parent, format);

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
