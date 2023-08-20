using System;
using System.Collections.Generic;
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
            string underTest = "someRandomString";


            //Act
            var avroConvertOptions = new AvroConvertOptions
            {
                AvroConverters = new List<IAvroConverter>
                {
                    new RandomStringConverter()
                }
            };

            var result = AvroConvert.Serialize(underTest, avroConvertOptions);
            var deserialized = AvroConvert.Deserialize<string>(result);


            //Assert
            Assert.NotNull(result);
            Assert.Equal("DUPA", deserialized);
        }
    }


    public class RandomStringConverter : IAvroConverter
    {
        public Type RuntimeType => typeof(string);
        public TypeSchema TypeSchema => new RandomStringSchema(RuntimeType);
        public void Serialize(object data, IWriter writer)
        {
            writer.WriteString("DUPA");
        }

        public object Deserialize(Type readType, IReader reader)
        {
            return reader.ReadString();
        }
    }

    public sealed class RandomStringSchema : TypeSchema
    {
        public RandomStringSchema(Type runtimeType) : base(runtimeType, new Dictionary<string, string>())
        {
            Name = "RandomSchema";
        }
    }
}
