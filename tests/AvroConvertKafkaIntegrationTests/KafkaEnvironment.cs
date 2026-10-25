using System;
using System.Threading.Tasks;
using Confluent.SchemaRegistry;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using NUnit.Framework;
using Testcontainers.Kafka;

namespace AvroConvertKafkaIntegrationTests;

/// <summary>One Kafka broker (KRaft) and one Confluent Schema Registry for the whole test run.</summary>
[SetUpFixture]
public class KafkaEnvironment
{
    private const string ConfluentVersion = "7.9.0";

    private static INetwork _network;
    private static KafkaContainer _kafka;
    private static IContainer _schemaRegistry;

    public static string BootstrapServers { get; private set; }

    public static string SchemaRegistryUrl { get; private set; }

    public static ISchemaRegistryClient CreateSchemaRegistryClient() =>
        new CachedSchemaRegistryClient(new SchemaRegistryConfig { Url = SchemaRegistryUrl });

    [OneTimeSetUp]
    public async Task Start()
    {
        try
        {
            _network = new NetworkBuilder().Build();
            _kafka = new KafkaBuilder($"confluentinc/cp-kafka:{ConfluentVersion}")
                .WithNetwork(_network)
                .WithListener("kafka:19092")
                .Build();
            await _kafka.StartAsync();

            _schemaRegistry = new ContainerBuilder($"confluentinc/cp-schema-registry:{ConfluentVersion}")
                .WithNetwork(_network)
                .WithPortBinding(8081, true)
                .WithEnvironment("SCHEMA_REGISTRY_HOST_NAME", "schema-registry")
                .WithEnvironment("SCHEMA_REGISTRY_LISTENERS", "http://0.0.0.0:8081")
                .WithEnvironment("SCHEMA_REGISTRY_KAFKASTORE_BOOTSTRAP_SERVERS", "PLAINTEXT://kafka:19092")
                .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(8081).ForPath("/subjects")))
                .Build();
            await _schemaRegistry.StartAsync();
        }
        catch (Exception ex) when (Environment.GetEnvironmentVariable("CI") == null && IsDockerUnavailable(ex))
        {
            Assert.Ignore($"Docker is not available, Kafka integration tests skipped: {ex.Message}");
        }

        BootstrapServers = _kafka.GetBootstrapAddress();
        SchemaRegistryUrl = $"http://{_schemaRegistry.Hostname}:{_schemaRegistry.GetMappedPublicPort(8081)}";
    }

    [OneTimeTearDown]
    public async Task Stop()
    {
        if (_schemaRegistry != null) await _schemaRegistry.DisposeAsync();
        if (_kafka != null) await _kafka.DisposeAsync();
        if (_network != null) await _network.DisposeAsync();
    }

    private static bool IsDockerUnavailable(Exception ex) =>
        ex.GetType().Name.Contains("DockerUnavailable") || ex.Message.Contains("Docker", StringComparison.OrdinalIgnoreCase);
}
