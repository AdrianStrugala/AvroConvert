#!/usr/bin/env bash
# Builds, tests and packs AvroConvert and its satellite packages into ./artifacts.
# Version is the single <Version> in each csproj. Publish afterwards with:
#   dotnet nuget push artifacts/*.nupkg --source https://api.nuget.org/v3/index.json --api-key "$NUGET_API_KEY"
set -euo pipefail
cd "$(dirname "$0")/.."

CONFIGURATION="${CONFIGURATION:-Release}"
OUTPUT="${OUTPUT:-artifacts}"

if [[ "${SKIP_TESTS:-0}" != "1" ]]; then
  dotnet test AvroConvert.sln -c "$CONFIGURATION"
fi

rm -rf "$OUTPUT"
for project in \
  src/AvroConvert/AvroConvert.csproj \
  src/SolTechnology.Avro.Http/SolTechnology.Avro.Http.csproj \
  src/Soltechnology.Avro.Kafka/SolTechnology.Avro.Kafka.csproj; do
  dotnet pack "$project" -c "$CONFIGURATION" -o "$OUTPUT"
done

ls -l "$OUTPUT"
