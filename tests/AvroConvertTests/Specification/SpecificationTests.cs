using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using FluentAssertions;
using SolTechnology.Avro;
using SolTechnology.Avro.Infrastructure.Attributes;
using SolTechnology.Avro.Infrastructure.Exceptions;
using Xunit;

namespace AvroConvertComponentTests.Specification
{
    public class SpecificationLogicalTypeTests
    {
        public class Timestamps
        {
            [AvroType(AvroTypeRepresentation.TimestampNanoseconds)]
            public DateTime Nanos { get; set; }

            [AvroType(AvroTypeRepresentation.LocalTimestampMilliseconds)]
            public DateTime LocalMillis { get; set; }

            [AvroType(AvroTypeRepresentation.LocalTimestampMicroseconds)]
            public DateTime? LocalMicros { get; set; }

            [AvroType(AvroTypeRepresentation.LocalTimestampNanoseconds)]
            public DateTimeOffset LocalNanos { get; set; }
        }

        public class Identifiers
        {
            [AvroType(AvroTypeRepresentation.UuidFixed)]
            public Guid Id { get; set; }

            [AvroType(AvroTypeRepresentation.UuidFixed)]
            public Guid? OtherId { get; set; }
        }

        public class Amounts
        {
            [AvroType(AvroTypeRepresentation.BigDecimal)]
            public decimal Amount { get; set; }
        }

        [Fact]
        public void Timestamps_RoundTripWithUnitPrecision()
        {
            //Arrange
            var instant = new DateTime(2024, 5, 6, 7, 8, 9, DateTimeKind.Utc).AddTicks(1_234_567);
            var value = new Timestamps
            {
                Nanos = instant,
                LocalMillis = instant,
                LocalMicros = instant,
                LocalNanos = new DateTimeOffset(instant.Ticks, TimeSpan.Zero)
            };


            //Act
            var result = AvroConvert.Deserialize<Timestamps>(AvroConvert.Serialize(value));


            //Assert
            result.Nanos.Should().Be(instant);
            result.Nanos.Kind.Should().Be(DateTimeKind.Utc);
            result.LocalMillis.Should().Be(new DateTime(instant.Ticks - instant.Ticks % TimeSpan.TicksPerMillisecond));
            result.LocalMillis.Kind.Should().Be(DateTimeKind.Unspecified);
            result.LocalMicros.Should().Be(new DateTime(instant.Ticks - instant.Ticks % 10));
            result.LocalNanos.Should().Be(value.LocalNanos);
        }

        [Fact]
        public void Timestamps_GeneratedSchema_UsesRequestedLogicalTypes()
        {
            //Act
            var schema = AvroConvert.GenerateSchema(typeof(Timestamps));


            //Assert
            schema.Should().Contain(@"{""name"":""Nanos"",""type"":{""type"":""long"",""logicalType"":""timestamp-nanos""}}");
            schema.Should().Contain(@"{""type"":""long"",""logicalType"":""local-timestamp-millis""}");
            schema.Should().Contain(@"[""null"",{""type"":""long"",""logicalType"":""local-timestamp-micros""}]");
            schema.Should().Contain(@"{""type"":""long"",""logicalType"":""local-timestamp-nanos""}");
        }

        [Fact]
        public void UuidFixed_RoundTrips_AndSchemaDefinesFixedOnce()
        {
            //Arrange
            var value = new Identifiers { Id = Guid.NewGuid(), OtherId = Guid.NewGuid() };


            //Act
            var schema = AvroConvert.GenerateSchema(typeof(Identifiers));
            var result = AvroConvert.Deserialize<Identifiers>(AvroConvert.Serialize(value));


            //Assert
            result.Should().BeEquivalentTo(value);
            schema.Should().EndWith(
                @"""fields"":[" +
                @"{""name"":""Id"",""type"":{""type"":""fixed"",""size"":16,""name"":""uuid"",""logicalType"":""uuid""}}," +
                @"{""name"":""OtherId"",""type"":[""null"",""AvroConvertComponentTests.Specification.uuid""]}]}");
        }

        [Fact]
        public void UuidFixed_ReadsIntoStringAndGuid()
        {
            //Arrange
            const string schema = @"{""type"":""fixed"",""name"":""u"",""size"":16,""logicalType"":""uuid""}";
            var guid = Guid.Parse("00112233-4455-6677-8899-aabbccddeeff");
            var avro = AvroConvert.SerializeHeadless(guid, schema);


            //Act & Assert
            avro.Should().Equal(Convert.FromHexString("00112233445566778899aabbccddeeff"));
            AvroConvert.DeserializeHeadless<Guid>(avro, schema).Should().Be(guid);
            AvroConvert.Avro2Json(avro, schema).Should().Be(@"""00112233-4455-6677-8899-aabbccddeeff""");
        }

        [Theory]
        [InlineData("123.45")]
        [InlineData("-0.0001")]
        [InlineData("0")]
        [InlineData("79228162514264337593543950335")]
        [InlineData("-1234567890.123456789012345678")]
        public void BigDecimal_RoundTripsWithItsOwnScale(string text)
        {
            //Arrange
            var value = new Amounts { Amount = decimal.Parse(text, System.Globalization.CultureInfo.InvariantCulture) };


            //Act
            var result = AvroConvert.Deserialize<Amounts>(AvroConvert.Serialize(value));


            //Assert
            result.Amount.Should().Be(value.Amount);
            result.Amount.Scale.Should().Be(value.Amount.Scale);
        }

        public class RawNanos
        {
            [AvroType(AvroTypeRepresentation.TimestampNanoseconds)]
            public long At { get; set; }

            [AvroType(AvroTypeRepresentation.LocalTimestampNanoseconds)]
            public long? LocalAt { get; set; }
        }

        [Fact]
        public void TimestampAsLong_KeepsExactNanoseconds()
        {
            //Arrange
            var value = new RawNanos { At = 1_700_000_000_123_456_789L, LocalAt = -5L };


            //Act
            var avro = AvroConvert.Serialize(value);
            var result = AvroConvert.Deserialize<RawNanos>(avro);


            //Assert
            result.Should().BeEquivalentTo(value);
            AvroConvert.GenerateSchema(typeof(RawNanos)).Should().Contain(@"{""type"":""long"",""logicalType"":""timestamp-nanos""}");
        }

        [Fact]
        public void TimestampNanos_OutOfRange_Throws()
        {
            var act = () => AvroConvert.Serialize(new Timestamps { Nanos = new DateTime(2300, 1, 1) });

            act.Should().Throw<ArgumentOutOfRangeException>().Which.Message.Should().Contain("2262-04-11");
        }

        [Theory]
        [InlineData(new byte[] { 0x02, 0x01, 0x05 }, "1000")]                  // unscaled 1, scale -3 (Java 1E+3)
        [InlineData(new byte[] { 0x02, 0x01, 0x50 }, "0.0000000000000000000000000000")] // unscaled 1, scale 40: below decimal resolution
        [InlineData(new byte[] { 0x02, 0x0F, 0x3A }, "0.0000000000000000000000000002")] // unscaled 15, scale 29: rounded half away from zero
        public void BigDecimal_JavaScales_AreRead(byte[] payload, string expected)
        {
            //Arrange
            const string schema = @"{""type"":""bytes"",""logicalType"":""big-decimal""}";
            var avro = new byte[payload.Length + 1];
            avro[0] = (byte)(payload.Length * 2);
            payload.CopyTo(avro, 1);


            //Act
            var result = AvroConvert.DeserializeHeadless<decimal>(avro, schema);


            //Assert
            result.Should().Be(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture));
        }

        [Fact]
        public void BigDecimal_UsesJavaLayout()
        {
            //Arrange: unscaled 12345 (0x3039) and scale 2, each Avro-encoded, wrapped in Avro bytes
            const string schema = @"{""type"":""bytes"",""logicalType"":""big-decimal""}";


            //Act
            var avro = AvroConvert.SerializeHeadless(123.45m, schema);


            //Assert
            avro.Should().Equal(0x08, 0x04, 0x30, 0x39, 0x04);
            AvroConvert.DeserializeHeadless<decimal>(avro, schema).Should().Be(123.45m);
        }

        [Fact]
        public void DecimalOnFixed_RoundTrips()
        {
            //Arrange
            const string schema = @"{""type"":""fixed"",""name"":""Money"",""size"":8,""logicalType"":""decimal"",""precision"":12,""scale"":2}";


            //Act
            var avro = AvroConvert.SerializeHeadless(-12.5m, schema);


            //Assert
            avro.Should().HaveCount(8);
            AvroConvert.DeserializeHeadless<decimal>(avro, schema).Should().Be(-12.5m);
        }
    }

    public class NamedTypesAcrossNamespacesTests
    {
        [DataContract(Name = "Outer", Namespace = "a")]
        public class Outer
        {
            [DataMember] public TimeSpan Duration { get; set; }
            [DataMember, AvroType(AvroTypeRepresentation.UuidFixed)] public Guid Id { get; set; }
            [DataMember] public Inner Inner { get; set; }
            [DataMember] public TimeSpan OtherDuration { get; set; }
        }

        [DataContract(Name = "Inner", Namespace = "b")]
        public class Inner
        {
            [DataMember] public TimeSpan Duration { get; set; }
            [DataMember, AvroType(AvroTypeRepresentation.UuidFixed)] public Guid Id { get; set; }
        }

        [Fact]
        public void LogicalFixed_ReusedInOtherNamespace_IsDefinedThereOrReferencedByFullName()
        {
            //Arrange
            var value = new Outer
            {
                Duration = TimeSpan.FromHours(1), Id = Guid.NewGuid(), OtherDuration = TimeSpan.FromSeconds(3),
                Inner = new Inner { Duration = TimeSpan.FromMinutes(2), Id = Guid.NewGuid() }
            };


            //Act
            var schema = AvroConvert.GenerateSchema(typeof(Outer));
            var result = AvroConvert.Deserialize<Outer>(AvroConvert.Serialize(value));


            //Assert
            result.Should().BeEquivalentTo(value);
            schema.Should().Contain(@"{""name"":""OtherDuration"",""type"":""a.duration""}");
            schema.Should().Contain(@"""namespace"":""b"",""type"":""record"",""fields"":[{""name"":""Duration"",""type"":{""type"":""fixed"",""size"":12,""name"":""duration""");
            AvroSchema.Parse(schema).CanonicalForm.Should().Contain(@"{""name"":""b.duration"",""type"":""fixed"",""size"":12}");
        }

        [Fact]
        public void LogicalTypeDefault_IsWrittenBackAsDeclared()
        {
            //Arrange
            const string uuidDefault = @"\u0000\u0001\u0002\u0003\u0004\u0005\u0006\u0007\u0008\u0009\u000a\u000b\u000c\u000d\u000e\u000f";
            const string schema = @"{""type"":""record"",""name"":""D"",""fields"":[{""name"":""a"",""type"":""int""}," +
                                  @"{""name"":""t"",""type"":{""type"":""long"",""logicalType"":""timestamp-millis""},""default"":1000}," +
                                  @"{""name"":""u"",""type"":{""type"":""fixed"",""name"":""U"",""size"":16,""logicalType"":""uuid""},""default"":""" + uuidDefault + @"""}]}";


            //Act
            var avro = AvroConvert.JsonEncoding2Avro(@"{""a"":1}", schema);


            //Assert
            AvroConvert.GetSchema(avro).Should().Contain(@"""default"":1000");
            AvroConvert.Avro2JsonEncoding(avro).Should().Be(
                @"{""a"":1,""t"":1000,""u"":""\u0000\u0001\u0002\u0003\u0004\u0005\u0006\u0007\b\t\n\u000b\f\r\u000e\u000f""}");
        }
    }

    public class ReviewRegressionTests
    {
        public class Holder<T>
        {
            public T X { get; set; }
        }

        public static TheoryData<string, byte[], object> UnionBranches => new()
        {
            { @"[""null"",""string"",""bytes""]", new byte[] { 0x04, 0x04, 0xFF, 0xFE }, new byte[] { 0xFF, 0xFE } },
            { @"[""null"",""bytes"",""string""]", new byte[] { 0x04, 0x0A, 0x68, 0x65, 0x6C, 0x6C, 0x6F }, "hello" },
            { @"[""null"",""long"",""int""]", new byte[] { 0x04, 0x0A }, 5 }
        };

        [Theory]
        [MemberData(nameof(UnionBranches))]
        public void UnionIntoDynamic_KeepsTheWrittenBranchType(string schema, byte[] avro, object expected)
        {
            //Act (branch index 2 in every case)
            object result = AvroConvert.DeserializeHeadless(avro, schema, typeof(object));


            //Assert
            result.Should().BeOfType(expected.GetType());
            result.Should().BeEquivalentTo(expected);
        }

        private static T ReadMissingFieldDefault<T>(string fieldType, string defaultJson)
        {
            var writer = @"{""type"":""record"",""name"":""Holder"",""fields"":[]}";
            var reader = $@"{{""type"":""record"",""name"":""Holder"",""fields"":[{{""name"":""X"",""type"":{fieldType},""default"":{defaultJson}}}]}}";
            return AvroConvert.DeserializeHeadless<Holder<T>>(Array.Empty<byte>(), AvroSchema.Parse(writer), AvroSchema.Parse(reader)).X;
        }

        [Fact]
        public void LogicalDefault_IsReadIntoAnyMemberTypeLikeData()
        {
            const string nanos = @"{""type"":""long"",""logicalType"":""timestamp-nanos""}";
            const string millis = @"{""type"":""long"",""logicalType"":""timestamp-millis""}";

            ReadMissingFieldDefault<long>(nanos, "1700000000123456789").Should().Be(1700000000123456789L);
            ReadMissingFieldDefault<long?>(nanos, "1700000000123456789").Should().Be(1700000000123456789L);
            ReadMissingFieldDefault<DateTime>(nanos, "1700000000123456789").Should().Be(DateTime.UnixEpoch.AddTicks(17000000001234567L));
            ReadMissingFieldDefault<long>(millis, "1500").Should().Be(1500L);
            ReadMissingFieldDefault<DateTimeOffset>(millis, "1500").Should().Be(DateTimeOffset.FromUnixTimeMilliseconds(1500));
            ReadMissingFieldDefault<TimeOnly>(@"{""type"":""long"",""logicalType"":""time-micros""}", "1000").Should().Be(new TimeOnly(10_000));
            ReadMissingFieldDefault<string>(@"[""null"",""string""]", "null").Should().BeNull();
            ReadMissingFieldDefault<int>(@"[""int"",""null""]", "7").Should().Be(7);
        }

        [Fact]
        public void JsonEncoding2Avro_MissingFields_UseSchemaDefaultsExactly()
        {
            //Arrange
            const string schema = @"{""type"":""record"",""name"":""D"",""fields"":[
                {""name"":""n"",""type"":{""type"":""long"",""logicalType"":""timestamp-nanos""},""default"":1700000000123456789},
                {""name"":""d"",""type"":{""type"":""fixed"",""name"":""Dur"",""size"":12,""logicalType"":""duration""},""default"":""\u0001\u0000\u0000\u0000\u0002\u0000\u0000\u0000\u0003\u0000\u0000\u0000""},
                {""name"":""t"",""type"":{""type"":""long"",""logicalType"":""time-micros""},""default"":1000},
                {""name"":""r"",""type"":{""type"":""record"",""name"":""Inner"",""fields"":[{""name"":""a"",""type"":""int""},{""name"":""b"",""type"":""string"",""default"":""q""}]},""default"":{""a"":1}}]}";


            //Act
            var json = AvroConvert.Avro2JsonEncoding(AvroConvert.JsonEncoding2Avro("{}", schema));


            //Assert
            json.Should().Be(@"{""n"":1700000000123456789,""d"":""\u0001\u0000\u0000\u0000\u0002\u0000\u0000\u0000\u0003\u0000\u0000\u0000"",""t"":1000,""r"":{""a"":1,""b"":""q""}}");
        }

        [Fact]
        public void JsonEncoding2Avro_NestedDefaultMissingRequiredField_Throws()
        {
            const string schema = @"{""type"":""record"",""name"":""E"",""fields"":[
                {""name"":""r"",""type"":{""type"":""record"",""name"":""In"",""fields"":[{""name"":""a"",""type"":""int""},{""name"":""b"",""type"":""string""}]},""default"":{""a"":1}}]}";

            var act = () => AvroConvert.JsonEncoding2Avro("{}", schema);

            act.Should().Throw<Exception>().Which.Message.Should().Contain("'b'");
        }
    }

    public class SchemaResolutionTests
    {
        public class WithBytes
        {
            public byte[] Value { get; set; }
        }

        public class WithString
        {
            public string Value { get; set; }
        }

        [Fact]
        public void IntWrittenLongRead_DynamicTakesReaderType()
        {
            //Arrange
            var avro = AvroConvert.SerializeHeadless(5, @"""int""");


            //Act
            object result = AvroConvert.DeserializeHeadless(avro, @"""int""", @"""long""", typeof(object));


            //Assert
            result.Should().BeOfType<long>().Which.Should().Be(5L);
        }

        [Fact]
        public void StringWrittenBytesRead_IsUtf8()
        {
            //Arrange
            var avro = AvroConvert.Serialize(new WithString { Value = "zażółć" });


            //Act
            var result = AvroConvert.Deserialize<WithBytes>(avro);


            //Assert
            result.Value.Should().Equal(System.Text.Encoding.UTF8.GetBytes("zażółć"));
        }

        [Fact]
        public void BytesWrittenStringRead_IsUtf8()
        {
            //Arrange
            var avro = AvroConvert.Serialize(new WithBytes { Value = System.Text.Encoding.UTF8.GetBytes("zażółć") });


            //Act
            var result = AvroConvert.Deserialize<WithString>(avro);


            //Assert
            result.Value.Should().Be("zażółć");
        }

        [Fact]
        public void EnumSymbolUnknownToReader_ResolvesToReaderDefault()
        {
            //Arrange
            const string writer = @"{""type"":""enum"",""name"":""Color"",""symbols"":[""RED"",""GREEN"",""BLUE""]}";
            const string reader = @"{""type"":""enum"",""name"":""Color"",""symbols"":[""RED"",""GREEN""],""default"":""RED""}";
            var avro = AvroConvert.SerializeHeadless("BLUE", writer);


            //Act
            var result = AvroConvert.DeserializeHeadless<string>(avro, writer, reader, 1);


            //Assert
            result.Should().Be("RED");
        }
    }
}
