using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Confluent.SchemaRegistry;
using SolTechnology.Avro;

namespace AvroConvertCliTests;

/// <summary>In-memory Schema Registry with the calls the CLI makes; compatibility is BACKWARD against the latest version.</summary>
public class FakeSchemaRegistry : DispatchProxy
{
    private readonly List<RegisteredSchema> _schemas = new();

    public static (ISchemaRegistryClient Client, FakeSchemaRegistry Registry) Create()
    {
        var client = Create<ISchemaRegistryClient, FakeSchemaRegistry>();
        return (client, (FakeSchemaRegistry)(object)client);
    }

    public SchemaRegistryConfig LastConfig { get; set; }

    public RegisteredSchema Register(string subject, string schemaString)
    {
        var registered = new RegisteredSchema(subject, _schemas.Count(s => s.Subject == subject) + 1, _schemas.Count + 1,
            Guid.NewGuid().ToString(), schemaString, SchemaType.Avro, new List<SchemaReference>());
        _schemas.Add(registered);
        return registered;
    }

    protected override object Invoke(MethodInfo method, object[] args)
    {
        switch (method.Name)
        {
            case "Dispose":
                return null;
            case "GetLatestSchemaAsync":
            {
                var latest = _schemas.LastOrDefault(s => s.Subject == (string)args[0]);
                return latest != null
                    ? Task.FromResult(latest)
                    : Task.FromException<RegisteredSchema>(new SchemaRegistryException("Subject not found", HttpStatusCode.NotFound, 40401));
            }
            case "GetRegisteredSchemaAsync":
                return Task.FromResult(_schemas.First(s => s.Subject == (string)args[0] && s.Version == (int)args[1]));
            case "GetSchemaAsync" when args[0] is int id:
                return Task.FromResult<Schema>(_schemas.First(s => s.Id == id));
            case "IsCompatibleAsync" when args[1] is Schema schema:
            {
                var latest = _schemas.Last(s => s.Subject == (string)args[0]);
                return Task.FromResult(AvroSchema.CheckCompatibility(AvroSchema.Parse(latest.SchemaString), AvroSchema.Parse(schema.SchemaString), AvroCompatibilityMode.Backward).IsCompatible);
            }
            default:
                throw new NotSupportedException($"FakeSchemaRegistry does not implement {method.Name}");
        }
    }
}
