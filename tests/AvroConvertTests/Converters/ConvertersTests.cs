using System;
using System.Collections.Generic;
using AutoFixture;
using SolTechnology.Avro;
using SolTechnology.Avro.AvroObjectServices.Read;
using SolTechnology.Avro.AvroObjectServices.Schemas;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;
using SolTechnology.Avro.AvroObjectServices.Write;
using SolTechnology.Avro.Converters;
using Xunit;

namespace AvroConvertComponentTests.Converters
{
    public class ConvertersTests
    {


        [Fact]
        public void Custom_converter_is_used_for_schema_generation_and_serialization_and_deserialization()
        {
            //Arrange
            string underTest = "stringThatShouldBeOverriden";


            //Act
            var avroConvertOptions = new AvroConvertOptions
            {
                AvroConverters = new List<IAvroConverter>
                {
                    new RandomStringConverter()
                }
            };

            var result = AvroConvert.Serialize(underTest, avroConvertOptions);
            var deserialized = AvroConvert.Deserialize<string>(result, avroConvertOptions);


            //Assert
            Assert.NotNull(result);
            Assert.NotEqual(underTest, deserialized);
        }
    }


    public class RandomStringConverter : IAvroConverter
    {
        Fixture fixture = new Fixture();

        public TypeSchema TypeSchema => new RandomStringSchema(typeof(string), AvroType.String, nameof(RandomStringSchema));

        public void Serialize(object data, IWriter writer)
        {
            writer.WriteString(fixture.Create<string>());
        }

        public object Deserialize(IReader reader)
        {
            return reader.ReadString();
        }
    }

    public sealed class RandomStringSchema : BaseConverterSchema
    {
        public RandomStringSchema(Type runtimeType, AvroType avroType, string name, IDictionary<string, string> attributes = null)
            : base(runtimeType, avroType, name, attributes)
        {
        }
    }
}
