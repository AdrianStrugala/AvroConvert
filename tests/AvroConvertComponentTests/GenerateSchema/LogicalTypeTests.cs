using AwesomeAssertions;
using System;
using SolTechnology.Avro;
using NUnit.Framework;

namespace AvroConvertComponentTests.GenerateSchema
{
    public class LogicalTypeTests
    {
        [TestCase(typeof(Guid?), @"[""null"",{""type"":""string"",""logicalType"":""uuid""}]")]
        [TestCase(typeof(Guid), @"{""type"":""string"",""logicalType"":""uuid""}")]
        [TestCase(typeof(DateTime?), @"[""null"",{""type"":""long"",""logicalType"":""timestamp-micros""}]")]
        [TestCase(typeof(DateTime), @"{""type"":""long"",""logicalType"":""timestamp-micros""}")]
        [TestCase(typeof(TimeSpan?), @"[""null"",{""type"":""fixed"",""size"":12,""name"":""duration"",""logicalType"":""duration""}]")]
        [TestCase(typeof(TimeSpan), @"{""type"":""fixed"",""size"":12,""name"":""duration"",""logicalType"":""duration""}")]

        //Decimals are handled atm as strings
        // [TestCase(typeof(decimal?), @"[""null"",{""type"":""bytes"",""logicalType"":""decimal"",""precision"":29,""scale"":14}]")]
        // [TestCase(typeof(decimal), @"{""type"":""bytes"",""logicalType"":""decimal"",""precision"":29,""scale"":14}")]
        public void GenerateSchema_DotNetTypeCompatibleWithLogicType(Type type, string schema)
        {
            //Arrange


            //Act
            var result = AvroConvert.GenerateSchema(type);


            //Assert
            result.Should().Be(schema);
        }
    }
}
