using System.Runtime.Serialization;

namespace AvroConvertTests.TwitterExample
{
    [Equals]
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
