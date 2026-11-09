using AwesomeAssertions;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using SolTechnology.Avro;
using NUnit.Framework;

namespace AvroConvertComponentTests.GenerateModel
{
    public class GenerateModelTests
    {
        private readonly byte[] _avroBytes;
        private readonly byte[] _schemaOnlyAvroBytes;

        public GenerateModelTests()
        {
            _avroBytes = File.ReadAllBytes("example2.avro");
            _schemaOnlyAvroBytes = File.ReadAllBytes("header_only.avro");
        }

        [Test]
        public void GenerateClass_UserClass_OutputIsEqualToExpected()
        {
            //Arrange


            //Act
            string resultClass = AvroConvert.GenerateModel(_avroBytes);


            //Assert
            resultClass.Should().Be("#nullable enable annotations\r\n\r\n" +
                "using System.Runtime.Serialization;\r\n" +
                "\r\n" +
                "[DataContract(Namespace = \"example.avro\")]\r\n" +
                "public class User\r\n" +
                "{\r\n" +
                "\t[DataMember]\r\n" +
                "\tpublic string name { get; set; }\r\n" +
                "\t[DataMember]\r\n" +
                "\tpublic int? favorite_number { get; set; }\r\n" +
                "\t[DataMember]\r\n" +
                "\tpublic string? favorite_color { get; set; }\r\n" +
                "}\r\n" +
                "\r\n");
        }

        [Test]
        public void GenerateClass_SchemaOnlyAvroFile_OutputIsEqualToExpected()
        {
            //Arrange


            //Act
            string resultClass = AvroConvert.GenerateModel(_schemaOnlyAvroBytes);


            //Assert
            resultClass.Should().Be("using System.Runtime.Serialization;\r\n" +
                "\r\n" +
                "[DataContract(Namespace = \"AvroConvertComponentTests\")]\r\n" +
                "public class User\r\n" +
                "{\r\n" +
                "\t[DataMember]\r\n" +
                "\tpublic string name { get; set; }\r\n" +
                "\t[DataMember]\r\n" +
                "\tpublic int? favorite_number { get; set; }\r\n" +
                "\t[DataMember]\r\n" +
                "\tpublic string favorite_color { get; set; }\r\n" +
                "}\r\n" +
                "\r\n");
        }


        [Test]
        public void GenerateClass_NestedClass_OutputIsEqualToExpected()
        {
            //Arrange
            var schema = AvroConvert.GenerateSchema(typeof(BaseTestClass));

            //Act
            string resultSchema = AvroConvert.GenerateModel(schema);


            //Assert
            resultSchema.Should().Be("#nullable enable annotations\r\n\r\n" +
                "using System.Runtime.Serialization;\r\n" +
                "\r\n" +
                "[DataContract(Namespace = \"AvroConvertComponentTests\")]\r\n" +
                "public class BaseTestClass\r\n" +
                "{\r\n" +
                "\t[DataMember]\r\n" +
                "\tpublic string justSomeProperty { get; set; }\r\n" +
                "\t[DataMember]\r\n" +
                "\tpublic long andLongProperty { get; set; }\r\n" +
                "\t[DataMember]\r\n" +
                "\tpublic User objectProperty { get; set; }\r\n" +
                "}\r\n" +
                "\r\n" +
                "[DataContract(Namespace = \"AvroConvertComponentTests\")]\r\n" +
                "public class User\r\n" +
                "{\r\n" +
                "\t[DataMember]\r\n" +
                "\tpublic string? name { get; set; }\r\n" +
                "\t[DataMember]\r\n" +
                "\tpublic int? favorite_number { get; set; }\r\n" +
                "\t[DataMember]\r\n" +
                "\tpublic string favorite_color { get; set; }\r\n" +
                "}\r\n" +
                "\r\n");
        }

        [Test]
        public void GenerateClass_ClassWithEnum_GeneratedModelIsAsExpected()
        {
            //Arrange
            var schema = AvroConvert.GenerateSchema(typeof(ClassWithEnum));

            //Act
            string resultSchema = AvroConvert.GenerateModel(schema);


            //Assert
            resultSchema.Should().Be("using System.ComponentModel;\r\n" +
                "using System.Runtime.Serialization;\r\n" +
                "\r\n" +
                "[DataContract(Namespace = \"AvroConvertComponentTests\")]\r\n" +
                "public class ClassWithEnum\r\n" +
                "{\r\n" +
                "\t[DefaultValue(\"be\")]\r\n" +
                "\t[DataMember]\r\n" +
                "\tpublic TestEnum? EnumProp { get; set; }\r\n" +
                "\t[DefaultValue(\"ca\")]\r\n" +
                "\t[DataMember]\r\n" +
                "\tpublic TestEnum? SecondEnumProp { get; set; }\r\n" +
                "}\r\n" +
                "\r\n" +
                "[DataContract(Namespace = \"AvroConvertComponentTests\")]\r\n" +
                "public enum TestEnum\r\n" +
                "{\r\n" +
                "\ta,\r\n" +
                "\tbe,\r\n" +
                "\tca,\r\n" +
                "\tdlo\r\n" +
                "}\r\n" +
                "\r\n");
        }

        [Test]
        public void GenerateClass_VeryComplexClass_OutputIsEqualToExpected()
        {
            //Arrange
            var schema = AvroConvert.GenerateSchema(typeof(VeryComplexClass));

            //Act
            string resultSchema = AvroConvert.GenerateModel(schema);


            //Assert
            resultSchema.Should().Be("#nullable enable annotations\r\n\r\n" +
                "using System;\r\n" +
                "using System.Runtime.Serialization;\r\n" +
                "\r\n" +
                "[DataContract(Namespace = \"AvroConvertComponentTests\")]\r\n" +
                "public class VeryComplexClass\r\n" +
                "{\r\n" +
                "\t[DataMember]\r\n" +
                "\tpublic ClassWithArray[] ClassesWithArray { get; set; }\r\n" +
                "\t[DataMember]\r\n" +
                "\tpublic ClassWithGuid[] ClassesWithGuid { get; set; }\r\n" +
                "\t[DataMember]\r\n" +
                "\tpublic ClassWithConstructorPopulatingProperty anotherClass { get; set; }\r\n" +
                "\t[DataMember]\r\n" +
                "\tpublic User simpleClass { get; set; }\r\n" +
                "\t[DataMember]\r\n" +
                "\tpublic int simpleObject { get; set; }\r\n" +
                "\t[DataMember]\r\n" +
                "\tpublic bool[] bools { get; set; }\r\n" +
                "\t[DataMember]\r\n" +
                "\tpublic double doubleProperty { get; set; }\r\n" +
                "\t[DataMember]\r\n" +
                "\tpublic float floatProperty { get; set; }\r\n" +
                "\t[DataMember]\r\n" +
                "\tpublic int? Size { get; set; }\r\n" +
                "}\r\n" +
                "\r\n" +
                "[DataContract(Namespace = \"AvroConvertComponentTests\")]\r\n" +
                "public class ClassWithArray\r\n" +
                "{\r\n" +
                "\t[DataMember]\r\n" +
                "\tpublic int[] theArray { get; set; }\r\n" +
                "}\r\n" +
                "\r\n" +
                "[DataContract(Namespace = \"AvroConvertComponentTests\")]\r\n" +
                "public class ClassWithGuid\r\n" +
                "{\r\n" +
                "\t[DataMember]\r\n" +
                "\tpublic Guid theGuid { get; set; }\r\n" +
                "}\r\n" +
                "\r\n" +
                "[DataContract(Namespace = \"AvroConvertComponentTests\")]\r\n" +
                "public class ClassWithConstructorPopulatingProperty\r\n" +
                "{\r\n" +
                "\t[DataMember]\r\n" +
                "\tpublic BaseTestClass[] nestedList { get; set; }\r\n" +
                "\t[DataMember]\r\n" +
                "\tpublic ClassWithSimpleList[] anotherList { get; set; }\r\n" +
                "\t[DataMember]\r\n" +
                "\tpublic string stringProperty { get; set; }\r\n" +
                "}\r\n" +
                "\r\n" +
                "[DataContract(Namespace = \"AvroConvertComponentTests\")]\r\n" +
                "public class BaseTestClass\r\n" +
                "{\r\n" +
                "\t[DataMember]\r\n" +
                "\tpublic string justSomeProperty { get; set; }\r\n" +
                "\t[DataMember]\r\n" +
                "\tpublic long andLongProperty { get; set; }\r\n" +
                "\t[DataMember]\r\n" +
                "\tpublic User objectProperty { get; set; }\r\n" +
                "}\r\n" +
                "\r\n" +
                "[DataContract(Namespace = \"AvroConvertComponentTests\")]\r\n" +
                "public class User\r\n" +
                "{\r\n" +
                "\t[DataMember]\r\n" +
                "\tpublic string? name { get; set; }\r\n" +
                "\t[DataMember]\r\n" +
                "\tpublic int? favorite_number { get; set; }\r\n" +
                "\t[DataMember]\r\n" +
                "\tpublic string favorite_color { get; set; }\r\n" +
                "}\r\n" +
                "\r\n" +
                "[DataContract(Namespace = \"AvroConvertComponentTests\")]\r\n" +
                "public class ClassWithSimpleList\r\n" +
                "{\r\n" +
                "\t[DataMember]\r\n" +
                "\tpublic int[] someList { get; set; }\r\n" +
                "}\r\n" +
                "\r\n");
        }

        [Test]
        public void GenerateClass_ClassWithLogicalTypes_OutputIsEqualToExpected()
        {
            //Arrange
            var schema = AvroConvert.GenerateSchema(typeof(LogicalTypesClass));

            //Act
            string resultSchema = AvroConvert.GenerateModel(schema);


            //Assert
            resultSchema.Should().Be("using System;\r\n" +
                "using System.Runtime.Serialization;\r\n" +
                "\r\n" +
                "[DataContract(Namespace = \"AvroConvertComponentTests\")]\r\n" +
                "public class LogicalTypesClass\r\n" +
                "{\r\n" +
                "\t[DataMember]\r\n" +
                "\tpublic decimal One { get; set; }\r\n" +
                "\t[DataMember]\r\n" +
                "\tpublic Guid? Two { get; set; }\r\n" +
                "\t[DataMember]\r\n" +
                "\tpublic TimeSpan Three { get; set; }\r\n" +
                "\t[DataMember]\r\n" +
                "\tpublic DateTime? Four { get; set; }\r\n" +
                "\t[DataMember]\r\n" +
                "\tpublic DateTime Five { get; set; }\r\n" +
                "}\r\n" +
                "\r\n");
        }

        [Test]
        public void GenerateClass_Enum_GeneratedModelIsAsExpected()
        {
            //Arrange
            var schema = AvroConvert.GenerateSchema(typeof(TestEnum));

            //Act
            string resultSchema = AvroConvert.GenerateModel(schema);


            //Assert
            resultSchema.Should().Be("using System.Runtime.Serialization;\r\n" +
                "\r\n" +
                "[DataContract(Namespace = \"AvroConvertComponentTests\")]\r\n" +
                "public enum TestEnum\r\n" +
                "{\r\n" +
                "\ta,\r\n" +
                "\tbe,\r\n" +
                "\tca,\r\n" +
                "\tdlo\r\n" +
                "}\r\n" +
                "\r\n");
        }

        [Test]
        public void GenerateClass_TwoClassesOfTheSameName_TheyAreGeneratedWithNamespaces()
        {
            //Arrange
            string schema = @"
                        {
                           ""type"":""record"",
                           ""name"":""AvroConvertComponentTests.BaseTestClass"",
                           ""fields"":[
                              {
                                 ""name"":""fakeUserProperty"",
                                 ""type"":{
                                    ""type"":""record"",
                                    ""name"":""FakeUser.User"",
                                    ""fields"":[
                                       {
                                          ""name"":""name"",
                                          ""type"":""string""
                                       }
                                    ]
                                 }
                              },
                              {
                                 ""name"":""objectProperty"",
                                 ""type"":{
                                    ""type"":""record"",
                                    ""name"":""AvroConvertComponentTests.User"",
                                    ""fields"":[
                                       {
                                          ""name"":""name"",
                                          ""type"":""string""
                                       },
                                       {
                                          ""name"":""favorite_number"",
                                          ""type"":[
                                             ""null"",
                                             ""int""
                                          ]
                                       },
                                       {
                                          ""name"":""favorite_color"",
                                          ""type"":""string""
                                       }
                                    ]
                                 }
                              }
                           ]
                        }";

            //Act
            string resultClass = AvroConvert.GenerateModel(schema);


            //Assert
            resultClass.Should().Be("using System.Runtime.Serialization;\r\n" +
                "\r\n" +
                "[DataContract(Namespace = \"AvroConvertComponentTests\")]\r\n" +
                "public class BaseTestClass\r\n" +
                "{\r\n" +
                "\t[DataMember]\r\n" +
                "\tpublic FakeUserUser fakeUserProperty { get; set; }\r\n" +
                "\t[DataMember]\r\n" +
                "\tpublic AvroConvertComponentTestsUser objectProperty { get; set; }\r\n" +
                "}\r\n" +
                "\r\n" +
                "[DataContract(Name = \"User\", Namespace = \"FakeUser\")]\r\n" +
                "public class FakeUserUser\r\n" +
                "{\r\n" +
                "\t[DataMember]\r\n" +
                "\tpublic string name { get; set; }\r\n" +
                "}\r\n" +
                "\r\n" +
                "[DataContract(Name = \"User\", Namespace = \"AvroConvertComponentTests\")]\r\n" +
                "public class AvroConvertComponentTestsUser\r\n" +
                "{\r\n" +
                "\t[DataMember]\r\n" +
                "\tpublic string name { get; set; }\r\n" +
                "\t[DataMember]\r\n" +
                "\tpublic int? favorite_number { get; set; }\r\n" +
                "\t[DataMember]\r\n" +
                "\tpublic string favorite_color { get; set; }\r\n" +
                "}\r\n" +
                "\r\n");
        }

        [Test]
        public void GenerateClass_SameNameClassesOneFieldIsAnArray_TheyAreGeneratedWithNamespaces()
        {
            //Arrange
            string schema = @"
                        {
  ""type"": ""record"",
  ""name"": ""Result"",
  ""fields"": [
    {
      ""name"": ""module"",
      ""type"": {
        ""type"": ""record"",
        ""name"": ""module"",
        ""fields"": [
          {
            ""name"": ""moduleIdName"",
            ""type"": [
              ""null"",
              ""string""
            ],
            ""default"": null
          },
          {
            ""name"": ""data"",
            ""type"": {
              ""type"": ""array"",
              ""items"": {
                ""type"": ""record"",
                ""name"": ""module"",
                ""namespace"": ""data"",
                ""fields"": [
                  {
                    ""name"": ""isControl"",
                    ""type"": [
                      ""null"",
                      ""boolean""
                    ],
                    ""default"": null
                  }
                ]
              }
            }
          },
          {
            ""name"": ""instrumentId"",
            ""type"": [
              ""null"",
              ""int""
            ],
            ""default"": null
          }
        ]
      }
    },
    {
      ""name"": ""customerInfo"",
      ""type"": {
        ""type"": ""record"",
        ""name"": ""customerInfo"",
        ""fields"": [
          {
            ""name"": ""country"",
            ""type"": ""string"",
            ""doc"": """"
          },
          {
            ""name"": ""customerNumber"",
            ""type"": ""string"",
            ""doc"": """"
          }
        ]
      },
      ""doc"": """"
    },
    {
      ""name"": ""deviceId"",
      ""type"": ""string""
    },
    {
      ""name"": ""url"",
      ""type"": ""string"",
      ""doc"": """"
    }
  ]
}";

            //Act
            string resultClass = AvroConvert.GenerateModel(schema);


            //Assert
            resultClass.Should().Be("#nullable enable annotations\r\n\r\n" +
                "using System.Runtime.Serialization;\r\n" +
                "\r\n" +
                "public class Result\r\n" +
                "{\r\n" +
                "\tpublic module module { get; set; }\r\n" +
                "\tpublic customerInfo customerInfo { get; set; }\r\n" +
                "\tpublic string deviceId { get; set; }\r\n" +
                "\tpublic string url { get; set; }\r\n" +
                "}\r\n" +
                "\r\n" +
                "public class module\r\n" +
                "{\r\n" +
                "\tpublic string? moduleIdName { get; set; }\r\n" +
                "\tpublic datamodule[] data { get; set; }\r\n" +
                "\tpublic int? instrumentId { get; set; }\r\n" +
                "}\r\n" +
                "\r\n" +
                "[DataContract(Name = \"module\", Namespace = \"data\")]\r\n" +
                "public class datamodule\r\n" +
                "{\r\n" +
                "\t[DataMember]\r\n" +
                "\tpublic bool? isControl { get; set; }\r\n" +
                "}\r\n" +
                "\r\n" +
                "public class customerInfo\r\n" +
                "{\r\n" +
                "\tpublic string country { get; set; }\r\n" +
                "\tpublic string customerNumber { get; set; }\r\n" +
                "}\r\n" +
                "\r\n");
        }

        [Test]
        public void GenerateClass_TheyAreGeneratedWithDocumentation()
        {
            //Arrange
            string schema = @"
                                {
          ""type"": ""record"",
          ""name"": ""Result"",
          ""fields"": [
            {
              ""name"": ""testString"",
              ""type"": ""string"",
              ""doc"": ""This is a doc field""
            },
            {
              ""name"": ""testBoolean"",
              ""type"": ""boolean""
            },
            {
              ""name"": ""testInt"",
              ""type"": ""int""
            },
            {
              ""name"": ""testLong"",
              ""type"": ""long""
            },
            {
              ""name"": ""testFloat"",
              ""type"": ""float""
            },
            {
              ""name"": ""testDouble"",
              ""type"": ""double""
            }
          ]
        }";


            //Act
            string resultClass = AvroConvert.GenerateModel(schema);

            //Assert
            resultClass.Should().Be("public class Result\r\n" +
                "{\r\n" +
                "\t/// <summary>\r\n" +
                "\t/// This is a doc field\r\n" +
                "\t/// </summary>\r\n" +
                "\tpublic string testString { get; set; }\r\n" +
                "\tpublic bool testBoolean { get; set; }\r\n" +
                "\tpublic int testInt { get; set; }\r\n" +
                "\tpublic long testLong { get; set; }\r\n" +
                "\tpublic float testFloat { get; set; }\r\n" +
                "\tpublic double testDouble { get; set; }\r\n" +
                "}\r\n" +
                "\r\n");
        }


        [Test]
        public void GenerateClass_TheyAreGeneratedWithDefaults()
        {
            //Arrange
            string schema = @"
                                {
          ""type"": ""record"",
          ""name"": ""Result"",
          ""fields"": [
            {
              ""name"": ""testString"",
              ""type"": ""string"",
              ""default"": ""Default String""
            },
            {
              ""name"": ""testBoolean"",
              ""type"": ""boolean"",
              ""default"": ""TRUE""
            },
            {
              ""name"": ""testInt"",
              ""type"": ""int"",
              ""default"": 123
            },
            {
              ""name"": ""testLong"",
              ""type"": ""long"",
              ""default"": 123
            },
            {
              ""name"": ""testFloat"",
              ""type"": ""float"",
              ""default"": 1.23
            },
            {
              ""name"": ""testDouble"",
              ""type"": ""double"",
              ""default"": 1.23
            }
          ]
        }";

            //Act
            string resultClass = AvroConvert.GenerateModel(schema);

            //Assert
            resultClass.Should().Be("using System.ComponentModel;\r\n" +
                "\r\n" +
                "public class Result\r\n" +
                "{\r\n" +
                "\t[DefaultValue(\"Default String\")]\r\n" +
                "\tpublic string testString { get; set; }\r\n" +
                "\t[DefaultValue(true)]\r\n" +
                "\tpublic bool testBoolean { get; set; }\r\n" +
                "\t[DefaultValue(123)]\r\n" +
                "\tpublic int testInt { get; set; }\r\n" +
                "\t[DefaultValue(123)]\r\n" +
                "\tpublic long testLong { get; set; }\r\n" +
                "\t[DefaultValue(1.23)]\r\n" +
                "\tpublic float testFloat { get; set; }\r\n" +
                "\t[DefaultValue(1.23)]\r\n" +
                "\tpublic double testDouble { get; set; }\r\n" +
                "}\r\n" +
                "\r\n");
        }


        [Test]
        public void GenerateClass_CommaNumberDecimalSeparator()
        {
            //Arrange
            string schema = @"
                                {
          ""type"": ""record"",
          ""name"": ""Result"",
          ""fields"": [
            {
              ""name"": ""testFloat"",
              ""type"": ""float"",
              ""default"": 1.23
            },
            {
              ""name"": ""testDouble"",
              ""type"": ""double"",
              ""default"": 1.23
            }
          ]
        }";

            string resultClass = null;
            var tempCulture = Thread.CurrentThread.CurrentCulture;

            //Act

            try
            {
                Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("de");
                resultClass = AvroConvert.GenerateModel(schema);
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = tempCulture;
            }

            //Assert
            resultClass.Should().Be("using System.ComponentModel;\r\n" +
                "\r\n" +
                "public class Result\r\n" +
                "{\r\n" +
                "\t[DefaultValue(1.23)]\r\n" +
                "\tpublic float testFloat { get; set; }\r\n" +
                "\t[DefaultValue(1.23)]\r\n" +
                "\tpublic double testDouble { get; set; }\r\n" +
                "}\r\n" +
                "\r\n");
        }

        [Test]
        public void GenerateClass_SchemaContainsMap_ItIsTranslatedToDictionary()
        {
            //Arrange
            string schema = @"
         {
    ""type"": ""record"",
    ""name"": ""exampleAvro"",
    ""fields"": [
        {
            ""name"": ""mapdata"",
            ""type"": ""map"",
            ""values"": ""int""
        }
    ]
}";

            //Act
            string resultClass = AvroConvert.GenerateModel(schema);

            //Assert
            resultClass.Should().Be("using System.Collections.Generic;\r\n" +
                "\r\n" +
                "public class exampleAvro\r\n" +
                "{\r\n" +
                "\tpublic Dictionary<string,int> mapdata { get; set; }\r\n" +
                "}\r\n" +
                "\r\n");
        }

        [Test]
        public void GenerateClass_SchemaContainsMapOfUnionValues_ItIsTranslatedToDictionaryOfObjects()
        {
            //Arrange
            string schema = @"
         {
    ""type"": ""record"",
    ""name"": ""EventData"",
    ""fields"": [
        {
            ""name"":""SystemProperties"",
            ""type"":
                {
                    ""type"":""map"",
                    ""values"":[""long"",""double"",""string"",""bytes""]
                }
        },
        {
            ""name"":""Properties"",
            ""type"":
                {
                    ""type"":""map"",
                    ""values"":[""null"",""double""]
                }
        }
    ]
}";

            //Act
            string resultClass = AvroConvert.GenerateModel(schema);

            //Assert
            resultClass.Should().Be("using System.Collections.Generic;\r\n" +
                "\r\n" +
                "public class EventData\r\n" +
                "{\r\n" +
                "\tpublic Dictionary<string,object> SystemProperties { get; set; }\r\n" +
                "\tpublic Dictionary<string,double?> Properties { get; set; }\r\n" +
                "}\r\n" +
                "\r\n");
        }

        [Test]
        public void GenerateClass_SchemaContainsFixed_ItIsTranslatedToByteArray()
        {
            //Arrange
            string schema = @"
         {
    ""type"": ""record"",
    ""name"": ""exampleAvro"",
    ""fields"": [
        {
            ""name"": ""bdata"",
            ""type"": ""fixed"",
            ""size"": 1048576
        }
    ]
}";

            //Act
            string resultClass = AvroConvert.GenerateModel(schema);

            //Assert
            resultClass.Should().Be("public class exampleAvro\r\n" +
                "{\r\n" +
                "\tpublic byte[] bdata { get; set; }\r\n" +
                "}\r\n" +
                "\r\n");
        }

        [Test]
        public void GenerateClass_SchemaContainsBytes_ItIsTranslatedToByteArray()
        {
            //Arrange
            string schema = @"
         {
    ""type"": ""record"",
    ""name"": ""exampleAvro"",
    ""fields"": [
        {
            ""name"": ""bdata"",
            ""type"": ""bytes""
        }
    ]
}";

            //Act
            string resultClass = AvroConvert.GenerateModel(schema);

            //Assert
            resultClass.Should().Be("public class exampleAvro\r\n" +
                "{\r\n" +
                "\tpublic byte[] bdata { get; set; }\r\n" +
                "}\r\n" +
                "\r\n");
        }


        [Test]
        public void GenerateClass_TypeIsArrayOfDifferentTypes_ItIsGeneralizedToObject()
        {
            //Arrange
            string schema = @"
                       {
   ""type"":""record"",
   ""name"":""ClassWithMultipleTypesProperties"",
   ""fields"":[
      {
         ""name"":""Name"",
         ""type"":[
            ""null"",
            ""string"",
            {
               ""type"":""record"",
               ""name"":""Switchable_PersonName"",
               ""fields"":[
                  {
                     ""name"":""Salutation"",
                     ""type"":[
                        ""null"",
                        ""string""
                     ],
                     ""default"":null
                  },
                  {
                     ""name"":""FirstName"",
                     ""type"":[
                        ""null"",
                        ""string""
                     ],
                     ""default"":null
                  },
                  {
                     ""name"":""LastName"",
                     ""type"":[
                        ""null"",
                        ""string""
                     ],
                     ""default"":null
                  },
                  {
                     ""name"":""MiddleName"",
                     ""type"":[
                        ""null"",
                        ""string""
                     ],
                     ""default"":null
                  },
                  {
                     ""name"":""InformalName"",
                     ""type"":[
                        ""null"",
                        ""string""
                     ],
                     ""default"":null
                  },
                  {
                     ""name"":""Suffix"",
                     ""type"":[
                        ""null"",
                        ""string""
                     ],
                     ""default"":null
                  }
               ]
            }
         ],
         ""doc"":""Data:Switchable_PersonName"",
         ""default"":null
      },
   {
         ""name"":""AgeOrSex"",
         ""type"":[
            ""int"",
            ""string"",
         ],
      }
   ]
}";



            //Act
            string resultClass = AvroConvert.GenerateModel(schema);


            //Assert
            resultClass.Should().Be("#nullable enable annotations\r\n\r\n" +
                "using SolTechnology.Avro.Infrastructure.Attributes;\r\n" +
                "\r\n" +
                "public class ClassWithMultipleTypesProperties\r\n" +
                "{\r\n" +
                "\t/// <summary>\r\n" +
                "\t/// Data:Switchable_PersonName\r\n" +
                "\t/// </summary>\r\n" +
                "\t[AvroUnion(typeof(string), typeof(Switchable_PersonName))]\r\n" +
                "\tpublic object? Name { get; set; }\r\n" +
                "\t[AvroUnion(typeof(int), typeof(string))]\r\n" +
                "\tpublic object AgeOrSex { get; set; }\r\n" +
                "}\r\n" +
                "\r\n" +
                "public class Switchable_PersonName\r\n" +
                "{\r\n" +
                "\tpublic string? Salutation { get; set; }\r\n" +
                "\tpublic string? FirstName { get; set; }\r\n" +
                "\tpublic string? LastName { get; set; }\r\n" +
                "\tpublic string? MiddleName { get; set; }\r\n" +
                "\tpublic string? InformalName { get; set; }\r\n" +
                "\tpublic string? Suffix { get; set; }\r\n" +
                "}\r\n" +
                "\r\n");
        }

        [Test]
        public void GenerateClass_UnionOfRecords_PropertyListsEveryBranchInAvroUnion()
        {
            //Arrange (issue #174): an inline record, a record referenced by name, an enum and a primitive
            const string schema = @"{""type"":""record"",""name"":""Envelope"",""namespace"":""shop"",""fields"":[
                {""name"":""First"",""type"":{""type"":""record"",""name"":""Paid"",""fields"":[{""name"":""Amount"",""type"":""int""}]}},
                {""name"":""Kind"",""type"":{""type"":""enum"",""name"":""Kind"",""symbols"":[""A""]}},
                {""name"":""Event"",""type"":[""null"",""Paid"",{""type"":""record"",""name"":""Cancelled"",""fields"":[{""name"":""Reason"",""type"":""string""}]},""Kind"",""string""]}]}";


            //Act
            var model = AvroConvert.GenerateModel(schema);


            //Assert
            model.Should().Contain("\t[AvroUnion(typeof(Paid), typeof(Cancelled), typeof(Kind), typeof(string))]\r\n\t[DataMember]\r\n\tpublic object? Event { get; set; }\r\n", "every branch, so the class gives back the same union");
            model.Should().Contain("public class Cancelled");
        }

        private const string ShopOrderSchema = @"{""type"":""record"",""name"":""Order"",""namespace"":""com.acme.shop"",""fields"":[
            {""name"":""createdAt"",""type"":{""type"":""long"",""logicalType"":""timestamp-millis""}},
            {""name"":""shippedOn"",""type"":[""null"",{""type"":""int"",""logicalType"":""date""}],""default"":null},
            {""name"":""openAt"",""type"":{""type"":""int"",""logicalType"":""time-millis""}},
            {""name"":""total"",""type"":{""type"":""bytes"",""logicalType"":""decimal"",""precision"":10,""scale"":2}},
            {""name"":""address"",""type"":{""type"":""record"",""name"":""Address"",""fields"":[{""name"":""city"",""type"":""string""}]}}]}";

        [Test]
        public void GenerateClass_LogicalTypesAndNamespace_KeepTheSchema()
        {
            //Act
            var model = AvroConvert.GenerateModel(ShopOrderSchema);


            //Assert
            model.Should().StartWith("using System;\r\nusing System.Runtime.Serialization;\r\nusing SolTechnology.Avro.Infrastructure.Attributes;\r\n\r\n");
            model.Should().Contain("[DataContract(Namespace = \"com.acme.shop\")]\r\npublic class Order\r\n");
            model.Should().Contain("\t[AvroType(AvroTypeRepresentation.TimestampMilliseconds)]\r\n\t[DataMember]\r\n\tpublic DateTime createdAt { get; set; }\r\n");
            model.Should().Contain("\t[DataMember]\r\n\tpublic DateOnly? shippedOn { get; set; }\r\n");
            model.Should().Contain("\t[AvroType(AvroTypeRepresentation.TimeMilliseconds)]\r\n\t[DataMember]\r\n\tpublic TimeOnly openAt { get; set; }\r\n");
            model.Should().Contain("\t[AvroDecimal(Precision = 10, Scale = 2)]\r\n\t[DataMember]\r\n\tpublic decimal total { get; set; }\r\n");
            model.Should().Contain("[DataContract(Namespace = \"com.acme.shop\")]\r\npublic class Address\r\n", "a nested record inherits the namespace of the enclosing one");
        }

        [Test]
        public void GeneratedModel_GivesBackTheSameSchema()
        {
            // The classes below are the output of GenerateModel(ShopOrderSchema). The canonical form leaves out logical
            // types, precision and scale, so the full schemas are compared (without "default": null, which is not generated).
            var original = AvroSchema.Parse(ShopOrderSchema).Json;

            var fromModel = AvroSchema.Create<GeneratedShop.Order>().Json;

            WithoutDefaults(fromModel).Should().Be(WithoutDefaults(original));
        }

        [Test]
        public void GeneratedModel_WithIncludeOnlyDataContractMembers_KeepsEveryField()
        {
            var original = AvroSchema.Parse(ShopOrderSchema).Json;

            var fromModel = AvroSchema.Create<GeneratedShop.Order>(new AvroConvertOptions { IncludeOnlyDataContractMembers = true }).Json;

            WithoutDefaults(fromModel).Should().Be(WithoutDefaults(original));
        }

        private const string RichSchema = @"{""type"":""record"",""name"":""Order"",""namespace"":""shop"",""fields"":[
            {""name"":""hash"",""type"":{""type"":""fixed"",""name"":""MD5"",""size"":16}},
            {""name"":""previousHash"",""type"":[""null"",""MD5""],""default"":null},
            {""name"":""value"",""type"":[""boolean"",""int"",""long"",""float"",""string"",{""type"":""array"",""items"":""string""},{""type"":""map"",""values"":""long""}]},
            {""name"":""createdOrText"",""type"":[""string"",{""type"":""long"",""logicalType"":""timestamp-millis""}]},
            {""name"":""status"",""type"":{""type"":""enum"",""name"":""Status"",""symbols"":[""default"",""Paid""]},""default"":""Paid""},
            {""name"":""kind"",""type"":[""null"",""string"",""Status""],""default"":null},
            {""name"":""matrix"",""type"":{""type"":""array"",""items"":{""type"":""array"",""items"":""int""}}},
            {""name"":""lines"",""type"":{""type"":""map"",""values"":{""type"":""record"",""name"":""Line"",""fields"":[{""name"":""qty"",""type"":""int""}]}}},
            {""name"":""class"",""type"":""string"",""default"":""say \""hi\""\nbye"",""doc"":""a <b> & c\nsecond line""},
            {""name"":""active"",""type"":[""boolean"",""null""],""default"":true},
            {""name"":""ratio"",""type"":""double"",""default"":""NaN""}]}";

        [Test]
        public void GenerateClass_FixedUnionsKeywordsAndDefaults_CodeKeepsTheSchema()
        {
            //Act
            var model = AvroConvert.GenerateModel(RichSchema);


            //Assert
            model.Should().Contain("\t[AvroFixed(16, \"MD5\", \"shop\")]\r\n\t[DataMember]\r\n\tpublic byte[] hash { get; set; }\r\n", "a fixed keeps its name and size");
            model.Should().Contain("\t[AvroFixed(16, \"MD5\", \"shop\")]\r\n\t[DataMember]\r\n\tpublic byte[]? previousHash { get; set; }\r\n", "a fixed referred to by name");
            model.Should().Contain("\t[AvroUnion(typeof(bool), typeof(int), typeof(long), typeof(float), typeof(string), typeof(string[]), typeof(Dictionary<string,long>))]\r\n", "every branch of a union");
            model.Should().Contain("\t[AvroType(AvroTypeRepresentation.TimestampMilliseconds)]\r\n\t[AvroUnion(typeof(string), typeof(DateTime))]\r\n");
            model.Should().Contain("\t[DefaultValue(\"Paid\")]\r\n\t[DataMember]\r\n\tpublic Status status { get; set; }\r\n");
            model.Should().Contain("\t[AvroUnion(typeof(string), typeof(Status))]\r\n\t[DataMember]\r\n\tpublic object? kind { get; set; }\r\n");
            model.Should().Contain("\tpublic int[][] matrix { get; set; }\r\n");
            model.Should().Contain("\tpublic Dictionary<string,Line> lines { get; set; }\r\n");
            model.Should().Contain("\t/// a &lt;b&gt; &amp; c\r\n\t/// second line\r\n");
            model.Should().Contain("\t[DefaultValue(\"say \\\"hi\\\"\\nbye\")]\r\n\t[DataMember]\r\n\tpublic string @class { get; set; }\r\n");
            model.Should().Contain("\t[DefaultValue(true)]\r\n\t[DataMember]\r\n\tpublic bool? active { get; set; }\r\n");
            model.Should().Contain("\t[DefaultValue(double.NaN)]\r\n");
            model.Should().Contain("\t@default,\r\n\tPaid\r\n");
        }

        [Test]
        public void GeneratedModel_WithFixedUnionsAndDefaults_GivesBackTheSameSchema()
        {
            // The classes in GeneratedRich are the output of GenerateModel(RichSchema).
            AvroSchema.Create<GeneratedRich.Order>().CanonicalForm.Should().Be(AvroSchema.Parse(RichSchema).CanonicalForm);
            // "doc" comes from [Description] and the generated code has it as a comment, so the full schemas are compared without it.
            WithoutDefaults(AvroSchema.Create<GeneratedRich.Order>().Json).Should().Be(WithoutDefaults(AvroSchema.Parse(RichSchema).Json, "doc"));
        }

        [Test]
        public void GeneratedModel_WithFixedUnionsAndDefaults_ReadsAndWritesTheSameData()
        {
            var order = new GeneratedRich.Order
            {
                hash = Enumerable.Range(0, 16).Select(i => (byte)i).ToArray(),
                previousHash = null,
                value = new Dictionary<string, long> { ["a"] = 1 },
                createdOrText = new DateTime(2026, 11, 8, 12, 0, 0, 123, DateTimeKind.Utc),
                status = GeneratedRich.Status.@default,
                kind = GeneratedRich.Status.Paid,
                matrix = new[] { new[] { 1, 2 }, new int[0] },
                lines = new Dictionary<string, GeneratedRich.Line> { ["x"] = new() { qty = 3 } },
                @class = "c",
                active = null,
                ratio = 0.5
            };

            var json = AvroConvert.Avro2JsonEncoding(AvroConvert.Serialize(order, AvroSchema.Parse(RichSchema)));
            var back = AvroConvert.Deserialize<GeneratedRich.Order>(AvroConvert.JsonEncoding2Avro(json, AvroSchema.Parse(RichSchema)));

            json.Should().Contain("\"value\":{\"map\":{\"a\":1}}").And.Contain("\"kind\":{\"shop.Status\":\"Paid\"}").And.Contain("\"createdOrText\":{\"long\":1794139200123}");
            back.Should().BeEquivalentTo(order);
        }

        [Test]
        public void GenerateClass_ErrorTypeAndArrayOfUnions_AreClasses()
        {
            const string schema = @"{""type"":""record"",""name"":""Result"",""fields"":[
                {""name"":""failure"",""type"":[""null"",{""type"":""error"",""name"":""Failure"",""fields"":[{""name"":""message"",""type"":""string""}]}]},
                {""name"":""scores"",""type"":{""type"":""array"",""items"":[""null"",{""type"":""int"",""logicalType"":""date""}]}},
                {""name"":""grid"",""type"":{""type"":""array"",""items"":{""type"":""array"",""items"":[""double"",""string"",""null""]}}}]}";

            var model = AvroConvert.GenerateModel(schema);

            model.Should().Contain("\tpublic Failure? failure { get; set; }\r\n").And.Contain("public class Failure\r\n");
            model.Should().Contain("\tpublic DateOnly?[] scores { get; set; }\r\n");
            model.Should().Contain("\tpublic object?[][] grid { get; set; }\r\n");
        }

        [Test]
        public void GenerateClass_RecordsOfOneNameInTwoNamespaces_AreTwoClasses()
        {
            const string schema = @"{""type"":""record"",""name"":""Envelope"",""namespace"":""a"",""fields"":[
                {""name"":""first"",""type"":{""type"":""record"",""name"":""Id"",""fields"":[{""name"":""value"",""type"":""string""}]}},
                {""name"":""inner"",""type"":{""type"":""record"",""name"":""Inner"",""namespace"":""b"",""fields"":[
                    {""name"":""ids"",""type"":[""null"",{""type"":""array"",""items"":{""type"":""record"",""name"":""Id"",""fields"":[{""name"":""number"",""type"":""long""}]}}]}]}}]}";

            var model = AvroConvert.GenerateModel(schema);

            model.Should().Contain("public class aId\r\n").And.Contain("public class bId\r\n");
            model.Should().Contain("\tpublic aId first { get; set; }\r\n").And.Contain("\tpublic bId[]? ids { get; set; }\r\n");
        }

        [Test]
        public void GenerateClass_SchemaWithComments_IsGenerated()
        {
            var model = AvroConvert.GenerateModel("// generated by a tool\n{\"type\":\"record\",\"name\":\"Item\",\"fields\":[{\"name\":\"id\",\"type\":\"string\"}]}");

            model.Should().Contain("public class Item\r\n");
        }

        [Test]
        public void GenerateClass_FixedBranchOfAUnion_KeepsTheFixed()
        {
            const string schema = @"{""type"":""record"",""name"":""Token"",""namespace"":""auth"",""fields"":[
                {""name"":""value"",""type"":[{""type"":""fixed"",""name"":""Hash"",""size"":4},""int""]}]}";

            var model = AvroConvert.GenerateModel(schema);

            model.Should().Contain("\t[AvroFixed(4, \"Hash\", \"auth\")]\r\n\t[AvroUnion(typeof(byte[]), typeof(int))]\r\n\t[DataMember]\r\n\tpublic object value { get; set; }\r\n");
            AvroSchema.Create<GeneratedToken.Token>().CanonicalForm.Should().Be(AvroSchema.Parse(schema).CanonicalForm);
            var back = AvroConvert.Deserialize<GeneratedToken.Token>(AvroConvert.Serialize(new GeneratedToken.Token { value = new byte[] { 1, 2, 3, 4 } }));
            back.value.Should().BeEquivalentTo(new byte[] { 1, 2, 3, 4 });
        }

        [Test]
        public void GenerateClass_PrimitiveSchema_IsAClearError()
        {
            Recorded.Exception(() => AvroConvert.GenerateModel("\"string\"")).Message.Should().Contain("no record or enum");
        }

        private static string WithoutDefaults(string json, string alsoWithout = null)
        {
            var node = System.Text.Json.Nodes.JsonNode.Parse(json);
            Strip(node);
            return node!.ToJsonString();

            void Strip(System.Text.Json.Nodes.JsonNode node)
            {
                switch (node)
                {
                    case System.Text.Json.Nodes.JsonObject obj:
                        obj.Remove("default");
                        if (alsoWithout != null) obj.Remove(alsoWithout);
                        foreach (var property in obj.ToList()) Strip(property.Value);
                        break;
                    case System.Text.Json.Nodes.JsonArray array:
                        foreach (var item in array) Strip(item);
                        break;
                }
            }
        }
    }
}

namespace AvroConvertComponentTests.GenerateModel.GeneratedShop
{
    using System;
    using System.Runtime.Serialization;
    using SolTechnology.Avro.Infrastructure.Attributes;

    [DataContract(Namespace = "com.acme.shop")]
    public class Order
    {
        [AvroType(AvroTypeRepresentation.TimestampMilliseconds)]
        [DataMember]
        public DateTime createdAt { get; set; }
        [DataMember]
        public DateOnly? shippedOn { get; set; }
        [AvroType(AvroTypeRepresentation.TimeMilliseconds)]
        [DataMember]
        public TimeOnly openAt { get; set; }
        [AvroDecimal(Precision = 10, Scale = 2)]
        [DataMember]
        public decimal total { get; set; }
        [DataMember]
        public Address address { get; set; }
    }

    [DataContract(Namespace = "com.acme.shop")]
    public class Address
    {
        [DataMember]
        public string city { get; set; }
    }
}

#nullable enable
#pragma warning disable CS8618 // the generated properties have no initialisers
// The generated code is for projects with nullable reference types on: byte[]? and object? are unions with null.
namespace AvroConvertComponentTests.GenerateModel.GeneratedRich
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Runtime.Serialization;
    using SolTechnology.Avro.Infrastructure.Attributes;

    // Output of GenerateModel(RichSchema), as it is.
    [DataContract(Namespace = "shop")]
    public class Order
    {
        [AvroFixed(16, "MD5", "shop")]
        [DataMember]
        public byte[] hash { get; set; }
        [AvroFixed(16, "MD5", "shop")]
        [DataMember]
        public byte[]? previousHash { get; set; }
        [AvroUnion(typeof(bool), typeof(int), typeof(long), typeof(float), typeof(string), typeof(string[]), typeof(Dictionary<string,long>))]
        [DataMember]
        public object value { get; set; }
        [AvroType(AvroTypeRepresentation.TimestampMilliseconds)]
        [AvroUnion(typeof(string), typeof(DateTime))]
        [DataMember]
        public object createdOrText { get; set; }
        [DefaultValue("Paid")]
        [DataMember]
        public Status status { get; set; }
        [AvroUnion(typeof(string), typeof(Status))]
        [DataMember]
        public object? kind { get; set; }
        [DataMember]
        public int[][] matrix { get; set; }
        [DataMember]
        public Dictionary<string,Line> lines { get; set; }
        /// <summary>
        /// a &lt;b&gt; &amp; c
        /// second line
        /// </summary>
        [DefaultValue("say \"hi\"\nbye")]
        [DataMember]
        public string @class { get; set; }
        [DefaultValue(true)]
        [DataMember]
        public bool? active { get; set; }
        [DefaultValue(double.NaN)]
        [DataMember]
        public double ratio { get; set; }
    }

    [DataContract(Namespace = "shop")]
    public enum Status
    {
        @default,
        Paid
    }

    [DataContract(Namespace = "shop")]
    public class Line
    {
        [DataMember]
        public int qty { get; set; }
    }
}

namespace AvroConvertComponentTests.GenerateModel.GeneratedToken
{
    using System.Runtime.Serialization;
    using SolTechnology.Avro.Infrastructure.Attributes;

    // Output of GenerateModel for the schema in GenerateClass_FixedBranchOfAUnion_KeepsTheFixed.
    [DataContract(Namespace = "auth")]
    public class Token
    {
        [AvroFixed(4, "Hash", "auth")]
        [AvroUnion(typeof(byte[]), typeof(int))]
        [DataMember]
        public object value { get; set; }
    }
}
#pragma warning restore CS8618
#nullable restore
