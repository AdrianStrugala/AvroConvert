using System;
using SolTechnology.Avro;
using Xunit;

namespace AvroConvertTests.GenerateSchemaTests
{
    public class LogicalTypeTests
    {
        [Theory]
        [InlineData(typeof(Guid?), @"[""null"", {""type"": ""string"", ""logicalType"": ""uuid"" } ]")]
        [InlineData(typeof(Guid), @"{""type"": ""string"", ""logicalType"": ""uuid"" } ]")]
        [InlineData(typeof(DateTime?), @"[""null"", {""type"": ""long"", ""logicalType"": ""timestamp-millis""}]")]
        [InlineData(typeof(DateTime), @"{""type"": ""long"", ""logicalType"": ""timestamp-millis""}]")]
        [InlineData(typeof(DateTime?), @"[""null"", {""type"": ""long"", ""logicalType"": ""timestamp-micros""}]")]
        [InlineData(typeof(DateTime), @"{""type"": ""long"", ""logicalType"": ""timestamp-micros""}")]
        [InlineData(typeof(TimeSpan?), @"[""null"", {""type"": ""long"", ""logicalType"": ""time-micros""}]")]
        [InlineData(typeof(TimeSpan), @"{""type"": ""long"", ""logicalType"": ""time-micros""}")]
        [InlineData(typeof(TimeSpan?), @"[""null"", {""type"": ""int"", ""logicalType"": ""time-millis""}]")]
        [InlineData(typeof(TimeSpan), @"{""type"": ""int"", ""logicalType"": ""time-millis""}")]
        [InlineData(typeof(decimal?), @"[""null"", {""type"": ""bytes"", ""logicalType"": ""decimal"", ""precision"": 4, ""scale"": 2}]")]
        [InlineData(typeof(decimal), @"{""type"": ""bytes"", ""logicalType"": ""decimal"", ""precision"": 4, ""scale"": 2}")]
        public void GenerateSchema_DotNetTypeCompatibleWithLogicType(Type type, string schema)
        {
            //Arrange


            //Act
            var result = AvroConvert.GenerateSchema(type);


            //Assert
            Assert.Equal(schema, result);
        }
    }
}
