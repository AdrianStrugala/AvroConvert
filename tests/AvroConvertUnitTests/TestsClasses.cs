using System.ComponentModel;
using System.Runtime.Serialization;
using SolTechnology.Avro.Attributes;

namespace AvroConvertUnitTests
{
    [DataContract(Name = "User", Namespace = "userspace")]
    [Description("This is Doc of User Class")]
    public class AttributeClass
    {
        [DataMember(Name = "name")]
        [Description("This is Doc of record field")]
        public string StringProperty { get; set; }

        [DataMember(Name = "favorite_number")]
        [DefaultValue(2137)]
        public int? NullableIntPropertyWithDefaultValue { get; set; }

        [DataMember(Name = "not_favorite_number")]
        [NullableSchema]
        public int AnotherWayOrIndicatingNullableProperty { get; set; }

        [DataMember(Name = "favorite_color")]
        [NullableSchema]
        public string AndAnotherString { get; set; }

        [AvroDecimal(Scale = 2, Precision = 4)]
        public decimal AvroDecimal { get; set; }
    }
}
