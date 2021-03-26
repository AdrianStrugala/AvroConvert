using System.Runtime.Serialization;

namespace AvroConvertComponentTests.CodecTests
{
    [Equals(DoNotAddEqualityOperators = true)]
    public class TwitterModel
    {
        [DataMember(Name = "username")]
        public string Username { get; set; }

        [DataMember(Name = "tweet")]
        public string Tweet { get; set; }

        [DataMember(Name = "timestamp")]
        public long Timestamp { get; set; }
    }
}
