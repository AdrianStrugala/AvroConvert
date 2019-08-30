
[![Nuget](https://img.shields.io/badge/Nuget-v1.8.0-blue?logo=nuget)](https://www.nuget.org/packages/AvroConvert)
[![Github](https://img.shields.io/badge/Downloads-3k-blue?logo=github)](https://github.com/AdrianStrugala/AvroConvert)

# AvroConvert

**Avro format combines readability of JSON format and compression of binary data serialization.**
<br></br>

The main purpose of the project was to enhance communication between microservices. Replacing JSON with Avro brought two main benefits:
* Reduced amount of data send via HTTP by about 30%
* Increased communication security - the data was not visible in plain JSON text


## Documentation

General information: http://avro.apache.org/
<br></br>
Wiki: https://cwiki.apache.org/confluence/display/AVRO/Index

## Code samples

#### Serialization
```csharp
 byte[] avroObject = AvroConvert.Serialize(object yourObject);
```

#### Deserialization

Deserialization to known type
```csharp
CustomClass deserializedObject = AvroConvert.Deserialize<CustomClass>(byte[] avroObject);
```

Deserialization to map of property names and values
```csharp
Dictionary<string, object> mapOfPropertiesAndValues = AvroConvert.Deserialize(byte[] avroObject);  
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
```

#### Generating Avro schema for C# classes

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

#### Reading Avro schema from Avro encoded object
```csharp
  string schemaInJsonFormat = AvroConvert.GetSchema(byte[] avroObject)
```

## Contribution

We want to improve AvroConvert as much as possible. If you have any idea, found next possible feature, optimization opportunity or better way for integration, leave a comment or pull request. 
