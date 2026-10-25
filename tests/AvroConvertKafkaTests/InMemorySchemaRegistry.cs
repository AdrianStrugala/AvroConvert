using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Confluent.SchemaRegistry;

namespace AvroConvertKafkaTests;

/// <summary>
/// Minimal in-memory Schema Registry: ids are global, versions per subject, an identical schema under the same subject
/// keeps its id. Only the calls made by the serializers are implemented.
/// </summary>
public class InMemorySchemaRegistry : DispatchProxy
{
    private readonly List<RegisteredSchema> _schemas = new();

    public static (ISchemaRegistryClient Client, InMemorySchemaRegistry Registry) Create()
    {
        var client = Create<ISchemaRegistryClient, InMemorySchemaRegistry>();
        return (client, (InMemorySchemaRegistry)(object)client);
    }

    public IReadOnlyList<RegisteredSchema> Schemas => _schemas;

    public int RegisterCalls { get; private set; }

    public RegisteredSchema Register(string subject, string schemaString)
    {
        RegisterCalls++;
        var existing = _schemas.FirstOrDefault(s => s.Subject == subject && s.SchemaString == schemaString);
        if (existing != null)
        {
            return existing;
        }

        var same = _schemas.FirstOrDefault(s => s.SchemaString == schemaString);
        var id = same?.Id ?? _schemas.Count + 1;
        var guid = same?.Guid ?? Guid.NewGuid().ToString();
        var version = _schemas.Count(s => s.Subject == subject) + 1;
        var registered = new RegisteredSchema(subject, version, id, guid, schemaString, SchemaType.Avro, new List<SchemaReference>());
        _schemas.Add(registered);
        return registered;
    }

    protected override object Invoke(MethodInfo method, object[] args)
    {
        switch (method.Name)
        {
            case "get_Config":
                return Enumerable.Empty<KeyValuePair<string, string>>();
            case "get_MaxCachedSchemas":
                return 1000;
            case "ConstructValueSubjectName":
                return $"{args[0]}-value";
            case "ConstructKeySubjectName":
                return $"{args[0]}-key";
            case "RegisterSchemaWithResponseAsync":
                return Task.FromResult(Register((string)args[0], ((Schema)args[1]).SchemaString));
            case "RegisterSchemaAsync" when args[1] is Schema schema:
                return Task.FromResult(Register((string)args[0], schema.SchemaString).Id);
            case "LookupSchemaAsync":
            {
                var subject = (string)args[0];
                var schemaString = ((Schema)args[1]).SchemaString;
                var found = _schemas.FirstOrDefault(s => s.Subject == subject && s.SchemaString == schemaString);
                return found != null
                    ? Task.FromResult(found)
                    : Task.FromException<RegisteredSchema>(new SchemaRegistryException("Schema not found", System.Net.HttpStatusCode.NotFound, 40403));
            }
            case "GetSchemaBySubjectAndIdAsync":
            case "GetSchemaAsync" when args[0] is int:
            {
                var id = (int)(method.Name == "GetSchemaAsync" ? args[0] : args[1]);
                var found = _schemas.FirstOrDefault(s => s.Id == id);
                return found != null
                    ? Task.FromResult<Schema>(found)
                    : Task.FromException<Schema>(new SchemaRegistryException("Schema not found", System.Net.HttpStatusCode.NotFound, 40403));
            }
            case "GetSchemaByGuidAsync":
            {
                var found = _schemas.FirstOrDefault(s => s.Guid == (string)args[0]);
                return found != null
                    ? Task.FromResult<Schema>(found)
                    : Task.FromException<Schema>(new SchemaRegistryException("Schema not found", System.Net.HttpStatusCode.NotFound, 40403));
            }
            case "GetLatestSchemaAsync":
                return Task.FromResult(_schemas.Last(s => s.Subject == (string)args[0]));
            default:
                throw new NotSupportedException($"InMemorySchemaRegistry does not implement {method.Name}");
        }
    }
}
