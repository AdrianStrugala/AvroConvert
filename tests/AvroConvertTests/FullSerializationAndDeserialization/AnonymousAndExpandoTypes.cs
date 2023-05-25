using System;
using System.Dynamic;
using FluentAssertions;
using SolTechnology.Avro;
using Xunit;

namespace AvroConvertComponentTests.FullSerializationAndDeserialization
{
    //from issue: https://github.com/AdrianStrugala/AvroConvert/issues/94
    public class AnonymousAndExpandoTypes
    {
        [Theory]
        [MemberData(nameof(TestEngine.CoreUsingSchema), MemberType = typeof(TestEngine))]
        public void Anonymous_type(Func<object, Type, string, string, dynamic> engine)
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
            var deserialized = (AnonymousLikeClass)engine.Invoke(item, typeof(AnonymousLikeClass), valschema, valschema);

            //Assert
            deserialized.Should().BeEquivalentTo(item);

        }

        [Theory(Skip = "Not working for Default flow for now")]
        [MemberData(nameof(TestEngine.CoreUsingSchema), MemberType = typeof(TestEngine))]
        public void Expando_object(Func<object, Type, string, string, dynamic> engine)
        {
            //Arrange
            var valSchema = @"
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
            var deserialized = (AnonymousLikeClass)engine.Invoke(item, typeof(AnonymousLikeClass), valSchema, valSchema);


            //Assert
            deserialized.Should().BeEquivalentTo(new { Value = new { KeyNested = "key1", ValueNested = "val1" } });

        }
    }

    public class AnonymousLikeClass
    {
        public NestedClass Value { get; set; }
    }

    public class NestedClass
    {
        public string KeyNested { get; set; }
        public string ValueNested { get; set; }
    }
}
