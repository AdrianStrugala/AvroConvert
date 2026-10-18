using System.Collections.Generic;
using SolTechnology.Avro;
using Xunit;

namespace AvroConvertComponentTests.GenerateSchema
{
    public class AbstractClassTests
    {

        [Fact]
        public void GenerateSchema_ClassIsChildOfAbstractClass_SchemaIsGenerated()
        {
            //Arrange


            //Act
            string schema = AvroConvert.GenerateSchema(typeof(Farm));


            //Assert
            Assert.Equal(@"{""name"":""Farm"",""namespace"":""AvroConvertComponentTests.GenerateSchema"",""type"":""record"",""fields"":[{""name"":""Name"",""type"":""string""},{""name"":""Animals"",""type"":{""type"":""array"",""items"":{""name"":""Animal"",""namespace"":""AvroConvertComponentTests.GenerateSchema"",""type"":""record"",""fields"":[{""name"":""IsOutside"",""type"":""boolean""}]}}},{""name"":""Plants"",""type"":{""type"":""array"",""items"":{""name"":""IPlant"",""namespace"":""AvroConvertComponentTests.GenerateSchema"",""type"":""record"",""fields"":[{""name"":""Color"",""type"":""string""}]}}}]}", schema);
        }


        public class Farm
        {
            public string Name { get; set; }
            public List<Animal> Animals { get; set; }
            public List<IPlant> Plants { get; set; }
        }

        public abstract class Animal
        {
            public bool IsOutside { get; set; }
        }

        public interface IPlant
        {
            public string Color { get; set; }
        }
    }
}
