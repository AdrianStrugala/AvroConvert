# Command line: SolTechnology.Avro.Cli

`avroconvert` is a .NET tool for the most common Avro jobs. The commands have the same names as in Apache [avro-tools](https://github.com/apache/avro/tree/main/lang/java/tools), so if you know `java -jar avro-tools.jar tojson`, you know `avroconvert tojson`. On top of that: schema compatibility and Schema Registry checks for CI.

```
dotnet tool install --global SolTechnology.Avro.Cli --prerelease
avroconvert --help
```

| Command | What it does | avro-tools |
|---|---|---|
| `induce <assembly> <type>` | Avro schema of a .NET type | `induce` (from a Java class) |
| `compile schema <input>... <outputdir>` | C# classes from schemas | `compile schema` (Java classes) |
| `tojson <file> [--pretty] [--head [N]]` | `.avro` file as JSON, one record per line | `tojson` |
| `getschema <file>` | writer schema of an `.avro` file | `getschema` |
| `canonical <infile> [outfile]` | parsing canonical form | `canonical` |
| `fingerprint <files>... [--fingerprint ALG]` | CRC-64-AVRO, MD5 or SHA-256 of the canonical form | `fingerprint` |
| `compat <previous> <next> [--mode M]` | schema compatibility | – |
| `registry get`, `registry test` | Confluent Schema Registry | – |

Exit codes: `0` success, `1` the check failed (the schemas are not compatible), `2` an error (wrong arguments, missing file, unreachable registry). So `compat` and `registry test` can stop a build. Wherever a file is expected, `-` means standard input (or standard output for the output of `compile` and `canonical`). Schema arguments accept `.avsc` files and `.avro` files (their header schema is used).

## induce: Avro schema of a .NET type

```
avroconvert induce bin/Release/net10.0/MyApp.dll MyApp.Contracts.Order
```

The type is loaded from the built assembly, together with its dependencies (from the `.deps.json` next to it). The short name works when it is unique: `Order`. `--compact` prints the schema on one line.

## compile: C# classes from schemas

```
avroconvert compile schema order.avsc customer.avsc src/Generated      # Order.cs, Customer.cs
avroconvert compile schema data.avro -                                   # the schema from the file header, to standard output
curl -s $SCHEMA_REGISTRY_URL/subjects/orders-value/versions/latest/schema | avroconvert compile schema - -
```

One `.cs` file per input, named after the top-level record. Avro IDL protocols are not supported.

## tojson and getschema: look into an .avro file

```
avroconvert tojson data.avro                 # one record per line
avroconvert tojson data.avro --pretty --head # the first 10 records, indented
avroconvert getschema data.avro
```

Data without a header (a Kafka message, a BigQuery row) needs its schema, so it is not supported here; use `DeserializeHeadless` or the [Kafka message decoder](https://soltechnology.dev/avroconvert/online/kafka-message-decoder/). Unlike avro-tools, `tojson` has no `--reader-schema` yet.

## canonical and fingerprint

```
$ avroconvert canonical order.avsc
{"name":"shop.Order","type":"record","fields":[{"name":"Id","type":"int"},{"name":"Customer","type":"string"}]}

$ avroconvert fingerprint order.avsc
5a50a2cc6fc02e36 order.avsc

$ avroconvert fingerprint order.avsc --fingerprint SHA-256
```

The fingerprint is calculated from the parsing canonical form, so `doc` and `default` do not change it.

## compat: is the new schema compatible?

```
avroconvert compat v1.avsc v2.avsc                  # Backward: new readers can read old data
avroconvert compat v1.avsc v2.avsc --mode Full
```

Modes: `Backward` (default), `Forward`, `Full`, `BackwardTransitive`, `ForwardTransitive`, `FullTransitive`. Example output:

```
Not compatible (Backward):
  Order.Note: field added without a default (breaking)
```

## registry: Confluent Schema Registry

Works with Confluent Platform, Confluent Cloud, and registries with the same API (Redpanda, Karapace). The URL and credentials come from options or from the `SCHEMA_REGISTRY_URL` and `SCHEMA_REGISTRY_USER_INFO` (`user:password`, for Confluent Cloud `key:secret`) environment variables.

```
avroconvert registry get --subject orders-value              # latest version
avroconvert registry get --subject orders-value --version 3
avroconvert registry get --id 42
```

`registry test` asks the registry whether the schema of a .NET type could be registered under the subject. The compatibility level of the subject decides. When it cannot, the differences against the latest version are listed and the exit code is `1`:

```
avroconvert registry test --subject orders-value --assembly bin/Release/net10.0/MyApp.dll --type MyApp.Contracts.Order
```

A step in CI, before deploying a producer:

```yaml
- run: dotnet tool install --global SolTechnology.Avro.Cli --prerelease
- run: avroconvert registry test -s orders-value -a src/MyApp/bin/Release/net10.0/MyApp.dll -t MyApp.Contracts.Order
  env:
    SCHEMA_REGISTRY_URL: ${{ secrets.SCHEMA_REGISTRY_URL }}
    SCHEMA_REGISTRY_USER_INFO: ${{ secrets.SCHEMA_REGISTRY_USER_INFO }}
```

## Licence

The tool is part of AvroConvert and has the same licence: free for non-commercial use and for companies under 100 people and USD 1M revenue; other companies need a commercial licence ([soltechnology.dev/avroconvert](https://soltechnology.dev/avroconvert/)). Without a declared licence it prints one line to standard error; set `AVROCONVERT_LICENSE` (`NonCommercial`, `SmallBusiness` or the key) to silence it. Standard output stays clean for pipes.
