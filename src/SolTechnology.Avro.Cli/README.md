# SolTechnology.Avro.Cli

`avroconvert` on the command line, with the command names of Apache avro-tools (`induce`, `compile`, `tojson`, `getschema`, `canonical`, `fingerprint`), plus schema compatibility and Confluent Schema Registry checks for CI. Built on [AvroConvert](https://www.nuget.org/packages/AvroConvert).

```
dotnet tool install --global SolTechnology.Avro.Cli --prerelease
```

```
avroconvert induce      bin/Release/net10.0/MyApp.dll MyApp.Contracts.Order
avroconvert compile     schema order.avsc src/Generated
avroconvert tojson      data.avro --pretty --head
avroconvert getschema   data.avro
avroconvert fingerprint order.avsc
avroconvert compat      v1.avsc v2.avsc --mode Backward
avroconvert registry test --subject orders-value --assembly bin/Release/net10.0/MyApp.dll --type MyApp.Contracts.Order
```

Exit codes: `0` success, `1` not compatible, `2` error, so `compat` and `registry test` can stop a build. The registry URL and credentials can also come from the `SCHEMA_REGISTRY_URL` and `SCHEMA_REGISTRY_USER_INFO` environment variables.

## Links

- [Documentation](https://github.com/AdrianStrugala/AvroConvert/blob/master/docs/Documentation.Cli.md)
- [AvroConvert Online](https://soltechnology.dev/avroconvert/online/): the same tools in the browser

## Licence

Free for noncommercial use and for companies under 100 people and USD 1M revenue (PolyForm Noncommercial / Small Business). Other organisations need a commercial licence: [soltechnology.dev/avroconvert](https://soltechnology.dev/avroconvert/). See LICENSE.md in the package.
