using System.IO;
using SolTechnology.Avro;
using Xunit;

namespace AvroConvertComponentTests.GenerateClass
{
    public class GenerateClassTests
    {
        private readonly byte[] _avroBytes;

        public GenerateClassTests()
        {
            _avroBytes = File.ReadAllBytes("example2.avro");
        }

        [Fact]
        public void GenerateClass_UserClass_OutputIsEqualToExpected()
        {
            //Arrange


            //Act
            string resultSchema = AvroConvert.GenerateClass(_avroBytes);


            //Assert
            Assert.Equal(
"public class User\r\n" +
"{\r\n" +
"\tpublic string name { get; set; }\r\n" +
"\tpublic int? favorite_number { get; set; }\r\n" +
"\tpublic string? favorite_color { get; set; }\r\n" +
"}\r\n" +
"\r\n",
resultSchema);
        }


        [Fact]
        public void GenerateClass_NestedClass_OutputIsEqualToExpected()
        {
            //Arrange
            var schema = AvroConvert.GenerateSchema(typeof(BaseTestClass));

            //Act
            string resultSchema = AvroConvert.GenerateClass(schema);


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
        public void GenerateClass_ClassWithEnum_OutputIsEqualToExpected()
        {
            //Arrange
            var schema = AvroConvert.GenerateSchema(typeof(ClassWithEnum));

            //Act
            string resultSchema = AvroConvert.GenerateClass(schema);


            //Assert
            Assert.Equal(
                "public class ClassWithEnum\r\n" +
                "{\r\n" +
                "\tpublic TestEnum EnumProp { get; set; }\r\n" +
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
            string resultSchema = AvroConvert.GenerateClass(schema);


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
                "\r\n",
                "public class ClassWithSimpleList\r\n" +
                "{\r\n" +
                "\tpublic int[] someList { get; set; }\r\n" +
                "}\r\n" +
                "\r\n" +
                
                resultSchema);
        }
    }
}