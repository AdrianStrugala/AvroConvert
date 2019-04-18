namespace AvroConvertTests
{
    using System;
    using Microsoft.Hadoop.Avro;
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
        public string stringProperty { get; set; }

        public ClassWithConstructorPopulatingProperty()
        {
            nestedList = new List<NestedTestClass>();
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
}
