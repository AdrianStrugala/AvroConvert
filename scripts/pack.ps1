#!/usr/bin/env pwsh
# Builds, tests and packs AvroConvert and its satellite packages into ./artifacts.
# Version is the single <Version> in each csproj. Publish afterwards with:
#   dotnet nuget push artifacts/*.nupkg --source https://api.nuget.org/v3/index.json --api-key $env:NUGET_API_KEY
param(
    [string]$Configuration = 'Release',
    [string]$Output = 'artifacts',
    [switch]$SkipTests
)

$ErrorActionPreference = 'Stop'
Set-Location (Join-Path $PSScriptRoot '..')

if (-not $SkipTests) {
    dotnet test AvroConvert.sln -c $Configuration
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

Remove-Item -Recurse -Force $Output -ErrorAction SilentlyContinue
foreach ($project in @(
    'src/AvroConvert/AvroConvert.csproj',
    'src/SolTechnology.Avro.Http/SolTechnology.Avro.Http.csproj',
    'src/Soltechnology.Avro.Kafka/SolTechnology.Avro.Kafka.csproj')) {
    dotnet pack $project -c $Configuration -o $Output
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

Get-ChildItem $Output
