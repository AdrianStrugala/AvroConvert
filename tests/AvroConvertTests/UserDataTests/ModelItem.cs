using System.Runtime.Serialization;

namespace AvroConvertTests.RealExample2
{
    public class ModelItem
    {
        [DataMember(Name = "registration_dttm")]
        public string RegisteredAt { get; set; }

        [DataMember(Name = "id")]
        public long Id { get; set; }

        [DataMember(Name = "first_name")]
        public string FirstName { get; set; }

        [DataMember(Name = "last_name")]
        public string LastName { get; set; }

        [DataMember(Name = "email")]
        public string Email { get; set; }

        [DataMember(Name = "gender")]
        public string Gender { get; set; }

        [DataMember(Name = "ip_address")]
        public string Ip { get; set; }

        [DataMember(Name = "cc")]
        public long? CC { get; set; }

        [DataMember(Name = "country")]
        public string Country { get; set; }

        [DataMember(Name = "birthdate")]
        public string BirthDate { get; set; }

        [DataMember(Name = "salary")]
        public double? Salary { get; set; }

        [DataMember(Name = "title")]
        public string Title { get; set; }

        [DataMember(Name = "comments")]
        public string Comments { get; set; }
    }
}
