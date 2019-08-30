using System.ComponentModel;

namespace AvroConvertTests
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Serialization;
    using AvroConvert.Attributes;

    [Equals]
    public class User
    {
        public string name { get; set; }

        public int? favorite_number { get; set; }

        public string favorite_color { get; set; }
    }

    [Equals]
    public class UserNameClass
    {
        public string name { get; set; }
    }

    [Equals]
    public class SomeTestClass
    {
        public NestedTestClass objectProperty { get; set; }

        public int simpleProperty { get; set; }
    }

    [Equals]
    public class NestedTestClass
    {
        public string justSomeProperty { get; set; }

        public long andLongProperty { get; set; }
    }

    [Equals]
    public class SmallerNestedTestClass
    {
        public string justSomeProperty { get; set; }
    }

    [Equals]
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
    [Equals]
    public class ClassWithSimpleList
    {
        public List<int> someList { get; set; }

        public ClassWithSimpleList()
        {
            someList = new List<int>();
        }
    }

    [Equals]
    public class ClassWithArray
    {
        public int[] theArray { get; set; }
    }

    [Equals]
    public class ClassWithGuid
    {
        public Guid theGuid { get; set; }
    }

    [Equals]
    public class VeryComplexClass
    {
        public List<ClassWithArray> ClassesWithArray { get; set; }
        public ClassWithGuid[] ClassesWithGuid { get; set; }
        public ClassWithConstructorPopulatingProperty anotherClass { get; set; }
        public User simpleClass { get; set; }
        public int simpleObject { get; set; }
        public List<bool> bools { get; set; }
        public double doubleProperty { get; set; }
        public float floatProperty { get; set; }
    }


    [Equals]
    [DataContract(Name = "User", Namespace = "user")]
    public class AttributeClass
    {
        [DataMember(Name = "name")]
        public string StringProperty { get; set; }

        [DataMember(Name = "favorite_number")]
        [NullableSchema]
        public int? NullableIntProperty { get; set; }

        [DataMember(Name = "favorite_color")]
        public string AndAnotherString { get; set; }
    }

    [Equals]
    [DataContract(Name = "User", Namespace = "database")]
    public class SmallerAttributeClass
    {
        [DataMember(Name = "name")]
        public string StringProperty { get; set; }

        [DataMember(Name = "favorite_number")]
        [NullableSchema]
        public int? NullableIntProperty { get; set; }
    }

    [Equals]
    public class ClassWithDateTime
    {
        public string From { get; set; }
        public string To { get; set; }

        public int Count { get; set; }
        public DateTime ArriveBy { get; set; }
    }

    [Equals]
    public class ClassWithDateTimeOffset
    {
        public DateTimeOffset yeah { get; set; }
    }

    [Equals]
    public class ConcurrentBagClass
    {
        public ConcurrentBag<ComplexClassWithoutGetters> concurentBagField;
    }

    [Equals]
    public class ClassWithoutGetters
    {
        public string SomeString;
        public int Count;
    }

    [Equals]
    public class ComplexClassWithoutGetters
    {
        public List<ClassWithArray> ClassesWithArray;
        public ClassWithGuid[] ClassesWithGuid;
        public ClassWithConstructorPopulatingProperty AnotherClass;
        public User SimpleClass;
        public int SimpleObject;
        public List<bool> Bools;
        public double DoubleProperty;
        public float FloatProperty;
    }

    [Equals]
    [DataContract(Name = "User", Namespace = "user")]
    public class AttributeClassWithoutGetters
    {
        [DataMember(Name = "name")]
        public string StringProperty;

        [DataMember(Name = "favorite_number")]
        [NullableSchema]
        public int? NullableIntProperty;

        [DataMember(Name = "favorite_color")]
        public string AndAnotherString;
    }

    [Equals]
    public struct ComplexStruct
    {
        [DataMember]
        public List<int> savedValues;

        public ComplexStruct(List<int> vals)
        {
            this.savedValues = vals;
        }
    }

    [Equals]
    public class MultidimensionalArrayClass
    {
        public int[,,] ArrayField { get; set; }
    }


    [Equals]
    public class DefaultValueClass
    {
        [DefaultValue("Let's go")]
        public string justSomeProperty { get; set; }

        public long? andLongProperty { get; set; }

        [DefaultValue(9200000000000000007)]
        public long? andLongBigDefaultedProperty { get; set; }

        [DefaultValue(100)]
        public long? andLongSmallDefaultedProperty { get; set; }

        [DefaultValue(null)]
        public long? andNullProperty { get; set; }
    }


    [Equals]
    [DataContract]
    public struct MixedDataMembers
    {
        [DataMember]
        public List<int> savedValues;

        public long? dontSerializeMe { get; set; }

        [DataMember]
        public long? andAnother { get; set; }

        public int anIntField;

    }
}
