using System;
using System.Runtime.Serialization;
using SolTechnology.Avro;
using SolTechnology.Avro.AvroObjectServices.BuildSchema;
using SolTechnology.Avro.AvroObjectServices.Read;
using SolTechnology.Avro.AvroObjectServices.Schemas;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;
using Xunit;

namespace AvroConvertUnitTests.BuildSchemaTests
{
    public class SpecificationSchemaTests
    {
        [Theory]
        [InlineData("timestamp-nanos", false, 1L)]
        [InlineData("local-timestamp-millis", true, 1_000_000L)]
        [InlineData("local-timestamp-micros", true, 1_000L)]
        [InlineData("local-timestamp-nanos", true, 1L)]
        public void Parse_TimestampLogicalTypes_AreRecognised(string logicalType, bool local, long nanosecondsPerUnit)
        {
            //Act
            var schema = Schema.Parse($@"{{""type"":""long"",""logicalType"":""{logicalType}""}}");


            //Assert
            var timestamp = Assert.IsType<TimestampSchema>(schema);
            Assert.Equal(logicalType, timestamp.LogicalTypeName);
            Assert.Equal(local, timestamp.IsLocal);
            Assert.Equal(nanosecondsPerUnit, timestamp.NanosecondsPerUnit);
            Assert.Equal($@"{{""type"":""long"",""logicalType"":""{logicalType}""}}", schema.ToString());
        }

        [Fact]
        public void Parse_UuidOnFixed16_KeepsFixedBase()
        {
            //Act
            var schema = Schema.Parse(@"{""type"":""fixed"",""name"":""Id"",""namespace"":""ns"",""size"":16,""logicalType"":""uuid""}");


            //Assert
            var uuid = Assert.IsType<UuidSchema>(schema);
            var fixedBase = Assert.IsType<FixedSchema>(uuid.BaseTypeSchema);
            Assert.Equal("ns.Id", fixedBase.FullName);
            Assert.Equal(16, fixedBase.Size);
            Assert.Equal(@"{""type"":""fixed"",""size"":16,""name"":""Id"",""namespace"":""ns"",""logicalType"":""uuid""}", schema.ToString());
        }

        [Fact]
        public void Parse_DecimalOnFixed_KeepsFixedBase()
        {
            //Act
            var schema = Schema.Parse(@"{""type"":""fixed"",""name"":""Money"",""size"":8,""logicalType"":""decimal"",""precision"":12,""scale"":2}");


            //Assert
            var decimalSchema = Assert.IsType<DecimalSchema>(schema);
            Assert.Equal(AvroType.Fixed, decimalSchema.BaseTypeSchema.Type);
            Assert.Equal(12, decimalSchema.Precision);
            Assert.Equal(2, decimalSchema.Scale);
            Assert.Equal(@"{""type"":""fixed"",""size"":8,""name"":""Money"",""logicalType"":""decimal"",""precision"":12,""scale"":2}", schema.ToString());
        }

        [Fact]
        public void Parse_BigDecimal_IsRecognised()
        {
            //Act
            var schema = Schema.Parse(@"{""type"":""bytes"",""logicalType"":""big-decimal""}");


            //Assert
            Assert.IsType<BigDecimalSchema>(schema);
            Assert.Equal(@"{""type"":""bytes"",""logicalType"":""big-decimal""}", schema.ToString());
        }

        [Fact]
        public void Parse_FixedWithLogicalTypeReferencedByName_KeepsLogicalType()
        {
            //Arrange
            var json = @"{""type"":""record"",""name"":""R"",""fields"":[
                {""name"":""a"",""type"":{""type"":""fixed"",""name"":""Uid"",""size"":16,""logicalType"":""uuid""}},
                {""name"":""b"",""type"":""Uid""}]}";


            //Act
            var schema = (RecordSchema)Schema.Parse(json);


            //Assert
            Assert.IsType<UuidSchema>(schema.GetField("a").TypeSchema);
            Assert.Same(schema.GetField("a").TypeSchema, schema.GetField("b").TypeSchema);
            Assert.Equal(
                @"{""name"":""R"",""type"":""record"",""fields"":[{""name"":""a"",""type"":{""type"":""fixed"",""size"":16,""name"":""Uid"",""logicalType"":""uuid""}},{""name"":""b"",""type"":""Uid""}]}",
                schema.ToString());
        }

        [Fact]
        public void Parse_RepeatedIdenticalFixedDefinition_IsTolerated()
        {
            //Arrange: AvroConvert 3.x inlined the "duration" fixed for every TimeSpan member
            var duration = @"{""type"":""fixed"",""size"":12,""name"":""duration"",""logicalType"":""duration""}";
            var json = $@"{{""type"":""record"",""name"":""R"",""fields"":[{{""name"":""a"",""type"":{duration}}},{{""name"":""b"",""type"":{duration}}}]}}";


            //Act
            var schema = (RecordSchema)Schema.Parse(json);


            //Assert
            Assert.IsType<DurationSchema>(schema.GetField("a").TypeSchema);
            Assert.IsType<DurationSchema>(schema.GetField("b").TypeSchema);
        }

        [Fact]
        public void Parse_ConflictingFixedRedefinition_Throws()
        {
            var json = @"{""type"":""record"",""name"":""Dup"",""fields"":[
                {""name"":""a"",""type"":{""type"":""fixed"",""name"":""F"",""size"":2}},
                {""name"":""b"",""type"":{""type"":""fixed"",""name"":""F"",""size"":4}}]}";

            Assert.Throws<SerializationException>(() => Schema.Parse(json));
        }

        [Fact]
        public void Parse_FieldOrder_IsPreserved()
        {
            //Arrange
            var json = @"{""name"":""R"",""type"":""record"",""fields"":[{""name"":""a"",""type"":""int"",""order"":""descending""},{""name"":""b"",""type"":""int""}]}";


            //Act
            var schema = (RecordSchema)Schema.Parse(json);


            //Assert
            Assert.Equal("descending", schema.GetField("a").Order);
            Assert.Null(schema.GetField("b").Order);
            Assert.Equal(json, schema.ToString());
        }

        [Fact]
        public void Parse_InvalidFieldOrder_Throws()
        {
            Assert.Throws<SerializationException>(() =>
                Schema.Parse(@"{""name"":""R2"",""type"":""record"",""fields"":[{""name"":""a"",""type"":""int"",""order"":""sideways""}]}"));
        }

        [Fact]
        public void Parse_EnumDefault_IsPreserved()
        {
            //Arrange
            var json = @"{""type"":""enum"",""name"":""Color"",""symbols"":[""RED"",""GREEN""],""default"":""GREEN""}";


            //Act
            var schema = Schema.Parse(json);


            //Assert
            var enumSchema = Assert.IsType<EnumSchema>(schema);
            Assert.Equal("GREEN", enumSchema.Default);
            Assert.Empty(enumSchema.Attributes);
            Assert.Equal(json, schema.ToString());
        }

        [Fact]
        public void Parse_EnumDefaultNotASymbol_Throws()
        {
            Assert.Throws<SerializationException>(() =>
                Schema.Parse(@"{""type"":""enum"",""name"":""Color2"",""symbols"":[""RED""],""default"":""BLUE""}"));
        }

        [Fact]
        public void TimestampField_DefaultValue_IsConvertedToDateTime()
        {
            //Act
            var schema = (RecordSchema)Schema.Parse(
                @"{""type"":""record"",""name"":""D"",""fields"":[{""name"":""t"",""type"":{""type"":""long"",""logicalType"":""timestamp-millis""},""default"":1000}]}");


            //Assert
            Assert.Equal(DateTime.UnixEpoch.AddSeconds(1), schema.GetField("t").DefaultValue);
        }
    }

    public class SchemaPromotionTests
    {
        [Theory]
        [InlineData(AvroType.Int, AvroType.Long, true)]
        [InlineData(AvroType.Int, AvroType.Float, true)]
        [InlineData(AvroType.Int, AvroType.Double, true)]
        [InlineData(AvroType.Long, AvroType.Float, true)]
        [InlineData(AvroType.Long, AvroType.Double, true)]
        [InlineData(AvroType.Float, AvroType.Double, true)]
        [InlineData(AvroType.String, AvroType.Bytes, true)]
        [InlineData(AvroType.Bytes, AvroType.String, true)]
        [InlineData(AvroType.Long, AvroType.Int, false)]
        [InlineData(AvroType.Double, AvroType.Float, false)]
        [InlineData(AvroType.Int, AvroType.String, false)]
        [InlineData(AvroType.Boolean, AvroType.Int, false)]
        public void IsSpecPromotion_FollowsSpecification(AvroType writer, AvroType reader, bool expected)
        {
            Assert.Equal(expected, SchemaPromotion.IsSpecPromotion(writer, reader));
        }

        [Fact]
        public void PrimitiveCanRead_IncludesStringBytesPromotion()
        {
            Assert.True(new BytesSchema().CanRead(new StringSchema()));
            Assert.True(new StringSchema().CanRead(new BytesSchema()));
            Assert.True(new IntSchema().CanRead(new LongSchema())); // AvroConvert extension (narrowing), kept from 3.x
            Assert.False(new IntSchema().CanRead(new StringSchema()));
        }
    }
}
