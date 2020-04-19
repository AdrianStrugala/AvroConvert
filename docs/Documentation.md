### Serialization
```csharp
 byte[] avroObject = AvroConvert.Serialize(object yourObject);
```

Using encoding
```csharp
 byte[] avroObject = AvroConvert.Serialize(object yourObject, CodecType.Snappy);
```
Supported encoding types:
- Null (default)
- Deflate
- Snappy
- GZip



### Deserialization

```csharp
//Using generic method
CustomClass deserializedObject = AvroConvert.Deserialize<CustomClass>(byte[] avroObject);

//Using dynamic method
CustomClass deserializedObject = AvroConvert.Deserialize(byte[] avroObject, typeof(CustomClass));
```

Deserialization when a property value is null, but schema contains information about default value
```csharp
//Model used for serialization
public class DefaultValueClass
{
    [DefaultValue("Let's go")]
    public string justSomeProperty { get; set; }

    [DefaultValue(2137)]
    public long? andLongProperty { get; set; }
}

//Deserializing object with null data
 DefaultValueClass deserializedObject = AvroConvert.Deserialize<DefaultValueClass>(byte[] avroObject);

//Produces following object:
> deserializedObject.justSomeProperty
> "Let's go"

> deserializedObject.andLongProperty
> 2137
```



### Generating Avro schema for C# classes

Using simple class
```csharp

//Model
public class SimpleTestClass
{
	public string justSomeProperty { get; set; }

	public long andLongProperty { get; set; }
}


//Action
string schemaInJsonFormat = AvroConvert.GenerateSchema(typeof(SimpleTestClass));


//Produces following schema:
"{"type":"record","name":"AvroConvert.SimpleTestClass","fields":[{"name":"justSomeProperty","type":["null","string"]},{"name":"andLongProperty","type":"long"}]}"
```

Using class decorated with attributes
```csharp
//Model
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


//Action
string schemaInJsonFormat = AvroConvert.GenerateSchema(typeof(AttributeClass));


//Produces following schema:
"{"type":"record","name":"user.User","fields":[{"name":"name","type":["null","string"]},{"name":"favorite_number","type":["null","int"]},{"name":"favorite_color","type":["null","string"]}]}"
```  



### Reading Avro schema from Avro encoded object
```csharp
string schemaInJsonFormat = AvroConvert.GetSchema(byte[] avroObject)
```