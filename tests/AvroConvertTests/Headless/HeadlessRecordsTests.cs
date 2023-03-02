using System.Dynamic;
using FluentAssertions;
using SolTechnology.Avro;
using Xunit;

namespace AvroConvertComponentTests.Headless
{
    //from issue: https://github.com/AdrianStrugala/AvroConvert/issues/94
    public class HeadlessRecordsTests
    {
        [Fact]
        public void Anonymous_type_is_properly_headless_serialized()
        {
            //Arrange
            var valschema = @"
    {
      ""type"": ""record"",
      ""name"": ""TestObject"",
      ""namespace"": ""ca.dataedu"",
      ""fields"": [
        {
          ""name"": ""Value"",
          ""type"":[
            ""null"",
            {
              ""type"": ""record"",
              ""name"": ""Value"",
              ""fields"": [
                {
                  ""name"": ""KeyNested"",
                  ""type"": ""string""
                },
                {
                  ""name"": ""ValueNested"",
                  ""type"": ""string""
                }
              ]
            }]
        }
      ]
    }
";

            var item = new { Value = new { KeyNested = "key1", ValueNested = "val1" } };

            //Act
            byte[] value = AvroConvert.SerializeHeadless(item, valschema);
            var deserialized = AvroConvert.DeserializeHeadless<AnonymousLikeClass>(value, valschema);

            //Assert
            value.Should().NotBeNullOrEmpty();
            deserialized.Should().BeEquivalentTo(item);

        }

        [Fact]
        public void Expando_object_is_properly_headless_serialized()
        {
            //Arrange
            var valschema = @"
    {
      ""type"": ""record"",
      ""name"": ""TestObject"",
      ""namespace"": ""ca.dataedu"",
      ""fields"": [
        {
          ""name"": ""Value"",
          ""type"":[
            ""null"",
            {
              ""type"": ""record"",
              ""name"": ""Value"",
              ""fields"": [
                {
                  ""name"": ""KeyNested"",
                  ""type"": ""string""
                },
                {
                  ""name"": ""ValueNested"",
                  ""type"": ""string""
                }
              ]
            }]
        }
      ]
    }
";
            dynamic item = new ExpandoObject();
            item.value = new ExpandoObject();
            item.value.KeyNested = "key1";
            item.value.ValueNested = "val1";

            //Act
            byte[] value = AvroConvert.SerializeHeadless(item, valschema);
            var deserialized = AvroConvert.DeserializeHeadless<AnonymousLikeClass?>(value, valschema);

            //Assert
            value.Should().NotBeNullOrEmpty();
            deserialized.Should().BeEquivalentTo(new { Value = new { KeyNested = "key1", ValueNested = "val1" } });

        }
    }

    public class AnonymousLikeClass
    {
        public NestedClass? Value { get; set; }
    }

    public class NestedClass
    {
        public string KeyNested { get; set; }
        public string ValueNested { get; set; }
    }
}
