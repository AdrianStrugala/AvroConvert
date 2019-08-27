
[![Nuget](https://img.shields.io/badge/Nuget-3k-blue)](https://www.nuget.org/packages/AvroConvert)

# AvroConvert
Small and fast library for serializing C# objects to avro format and vice versa


```csharp
// Serialize
  byte[] avroObject = AvroConvert.Serialize(object yourObject);

// Deserialize
  CustomClass deserializedObject = AvroConvert.Deserialize<CustomClass>(byte[] avroObject);

  Dictionary<string, object> mapOfPropertiesAndValues = AvroConvert.Deserialize(byte[] avroObject);  

// Generate Avro schema for object 
  string schemaInJsonFormat = AvroConvert.GenerateSchema(object yourObject);
  
//Get schema from Avro object
  string schemaInJsonFormat = AvroConvert.GetSchema(byte[] avroObject)
```

Serialization examples:

```csharp
1) Plain object:

    public class NestedTestClass
    {
        public string justSomeProperty { get; set; }

        public long andLongProperty { get; set; }
    }

//Produces following schema:
"{"type":"record","name":"AvroConvertTests.NestedTestClass","fields":[{"name":"justSomeProperty","type":["null","string"]},{"name":"andLongProperty","type":"long"}]}"

2) Using attributes:

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
	
//Produces following schema:
"{"type":"record","name":"user.User","fields":[{"name":"name","type":["null","string"]},{"name":"favorite_number","type":["null","int"]},{"name":"favorite_color","type":["null","string"]}]}"
```
