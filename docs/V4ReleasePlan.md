# AvroConvert 4.0 – zakres wydania

Dokument zbiorczy. Szczegóły w:
- `LicensingMigrationPlan.md` – zmiana licencji, Paddle, klucz licencyjny, formalności.
- `PerformanceRewriteAnalysis.md` – przepisanie rdzenia (fazy 0–2).

Stan: plan (06.10.2026), nic nie jest jeszcze wdrożone.

## 1. Zakres 4.0.0

| Obszar | Zakres | Źródło |
|---|---|---|
| Licencja | PolyForm NC + Small Business, licencja komercyjna przez Paddle, miękki klucz | `LicensingMigrationPlan.md` |
| Wydajność | Typowany rdzeń, `Span`/`IBufferWriter`, strumieniowy decoder | `PerformanceRewriteAnalysis.md` §5 fazy 0–2 |
| Zgodność ze specyfikacją Avro 1.12 | §2 poniżej | spec 1.12.0 |
| Otwarte issues GitHub | §3 poniżej | 17 otwartych (stan 06.10.2026) |
| Platforma | **wyłącznie `net10.0`** (decyzja 06.10.2026 – maksymalne uproszczenie kodu); 3.x pozostaje dla netstandard2.0 | §4 poniżej |

## 2. Zgodność ze specyfikacją Apache Avro

Punkt odniesienia: specyfikacja **1.12.0** (najnowsza opublikowana; strona docs ostatnio modyfikowana 01.10.2026, commit "Add .NET 9.0 and .NET 10.0 support" w Apache.Avro C#). Przed startem sprawdzić, czy nie wyszło 1.13 – wtedy porównać changelog.

### 2.1 Błędy zgodności (naprawić w 4.0)

| # | Problem | Gdzie | Skutek |
|---|---|---|---|
| S1 | **`time-micros` zapisane jako `"time-micros "` (spacja na końcu)** | [LogicalTypeSchema.cs](../src/AvroConvert/AvroObjectServices/Schemas/Abstract/LogicalTypeSchema.cs#L37) | Generowane schematy mają nieprawidłową nazwę typu logicznego; schematy z zewnątrz z poprawnym `time-micros` wpadają w `default` → `SerializationException` |
| S2 | **Nieznany `logicalType` rzuca wyjątek.** Spec: "implementations must ignore unknown logical types when reading, and should use the underlying Avro type". | [TypeSchemaBuilder.cs](../src/AvroConvert/AvroObjectServices/BuildSchema/TypeSchemaBuilder.cs#L331) | Nie da się czytać plików z BigQuery (`datetime`), Debezium, Iceberg itp. – issue #69 |
| S3 | **Snappy: CRC32 liczony z danych skompresowanych i zapisany little-endian.** Spec: CRC32 danych **nieskompresowanych**, **big-endian**. | [SnappyCodec.cs](../src/AvroConvert/AvroObjectServices/FileHeader/Codec/SnappyCodec.cs#L33) | Pliki Snappy z AvroConvert nie przechodzą walidacji w Javie/Pythonie; odczyt cudzych plików działa tylko dlatego, że CRC jest ignorowane |
| S4 | **Schema resolution: brak obsługi pola, które jest w schemacie czytelnika, a nie ma go u pisarza.** Spec: użyć `default` czytelnika, a bez `default` → błąd. Dziś iterowane są tylko pola pisarza, a jako default używany jest `wf.DefaultValue` (pisarza) zamiast `rf.DefaultValue`. | [Read/Record.cs](../src/AvroConvert/AvroObjectServices/Read/Resolvers/Record.cs#L55) | Brakujące pola dostają `default(T)` zamiast wartości ze schematu; brak błędu dla pól obowiązkowych – issue #87 |
| S5 | **Nazwy typów generycznych** – `Field<string>` i `Field<int>` dają ten sam `name: "Field1"` → duplikat nazwy w jednym schemacie (spec: fullname musi być unikalny). | `AvroContractResolver.ResolveType` | Schema Registry (Azure, Confluent) odrzuca schemat – issue #159 |
| S6 | **Avro2Json nie stosuje JSON Encoding dla unii** (`{"string": "x"}` zamiast `"x"`). | `Features/AvroToJson` | Niezgodne z sekcją "JSON Encoding"; issue #156 |

### 2.2 Brakujące elementy specyfikacji (dodać w 4.0)

| # | Element | Uwagi |
|---|---|---|
| S7 | Typy logiczne: `timestamp-nanos`, `local-timestamp-millis`, `local-timestamp-micros`, `local-timestamp-nanos` | Mapowanie: `timestamp-*` ↔ `DateTime`(Utc)/`DateTimeOffset`; `local-timestamp-*` ↔ `DateTime`(Unspecified). Wybór precyzji przez atrybut (`[AvroLogicalType(...)]`) – zamyka issue #153 |
| S8 | `uuid` na `fixed(16)` (od 1.12) | Dziś tylko `string`; odczyt obu, zapis konfigurowalny |
| S9 | `big-decimal` (bytes, skala w wartości) | Spec oznacza jako "currently only Java and Rust" – odczyt tak, zapis opcjonalnie |
| S10 | Kodek `zstandard` | Najpopularniejszy kodek w ekosystemie (Kafka, Spark). `ZstdSharp.Port` (managed) lub `ZstdNet`. `bzip2`/`xz` – pominąć |
| S11 | Kodek `brotli` **nie jest w specyfikacji** | Zostawić jako rozszerzenie, ale ostrzec w docs, że pliki nie są przenośne; nie używać jako domyślnego |
| S12 | Single-object encoding (`C3 01` + CRC-64-AVRO fingerprint + payload) | Standard dla Kafka bez Schema Registry; `SerializeSingleObject`/`DeserializeSingleObject` + rejestr schematów po fingerprincie. Zamyka część #118 (ADX) |
| S13 | Parsing Canonical Form + fingerprint (CRC-64-AVRO, MD5, SHA-256) | Potrzebne do S12, do klucza cache serializerów (perf §4.4) i do `CompareSchema()` – issue #73 |
| S14 | `order` w polach rekordu i `default` dla `enum` | Parsowanie i zachowanie w round-trip schematu (dziś gubione); sort order samych danych – poza zakresem |
| S15 | Pełna lista promocji typów przy resolution (`int→long→float→double`, `string↔bytes`) jako jawna tabela | Dziś tolerowane przez `Convert.ChangeType`; w typowanym rdzeniu musi być jawne |

### 2.3 Test zgodności

- Dodać projekt `tests/AvroConvertInteropTests` z `Apache.Avro` 1.12.x jako wyrocznią: round-trip AvroConvert → Apache.Avro → AvroConvert dla każdego typu logicznego, każdego kodeka (w tym snappy z walidacją CRC), unii, aliasów, defaultów.
- Zestaw plików referencyjnych z `apache/avro/share/test/data` (interop.avro w każdym kodeku) jako testy odczytu.

## 3. Otwarte issues GitHub (stan 06.10.2026)

Legenda: **4.0** – wchodzi do wydania; **4.x** – po 4.0 bez łamania API; **zamknąć** – nieaktualne / duplikat / odpowiedź.

| # | Tytuł | Etykiety | Decyzja | Jak |
|---|---|---|---|---|
| [174](https://github.com/AdrianStrugala/AvroConvert/issues/174) | Union dwóch lub więcej rekordów – `GenerateModel` daje `object`, deserializacja pusta | bug, V4 candidate | **4.0** | Generator modelu emituje typ bazowy/interfejs + `[AvroUnion(typeof(ObjA), typeof(ObjB))]`; `OpenDeserializer<T>` z przeciążeniem przyjmującym schemat czytelnika. 23 komentarze – najgłośniejszy issue |
| [163](https://github.com/AdrianStrugala/AvroConvert/issues/163) | `SerializeHeadless` – brak `using` na `MemoryStream` | issue | **4.0** | Znika w przepisaniu (pooled bufor); do tego czasu `using` |
| [159](https://github.com/AdrianStrugala/AvroConvert/issues/159) | Schemat dla typów generycznych – duplikat nazwy | issue | **4.0** | = S5. Nazwa `Field_String`, `Field_Int32` (argumenty generyczne w nazwie, bez znaków spoza `[A-Za-z0-9_]`) |
| [156](https://github.com/AdrianStrugala/AvroConvert/issues/156) | Avro2Json nie stosuje JSON Encoding dla unii | feature | **4.0** | = S6. Opcja `AvroConvertOptions.JsonEncoding = Spec | Simplified`, domyślnie Spec w 4.0 (zmiana łamiąca → major) |
| [153](https://github.com/AdrianStrugala/AvroConvert/issues/153) | Wybór `timestamp-millis` vs `micros` dla `DateTime` | add docs | **4.0** | = S7. `[AvroLogicalType(AvroLogicalType.TimestampMillis)]` + opcja globalna w `AvroConvertOptions` |
| [135](https://github.com/AdrianStrugala/AvroConvert/issues/135) | Usunąć boxing w Record writer | feature | **4.0** | = perf §3.1 #1. Autor issue podał gotowy szkic Expression tree – zbieżny z planem |
| [119](https://github.com/AdrianStrugala/AvroConvert/pull/119) | PR: `SerializeContainer`/`DeserializeContainer` | PR | **zamknąć** z podziękowaniem | Zakres pokryty przez nowe API strumieniowe (`IAsyncEnumerable<T>`, `IBufferWriter<byte>`) i S12; PR z 2023 nie zmerguje się na nowy rdzeń |
| [118](https://github.com/AdrianStrugala/AvroConvert/issues/118) | Azure Data Explorer nie przyjmuje pliku z tablicą | feature, V4 candidate | **4.0** | ADX oczekuje kontenera z N rekordami (po jednym na obiekt), a nie jednego rekordu typu `array`. `Serialize(IEnumerable<T>)` ma zapisywać N obiektów w blokach (zgodnie ze spec OCF), `Deserialize<List<T>>` czytać je z powrotem. To zmiana łamiąca format dla kolekcji top-level → major. Dla zachowania starego zachowania: `AvroConvertOptions.TopLevelCollectionAsSingleRecord = true` |
| [112](https://github.com/AdrianStrugala/AvroConvert/issues/112) | `ExpandoObject` – pełne wsparcie | feature | **4.x** | Zostaje na ścieżce fallback; sprawdzić AC (headless, 2Json, kodeki) i domknąć |
| [107](https://github.com/AdrianStrugala/AvroConvert/issues/107) | Headless deserializacja do `dynamic`/`ExpandoObject` | issue | **4.x** | Razem z #112 |
| [103](https://github.com/AdrianStrugala/AvroConvert/issues/103) | .NET Framework: `FileNotFoundException` dla `SolTechnology.Avro` | bug, help wanted | **zamknąć** | 4.x nie wspiera .NET Framework (tylko net10.0). Odpowiedź: używać 3.4.x (netstandard2.0) z `AutoGenerateBindingRedirects`/`PackageReference`; sekcja w docs |
| [100](https://github.com/AdrianStrugala/AvroConvert/issues/100) | Redukcja zależności (BrotliSharpLib, FastMember, IronSnappy, Newtonsoft) | issue, V4 candidate | **4.0 w całości** | Na net10.0: `BrotliSharpLib` → `System.IO.Compression.Brotli`; `Portable.System.DateTimeOnly` i `Microsoft.CSharp` → zbędne; FastMember → `UnsafeAccessor` + Expression trees, projekt usunięty; Newtonsoft **zostaje** (decyzja 08.10.2026). Zostają `Newtonsoft.Json` i `IronSnappy` (+ `ZstdSharp.Port` dla S10) |
| [87](https://github.com/AdrianStrugala/AvroConvert/issues/87) | Brak błędu dla pól obowiązkowych | bug, V4 candidate | **4.0** | = S4. Pole czytelnika bez `default`, nieobecne u pisarza → `AvroTypeMismatchException`; opcja `AvroConvertOptions.MissingFieldHandling = Throw | UseDefault` (domyślnie `Throw` zgodnie ze spec; w 3.x było `UseDefault`) |
| [73](https://github.com/AdrianStrugala/AvroConvert/issues/73) | `CompareSchema()` | feature | **4.0** | Na bazie S13 (Parsing Canonical Form): `SchemaConvert.Compare(a, b)` → lista różnic + flaga "czytelne wg reguł resolution" |
| [69](https://github.com/AdrianStrugala/AvroConvert/issues/69) | BigQuery: `Logical Type: datetime` nieznany; wiele wierszy w `DeserializeHeadless` | learning | **4.0** | = S2 (ignorować nieznany logicalType → typ bazowy). Wiele wierszy: `DeserializeHeadless<IEnumerable<T>>` / `OpenHeadlessDeserializer<T>` czytający do końca bufora |
| [51](https://github.com/AdrianStrugala/AvroConvert/issues/51) | Prywatne pola z `[DataMember]` ignorowane | help wanted, issue | **4.0** | W typowanym rdzeniu dostęp do prywatnych pól to `Expression.Field` / `UnsafeAccessor` (net8+) – zero narzutu w runtime, argument o wydajności z 2021 przestaje obowiązywać |
| [26](https://github.com/AdrianStrugala/AvroConvert/issues/26) | Tablice wielowymiarowe `int[,,]` | feature | **4.x** | Avro nie ma typu wielowymiarowego; mapować na `array<array<...>>` + metadane kształtu. Niska wartość, niski priorytet |

Podsumowanie: **13 do 4.0**, 3 do 4.x, 2 do zamknięcia (#103, PR #119). Issues #118 i #87 zmieniają domyślne zachowanie – wymagają wpisu w sekcji "Breaking changes" w CHANGELOG i opcji przywracających stare działanie.

## 4. Migracja do .NET 10

Lokalnie zainstalowane SDK: 8.0.416, **10.0.100**. .NET 10 to LTS (listopad 2025, wsparcie do listopada 2028).

**Decyzja (06.10.2026): 4.0 targetuje wyłącznie `net10.0`.** Cel: jeden target, zero `#if`, zero polyfilli, pełny dostęp do `Span`, `UnsafeAccessor`, `IBufferWriter`, `System.Text.Json`, wbudowanego Brotli. Użytkownicy .NET Framework / .NET 6–9 zostają na 3.4.x; gałąź `release/3.x` dostaje tylko poprawki bezpieczeństwa i krytyczne bugi (deklaracja w README).

### 4.1 Targety

| Projekt | Dziś | 4.0 | Uwagi |
|---|---|---|---|
| `AvroConvert` | `netstandard2.0;net6.0` | **`net10.0`** | Usunąć wszystkie `#if NET6_0_OR_GREATER` (zostawić gałąź `Span`-ową); `IsAotCompatible=true`, `IsTrimmable=true` od początku, żeby analizator wskazywał refleksję |
| `SolTechnology.FastMember` | `netstandard2.0` | **usunięty** | Zastąpiony `UnsafeAccessor` (prywatne pola) i Expression trees / source generatorem; `ObjectReader` nieużywany |
| `SolTechnology.Avro.Http` | `net6.0` | `net10.0` | `Microsoft.AspNetCore.Mvc.Core` 2.3.0 → `<FrameworkReference Include="Microsoft.AspNetCore.App" />` |
| `SolTechnology.Avro.Kafka` | `netstandard2.0` | `net10.0` | `Confluent.Kafka` 1.9.3 → 2.x; `Apache.Avro` 1.12.x; `Confluent.Apache.Avro` 1.7.7.7 (martwy fork) → usunąć |
| `AvroConvertOnline` (Blazor WASM) | `net6.0` | `net10.0` | `Expression.Compile()` pod WASM działa w interpreterze – dla konwertera schemat→model to bez znaczenia (nie serializuje danych), ale jeśli online ma pokazywać Avro↔JSON, potrzebny source generator albo akceptacja wolniejszej ścieżki |
| Testy (10 projektów) | `net6.0`, `netcoreapp3.1` (KafkaApp) | `net10.0` | xunit → xunit.v3, `Microsoft.NET.Test.Sdk` / BenchmarkDotNet → aktualne; `FodyWeavers` w testach komponentowych – sprawdzić, czy nadal potrzebne |

### 4.2 Zależności po migracji

| Pakiet | Dziś | 4.0 |
|---|---|---|
| `Newtonsoft.Json` | parser schematu, `JObject` w API, `Avro2Json`/`Json2Avro` | **zostaje** (decyzja 08.10.2026) – poza gorącą ścieżką po fazie 2 |
| `BrotliSharpLib` | kodek brotli | **usunięty** → `System.IO.Compression.BrotliEncoder/Decoder` |
| `Portable.System.DateTimeOnly` | ns2.0 polyfill | **usunięty** |
| `Microsoft.CSharp` | `dynamic` na ns2.0 | **usunięty** (w shared framework) |
| `IronSnappy` | kodek snappy | zostaje (ma API na `Span`) |
| `ZstdSharp.Port` | – | **nowy** (S10, kodek zstandard; managed, bez natywnych bibliotek) |
| `SolTechnology.FastMember` (projekt) | IL-emit accessor | **usunięty** |

Wynik: dwie zależności zewnętrzne zamiast pięciu, oba kodeki managed.

### 4.3 API obsolete / do zamiany

| Użycie | Problem | Zamiana |
|---|---|---|
| `FormatterServices.GetUninitializedObject` (2×) | `SYSLIB0050` | `RuntimeHelpers.GetUninitializedObject` |
| `Assembly.GetEntryAssembly()` + `AppDomain.CurrentDomain.GetAssemblies()` (lookup CLR type dla `dynamic`) | Niekompatybilne z trimming/AOT | Zostaje tylko na ścieżce `dynamic`; `[RequiresUnreferencedCode]` |
| `Hashtable` + `lock` (FastMember) | – | znika razem z FastMember |
| `new Random()` per encoder | przewidywalny sync marker | `RandomNumberGenerator.Fill` |
| `BitConverter.GetBytes`, `Encoding.UTF8.GetBytes(string)` w gałęziach `#else` | alokacje | usunięte razem z `#if` |
| `Regex` w `SchemaName` | – | `[GeneratedRegex]` |

### 4.4 Co .NET 10 / C# 14 daje przepisaniu

- `UnsafeAccessor` – dostęp do prywatnych pól/właściwości bez refleksji i IL-emit, działa pod AOT → zamyka issue #51 bez kosztu.
- `Span<T>`/`ReadOnlySpan<T>`, `params ReadOnlySpan<T>`, `SearchValues<byte>`, `Utf8.TryWrite`, `IUtf8SpanFormattable` – warstwa binarna bez alokacji.
- `ArrayPool`, `ArrayBufferWriter`, `IBufferWriter<byte>`, `System.IO.Pipelines` – bufory.
- ~~`System.Text.Json` (`Utf8JsonReader`, `JsonNode`) – parser schematu i API JSON bez Newtonsoft.~~ Wycofane – Newtonsoft zostaje.
- `System.IO.Compression.Brotli` wbudowany.
- `[GeneratedRegex]`, `Random.Shared`, `string.Create`, `IAsyncEnumerable<T>`.
- C# 14 `field`, extension members – kosmetyka.
- `IsAotCompatible` / `IsTrimmable` – analizator od pierwszego dnia; cel: zielony AOT dla ścieżki z source generatorem, `[RequiresDynamicCode]` na ścieżce Expression trees / `dynamic`.

### 4.5 Kolejność

1. Gałąź `release/3.x` z obecnego `master` (utrzymanie dla ns2.0). Na `master`: target `net10.0`, usunięcie `#if` i polyfilli, podbicie pakietów, `SYSLIB0050`, zielone testy – **przed** przepisaniem rdzenia, osobny PR.
2. Dodać `tests/AvroConvertInteropTests` (Apache.Avro 1.12.x) – baza do walidacji §2.
3. Faza 1 perf (szybkie wygrane) + poprawki S1–S4.
4. Faza 2 perf (typowany rdzeń, `UnsafeAccessor`, usunięcie FastMember) – w nim S5, S7–S9, S15, #51, #135, #118, #87.
5. ~~Newtonsoft → System.Text.Json~~ – wycofane (08.10.2026).
6. S10 (zstd), S12–S13 (single-object, fingerprint, `CompareSchema`), #174, #156, #69.
7. Licencja + klucz (wg `LicensingMigrationPlan.md` §5), CHANGELOG z sekcją "Breaking changes", publikacja.

## 5. Zmiany łamiące w 4.0 (do CHANGELOG)

- Licencja: CC BY-NC-SA 3.0 → PolyForm NC / Small Business / Commercial.
- **Tylko `net10.0`.** Usunięte `netstandard2.0` i `net6.0`; .NET Framework / .NET 6–9 → pozostać na 3.4.x.
- ~~`Newtonsoft.Json.Linq.JObject` → `System.Text.Json.Nodes.JsonNode`~~ – **wycofane (08.10.2026)**: Newtonsoft zostaje zależnością i w API publicznym; po fazie 2 nie dotyka gorącej ścieżki (schemat parsowany raz per tekst, JSON schematu liczony raz per instancja). Ewentualne przeciążenia na `JsonNode` obok `JObject` – kandydat na 4.x bez łamania API.
- Usunięty pakiet `SolTechnology.FastMember` (był pakowany do AvroConvert jako zależność wewnętrzna – brak wpływu na użytkowników, chyba że ktoś referował go bezpośrednio).
- Kolekcja top-level serializowana jako N obiektów w kontenerze zamiast jednego rekordu `array` (#118). Dotyczy `List<T>`, `T[]`, `HashSet<T>`, `IEnumerable<T>` itp. z konkretnym typem elementu (nie `object`, nie słowniki). Pliki 3.x (`array`) nadal się czytają. Opcja przywracająca: `AvroConvertOptions.CollectionMode = SingleArray`. Konsekwencja: plik z samym nagłówkiem (0 wpisów) deserializuje się do pustej kolekcji zamiast `null`.
- Brak pola obowiązkowego u pisarza → wyjątek zamiast `default(T)` (#87, S4). Dotyczy tylko pól, które nie mogą przyjąć `null` (typy wartościowe bez `Nullable<>`, bez `[DefaultValue]`, bez unii z `null`); `string`, klasy i `Nullable<T>` dostają `null` jak w 3.x. Opcja przywracająca: `AvroConvertOptions.MissingFieldHandling = UseDefault`.
- Avro2Json: JSON Encoding wg spec dla unii (#156, S6) – opcja przywracająca.
- Nazwy typów generycznych w schemacie zawierają argumenty generyczne (#159, S5) – schematy wygenerowane w 3.x dla typów generycznych nie są tekstowo identyczne; dane binarne pozostają kompatybilne, bo nazwy rekordów nie wpływają na encoding, ale Schema Registry zobaczy nową wersję.
- `time-micros` bez spacji (S1) – jw., tylko tekst schematu.
- Snappy: poprawny CRC (S3) – pliki z 3.x nadal się czytają (CRC nie jest walidowane przy odczycie; opcjonalnie `ValidateSnappyCrc = false` dla starych plików).
- Nieznany `logicalType` nie rzuca wyjątku (S2).

## 6. Publikacja 4.0.0 – checklista

Stan 08.10.2026: kod, testy (1134 + 47 + 27), licencje, dokumentacja i CHANGELOG gotowe na `release/4.0`. Wersja w csproj: `4.0.0-preview.1`.

1. **Preview** (można od razu): `scripts/pack.sh` (lub `pack.ps1`) → `artifacts/*.nupkg` → `dotnet nuget push artifacts/*.nupkg --source https://api.nuget.org/v3/index.json --api-key $NUGET_API_KEY`. Preview na nuget.org nie jest pokazywane jako „latest”, więc nikt nie dostanie go przez przypadek.
2. Zebrać feedback z preview (tydzień–dwa): issue na GitHubie „4.0 preview – breaking changes” z linkiem do CHANGELOG i §5.
3. Przed stabilnym 4.0.0:
   - przegląd prawny `licenses/Commercial.md`, realna nazwa prawna JDG w pliku i w `config.ts` strony;
   - Paddle: produkt + ceny (0 / 189 / 1299), Domain Review zaliczony, checkout na soltechnology.dev działa; webhook → e-mail z certyfikatem licencji (PDF) – do zbudowania;
   - wznowienie JDG (CEIDG) – dopiero gdy checkout gotowy;
   - `<Version>4.0.0</Version>` w trzech csproj, `docs/CHANGELOG.md` – data; tag `v4.0.0`;
   - merge `release/4.0` → `master` (CI: `.github/workflows/build&test.yml` na .NET 10, artefakty nupkg); `release/3.x` zostaje gałęzią utrzymaniową 3.4.x.
4. Po publikacji: GitHub Release z CHANGELOG, pinned issue o zmianie licencji, aktualizacja README/strony (badge wersji), zamknięcie issues #69, #87, #100 (częściowo), #118, zamknięcie PR #119 z podziękowaniem.
5. Transfer repo do organizacji `sol-technology` (GitHub robi redirecty; NuGet `RepositoryUrl` do zmiany w kolejnym wydaniu).
