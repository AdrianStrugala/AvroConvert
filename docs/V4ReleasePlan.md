# AvroConvert 4.0 – co zostało do wydania

Zrobione (licencja, `net10.0`, typowany rdzeń, zgodność ze specyfikacją Avro 1.12 S1–S15, issues #51, #69, #73, #87,
#100, #107, #112, #118, #135, #153, #156, #159, #163, #174, API strumieniowe, adnotacje AOT, testy na NUnit) jest
opisane w `CHANGELOG.md` i w dokumentacji – tutaj tylko to, co jeszcze otwarte. Szczegóły licencji i Paddle:
`LicensingMigrationPlan.md` (repo strony, prywatne); wydajność: `PerformanceRewriteAnalysis.md`.

Decyzje, które zostają w mocy:
- S12 (single-object encoding) odrzucone 18.10.2026 – pokrywa to format Confluent w `SolTechnology.Avro.Kafka`;
  wrócić, gdy ktoś poprosi o interop z Java `toByteBuffer()` / `fromByteBuffer()` (fingerprint jest w `AvroSchema.Fingerprint`).
- Newtonsoft.Json zostaje zależnością i w API publicznym (08.10.2026); przeciążenia na `JsonNode` – kandydat na 4.x.

## 1. Do zrobienia przed 4.0.0

- `git push origin release/3.x` – gałąź utrzymaniowa 3.4.x istnieje lokalnie (z `master`, 3.4.17), nie ma jej na `origin`.
  Tylko poprawki bezpieczeństwa i krytyczne (deklaracja w README).
- Przed wydaniem sprawdzić, czy nie wyszła specyfikacja Avro 1.13 – jeśli tak, porównać changelog ze zgodnością S1–S15.

## 2. Po 4.0 (4.x)

| # | Tytuł | Jak |
|---|---|---|
| [26](https://github.com/AdrianStrugala/AvroConvert/issues/26) | Tablice wielowymiarowe `int[,,]` | W 4.0 jawny błąd. Ewentualnie mapowanie na `array<array<...>>` + metadane kształtu; niska wartość |
| [103](https://github.com/AdrianStrugala/AvroConvert/issues/103) | .NET Framework: `FileNotFoundException` | Zamknąć z odpowiedzią: 4.x tylko `net10.0`; na .NET Framework 3.4.x (netstandard2.0) z `AutoGenerateBindingRedirects`/`PackageReference` |
| – | Native AOT bez ostrzeżeń | W 4.0 opcja A: `IsAotCompatible` + `[RequiresUnreferencedCode]`/`[RequiresDynamicCode]` na API refleksyjnym (łącznie z `AvroSchema.Parse` – `default` przez Newtonsoft). B: `[DynamicallyAccessedMembers]` na `Serialize<T>`/`Deserialize<T>` i interpreter planów zamiast `Expression.Compile`; C: source generator |

## 3. Zmiany łamiące w 4.0 (do sekcji "Breaking changes" w CHANGELOG)

- Licencja: CC BY-NC-SA 3.0 → PolyForm NC / Small Business / Commercial.
- **Tylko `net10.0`.** Usunięte `netstandard2.0` i `net6.0`; .NET Framework / .NET 6–9 → pozostać na 3.4.x.
- Usunięty pakiet `SolTechnology.FastMember` (był pakowany do AvroConvert jako zależność wewnętrzna – brak wpływu na użytkowników, chyba że ktoś referował go bezpośrednio).
- Kolekcja top-level serializowana jako N obiektów w kontenerze zamiast jednego rekordu `array` (#118). Dotyczy `List<T>`, `T[]`, `HashSet<T>`, `IEnumerable<T>` itp. z konkretnym typem elementu (nie `object`, nie słowniki). Pliki 3.x (`array`) nadal się czytają. Opcja przywracająca: `AvroConvertOptions.CollectionMode = SingleArray`. Konsekwencja: plik z samym nagłówkiem (0 wpisów) deserializuje się do pustej kolekcji zamiast `null`.
- Brak pola obowiązkowego u pisarza → wyjątek zamiast `default(T)` (#87, S4). Dotyczy tylko pól, które nie mogą przyjąć `null` (typy wartościowe bez `Nullable<>`, bez `[DefaultValue]`, bez unii z `null`); `string`, klasy i `Nullable<T>` dostają `null` jak w 3.x. Opcja przywracająca: `AvroConvertOptions.MissingFieldHandling = UseDefault`.
- Nazwy typów generycznych w schemacie zawierają argumenty generyczne (#159, S5) – schematy wygenerowane w 3.x dla typów generycznych nie są tekstowo identyczne; dane binarne pozostają kompatybilne, bo nazwy rekordów nie wpływają na encoding, ale Schema Registry zobaczy nową wersję.
- `time-micros` bez spacji (S1) – jw., tylko tekst schematu.
- Snappy: poprawny CRC (S3) – pliki z 3.x nadal się czytają (CRC nie jest walidowane przy odczycie).
- Nieznany `logicalType` nie rzuca wyjątku (S2).
- `SchemaConvert` usunięty na rzecz `AvroSchema` (S13, #73): `SchemaConvert.GenerateFromJson(json)` → `AvroSchema.InferFromJson(json).Json`.
- `DeserializeHeadless<List<T>>(bytes, schemaElementu)` czyta wszystkie wiersze do końca danych (#69); w 3.x zwracało listę z pierwszym wierszem. Liczbę wierszy nadal można podać przeciążeniem z `numberOfRows`.
- Trimming / AOT: metody refleksyjne mają `[RequiresUnreferencedCode]`/`[RequiresDynamicCode]` – aplikacje z `PublishTrimmed`/`PublishAot` zobaczą ostrzeżenia IL2026/IL3050 w miejscu wywołania.

## 4. Publikacja 4.0.0 – checklista

Stan 19.10.2026: kod, testy (unit 93, komponentowe 1513, interop 116), licencje i dokumentacja na `release/4.0`; w CHANGELOG brakuje sekcji "Breaking changes" (§3). Wersja w csproj: `4.0.0-preview.1`.

1. **Preview** (można od razu): `scripts/pack.sh` (lub `pack.ps1`) → `artifacts/*.nupkg` → `dotnet nuget push artifacts/*.nupkg --source https://api.nuget.org/v3/index.json --api-key $NUGET_API_KEY`. Preview na nuget.org nie jest pokazywane jako „latest”, więc nikt nie dostanie go przez przypadek.
2. Zebrać feedback z preview (tydzień–dwa): issue na GitHubie „4.0 preview – breaking changes” z linkiem do CHANGELOG i §5.
3. Przed stabilnym 4.0.0:
   - przegląd prawny `licenses/Commercial.md`, realna nazwa prawna JDG w pliku i w `config.ts` strony;
   - Paddle: produkt + ceny (0 / 189 / 1299), Domain Review zaliczony, checkout na soltechnology.dev działa; webhook → e-mail z certyfikatem licencji (PDF) – do zbudowania;
   - wznowienie JDG (CEIDG) – dopiero gdy checkout gotowy;
   - `<Version>4.0.0</Version>` w trzech csproj, `docs/CHANGELOG.md` – data; tag `v4.0.0`;
   - merge `release/4.0` → `master` (CI: `.github/workflows/build&test.yml` na .NET 10, artefakty nupkg); `release/3.x` zostaje gałęzią utrzymaniową 3.4.x.
4. Po publikacji: GitHub Release z CHANGELOG, pinned issue o zmianie licencji, aktualizacja README/strony (badge wersji), zamknięcie issues zrobionych w 4.0 (#51, #69, #73, #87, #100, #107, #112, #118, #135, #153, #156, #159, #163, #174), odpowiedź i zamknięcie #103, zamknięcie PR #119 z podziękowaniem – zastąpiony przez #118 (N wpisów w kontenerze) i API strumieniowe.
5. Transfer repo do organizacji `sol-technology` (GitHub robi redirecty; NuGet `RepositoryUrl` do zmiany w kolejnym wydaniu).
