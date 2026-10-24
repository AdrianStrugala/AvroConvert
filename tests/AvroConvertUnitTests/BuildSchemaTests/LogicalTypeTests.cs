using AwesomeAssertions;
using System;
using SolTechnology.Avro.AvroObjectServices.BuildSchema;
using SolTechnology.Avro.AvroObjectServices.Schemas;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;
using NUnit.Framework;

namespace AvroConvertUnitTests.BuildSchemaTests
{
    public class LogicalTypeTests
    {
        [TestCase(@"{""type"":""string"",""logicalType"":""datetime""}", AvroType.String)]
        [TestCase(@"{""type"":""long"",""logicalType"":""timestamp-picos""}", AvroType.Long)]
        [TestCase(@"{""type"":""string"",""logicalType"":""timestamp-millis""}", AvroType.String)]
        [TestCase(@"{""type"":""int"",""logicalType"":""uuid""}", AvroType.Int)]
        [TestCase(@"{""type"":""fixed"",""name"":""f"",""size"":8,""logicalType"":""uuid""}", AvroType.Fixed)]
        [TestCase(@"{""type"":""string"",""logicalType"":""big-decimal""}", AvroType.String)]
        [TestCase(@"{""type"":""bytes"",""logicalType"":""geography_wkb""}", AvroType.Bytes)]
        public void Parse_UnknownLogicalType_FallsBackToUnderlyingType(string json, AvroType expected)
        {
            //Act
            var schema = Schema.Parse(json);


            //Assert
            schema.Type.Should().Be(expected);
        }

        [Test]
        public void Parse_TimeMicros_IsRecognised()
        {
            //Act
            var schema = Schema.Parse(@"{""type"":""long"",""logicalType"":""time-micros""}");


            //Assert
            schema.Should().BeOfType<TimeMicrosecondsSchema>();
            ((LogicalTypeSchema)schema).LogicalTypeName.Should().Be("time-micros");
        }

        [TestCase(typeof(decimal), 29, 14)]
        public void BuildDecimalSchema(Type type, int precision, int scale)
        {
            //Act
            var builder = new ReflectionSchemaBuilder();
            TypeSchema schema = builder.BuildSchema(type);


            //Assert
            schema.Should().BeOfType<DecimalSchema>();
            var decimalSchema = (DecimalSchema)schema;
            
            schema.Should().NotBeNull();
            decimalSchema.Precision.Should().Be(precision);
            decimalSchema.Scale.Should().Be(scale);
        }

        [TestCase(typeof(decimal?), 29, 14)]
        public void BuildNullableDecimalSchema(Type type, int precision, int scale)
        {
            //Act
            var builder = new ReflectionSchemaBuilder();
            TypeSchema schema = builder.BuildSchema(type);


            //Assert
            schema.Should().BeOfType<UnionSchema>();
            var unionSchema = (UnionSchema)schema;

            unionSchema.Schemas[0].Should().BeOfType<NullSchema>();
            unionSchema.Schemas[1].Should().BeOfType<DecimalSchema>();

            var decimalSchema = (DecimalSchema)unionSchema.Schemas[1];

            schema.Should().NotBeNull();
            decimalSchema.Precision.Should().Be(precision);
            decimalSchema.Scale.Should().Be(scale);
        }

        [TestCase(typeof(Guid))]
        public void BuildGuidSchema(Type type)
        {
            //Act
            var builder = new ReflectionSchemaBuilder();
            TypeSchema schema = builder.BuildSchema(type);


            //Assert
            schema.Should().BeOfType<UuidSchema>();
            var uuidSchema = (UuidSchema)schema;

            uuidSchema.Should().NotBeNull();
        }

        [TestCase(typeof(Guid?))]
        public void BuildNullableGuidSchema(Type type)
        {
            //Act
            var builder = new ReflectionSchemaBuilder();
            TypeSchema schema = builder.BuildSchema(type);


            //Assert
            schema.Should().BeOfType<UnionSchema>();
            var unionSchema = (UnionSchema)schema;

            unionSchema.Schemas[0].Should().BeOfType<NullSchema>();
            unionSchema.Schemas[1].Should().BeOfType<UuidSchema>();

            var uuidSchema = (UuidSchema)unionSchema.Schemas[1];

            uuidSchema.Should().NotBeNull();
        }

        [TestCase(typeof(DateTime))]
        [TestCase(typeof(DateTimeOffset))]
        public void BuildDateTimeSchema(Type type)
        {
            //Act
            var builder = new ReflectionSchemaBuilder();
            TypeSchema schema = builder.BuildSchema(type);


            //Assert
            schema.Should().BeOfType<TimestampMicrosecondsSchema>();
            var resolvedSchema = (TimestampMicrosecondsSchema)schema;

            resolvedSchema.Should().NotBeNull();
        }

        [TestCase(typeof(DateTime?))]
        [TestCase(typeof(DateTimeOffset?))]
        public void BuildNullableDateTimeSchema(Type type)
        {
            //Act
            var builder = new ReflectionSchemaBuilder();
            TypeSchema schema = builder.BuildSchema(type);


            //Assert
            schema.Should().BeOfType<UnionSchema>();
            var unionSchema = (UnionSchema)schema;

            unionSchema.Schemas[0].Should().BeOfType<NullSchema>();
            unionSchema.Schemas[1].Should().BeOfType<TimestampMicrosecondsSchema>();

            var resolvedSchema = (TimestampMicrosecondsSchema)unionSchema.Schemas[1];

            resolvedSchema.Should().NotBeNull();
        }

        [TestCase(typeof(TimeSpan))]
        public void BuildTimespanSchema(Type type)
        {
            //Act
            var builder = new ReflectionSchemaBuilder();
            TypeSchema schema = builder.BuildSchema(type);


            //Assert
            schema.Should().BeOfType<DurationSchema>();
            var resolvedSchema = (DurationSchema)schema;

            resolvedSchema.Should().NotBeNull();
        }

        [TestCase(typeof(TimeSpan?))]
        public void BuildTimeSpanSchema(Type type)
        {
            //Act
            var builder = new ReflectionSchemaBuilder();
            TypeSchema schema = builder.BuildSchema(type);


            //Assert
            schema.Should().BeOfType<UnionSchema>();
            var unionSchema = (UnionSchema)schema;

            unionSchema.Schemas[0].Should().BeOfType<NullSchema>();
            unionSchema.Schemas[1].Should().BeOfType<DurationSchema>();

            var resolvedSchema = (DurationSchema)unionSchema.Schemas[1];

            resolvedSchema.Should().NotBeNull();
        }
    }
}
