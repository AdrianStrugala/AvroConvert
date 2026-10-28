using System.CommandLine;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Confluent.SchemaRegistry;

namespace SolTechnology.Avro.Cli;

/// <summary>
/// <c>avroconvert</c>: AvroConvert on the command line. Exit codes: 0 success, 1 the check failed (incompatible schema),
/// 2 an error (bad arguments, missing file, unreachable registry).
/// </summary>
public static class Program
{
    internal const int Success = 0;
    internal const int CheckFailed = 1;
    internal const int Error = 2;

    /// <summary>Creates the Schema Registry client; replaced in tests.</summary>
    internal static Func<SchemaRegistryConfig, ISchemaRegistryClient> RegistryClientFactory { get; set; } =
        config => new CachedSchemaRegistryClient(config);

    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

    public static Task<int> Main(string[] args) => RunAsync(args, Console.Out, Console.Error);

    internal static async Task<int> RunAsync(string[] args, TextWriter output, TextWriter error)
    {
        var root = BuildCommand(output);
        try
        {
            return await root.Parse(args).InvokeAsync(new InvocationConfiguration { Output = output, Error = error, EnableDefaultExceptionHandler = false });
        }
        catch (CliException e)
        {
            await error.WriteLineAsync(e.Message);
            return Error;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or SchemaRegistryException or HttpRequestException or ArgumentException or FormatException or JsonException)
        {
            await error.WriteLineAsync($"{e.GetType().Name}: {e.Message}");
            return Error;
        }
        catch (Exception e) when (e.InnerException is SchemaRegistryException or HttpRequestException)
        {
            await error.WriteLineAsync($"{e.InnerException.GetType().Name}: {e.InnerException.Message}");
            return Error;
        }
        catch (Exception e)
        {
            // Anything else (an invalid schema, a file that is not an assembly, data that does not match): exit code 2, no stack trace.
            await error.WriteLineAsync($"{e.GetType().Name}: {e.Message}");
            return Error;
        }
    }

    internal static RootCommand BuildCommand(TextWriter output)
    {
        var root = new RootCommand("AvroConvert on the command line. Commands are named like in Apache avro-tools. Docs: https://github.com/AdrianStrugala/AvroConvert/blob/master/docs/Documentation.Cli.md");
        root.Subcommands.Add(InduceCommand(output));
        root.Subcommands.Add(CompileCommand(output));
        root.Subcommands.Add(ToJsonCommand(output));
        root.Subcommands.Add(GetSchemaCommand(output));
        root.Subcommands.Add(CanonicalCommand(output));
        root.Subcommands.Add(FingerprintCommand(output));
        root.Subcommands.Add(CompatCommand(output));
        root.Subcommands.Add(RegistryCommand(output));
        return root;
    }

    // avroconvert induce bin/Release/net10.0/MyApp.dll MyApp.Order
    private static Command InduceCommand(TextWriter output)
    {
        var assembly = new Argument<string>("assembly") { Description = "Path to the built assembly (.dll) that contains the type." };
        var type = new Argument<string>("type") { Description = "Full name of the type, e.g. MyApp.Contracts.Order (the short name works when it is unique)." };
        var compact = CompactOption();
        var command = new Command("induce", "Avro schema of a .NET type, via reflection.") { assembly, type, compact };
        command.SetAction(parse =>
        {
            var schema = AvroSchema.Create(TypeLoader.Load(parse.GetValue(assembly)!, parse.GetValue(type)!));
            output.WriteLine(Format(schema.Json, parse.GetValue(compact)));
            return Success;
        });
        return command;
    }

    // avroconvert compile schema order.avsc customer.avsc src/Generated     |     avroconvert compile schema data.avro -
    private static Command CompileCommand(TextWriter output)
    {
        var kind = new Argument<string>("schema") { Description = "Always 'schema' (Avro IDL protocols are not supported)." };
        var paths = new Argument<string[]>("input... outputdir")
        {
            Description = "Schema files (.avsc, or .avro to use the file's schema, or - for standard input), then the directory for the C# files (- for standard output).",
            Arity = new ArgumentArity(2, 100_000)
        };
        var command = new Command("compile", "Generates C# classes from Avro schemas.") { kind, paths };
        command.SetAction(parse =>
        {
            if (parse.GetValue(kind) != "schema")
            {
                throw new CliException($"Expected 'schema', got '{parse.GetValue(kind)}'. Avro IDL protocols are not supported.");
            }

            var all = parse.GetValue(paths)!;
            var outputDir = all[^1];
            foreach (var input in all[..^1])
            {
                var bytes = ReadInput(input);
                var code = IsContainerFile(bytes) ? AvroConvert.GenerateModel(bytes) : AvroConvert.GenerateModel(Encoding.UTF8.GetString(bytes));
                if (outputDir == "-")
                {
                    output.WriteLine(code);
                    continue;
                }

                Directory.CreateDirectory(outputDir);
                var schemaJson = IsContainerFile(bytes) ? AvroConvert.GetSchema(bytes) : Encoding.UTF8.GetString(bytes);
                var path = Path.Combine(outputDir, FileNameFor(schemaJson, input) + ".cs");
                File.WriteAllText(path, code);
                output.WriteLine($"Written to {Path.GetFullPath(path)}");
            }

            return Success;
        });
        return command;
    }

    // avroconvert tojson data.avro [--pretty] [--head [N]]
    private static Command ToJsonCommand(TextWriter output)
    {
        var input = new Argument<string>("input-file") { Description = "An .avro file, or - for standard input." };
        var pretty = new Option<bool>("--pretty") { Description = "Turns on pretty printing." };
        var head = new Option<int?>("--head") { Description = $"Converts the first X records (default is {DefaultHead}).", Arity = ArgumentArity.ZeroOrOne };
        var command = new Command("tojson", "Dumps an Avro data file as JSON, record per line or pretty.") { input, pretty, head };
        command.SetAction(parse =>
        {
            var json = AvroConvert.Avro2Json(ReadContainer(parse.GetValue(input)!));

            var records = JsonNode.Parse(json) is JsonArray array ? array.ToList() : [JsonNode.Parse(json)];
            if (parse.GetResult(head) != null)
            {
                var count = parse.GetValue(head) ?? DefaultHead;
                if (count < 0)
                {
                    throw new CliException("--head count must not be negative.");
                }

                records = records.Take(count).ToList();
            }

            foreach (var record in records)
            {
                output.WriteLine(record == null ? "null" : parse.GetValue(pretty) ? record.ToJsonString(Indented) : record.ToJsonString());
            }

            return Success;
        });
        return command;
    }

    private const int DefaultHead = 10;

    // avroconvert getschema data.avro
    private static Command GetSchemaCommand(TextWriter output)
    {
        var input = new Argument<string>("input-file") { Description = "An .avro file, or - for standard input." };
        var command = new Command("getschema", "Prints out schema of an Avro data file.") { input };
        command.SetAction(parse =>
        {
            output.WriteLine(Format(AvroConvert.GetSchema(ReadContainer(parse.GetValue(input)!)), compact: false));
            return Success;
        });
        return command;
    }

    // avroconvert canonical order.avsc [out.avsc]
    private static Command CanonicalCommand(TextWriter output)
    {
        var input = new Argument<string>("infile") { Description = "Schema file (.avsc or .avro), or - for standard input." };
        var outfile = new Argument<string?>("outfile") { Description = "Output file, or - for standard output (the default).", Arity = ArgumentArity.ZeroOrOne };
        var command = new Command("canonical", "Converts an Avro schema to its parsing canonical form.") { input, outfile };
        command.SetAction(parse =>
        {
            var canonical = ReadSchema(parse.GetValue(input)!).CanonicalForm;
            var target = parse.GetValue(outfile);
            if (target is null or "-")
            {
                output.WriteLine(canonical);
            }
            else
            {
                File.WriteAllText(target, canonical);
            }

            return Success;
        });
        return command;
    }

    // avroconvert fingerprint order.avsc customer.avsc [--fingerprint SHA-256]
    private static Command FingerprintCommand(TextWriter output)
    {
        var inputs = new Argument<string[]>("input-files") { Description = "Schema files (.avsc or .avro), or - for standard input.", Arity = ArgumentArity.OneOrMore };
        var algorithm = new Option<string>("--fingerprint")
        {
            Description = "Fingerprint algorithm: CRC-64-AVRO (64-bit, the default), MD5 (128-bit) or SHA-256 (256-bit).",
            DefaultValueFactory = _ => "CRC-64-AVRO"
        };
        algorithm.AcceptOnlyFromAmong("CRC-64-AVRO", "MD5", "SHA-256");
        var command = new Command("fingerprint", "Returns the fingerprint of the parsing canonical form of each schema.") { inputs, algorithm };
        command.SetAction(parse =>
        {
            var kind = parse.GetValue(algorithm) switch
            {
                "MD5" => AvroFingerprintAlgorithm.Md5,
                "SHA-256" => AvroFingerprintAlgorithm.Sha256,
                _ => AvroFingerprintAlgorithm.Crc64Avro
            };

            foreach (var input in parse.GetValue(inputs)!)
            {
                output.WriteLine($"{Convert.ToHexStringLower(ReadSchema(input).GetFingerprint(kind))} {input}");
            }

            return Success;
        });
        return command;
    }

    // avroconvert compat v1.avsc v2.avsc --mode Backward
    private static Command CompatCommand(TextWriter output)
    {
        var previous = new Argument<string>("previous") { Description = "The schema in use: .avsc or .avro file." };
        var next = new Argument<string>("next") { Description = "The new schema: .avsc or .avro file." };
        var mode = ModeOption();
        var command = new Command("compat", "Checks whether the next schema is compatible with the previous one. Exit code 1 when it is not.") { previous, next, mode };
        command.SetAction(parse =>
        {
            var result = AvroSchema.CheckCompatibility(ReadSchema(parse.GetValue(previous)!), ReadSchema(parse.GetValue(next)!), parse.GetValue(mode));
            return Report(result, output);
        });
        return command;
    }

    private static Command RegistryCommand(TextWriter output)
    {
        var registry = new Command("registry", "Confluent Schema Registry (also Redpanda, Karapace and other compatible registries).");

        // avroconvert registry get --url http://localhost:8081 --subject orders-value [--version 3 | --id 42]
        var url = UrlOption();
        var userInfo = UserInfoOption();
        var subject = new Option<string?>("--subject", "-s") { Description = "Subject, e.g. orders-value." };
        var version = new Option<int?>("--version", "-v") { Description = "Version of the subject; the latest when omitted." };
        var id = new Option<int?>("--id") { Description = "Global schema ID, instead of --subject." };
        var compact = CompactOption();
        var get = new Command("get", "Prints a registered schema.") { url, userInfo, subject, version, id, compact };
        get.SetAction(async (parse, _) =>
        {
            using var client = CreateClient(parse.GetValue(url), parse.GetValue(userInfo));
            string json;
            if (parse.GetValue(id) is { } schemaId)
            {
                json = (await client.GetSchemaAsync(schemaId)).SchemaString;
            }
            else
            {
                var name = parse.GetValue(subject) ?? throw new CliException("Give --subject or --id.");
                json = parse.GetValue(version) is { } v
                    ? (await client.GetRegisteredSchemaAsync(name, v)).SchemaString
                    : (await client.GetLatestSchemaAsync(name)).SchemaString;
            }

            await output.WriteLineAsync(Format(json, parse.GetValue(compact)));
            return Success;
        });
        registry.Subcommands.Add(get);

        // avroconvert registry test --url ... --subject orders-value --assembly bin/.../MyApp.dll --type MyApp.Order
        var testUrl = UrlOption();
        var testUserInfo = UserInfoOption();
        var testSubject = new Option<string>("--subject", "-s") { Description = "Subject, e.g. orders-value.", Required = true };
        var assembly = AssemblyOption();
        var type = TypeOption();
        var test = new Command("test", "Asks the registry whether the schema of a .NET type can be registered under the subject (its compatibility setting decides). Exit code 1 when it cannot.")
        {
            testUrl, testUserInfo, testSubject, assembly, type
        };
        test.SetAction(async (parse, _) =>
        {
            var schema = AvroSchema.Create(TypeLoader.Load(parse.GetValue(assembly)!, parse.GetValue(type)!));
            var name = parse.GetValue(testSubject)!;
            using var client = CreateClient(parse.GetValue(testUrl), parse.GetValue(testUserInfo));

            RegisteredSchema latest;
            try
            {
                latest = await client.GetLatestSchemaAsync(name);
            }
            catch (SchemaRegistryException e) when (e.ErrorCode == 40401)
            {
                await output.WriteLineAsync($"Subject '{name}' has no versions yet, so any schema can be registered.");
                return Success;
            }

            if (await client.IsCompatibleAsync(name, new Schema(schema.Json, SchemaType.Avro)))
            {
                await output.WriteLineAsync($"Compatible with '{name}' (latest version {latest.Version}, id {latest.Id}).");
                return Success;
            }

            await output.WriteLineAsync($"Not compatible with '{name}' (latest version {latest.Version}, id {latest.Id}). Differences against the latest version:");
            var local = AvroSchema.CheckCompatibility(AvroSchema.Parse(latest.SchemaString), schema, AvroCompatibilityMode.Full);
            foreach (var difference in local.Incompatibilities)
            {
                await output.WriteLineAsync($"  {difference}");
            }

            return CheckFailed;
        });
        registry.Subcommands.Add(test);

        return registry;
    }

    private static Option<string> AssemblyOption() =>
        new("--assembly", "-a") { Description = "Path to the built assembly (.dll) that contains the type.", Required = true };

    private static Option<string> TypeOption() =>
        new("--type", "-t") { Description = "Full name of the type, e.g. MyApp.Contracts.Order (the short name works when it is unique).", Required = true };

    private static Option<bool> CompactOption() =>
        new("--compact") { Description = "Print JSON on one line." };

    private static Option<AvroCompatibilityMode> ModeOption() =>
        new("--mode", "-m") { Description = "Backward (new readers, old data), Forward, Full, or their Transitive forms.", DefaultValueFactory = _ => AvroCompatibilityMode.Backward };

    private static Option<string?> UrlOption() =>
        new("--url", "-u") { Description = "Schema Registry URL. Default: the SCHEMA_REGISTRY_URL environment variable." };

    private static Option<string?> UserInfoOption() =>
        new("--user-info") { Description = "Basic auth as user:password (Confluent Cloud: API key:secret). Default: the SCHEMA_REGISTRY_USER_INFO environment variable." };

    private static ISchemaRegistryClient CreateClient(string? url, string? userInfo)
    {
        url ??= Environment.GetEnvironmentVariable("SCHEMA_REGISTRY_URL");
        userInfo ??= Environment.GetEnvironmentVariable("SCHEMA_REGISTRY_USER_INFO");
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new CliException("Give the registry URL with --url or the SCHEMA_REGISTRY_URL environment variable.");
        }

        var config = new SchemaRegistryConfig { Url = url };
        if (!string.IsNullOrEmpty(userInfo))
        {
            config.BasicAuthCredentialsSource = AuthCredentialsSource.UserInfo;
            config.BasicAuthUserInfo = userInfo;
        }

        return RegistryClientFactory(config);
    }

    private static int Report(SchemaCompatibility result, TextWriter output)
    {
        if (result.IsCompatible)
        {
            output.WriteLine($"Compatible ({result.Mode}).");
            return Success;
        }

        output.WriteLine($"Not compatible ({result.Mode}):");
        foreach (var difference in result.Incompatibilities)
        {
            output.WriteLine($"  {difference}");
        }

        return CheckFailed;
    }

    private static AvroSchema ReadSchema(string path)
    {
        var bytes = ReadInput(path);
        return AvroSchema.Parse(IsContainerFile(bytes) ? AvroConvert.GetSchema(bytes) : Encoding.UTF8.GetString(bytes));
    }

    private static byte[] ReadInput(string path)
    {
        if (path == "-")
        {
            using var stdin = Console.OpenStandardInput();
            using var buffer = new MemoryStream();
            stdin.CopyTo(buffer);
            return buffer.ToArray();
        }

        if (!File.Exists(path))
        {
            throw new CliException($"File '{path}' does not exist.");
        }

        return File.ReadAllBytes(path);
    }

    private static byte[] ReadContainer(string path)
    {
        var bytes = ReadInput(path);
        return IsContainerFile(bytes)
            ? bytes
            : throw new CliException("This is not an Avro data file (it does not start with 'Obj' 0x01). Data without a header needs its schema; use AvroConvert Online or DeserializeHeadless.");
    }

    // The record name of the schema (Order for {"name":"shop.Order", ...}); the input file name for anything else.
    private static string FileNameFor(string schemaJson, string input)
    {
        var name = JsonNode.Parse(schemaJson) is JsonObject root && root["name"]?.GetValue<string>() is { } full ? full[(full.LastIndexOf('.') + 1)..] : null;
        return name ?? (input == "-" ? "Model" : Path.GetFileNameWithoutExtension(input));
    }

    private static bool IsContainerFile(byte[] bytes) =>
        bytes.Length >= 4 && bytes[0] == (byte)'O' && bytes[1] == (byte)'b' && bytes[2] == (byte)'j' && bytes[3] == 1;

    private static string Format(string json, bool compact)
    {
        var node = JsonNode.Parse(json);
        return compact ? node!.ToJsonString() : node!.ToJsonString(Indented);
    }
}
