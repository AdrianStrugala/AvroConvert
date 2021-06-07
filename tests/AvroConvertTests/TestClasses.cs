using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using SolTechnology.Avro.Infrastructure.Attributes;

namespace AvroConvertComponentTests
{
    [Equals(DoNotAddEqualityOperators = true)]
    public class User
    {
        public string name { get; set; }

        public int? favorite_number { get; set; }

        public string favorite_color { get; set; }
    }

    [Equals(DoNotAddEqualityOperators = true)]
    public class UserNameClass
    {
        public string name { get; set; }
    }

    [Equals(DoNotAddEqualityOperators = true)]
    public class SomeTestClass
    {
        public NestedTestClass objectProperty { get; set; }

        public int simpleProperty { get; set; }
    }

    [Equals(DoNotAddEqualityOperators = true)]
    public class NestedTestClass
    {
        public string justSomeProperty { get; set; }

        public long andLongProperty { get; set; }
    }

    [Equals(DoNotAddEqualityOperators = true)]
    public class DifferentCaseNestedTestClass
    {
        public string JustSomeProperty { get; set; }

        public long AndLongProperty { get; set; }
    }

    [Equals(DoNotAddEqualityOperators = true)]
    public class SmallerNestedTestClass
    {
        public string justSomeProperty { get; set; }
    }

    [Equals(DoNotAddEqualityOperators = true)]
    public class BiggerNestedTestClass
    {
        public string justSomeProperty { get; set; }
        public long andLongProperty { get; set; }
        public double DoubleProp { get; set; }
        public bool BoolProp { get; set; }
        public long LongToSkipProp { get; set; }
        public Guid TheGuid { get; set; }
        public int[] TheArray { get; set; }
        public List<bool> Bools;
        [DefaultValue(9200000000000000007)]
        public long? AndLongBigDefaultedProperty { get; set; }
        public ClassWithConstructorPopulatingProperty AnotherClass { get; set; }
        public double DoubleProperty { get; set; }
        public Dictionary<string, int> AvroMap { get; set; }
        public Dictionary<bool, int> OtherDictionary { get; set; }
        public float FloatProp { get; set; }
        public TestEnum EnumProp { get; set; }
    }


    [Equals(DoNotAddEqualityOperators = true)]
    public class LogicalTypesClass
    {
        public decimal One { get; set; }
   
        public Guid? Two { get; set; }

        public TimeSpan Three { get; set; }

        public DateTime? Four { get; set; }

        public DateTimeOffset Five { get; set; }
    }

    [Equals(DoNotAddEqualityOperators = true)]
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
    [Equals(DoNotAddEqualityOperators = true)]
    public class ClassWithSimpleList
    {
        public List<int> someList { get; set; }

        public ClassWithSimpleList()
        {
            someList = new List<int>();
        }
    }

    [Equals(DoNotAddEqualityOperators = true)]
    public class ClassWithArray
    {
        public int[] theArray { get; set; }
    }

    [Equals(DoNotAddEqualityOperators = true)]
    public class ClassWithGuid
    {
        public Guid theGuid { get; set; }
    }

    [Equals(DoNotAddEqualityOperators = true)]
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

    [Equals(DoNotAddEqualityOperators = true)]
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

    [Equals(DoNotAddEqualityOperators = true)]
    [DataContract(Name = "User", Namespace = "database")]
    public class SmallerAttributeClass
    {
        [DataMember(Name = "name")]
        public string StringProperty { get; set; }

        [DataMember(Name = "favorite_number")]
        [NullableSchema]
        public int? NullableIntProperty { get; set; }
    }

    [Equals(DoNotAddEqualityOperators = true)]
    public class ClassWithDateTime
    {
        public string From { get; set; }
        public string To { get; set; }

        public int Count { get; set; }
        public DateTime ArriveBy { get; set; }
    }

    [Equals(DoNotAddEqualityOperators = true)]
    public class ClassWithDateTimeOffset
    {
        public DateTimeOffset yeah { get; set; }
    }

    [Equals(DoNotAddEqualityOperators = true)]
    public class ConcurrentBagClass
    {
        public ConcurrentBag<ComplexClassWithoutGetters> concurentBagField;
    }

    [Equals(DoNotAddEqualityOperators = true)]
    public class ClassWithoutGetters
    {
        public string SomeString;
        public int Count;
    }

    [Equals(DoNotAddEqualityOperators = true)]
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

    [Equals(DoNotAddEqualityOperators = true)]
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

    [Equals(DoNotAddEqualityOperators = true)]
    public struct ComplexStruct
    {
        [DataMember]
        public List<int> savedValues;

        public ComplexStruct(List<int> vals)
        {
            savedValues = vals;
        }
    }

    [Equals(DoNotAddEqualityOperators = true)]
    public class MultidimensionalArrayClass
    {
        public int[,,] ArrayField { get; set; }
    }


    [Equals(DoNotAddEqualityOperators = true)]
    public class DefaultValueClass
    {
        [DefaultValue("Let's go")]
        [NullableSchema]
        public string justSomeProperty { get; set; }

        public long? andLongProperty { get; set; }

        [DefaultValue(9200000000000000007)]
        public long? andLongBigDefaultedProperty { get; set; }

        [DefaultValue(100)]
        public long? andLongSmallDefaultedProperty { get; set; }

        [DefaultValue(null)]
        public long? andNullProperty { get; set; }
    }


    [Equals(DoNotAddEqualityOperators = true)]
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

    public enum TestEnum
    {
        a,
        be,
        ca,
        dlo
    }

    [Equals(DoNotAddEqualityOperators = true)]
    public class ClassWithEnum
    {
        public TestEnum EnumProp { get; set; }
    }

    public record TestRecord
    {
        public string Name { get; set; }
    }

    [Equals(DoNotAddEqualityOperators = true)]
    public class SimpleClass
    {
        public string StringValue { get; set; }

        public decimal DecimalValue { get; set; }
    }

    [Equals(DoNotAddEqualityOperators = true)]
    public class SimpleClassWithMissingField
    {
        public string StringValue { get; set; }
    }
}
