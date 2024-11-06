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

### Deserialization of large collection of Avro objects one by one
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


### Avro2Json

Converts Avro serialized object directly to JSON format. Useful for a minimal API approach

```csharp

string resultJson = AvroConvert.Json2Avro(byte[] avroData);


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

### Json2Avro

Converts JSON serialized object directly to Avro format.

```csharp

byte[] resultAvro = Json2Avro<T>(string json, CodecType codecType);

example:
//Arrange
var user = _fixture.Creat<User>();
var jsonData = JsonConvert.SerializeObject(user);


//Act 
var resultAvro = AvroConvert.Json2Avro<User>(jsonData);


//Assert
var expectedAvro = AvroConvert.Serialize(user);
Assert.Equal(expectedAvro, resultAvro);

```