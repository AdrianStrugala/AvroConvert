using System;
using SolTechnology.Avro.BuildSchema;
using SolTechnology.Avro.BuildSchema.SchemaModels;
using SolTechnology.Avro.BuildSchema.SchemaModels.Abstract;
using Xunit;

namespace AvroConvertUnitTests.BuildSchemaTests
{
    public class LogicalTypeTests
    {
        [Theory]
        [InlineData(typeof(decimal), 29, 14)]
        public void BuildDecimalSchema(Type type, int precision, int scale)
        {
            //Act
            var builder = new ReflectionSchemaBuilder();
            TypeSchema schema = builder.BuildSchema(type);


            //Assert
            Assert.IsType<DecimalSchema>(schema);
            var decimalSchema = (DecimalSchema)schema;
            
            Assert.NotNull(schema);
            Assert.Equal(precision, decimalSchema.Precision);
            Assert.Equal(scale, decimalSchema.Scale);
        }

        [Theory]
        [InlineData(typeof(decimal?), 29, 14)]
        public void BuildNullableDecimalSchema(Type type, int precision, int scale)
        {
            //Act
            var builder = new ReflectionSchemaBuilder();
            TypeSchema schema = builder.BuildSchema(type);


            //Assert
            Assert.IsType<UnionSchema>(schema);
            var unionSchema = (UnionSchema)schema;

            Assert.IsType<NullSchema>(unionSchema.Schemas[0]);
            Assert.IsType<DecimalSchema>(unionSchema.Schemas[1]);

            var decimalSchema = (DecimalSchema)unionSchema.Schemas[1];

            Assert.NotNull(schema);
            Assert.Equal(precision, decimalSchema.Precision);
            Assert.Equal(scale, decimalSchema.Scale);
        }

        [Theory]
        [InlineData(typeof(Guid))]
        public void BuildGuidSchema(Type type)
        {
            //Act
            var builder = new ReflectionSchemaBuilder();
            TypeSchema schema = builder.BuildSchema(type);


            //Assert
            Assert.IsType<UuidSchema>(schema);
            var uuidSchema = (UuidSchema)schema;

            Assert.NotNull(uuidSchema);
        }

        [Theory]
        [InlineData(typeof(Guid?))]
        public void BuildNullableGuidSchema(Type type)
        {
            //Act
            var builder = new ReflectionSchemaBuilder();
            TypeSchema schema = builder.BuildSchema(type);


            //Assert
            Assert.IsType<UnionSchema>(schema);
            var unionSchema = (UnionSchema)schema;

            Assert.IsType<NullSchema>(unionSchema.Schemas[0]);
            Assert.IsType<UuidSchema>(unionSchema.Schemas[1]);

            var uuidSchema = (UuidSchema)unionSchema.Schemas[1];

            Assert.NotNull(uuidSchema);
        }

        [Theory]
        [InlineData(typeof(DateTime))]
        [InlineData(typeof(DateTimeOffset))]
        public void BuildDateTimeSchema(Type type)
        {
            //Act
            var builder = new ReflectionSchemaBuilder();
            TypeSchema schema = builder.BuildSchema(type);


            //Assert
            Assert.IsType<TimestampMillisecondsSchema>(schema);
            var resolvedSchema = (TimestampMillisecondsSchema)schema;

            Assert.NotNull(resolvedSchema);
        }

        [Theory]
        [InlineData(typeof(DateTime?))]
        [InlineData(typeof(DateTimeOffset?))]
        public void BuildNullableDateTimeSchema(Type type)
        {
            //Act
            var builder = new ReflectionSchemaBuilder();
            TypeSchema schema = builder.BuildSchema(type);


            //Assert
            Assert.IsType<UnionSchema>(schema);
            var unionSchema = (UnionSchema)schema;

            Assert.IsType<NullSchema>(unionSchema.Schemas[0]);
            Assert.IsType<TimestampMillisecondsSchema>(unionSchema.Schemas[1]);

            var resolvedSchema = (TimestampMillisecondsSchema)unionSchema.Schemas[1];

            Assert.NotNull(resolvedSchema);
        }

        [Theory]
        [InlineData(typeof(TimeSpan))]
        public void BuildTimespanSchema(Type type)
        {
            //Act
            var builder = new ReflectionSchemaBuilder();
            TypeSchema schema = builder.BuildSchema(type);


            //Assert
            Assert.IsType<DurationSchema>(schema);
            var resolvedSchema = (DurationSchema)schema;

            Assert.NotNull(resolvedSchema);
        }

        [Theory]
        [InlineData(typeof(TimeSpan?))]
        public void BuildTimeSpanSchema(Type type)
        {
            //Act
            var builder = new ReflectionSchemaBuilder();
            TypeSchema schema = builder.BuildSchema(type);


            //Assert
            Assert.IsType<UnionSchema>(schema);
            var unionSchema = (UnionSchema)schema;

            Assert.IsType<NullSchema>(unionSchema.Schemas[0]);
            Assert.IsType<DurationSchema>(unionSchema.Schemas[1]);

            var resolvedSchema = (DurationSchema)unionSchema.Schemas[1];

            Assert.NotNull(resolvedSchema);
        }
    }
}
