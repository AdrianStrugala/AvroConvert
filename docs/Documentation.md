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
- Brotli


### Deserialization

```csharp
//Using generic method
CustomClass deserializedObject = AvroConvert.Deserialize<CustomClass>(byte[] avroObject);

//Using dynamic method
CustomClass deserializedObject = AvroConvert.Deserialize(byte[] avroObject, typeof(CustomClass));

//Deserialization to dynamic result
dynamic deserializedObject = AvroConvert.Deserialize<dynamic>(byte[] avroObject);
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

Deserialization of large collection of Avro objects one by one
```csharp
using (var reader = AvroConvert.OpenDeserializer<CustomClass>(new MemoryStream(avroObject)))
{
    while (reader.HasNext())
    {
        var item = reader.ReadNext();
        // process item
    }
}
```

### Reading Avro schema from Avro encoded object
```csharp
string schemaInJsonFormat = AvroConvert.GetSchema(byte[] avroObject)
```


### Avro2Json

Converts Avro serialized object directly to JSON format. Useful for a minimal API approach

```csharp

var resultJson = AvroConvert.Avro2Json(byte[] avroData);


example:
//Arrange
var user = _fixture.Creat<User>();
var avroData = AvroConvert.Serialize(user);


//Act 
var resultJson = AvroConvert.Avro2Json(avroData);


//Assert
var expectedJson = JsonConvert.SerializeObject(user);
Assert.Equal(expectedJson, resultJson);

```



### Generate Model

Generates C# .NET classes (models) from Avro file or schema.
This action is inversed to GenerateSchema.

```csharp
//Given is schema
var schema = AvroConvert.GenerateSchema(typeof(User));

{
   "type":"record",
   "name":"AvroConvertComponentTests.User",
   "fields":[
      {
         "name":"name",
         "type":"string"
      },
      {
         "name":"favorite_number",
         "type":[
            "null",
            "int"
         ]
      },
      {
         "name":"favorite_color",
         "type":"string"
      }
   ]
}

//Action
string resultModel = AvroConvert.GenerateModel(schema); //string

OR

string resultModel = AvroConvert.GenerateModel(avroFileContent); //byte[]

//Produces following model:
public class User
{
    public string name { get; set; }
    public int? favorite_number { get; set; }
    public string? favorite_color { get; set; }
}


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
{
   "type":"record",
   "name":"AvroConvert.SimpleTestClass",
   "fields":[
      {
         "name":"justSomeProperty",
         "type":[
            "null",
            "string"
         ]
      },
      {
         "name":"andLongProperty",
         "type":"long"
      }
   ]
}
```

Using class decorated with attributes
```csharp
//Model
[DataContract(Name = "User", Namespace = "userspace")]
[Description("This is Doc of User Class")]
public class AttributeClass
{
	[DataMember(Name = "name")]
	[Description("This is Doc of record field")]
	public string StringProperty { get; set; }

	[DataMember(Name = "favorite_number")]
	[DefaultValue(2137)]
	public int? NullableIntPropertyWithDefaultValue { get; set; }

	[DataMember(Name = "not_favorite_number")]
	[NullableSchema]
	public int AnotherWayOrIndicatingNullableProperty { get; set; }

	[DataMember(Name = "favorite_color")]
	[NullableSchema]
	public string AndAnotherString { get; set; }
}


//Action
string schemaInJsonFormat = AvroConvert.GenerateSchema(typeof(AttributeClass));


//Produces following schema:
{
  "type": "record",
  "name": "userspace.User",
  "doc": "This is Doc of User Class",
  "fields": [
    {
      "name": "name",
      "doc": "This is Doc of record field",
      "aliases": [
        "StringProperty"
      ],
      "type": "string"
    },
    {
      "name": "favorite_number",
      "aliases": [
        "NullableIntPropertyWithDefaultValue"
      ],
      "type": [
        "int",
        "null"
      ],
      "default": 2137
    },
    {
      "name": "not_favorite_number",
      "aliases": [
        "AnotherWayOrIndicatingNullableProperty"
      ],
      "type": [
        "null",
        "int"
      ]
    },
    {
      "name": "favorite_color",
      "aliases": [
        "AndAnotherString"
      ],
      "type": [
        "null",
        "string"
      ]
    }
  ]
}
```  





### Headless serialization and deserialization

**Schemas provided for serialization and deserialization have to be *exactly* the same**

```csharp

string schema = "{"type":"record","name":"user.User","fields":[{"name":"name","type":,"string"},{"name":"favorite_number","type":["null","int"]},{"name":"favorite_color","type":["null","string"]}]}";

var serialized = AvroConvert.SerializeHeadless(toSerialize, schema);

var deserialized = AvroConvert.DeserializeHeadless<User>(serialized, schema);

```


### Merge

Merges multiple Avro objects of type T into one of type IEnumerable of T

```csharp

byte[] result = AvroConvert.Merge<T>(IEnumerable<byte[]> avroObjects);

example:
//Arrange
var users = _fixture.CreateMany<User>();
var avroObjects = users.Select(AvroConvert.Serialize);


//Act 
var result = AvroConvert.Merge<User>(avroObjects);


//Assert
var deserializedResult = AvroConvert.Deserialize<List<User>>(result);

```

