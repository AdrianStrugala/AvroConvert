using System;
using SolTechnology.Avro;
using Xunit;

namespace AvroConvertComponentTests.GenerateSchema
{
    public class LogicalTypeTests
    {
        [Theory]
        [InlineData(typeof(Guid?), @"[""null"",{""type"":""string"",""logicalType"":""uuid""}]")]
        [InlineData(typeof(Guid), @"{""type"":""string"",""logicalType"":""uuid""}")]
        [InlineData(typeof(DateTime?), @"[""null"",{""type"":""long"",""logicalType"":""timestamp-micros""}]")]
        [InlineData(typeof(DateTime), @"{""type"":""long"",""logicalType"":""timestamp-micros""}")]
        [InlineData(typeof(TimeSpan?), @"[""null"",{""type"":""fixed"",""size"":12,""name"":""duration"",""logicalType"":""duration""}]")]
        [InlineData(typeof(TimeSpan), @"{""type"":""fixed"",""size"":12,""name"":""duration"",""logicalType"":""duration""}")]

        //Decimals are handled atm as strings
        // [InlineData(typeof(decimal?), @"[""null"",{""type"":""bytes"",""logicalType"":""decimal"",""precision"":29,""scale"":14}]")]
        // [InlineData(typeof(decimal), @"{""type"":""bytes"",""logicalType"":""decimal"",""precision"":29,""scale"":14}")]
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
