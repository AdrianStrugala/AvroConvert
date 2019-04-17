namespace AvroConvertTests
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using Microsoft.Hadoop.Avro;


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
        public List<int> someList { get; set; }

      //  public List<NestedTestClass> nestedList { get; set; }
        public int[] realArray { get; set; }

        public ClassWithConstructorPopulatingProperty()
        {
      //      someList = new List<int>();
        }

    }
}
