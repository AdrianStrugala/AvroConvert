using AwesomeAssertions;
using System;
using System.Runtime.Serialization;
using SolTechnology.Avro;
using SolTechnology.Avro.AvroObjectServices.BuildSchema;
using SolTechnology.Avro.AvroObjectServices.Read;
using SolTechnology.Avro.AvroObjectServices.Schemas;
using SolTechnology.Avro.AvroObjectServices.Schemas.Abstract;
using NUnit.Framework;

namespace AvroConvertUnitTests.BuildSchemaTests
{
    public class SpecificationSchemaTests
    {
        [TestCase("timestamp-nanos", false, 1L)]
        [TestCase("local-timestamp-millis", true, 1_000_000L)]
        [TestCase("local-timestamp-micros", true, 1_000L)]
        [TestCase("local-timestamp-nanos", true, 1L)]
        public void Parse_TimestampLogicalTypes_AreRecognised(string logicalType, bool local, long nanosecondsPerUnit)
        {
            //Act
            var schema = Schema.Parse($@"{{""type"":""long"",""logicalType"":""{logicalType}""}}");


            //Assert
            var timestamp = schema.Should().BeOfType<TimestampSchema>().Subject;
            timestamp.LogicalTypeName.Should().Be(logicalType);
            timestamp.IsLocal.Should().Be(local);
            timestamp.NanosecondsPerUnit.Should().Be(nanosecondsPerUnit);
            schema.ToString().Should().Be($@"{{""type"":""long"",""logicalType"":""{logicalType}""}}");
        }

        [Test]
        public void Parse_UuidOnFixed16_KeepsFixedBase()
        {
            //Act
            var schema = Schema.Parse(@"{""type"":""fixed"",""name"":""Id"",""namespace"":""ns"",""size"":16,""logicalType"":""uuid""}");


            //Assert
            var uuid = schema.Should().BeOfType<UuidSchema>().Subject;
            var fixedBase = uuid.BaseTypeSchema.Should().BeOfType<FixedSchema>().Subject;
            fixedBase.FullName.Should().Be("ns.Id");
            fixedBase.Size.Should().Be(16);
            schema.ToString().Should().Be(@"{""type"":""fixed"",""size"":16,""name"":""Id"",""namespace"":""ns"",""logicalType"":""uuid""}");
        }

        [Test]
        public void Parse_DecimalOnFixed_KeepsFixedBase()
        {
            //Act
            var schema = Schema.Parse(@"{""type"":""fixed"",""name"":""Money"",""size"":8,""logicalType"":""decimal"",""precision"":12,""scale"":2}");


            //Assert
            var decimalSchema = schema.Should().BeOfType<DecimalSchema>().Subject;
            decimalSchema.BaseTypeSchema.Type.Should().Be(AvroType.Fixed);
            decimalSchema.Precision.Should().Be(12);
            decimalSchema.Scale.Should().Be(2);
            schema.ToString().Should().Be(@"{""type"":""fixed"",""size"":8,""name"":""Money"",""logicalType"":""decimal"",""precision"":12,""scale"":2}");
        }

        [Test]
        public void Parse_BigDecimal_IsRecognised()
        {
            //Act
            var schema = Schema.Parse(@"{""type"":""bytes"",""logicalType"":""big-decimal""}");


            //Assert
            schema.Should().BeOfType<BigDecimalSchema>();
            schema.ToString().Should().Be(@"{""type"":""bytes"",""logicalType"":""big-decimal""}");
        }

        [Test]
        public void Parse_FixedWithLogicalTypeReferencedByName_KeepsLogicalType()
        {
            //Arrange
            var json = @"{""type"":""record"",""name"":""R"",""fields"":[
                {""name"":""a"",""type"":{""type"":""fixed"",""name"":""Uid"",""size"":16,""logicalType"":""uuid""}},
                {""name"":""b"",""type"":""Uid""}]}";


            //Act
            var schema = (RecordSchema)Schema.Parse(json);


            //Assert
            schema.GetField("a").TypeSchema.Should().BeOfType<UuidSchema>();
            schema.GetField("b").TypeSchema.Should().BeSameAs(schema.GetField("a").TypeSchema);
            schema.ToString().Should().Be(@"{""name"":""R"",""type"":""record"",""fields"":[{""name"":""a"",""type"":{""type"":""fixed"",""size"":16,""name"":""Uid"",""logicalType"":""uuid""}},{""name"":""b"",""type"":""Uid""}]}");
        }

        [Test]
        public void Parse_RepeatedIdenticalFixedDefinition_IsTolerated()
        {
            //Arrange: AvroConvert 3.x inlined the "duration" fixed for every TimeSpan member
            var duration = @"{""type"":""fixed"",""size"":12,""name"":""duration"",""logicalType"":""duration""}";
            var json = $@"{{""type"":""record"",""name"":""R"",""fields"":[{{""name"":""a"",""type"":{duration}}},{{""name"":""b"",""type"":{duration}}}]}}";


            //Act
            var schema = (RecordSchema)Schema.Parse(json);


            //Assert
            schema.GetField("a").TypeSchema.Should().BeOfType<DurationSchema>();
            schema.GetField("b").TypeSchema.Should().BeOfType<DurationSchema>();
        }

        [Test]
        public void Parse_ConflictingFixedRedefinition_Throws()
        {
            var json = @"{""type"":""record"",""name"":""Dup"",""fields"":[
                {""name"":""a"",""type"":{""type"":""fixed"",""name"":""F"",""size"":2}},
                {""name"":""b"",""type"":{""type"":""fixed"",""name"":""F"",""size"":4}}]}";

            FluentActions.Invoking(() => Schema.Parse(json)).Should().ThrowExactly<SerializationException>();
        }

        [Test]
        public void Parse_FieldOrder_IsPreserved()
        {
            //Arrange
            var json = @"{""name"":""R"",""type"":""record"",""fields"":[{""name"":""a"",""type"":""int"",""order"":""descending""},{""name"":""b"",""type"":""int""}]}";


            //Act
            var schema = (RecordSchema)Schema.Parse(json);


            //Assert
            schema.GetField("a").Order.Should().Be("descending");
            schema.GetField("b").Order.Should().BeNull();
            schema.ToString().Should().Be(json);
        }

        [Test]
        public void Parse_InvalidFieldOrder_Throws()
        {
            FluentActions.Invoking(() =>
                Schema.Parse(@"{""name"":""R2"",""type"":""record"",""fields"":[{""name"":""a"",""type"":""int"",""order"":""sideways""}]}")).Should().ThrowExactly<SerializationException>();
        }

        [Test]
        public void Parse_EnumDefault_IsPreserved()
        {
            //Arrange
            var json = @"{""type"":""enum"",""name"":""Color"",""symbols"":[""RED"",""GREEN""],""default"":""GREEN""}";


            //Act
            var schema = Schema.Parse(json);


            //Assert
            var enumSchema = schema.Should().BeOfType<EnumSchema>().Subject;
            enumSchema.Default.Should().Be("GREEN");
            enumSchema.Attributes.Should().BeEmpty();
            schema.ToString().Should().Be(json);
        }

        [Test]
        public void Parse_EnumDefaultNotASymbol_Throws()
        {
            FluentActions.Invoking(() =>
                Schema.Parse(@"{""type"":""enum"",""name"":""Color2"",""symbols"":[""RED""],""default"":""BLUE""}")).Should().ThrowExactly<SerializationException>();
        }

        [Test]
        public void TimestampField_DefaultValue_IsConvertedToDateTime()
        {
            //Act
            var schema = (RecordSchema)Schema.Parse(
                @"{""type"":""record"",""name"":""D"",""fields"":[{""name"":""t"",""type"":{""type"":""long"",""logicalType"":""timestamp-millis""},""default"":1000}]}");


            //Assert
            schema.GetField("t").DefaultValue.Should().Be(DateTime.UnixEpoch.AddSeconds(1));
        }
    }

    public class SchemaPromotionTests
    {
        [TestCase(AvroType.Int, AvroType.Long, true)]
        [TestCase(AvroType.Int, AvroType.Float, true)]
        [TestCase(AvroType.Int, AvroType.Double, true)]
        [TestCase(AvroType.Long, AvroType.Float, true)]
        [TestCase(AvroType.Long, AvroType.Double, true)]
        [TestCase(AvroType.Float, AvroType.Double, true)]
        [TestCase(AvroType.String, AvroType.Bytes, true)]
        [TestCase(AvroType.Bytes, AvroType.String, true)]
        [TestCase(AvroType.Long, AvroType.Int, false)]
        [TestCase(AvroType.Double, AvroType.Float, false)]
        [TestCase(AvroType.Int, AvroType.String, false)]
        [TestCase(AvroType.Boolean, AvroType.Int, false)]
        public void IsSpecPromotion_FollowsSpecification(AvroType writer, AvroType reader, bool expected)
        {
            SchemaPromotion.IsSpecPromotion(writer, reader).Should().Be(expected);
        }

        [Test]
        public void PrimitiveCanRead_IncludesStringBytesPromotion()
        {
            new BytesSchema().CanRead(new StringSchema()).Should().BeTrue();
            new StringSchema().CanRead(new BytesSchema()).Should().BeTrue();
            new IntSchema().CanRead(new LongSchema()).Should().BeTrue(); // AvroConvert extension (narrowing), kept from 3.x
            new IntSchema().CanRead(new StringSchema()).Should().BeFalse();
        }
    }
}
