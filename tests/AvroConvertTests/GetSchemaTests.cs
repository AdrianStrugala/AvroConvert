using System.Collections.Generic;
using System.IO;
using System.Linq;
using AutoFixture;
using SolTechnology.Avro;
using Xunit;

namespace AvroConvertComponentTests
{
    public class GetSchemaTests
    {
        private readonly byte[] _avroBytes;
        private readonly string _example2schema;

        public GetSchemaTests()
        {
            _avroBytes = File.ReadAllBytes("example2.avro");

            _example2schema =
                "{\"type\": \"record\", \"name\": \"User\", \"namespace\": \"example.avro\", \"fields\": [{\"type\": \"string\", \"name\": \"name\"}, {\"type\": [\"int\", \"null\"], \"name\": \"favorite_number\"}, {\"type\": [\"string\", \"null\"], \"name\": \"favorite_color\"}]}";
        }

        [Fact]
        public void GetSchemaAsString_ValidBytes_SchemaIsReturned()
        {
            //Arrange

            //Act
            string schema = AvroConvert.GetSchema(_avroBytes);

            //Assert
            Assert.Equal(_example2schema, schema);
        }

        [Fact]
        public void GetSchemaAsString_ValidStream_SchemaIsReturned()
        {
            //Arrange

            //Act
            string schema = AvroConvert.GetSchema(new MemoryStream(_avroBytes));

            //Assert
            Assert.Equal(_example2schema, schema);
        }

        [Fact]
        public void GetSchemaAsString_ValidFile_SchemaIsReturned()
        {
            //Arrange

            //Act
            string schema = AvroConvert.GetSchema("example.avro");

            //Assert
            Assert.NotNull(schema);
        }
        
        
        
        [Fact]
        public void sss()
        {
            //Arrange

            var schema =
                "{\n  \"type\": \"record\",\n  \"name\": \"TypeWithUnionAvro\",\n  \"namespace\": \"TypeUnionExampleAvro\",\n  \"fields\": [\n    {\n      \"name\": \"TopLevelField\",\n      \"type\": \"string\"\n    },\n    {\n      \"name\": \"UnionField\",\n      \"type\": [\n        {\n          \"type\": \"record\",\n          \"name\": \"ObjA\",\n          \"fields\": [\n            {\n              \"name\": \"FieldA\",\n              \"type\": \"string\"\n            }\n          ]\n        },\n        {\n          \"type\": \"record\",\n          \"name\": \"ObjB\",\n          \"fields\": [\n            {\n              \"name\": \"FieldB\",\n              \"type\": \"int\"\n            }\n          ]\n        }\n      ]\n    }\n  ]\n}";

            var toSerialize = new List<TypeWithUnionAvro>
            {
                new TypeWithUnionAvro
                {
                    TopLevelField = "First",
                    UnionField = new ObjA
                    {
                        FieldA = "dupa"
                    }
                },
                new TypeWithUnionAvro
                {
                    TopLevelField = "Second",
                    UnionField = new ObjB
                    {
                        FieldB = 2137
                    } 
                }
            };
            
            //Act
            var result = new List<TypeWithUnionAvro>();
            foreach (var item in toSerialize)
            {
                var serialized = AvroConvert.Serialize(item, schema);
                result.Add(AvroConvert.Deserialize<TypeWithUnionAvro>(serialized, schema));
            }
            
            //Assert
            Assert.Equivalent(toSerialize, result);
        }
        
        
        public class TypeWithUnionAvro
        {
            public string TopLevelField { get; set; }
            public object UnionField { get; set; }
        }

        public class ObjA
        {
            public string FieldA { get; set; }
        }

        public class ObjB
        {
            public int FieldB { get; set; }
        }
    }
}
