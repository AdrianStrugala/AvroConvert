using System.IO;
using SolTechnology.Avro;
using Xunit;

namespace AvroConvertComponentTests.GenerateModel
{
    public class GenerateModelTests
    {
        private readonly byte[] _avroBytes;

        public GenerateModelTests()
        {
            _avroBytes = File.ReadAllBytes("example2.avro");
        }

        [Fact]
        public void GenerateClass_UserClass_OutputIsEqualToExpected()
        {
            //Arrange


            //Act
            string resultClass = AvroConvert.GenerateModel(_avroBytes);


            //Assert
            Assert.Equal(
"public class User\r\n" +
"{\r\n" +
"\tpublic string name { get; set; }\r\n" +
"\tpublic int? favorite_number { get; set; }\r\n" +
"\tpublic string? favorite_color { get; set; }\r\n" +
"}\r\n" +
"\r\n",
resultClass);
        }


        [Fact]
        public void GenerateClass_NestedClass_OutputIsEqualToExpected()
        {
            //Arrange
            var schema = AvroConvert.GenerateSchema(typeof(BaseTestClass));

            //Act
            string resultSchema = AvroConvert.GenerateModel(schema);


            //Assert
            Assert.Equal(
                "public class BaseTestClass\r\n" +
                "{\r\n" +
                "\tpublic string justSomeProperty { get; set; }\r\n" +
                "\tpublic long andLongProperty { get; set; }\r\n" +
                "\tpublic User objectProperty { get; set; }\r\n" +
                "}\r\n" +
                "\r\n" +
                "public class User\r\n" +
                "{\r\n" +
                "\tpublic string name { get; set; }\r\n" +
                "\tpublic int? favorite_number { get; set; }\r\n" +
                "\tpublic string favorite_color { get; set; }\r\n" +
                "}\r\n" +
                "\r\n",
                resultSchema);
        }

        [Fact]
        public void GenerateClass_ClassWithEnum_GeneratedModelIsAsExpected()
        {
            //Arrange
            var schema = AvroConvert.GenerateSchema(typeof(ClassWithEnum));

            //Act
            string resultSchema = AvroConvert.GenerateModel(schema);


            //Assert
            Assert.Equal(
                "public class ClassWithEnum\r\n" +
                "{\r\n" +
                "\tpublic TestEnum? EnumProp { get; set; }\r\n" +
                "}\r\n" +
                "\r\n" +
                "public enum TestEnum\r\n" +
                "{\r\n" +
                "\ta,\r\n" +
                "\tbe,\r\n" +
                "\tca,\r\n" +
                "\tdlo\r\n" +
                "}\r\n" +
                "\r\n",
                resultSchema);
        }

        [Fact]
        public void GenerateClass_VeryComplexClass_OutputIsEqualToExpected()
        {
            //Arrange
            var schema = AvroConvert.GenerateSchema(typeof(VeryComplexClass));

            //Act
            string resultSchema = AvroConvert.GenerateModel(schema);


            //Assert
            Assert.Equal(
                "public class VeryComplexClass\r\n" +
                "{\r\n" +
                "\tpublic ClassWithArray[] ClassesWithArray { get; set; }\r\n" +
                "\tpublic ClassWithGuid[] ClassesWithGuid { get; set; }\r\n" +
                "\tpublic ClassWithConstructorPopulatingProperty anotherClass { get; set; }\r\n" +
                "\tpublic User simpleClass { get; set; }\r\n" +
                "\tpublic int simpleObject { get; set; }\r\n" +
                "\tpublic bool[] bools { get; set; }\r\n" +
                "\tpublic double doubleProperty { get; set; }\r\n" +
                "\tpublic float floatProperty { get; set; }\r\n" +
                "}\r\n" +
                "\r\n" +
                "public class ClassWithArray\r\n" +
                "{\r\n" +
                "\tpublic int[] theArray { get; set; }\r\n" +
                "}\r\n" +
                "\r\n" +
                "public class ClassWithGuid\r\n" +
                "{\r\n" +
                "\tpublic Guid theGuid { get; set; }\r\n" +
                "}\r\n" +
                "\r\n" +
                "public class ClassWithConstructorPopulatingProperty\r\n" +
                "{\r\n" +
                "\tpublic BaseTestClass[] nestedList { get; set; }\r\n" +
                "\tpublic ClassWithSimpleList[] anotherList { get; set; }\r\n" +
                "\tpublic string stringProperty { get; set; }\r\n" +
                "}\r\n" +
                "\r\n" +
                "public class BaseTestClass\r\n" +
                "{\r\n" +
                "\tpublic string justSomeProperty { get; set; }\r\n" +
                "\tpublic long andLongProperty { get; set; }\r\n" +
                "\tpublic User objectProperty { get; set; }\r\n" +
                "}\r\n" +
                "\r\n" +
                "public class User\r\n" +
                "{\r\n" +
                "\tpublic string name { get; set; }\r\n" +
                "\tpublic int? favorite_number { get; set; }\r\n" +
                "\tpublic string favorite_color { get; set; }\r\n" +
                "}\r\n" +
                "\r\n" +
                "public class ClassWithSimpleList\r\n" +
                "{\r\n" +
                "\tpublic int[] someList { get; set; }\r\n" +
                "}\r\n" +
                "\r\n",

                resultSchema);
        }

        [Fact]
        public void GenerateClass_ClassWithLogicalTypes_OutputIsEqualToExpected()
        {
            //Arrange
            var schema = AvroConvert.GenerateSchema(typeof(LogicalTypesClass));

            //Act
            string resultSchema = AvroConvert.GenerateModel(schema);


            //Assert
            Assert.Equal(
                "public class LogicalTypesClass\r\n" +
                "{\r\n" +
                "\tpublic decimal One { get; set; }\r\n" +
                "\tpublic Guid? Two { get; set; }\r\n" +
                "\tpublic TimeSpan Three { get; set; }\r\n" +
                "\tpublic DateTime? Four { get; set; }\r\n" +
                "\tpublic DateTime Five { get; set; }\r\n" +
                "}\r\n" +
                "\r\n",
                resultSchema);
        }

        [Fact]
        public void GenerateClass_Enum_GeneratedModelIsAsExpected()
        {
            //Arrange
            var schema = AvroConvert.GenerateSchema(typeof(TestEnum));

            //Act
            string resultSchema = AvroConvert.GenerateModel(schema);


            //Assert
            Assert.Equal(
                "public enum TestEnum\r\n" +
                "{\r\n" +
                "\ta,\r\n" +
                "\tbe,\r\n" +
                "\tca,\r\n" +
                "\tdlo\r\n" +
                "}\r\n" +
                "\r\n",
                resultSchema);
        }

        [Fact]
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
            Assert.Equal(
                "public class BaseTestClass\r\n" +
                "{\r\n" +
                "\tpublic FakeUserUser fakeUserProperty { get; set; }\r\n" +
                "\tpublic AvroConvertComponentTestsUser objectProperty { get; set; }\r\n" +
                "}\r\n" +
                "\r\n" +
                "public class FakeUserUser\r\n" +
                "{\r\n" +
                "\tpublic string name { get; set; }\r\n" +
                "}\r\n" +
                "\r\n" +
                "public class AvroConvertComponentTestsUser\r\n" +
                "{\r\n" +
                "\tpublic string name { get; set; }\r\n" +
                "\tpublic int? favorite_number { get; set; }\r\n" +
                "\tpublic string favorite_color { get; set; }\r\n" +
                "}\r\n" +
                "\r\n",
                resultClass);
        }

        [Fact]
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
            Assert.Equal(
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
                "public class datamodule\r\n" +
                "{\r\n" +
                "\tpublic bool? isControl { get; set; }\r\n" +
                "}\r\n" +
                "\r\n" +
                "public class customerInfo\r\n" +
                "{\r\n" +
                "\tpublic string country { get; set; }\r\n" +
                "\tpublic string customerNumber { get; set; }\r\n" +
                "}\r\n" +
                "\r\n",
                resultClass);
        }

        [Fact]
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
            Assert.Equal(
                "public class Result\r\n" +
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
                "\r\n",
                resultClass);
        }


        [Fact]
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
            Assert.Equal(
                "public class Result\r\n" +
                "{\r\n" +
                "\tpublic string testString { get; set; } = \"Default String\";\r\n" +
                "\tpublic bool testBoolean { get; set; } = true;\r\n" +
                "\tpublic int testInt { get; set; } = 123;\r\n" +
                "\tpublic long testLong { get; set; } = 123;\r\n" +
                "\tpublic float testFloat { get; set; } = 1.23;\r\n" +
                "\tpublic double testDouble { get; set; } = 1.23;\r\n" +
                "}\r\n" +
                "\r\n",
                resultClass);
        }
    }
}