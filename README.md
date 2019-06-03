Nuget: (https://www.nuget.org/packages/AvroConvert)

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