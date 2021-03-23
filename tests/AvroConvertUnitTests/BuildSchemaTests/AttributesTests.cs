using System;
using System.Linq;
using SolTechnology.Avro;
using SolTechnology.Avro.BuildSchema;
using SolTechnology.Avro.Schema;
using SolTechnology.Avro.Schema.Abstract;
using Xunit;

namespace AvroConvertUnitTests.BuildSchemaTests
{
    public class AttributesTests
    {
        [Fact]
        public void BuildSchema_RecordWithAttributes_AttributesAreAppliedToSchema()
        {
            //Arrange
            var builder = new ReflectionSchemaBuilder();


            //Act
            TypeSchema schema = builder.BuildSchema(typeof(AttributeClass));

            //Assert
            Assert.IsType<RecordSchema>(schema);
            var resultSchema = (RecordSchema)schema;
            
            Assert.NotNull(resultSchema);
            Assert.Equal("This is Doc of User Class", resultSchema.Doc);
            Assert.Equal("User", resultSchema.Name);
            Assert.Equal("userspace", resultSchema.Namespace);

            var stringField = resultSchema.Fields.SingleOrDefault(f => f.Name == "name");
            Assert.NotNull(stringField);
            Assert.Equal("This is Doc of record field", stringField.Doc);

            var intField = resultSchema.Fields.SingleOrDefault(f => f.Name == "favorite_number");
            Assert.NotNull(intField);
            Assert.Equal(true, intField.HasDefaultValue);
            Assert.Equal(2137, intField.DefaultValue);
        }
    }
}
