using AwesomeAssertions;
using System;
using System.Linq;
using SolTechnology.Avro;
using SolTechnology.Avro.AvroObjectServices.BuildSchema;
using SolTechnology.Avro.AvroObjectServices.Schemas;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;
using NUnit.Framework;

namespace AvroConvertUnitTests.BuildSchemaTests
{
    public class AttributesTests
    {
        [Test]
        public void BuildSchema_RecordWithAttributes_AttributesAreAppliedToSchema()
        {
            //Arrange
            var builder = new ReflectionSchemaBuilder();


            //Act
            TypeSchema schema = builder.BuildSchema(typeof(AttributeClass));


            //Assert
            schema.Should().BeOfType<RecordSchema>();
            var resultSchema = (RecordSchema)schema;
            
            resultSchema.Should().NotBeNull();
            resultSchema.Doc.Should().Be("This is Doc of User Class");
            resultSchema.Name.Should().Be("User");
            resultSchema.Namespace.Should().Be("userspace");

            var stringField = resultSchema.Fields.SingleOrDefault(f => f.Name == "name");
            stringField.Should().NotBeNull();
            stringField.Doc.Should().Be("This is Doc of record field");

            var intField = resultSchema.Fields.SingleOrDefault(f => f.Name == "favorite_number");
            intField.Should().NotBeNull();
            intField.HasDefaultValue.Should().BeTrue();
            intField.DefaultValue.Should().Be(2137);


            var decimalField = resultSchema.Fields.SingleOrDefault(f => f.Name == nameof(AttributeClass.AvroDecimal));
            decimalField.TypeSchema.Should().BeOfType<DecimalSchema>();
            var decimalSchema = (DecimalSchema)decimalField.TypeSchema;
            decimalSchema.Scale.Should().Be(2);
            decimalSchema.Precision.Should().Be(4);
        }
    }
}
