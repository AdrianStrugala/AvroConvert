using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AwesomeAssertions;
using NUnit.Framework;
using SolTechnology.Avro;
using SolTechnology.Avro.Cli;

[assembly: NonParallelizable]

namespace AvroConvertCliTests
{
    public class Order
    {
        public int Id { get; set; }
        public string Customer { get; set; }
        public List<string> Tags { get; set; }
    }

    public class CliTests
    {
        private string _dir;
        private static string ThisAssembly => typeof(CliTests).Assembly.Location;

        private const string OrderV1 = """{"type":"record","name":"Order","fields":[{"name":"Id","type":"int"},{"name":"Customer","type":"string"}]}""";
        private const string OrderV2WithDefault = """{"type":"record","name":"Order","fields":[{"name":"Id","type":"int"},{"name":"Customer","type":"string"},{"name":"Note","type":"string","default":""}]}""";
        private const string OrderV2Required = """{"type":"record","name":"Order","fields":[{"name":"Id","type":"int"},{"name":"Customer","type":"string"},{"name":"Note","type":"string"}]}""";

        [SetUp]
        public void SetUp()
        {
            AvroConvert.License = AvroLicense.NonCommercial;
            _dir = Path.Combine(Path.GetTempPath(), "avroconvert-cli-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        [TearDown]
        public void TearDown() => Directory.Delete(_dir, recursive: true);

        private static async Task<(int Code, string Output, string Error)> Run(params string[] args)
        {
            var output = new StringWriter();
            var error = new StringWriter();
            var code = await Program.RunAsync(args, output, error);
            return (code, output.ToString(), error.ToString());
        }

        private string File(string name, string text)
        {
            var path = Path.Combine(_dir, name);
            System.IO.File.WriteAllText(path, text);
            return path;
        }

        private string File(string name, byte[] bytes)
        {
            var path = Path.Combine(_dir, name);
            System.IO.File.WriteAllBytes(path, bytes);
            return path;
        }

        [Test]
        public async Task Induce_FromTypeInAssembly()
        {
            var (code, output, _) = await Run("induce", ThisAssembly, "AvroConvertCliTests.Order");

            code.Should().Be(0);
            AvroSchema.Parse(output).Json.Should().Be(AvroSchema.Create<Order>().Json);
            output.Should().Contain("\n");
        }

        [Test]
        public async Task Induce_ShortTypeName_Compact()
        {
            var (code, output, _) = await Run("induce", ThisAssembly, "Order", "--compact");

            code.Should().Be(0);
            output.Trim().Should().Be(AvroSchema.Create<Order>().Json);
        }

        [Test]
        public async Task Induce_UnknownType_IsAnError()
        {
            var (code, _, error) = await Run("induce", ThisAssembly, "Nope");

            code.Should().Be(2);
            error.Should().Contain("Type 'Nope' was not found");
        }

        [Test]
        public async Task Induce_MissingAssembly_IsAnError()
        {
            var (code, _, error) = await Run("induce", Path.Combine(_dir, "missing.dll"), "Order");

            code.Should().Be(2);
            error.Should().Contain("does not exist");
        }

        [Test]
        public async Task Compile_SchemaToStandardOutput()
        {
            var (code, output, _) = await Run("compile", "schema", File("order.avsc", OrderV1), "-");

            code.Should().Be(0);
            output.Should().Contain("public class Order").And.Contain("public string Customer");
        }

        [Test]
        public async Task Compile_SeveralInputsIntoDirectory_OneFilePerRecordName()
        {
            var avro = File("orders.avro", AvroConvert.Serialize(new List<Order> { new() { Id = 1, Customer = "a", Tags = new List<string>() } }));
            var customer = File("customer.avsc", """{"type":"record","name":"shop.Customer","fields":[{"name":"Name","type":"string"}]}""");
            var target = Path.Combine(_dir, "Generated");

            var (code, output, _) = await Run("compile", "schema", avro, customer, target);

            code.Should().Be(0);
            output.Should().Contain("Written to");
            System.IO.File.ReadAllText(Path.Combine(target, "Order.cs")).Should().Contain("public class Order");
            System.IO.File.ReadAllText(Path.Combine(target, "Customer.cs")).Should().Contain("public class Customer");
        }

        [Test]
        public async Task Compile_Protocol_IsAnError()
        {
            var (code, _, error) = await Run("compile", "protocol", File("order.avsc", OrderV1), "-");

            code.Should().Be(2);
            error.Should().Contain("Expected 'schema'");
        }

        [Test]
        public async Task ToJson_RecordPerLine()
        {
            var avro = File("orders.avro", AvroConvert.Serialize(Orders(3)));

            var (code, output, _) = await Run("tojson", avro);

            code.Should().Be(0);
            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            lines.Should().HaveCount(3);
            lines[0].Should().Contain("\"Customer\":\"c0\"");
        }

        [Test]
        public async Task ToJson_Pretty_And_Head()
        {
            var avro = File("orders.avro", AvroConvert.Serialize(Orders(15)));

            var (code, output, _) = await Run("tojson", avro, "--pretty", "--head", "2");
            var defaultHead = await Run("tojson", avro, "--head");

            code.Should().Be(0);
            output.Should().Contain("c1").And.NotContain("c2").And.Contain("\n  ");
            defaultHead.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries).Should().HaveCount(10);
        }

        [Test]
        public async Task ToJson_NotADataFile_IsAnError()
        {
            var (code, _, error) = await Run("tojson", File("raw.bin", new byte[] { 2, 4, 6 }));

            code.Should().Be(2);
            error.Should().Contain("not an Avro data file");
        }

        [Test]
        public async Task GetSchema_PrintsTheWriterSchema()
        {
            var avro = File("orders.avro", AvroConvert.Serialize(Orders(1)));

            var (code, output, _) = await Run("getschema", avro);

            code.Should().Be(0);
            AvroSchema.Parse(output).Json.Should().Be(AvroSchema.Create<Order>().Json);
        }

        [Test]
        public async Task Canonical_ToStandardOutputAndFile()
        {
            var schema = File("order.avsc", OrderV2WithDefault);
            var target = Path.Combine(_dir, "canonical.avsc");

            var (code, output, _) = await Run("canonical", schema);
            var toFile = await Run("canonical", schema, target);

            code.Should().Be(0);
            output.Trim().Should().Be(AvroSchema.Parse(OrderV2WithDefault).CanonicalForm);
            output.Should().NotContain("default");
            toFile.Code.Should().Be(0);
            System.IO.File.ReadAllText(target).Should().Be(AvroSchema.Parse(OrderV2WithDefault).CanonicalForm);
        }

        [Test]
        public async Task Fingerprint_DefaultAndSha256()
        {
            var first = File("v1.avsc", OrderV1);
            var second = File("v2.avsc", OrderV2WithDefault);

            var (code, output, _) = await Run("fingerprint", first, second);
            var sha = await Run("fingerprint", first, "--fingerprint", "SHA-256");

            code.Should().Be(0);
            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            lines[0].Should().Be($"{Convert.ToHexStringLower(AvroSchema.Parse(OrderV1).GetFingerprint(AvroFingerprintAlgorithm.Crc64Avro))} {first}");
            lines[1].Should().EndWith(second);
            sha.Output.Trim().Should().Be($"{Convert.ToHexStringLower(AvroSchema.Parse(OrderV1).GetFingerprint(AvroFingerprintAlgorithm.Sha256))} {first}");
        }

        [Test]
        public async Task Fingerprint_UnknownAlgorithm_IsAnError()
        {
            var (code, _, _) = await Run("fingerprint", File("v1.avsc", OrderV1), "--fingerprint", "CRC-32");

            code.Should().NotBe(0);
        }

        private static List<Order> Orders(int count) =>
            Enumerable.Range(0, count).Select(i => new Order { Id = i, Customer = "c" + i, Tags = new List<string> { "t" } }).ToList();

        [Test]
        public async Task Compat_NewFieldWithDefault_IsBackwardCompatible()
        {
            var (code, output, _) = await Run("compat", File("v1.avsc", OrderV1), File("v2.avsc", OrderV2WithDefault));

            code.Should().Be(0);
            output.Should().Contain("Compatible (Backward)");
        }

        [Test]
        public async Task Compat_NewRequiredField_FailsWithExitCode1()
        {
            var (code, output, _) = await Run("compat", File("v1.avsc", OrderV1), File("v2.avsc", OrderV2Required), "--mode", "Backward");

            code.Should().Be(1);
            output.Should().Contain("Not compatible (Backward)").And.Contain("Note");
        }

        [Test]
        public async Task Compat_ForwardMode()
        {
            var (code, output, _) = await Run("compat", File("v1.avsc", OrderV1), File("v2.avsc", OrderV2Required), "-m", "forward");

            code.Should().Be(0);
            output.Should().Contain("Compatible (Forward)");
        }

        [Test]
        public async Task Compat_MissingFile_IsAnError()
        {
            var (code, _, error) = await Run("compat", Path.Combine(_dir, "nope.avsc"), File("v2.avsc", OrderV1));

            code.Should().Be(2);
            error.Should().Contain("does not exist");
        }

        [Test]
        public async Task UnknownCommand_IsAnError()
        {
            var (code, _, _) = await Run("frobnicate");

            code.Should().NotBe(0);
        }

        [Test]
        public async Task Registry_Get_LatestVersionAndId()
        {
            var (client, registry) = FakeSchemaRegistry.Create();
            Program.RegistryClientFactory = config => { registry.LastConfig = config; return client; };
            registry.Register("orders-value", OrderV1);
            var v2 = registry.Register("orders-value", OrderV2WithDefault);

            var latest = await Run("registry", "get", "--url", "http://registry", "--subject", "orders-value", "--compact", "--user-info", "key:secret");
            var first = await Run("registry", "get", "-u", "http://registry", "-s", "orders-value", "-v", "1", "--compact");
            var byId = await Run("registry", "get", "-u", "http://registry", "--id", v2.Id.ToString(), "--compact");

            latest.Output.Trim().Should().Be(OrderV2WithDefault);
            first.Output.Trim().Should().Be(OrderV1);
            byId.Output.Trim().Should().Be(OrderV2WithDefault);
            registry.LastConfig.Url.Should().Be("http://registry");
        }

        [Test]
        public async Task Registry_Get_WithoutUrl_IsAnError()
        {
            var previous = Environment.GetEnvironmentVariable("SCHEMA_REGISTRY_URL");
            Environment.SetEnvironmentVariable("SCHEMA_REGISTRY_URL", null);
            try
            {
                var (code, _, error) = await Run("registry", "get", "-s", "orders-value");

                code.Should().Be(2);
                error.Should().Contain("SCHEMA_REGISTRY_URL");
            }
            finally
            {
                Environment.SetEnvironmentVariable("SCHEMA_REGISTRY_URL", previous);
            }
        }

        [Test]
        public async Task Registry_Test_CompatibleType()
        {
            var (client, registry) = FakeSchemaRegistry.Create();
            Program.RegistryClientFactory = _ => client;
            registry.Register("orders-value", AvroSchema.Create<Order>().Json);

            var (code, output, _) = await Run("registry", "test", "-u", "http://registry", "-s", "orders-value", "-a", ThisAssembly, "-t", "AvroConvertCliTests.Order");

            code.Should().Be(0);
            output.Should().Contain("Compatible with 'orders-value'");
        }

        [Test]
        public async Task Registry_Test_IncompatibleType_FailsWithExitCode1()
        {
            var (client, registry) = FakeSchemaRegistry.Create();
            Program.RegistryClientFactory = _ => client;
            // The registered version has no Tags; the type adds Tags without a default, so new readers cannot read old data.
            var withoutTags = System.Text.Json.Nodes.JsonNode.Parse(AvroSchema.Create<Order>().Json)!;
            var fields = withoutTags["fields"]!.AsArray();
            fields.Remove(fields.First(f => (string)f!["name"] == "Tags"));
            registry.Register("orders-value", withoutTags.ToJsonString());

            var (code, output, _) = await Run("registry", "test", "-u", "http://registry", "-s", "orders-value", "-a", ThisAssembly, "-t", "AvroConvertCliTests.Order");

            code.Should().Be(1);
            output.Should().Contain("Not compatible").And.Contain("Tags");
        }

        [Test]
        public async Task Registry_Test_NewSubject_IsFine()
        {
            var (client, _) = FakeSchemaRegistry.Create();
            Program.RegistryClientFactory = _ => client;

            var (code, output, _) = await Run("registry", "test", "-u", "http://registry", "-s", "new-value", "-a", ThisAssembly, "-t", "Order");

            code.Should().Be(0);
            output.Should().Contain("has no versions yet");
        }
    }
}
