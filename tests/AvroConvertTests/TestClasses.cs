namespace AvroConvertTests
{
    using Microsoft.Hadoop.Avro;
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;


    [DataContract(Name = "User", Namespace = "user")]
    public class User
    {
        [DataMember(Name = "name")]
        public string name { get; set; }

        [DataMember(Name = "favorite_number")]
        [NullableSchema]
        public int? favorite_number { get; set; }

        [DataMember(Name = "favorite_color")]
        public string favorite_color { get; set; }
    }

    public class SomeTestClass
    {
        public NestedTestClass objectProperty { get; set; }

        public int simpleProperty { get; set; }
    }

    public class NestedTestClass
    {
        public string justSomeProperty { get; set; }

        public long andLongProperty { get; set; }
    }

    public class ClassWithConstructorPopulatingProperty
    {
        public List<NestedTestClass> nestedList { get; set; }
        public List<ClassWithSimpleList> anotherList { get; set; }
        public string stringProperty { get; set; }

        public ClassWithConstructorPopulatingProperty()
        {
            nestedList = new List<NestedTestClass>();
            anotherList = new List<ClassWithSimpleList>();
        }

    }

    public class ClassWithSimpleList
    {
        public List<int> someList { get; set; }

        public ClassWithSimpleList()
        {
            someList = new List<int>();
        }
    }

    public class ClassWithArray
    {
        public int[] theArray { get; set; }
    }

    public class ClassWithGuid
    {
        public Guid theGuid { get; set; }
    }

    public class VeryComplexClass
    {
        public List<ClassWithArray> ClassesWithArray { get; set; }
        public ClassWithGuid[] ClassesWithGuid { get; set; }
        public ClassWithConstructorPopulatingProperty anotherClass { get; set; }
        public User simpleClass { get; set; }
        public int simpleObject { get; set; }

    }
}
