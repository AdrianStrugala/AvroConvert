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
        public string? name { get; set; }
        public int? favorite_number { get; set; }
        public string favorite_color { get; set; }
    }

    [Equals(DoNotAddEqualityOperators = true)]
    public class UserWithNonNullableProperties
    {
        public string name { get; set; }
        public int favorite_number { get; set; }
        public string favorite_color { get; set; }
    }


    [Equals(DoNotAddEqualityOperators = true)]
    public class UserNameClass
    {
        public string name { get; set; }
    }

    [Equals(DoNotAddEqualityOperators = true)]
    public class BaseTestClass
    {
        public string justSomeProperty { get; set; }
        public long andLongProperty { get; set; }
        public User objectProperty { get; set; }
    }

    [Equals(DoNotAddEqualityOperators = true)]
    public class DifferentCaseBaseTestClass
    {
        public string JustSomeProperty { get; set; }
        public long AndLongProperty { get; set; }
        public User ObjectProperty { get; set; }
    }

    [Equals(DoNotAddEqualityOperators = true)]
    public class ReducedBaseTestClass
    {
        public string justSomeProperty { get; set; }
    }

    [Equals(DoNotAddEqualityOperators = true)]
    public class ExtendedBaseTestClass
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
        public User objectProperty { get; set; }
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
        public List<BaseTestClass> nestedList { get; set; }
        public List<ClassWithSimpleList> anotherList { get; set; }
        public string stringProperty { get; set; }

        public ClassWithConstructorPopulatingProperty()
        {
            nestedList = new List<BaseTestClass>();
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
        public short? Size { get; set; }
    }


    [Equals(DoNotAddEqualityOperators = true)]
    [DataContract(Name = "User", Namespace = "user")]
    public class AttributeClass
    {
        [DataMember(Name = "name")]
        [NullableSchema]
        public string NullableStringProperty { get; set; }

        [DataMember(Name = "favorite_number")]
        [NullableSchema]
        public int NullableIntProperty { get; set; }

        [DataMember(Name = "favorite_color")]
        public string AndAnotherString { get; set; }

        [DefaultValue(2137)]
        public int? NullableIntPropertyWithDefaultValue { get; set; }

        [IgnoreDataMember]
        public string IgnoredProperty { get; set; }
    }

    [Equals(DoNotAddEqualityOperators = true)]
    [DataContract(Name = "User", Namespace = "database")]
    public class SmallerAttributeClass
    {
        [DataMember(Name = "name")]
        [NullableSchema]
        public string NullableStringProperty { get; set; }

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
        public DateOnly MyDate { get; set; }
        public TimeOnly MyTime { get; set; }
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
        [NullableSchema]
        public string StringProperty;

        [DataMember(Name = "favorite_number")]
        [NullableSchema]
        public int? NullableIntProperty;

        [DataMember(Name = "favorite_color")]
        public string AndAnotherString;
    }

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
        [DefaultValue(TestEnum.be)]
        public TestEnum? EnumProp { get; set; }

        [DefaultValue("ca")]
        public TestEnum? SecondEnumProp { get; set; }
    }

    public record TestRecord
    {
        public string Name { get; set; }
    }

#nullable enable
    [Equals(DoNotAddEqualityOperators = true)]
    public class ClassWithNullableMembers
    {
        public string? NullableStringProperty { get; set; }

        public string? NullableField;
        public UserNameClass? NullableRecord { get; set; }
    }
#nullable disable

    public class BaseClass
    {
        public string BaseProp { get; set; }
    }

    public class InheritingClass : BaseClass
    {
    }

    public interface BaseInterface
    {
        public string BaseProp { get; set; }
    }

    public class InheritingClassFromInterface : BaseInterface
    {
        public string BaseProp { get; set; }
    }

    public class kylosample
    {
        public string registration_dttm { get; set; }
        public long id { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string email { get; set; }
        public string gender { get; set; }
        public string ip_address { get; set; }
        public long? cc { get; set; }
        public string country { get; set; }
        public string birthdate { get; set; }
        public double? salary { get; set; }
        public string title { get; set; }
        public string comments { get; set; }
    }



    public class ClassWithoutDefaultConstructor
    {
        public string Name { get; private set; }

        public ClassWithoutDefaultConstructor(string name)
        {
            Name = name;
        }
    }

    public class VeryComplexClassWithoutDefaultConstructor
    {
        public List<ClassWithArray> ClassesWithArray { get; set; }
        public ClassWithGuid[] ClassesWithGuid { get; set; }
        public ClassWithConstructorPopulatingProperty anotherClass { get; set; }
        public User simpleClass { get; set; }
        public int simpleObject { get; set; }
        public List<bool> bools { get; set; }
        public double doubleProperty { get; set; }
        public float floatProperty { get; set; }

        public VeryComplexClassWithoutDefaultConstructor(List<ClassWithArray> classesWithArray, ClassWithGuid[] classesWithGuid, ClassWithConstructorPopulatingProperty anotherClass, User simpleClass, int simpleObject, List<bool> bools, double doubleProperty, float floatProperty)
        {
            ClassesWithArray = classesWithArray;
            ClassesWithGuid = classesWithGuid;
            this.anotherClass = anotherClass;
            this.simpleClass = simpleClass;
            this.simpleObject = simpleObject;
            this.bools = bools;
            this.doubleProperty = doubleProperty;
            this.floatProperty = floatProperty;
        }
    }
}
