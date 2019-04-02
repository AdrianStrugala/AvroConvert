namespace AvroConvertTests
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using Xunit;

    public class SerializeTests
    {
        [Fact]
        public void Serialize_SomeObject_NoExceptionIsThrown()
        {
            //Arrange
            Dupa dupa = new Dupa
            {
                cycek = new Cycki
                {
                    lewy = "spoko",
                    prawy = 2137
                },
                numebr = 111111
            };

            Dupa dupa2 = new Dupa
            {
                cycek = new Cycki
                {
                    lewy = "loko",
                    prawy = 2137
                },
                numebr = 2135
            };

            List<Dupa> dupas = new List<Dupa>();
            dupas.Add(dupa);
            dupas.Add(dupa2);

            //Act
            var result = AvroConvert.AvroConvert.Serialize(dupas);

            //Assert
            Assert.NotNull(result);
        }

    }


    class User
    {
        public string name { get; set; }
        public int? favorite_number { get; set; }
        public string favorite_color { get; set; }
    }

    [DataContract(Name = "Dupa", Namespace = "test.demo")]
    public class Dupa
    {
        [DataMember(Name = "cycek")]
        public Cycki cycek { get; set; }

        [DataMember(Name = "numebr")]
        public int numebr { get; set; }
    }

    [DataContract(Name = "Cycek", Namespace = "pubsub.demo")]
    public class Cycki
    {
        [DataMember(Name = "lewy")]
        public string lewy { get; set; }
        [DataMember(Name = "prawy")]
        public long prawy { get; set; }
    }
}
