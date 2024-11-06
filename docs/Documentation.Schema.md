### Reading Avro schema from Avro encoded object
```csharp
string schemaInJsonFormat = AvroConvert.GetSchema(byte[] avroObject)
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
