namespace AvroConvertTests
{
    using System.Collections.Generic;
    using System.Reflection.Emit;
    using System.Runtime.Serialization;
    using Microsoft.Hadoop.Avro;
    using Xunit;

    public class SerializeTests
    {
        [Fact]
        public void Serialize_InputIsList_NoExceptionIsThrown()
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

        [Fact]
        public void Serialize_InputIsArray_NoExceptionIsThrown()
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

            Dupa[] dupas = new Dupa[2];
            dupas[0] = dupa;
            dupas[1] = dupa2;

            //Act
            var result = AvroConvert.AvroConvert.Serialize(dupas);

            //Assert
            Assert.NotNull(result);
        }


        [Fact]
        public void Serialize_InputIsObject_NoExceptionIsThrown()
        {
            //Arrange
            User user = new User();
            user.name = "Krzys";
            user.favorite_color = "yellow";
            user.favorite_number = null;

            //Act
            var result = AvroConvert.AvroConvert.Serialize(user);

            var xd = AvroConvert.AvroConvert.Deserialize(result);

            //Assert
            Assert.NotNull(result);
        }
    }


      [DataContract(Name = "Dupa", Namespace = "test.demo")]
    public class User
    {
        public string name { get; set; }
        [DataMember(Name = "favorite_number")]
        [NullableSchema]
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
