# AvroConvert – analiza przepisania pod kątem pamięci i wydajności

Stan: analiza (06.10.2026), bez zmian w kodzie. Dotyczy `src/AvroConvert` + `src/SolTechnology.FastMember`, targety `netstandard2.0;net6.0`.

**Decyzja (06.10.2026): przepisanie wchodzi do wydania 4.0.0** razem ze zmianą licencji (patrz `LicensingMigrationPlan.md`). Zakres: fazy 0–2 z §5. **4.0 targetuje wyłącznie `net10.0`** – wzmianki o netstandard2.0 poniżej opisują stan obecny, nie cel. Source generator jako 4.x. Pełny zakres 4.0 (zgodność ze spec, issues, .NET 10): `V4ReleasePlan.md`.

## 1. Podsumowanie

Biblioteka ma dobre fundamenty (cache schematów, IL-emit do dostępu do pól, `stackalloc`/`ArrayPool` dla stringów na net6.0), ale cały rdzeń działa w modelu **"wszystko przez `object`"**: każdy prymityw jest boxowany przy zapisie i odczycie, dispatch po typie schematu odbywa się **per wartość** w runtime, a dane są wielokrotnie kopiowane między `MemoryStream`-ami i tablicami. Na benchmarku z repo (`docs/articles/GrandeBenchmark...`) Avro_Default alokuje **76,6 MB vs 52,2 MB dla Newtonsoft.Json** na tych samych danych i generuje ~2× więcej kolekcji Gen0 – to głównie boxing i kopie buforów, nie format.

Realistyczny cel przepisania: **3–5× mniej alokacji, 2–4× szybciej** na typowych modelach (rekordy z prymitywami, listy, słowniki), zachowując publiczne API `AvroConvert.Serialize/Deserialize`.

## 2. Jak to działa dziś (hot path)

```mermaid
flowchart LR
  subgraph Serialize
    A[object] --> B[Schema.Create\nConcurrentDictionary cache]
    B --> C[WriteResolver.ResolveWriter\nraz per schemat → delegaty]
    C --> D[WriteRecordFields\nFunc&lt;object,string,object&gt;\nboxing każdego pola]
    D --> E[Writer → MemoryStream chunk\nWriteByte per bajt]
    E --> F[Codec.Compress\nToArray + nowy MemoryStream]
    F --> G[outStream → ToArray]
  end
  subgraph Deserialize
    H[byte[] → MemoryStream] --> I[ReadDataBlock per blok\nArray.Resize + Array.Copy]
    I --> J[new MemoryStream + Reader]
    J --> K[Resolver.Resolve\nswitch per wartość\ntry/catch per wartość\nreturn object]
    K --> L[FastMember TypeAccessor\nunboxing + Stfld]
  end
```

## 3. Ustalenia – według wagi

### 3.1 Krytyczne (dominują w profilu)

| # | Problem | Gdzie | Skutek |
|---|---|---|---|
| 1 | **Boxing każdej wartości w obu kierunkach.** Zapis: `Func<object,string,object>` zwraca `object` ([Record.cs](../src/AvroConvert/AvroObjectServices/Write/Resolvers/Record.cs#L74)), `Write<S>(object value, …)` z `is S` + fallback `Convert.ChangeType` w `try/catch` ([WriteResolver.cs](../src/AvroConvert/AvroObjectServices/Write/WriteResolver.cs#L130)). Odczyt: `Resolver.Resolve` zwraca `object` dla każdego int/long/double/bool ([Resolver.cs](../src/AvroConvert/AvroObjectServices/Read/Resolver.cs#L78)). | Write/Read resolvers | 1 alokacja (24 B) na każdy prymityw w każdym rekordzie; przy 40 k obiektów × ~10 pól = setki tysięcy alokacji Gen0 na jedno wywołanie |
| 2 | **Dispatch po schemacie per wartość przy odczycie.** `switch (writerSchema.Type)` + `FindBranchReaderSchema` + `try/catch` wykonują się dla każdej wartości, nie są prekompilowane w delegaty jak po stronie zapisu. | [Resolver.cs](../src/AvroConvert/AvroObjectServices/Read/Resolver.cs#L66) | Brak inliningu, branch misprediction, narzut per pole |
| 3 | **Dostęp do pól przez nazwę w runtime.** Zapis: hash nazwy pola liczony przy każdym polu każdego rekordu, `switch` po hashu w skompilowanym Expression; gdy wartość `null` → **refleksja `type.GetField(...).GetValue(...)` per pole** ([Record.cs](../src/AvroConvert/AvroObjectServices/Write/Resolvers/Record.cs#L77)). Odczyt: `accessor[result, name] = value` → `Dictionary<string,int>.TryGetValue` + IL switch per pole ([TypeAccessor.cs](../src/SolTechnology.FastMember/TypeAccessor.cs#L93)). | Record resolvers, FastMember | 2 lookupy po stringu na pole; każde pole `null` kosztuje wywołanie refleksyjne |
| 4 | **Kolekcje przy zapisie: `Cast<object>().ToList()`** kopiuje całą kolekcję do `List<object>` (boxing elementów) tylko po to, żeby policzyć elementy. | [Array.cs](../src/AvroConvert/AvroObjectServices/Write/Resolvers/Array.cs#L37) | 2× pamięć danych kolekcji, O(n) dodatkowych alokacji |
| 5 | **Składanie bloków przy odczycie `Array.Resize` + `Array.Copy` w pętli** → O(n²) kopii dla plików wieloblokowych, a potem `new MemoryStream(data)` + nowy `Reader`. | [Decoder.cs](../src/AvroConvert/Features/Deserialize/Decoder.cs#L60) | Dla 100 bloków po 16 KB: ~80 MB skopiowanych bajtów na 1,6 MB danych; alokacje LOH |
| 6 | **I/O bajt po bajcie przez `Stream`.** `Writer.WriteLong` woła `_stream.WriteByte` per bajt varint; `Reader.ReadLong`/`ReadDouble` wołają `_stream.ReadByte` per bajt (wirtualne wywołanie + sprawdzenie granic w `MemoryStream`). `ReadFloat` alokuje `byte[4]`. | [Writer.cs](../src/AvroConvert/AvroObjectServices/Write/Writer.cs#L65), [Reader.cs](../src/AvroConvert/AvroObjectServices/Read/Reader.cs#L75) | 8–10 wirtualnych wywołań na jeden `long`; brak możliwości wektoryzacji |

### 3.2 Istotne

| # | Problem | Gdzie |
|---|---|---|
| 7 | **Łańcuch kopii buforów przy kompresji.** `_memoryChunk` → `ToArray()` → kodek → nowy `MemoryStream` → `WriteStream` → `CopyTo`. Snappy dodatkowo `compressedData.Concat(checksum).ToArray()`. Po każdym bloku `new MemoryStream()` zamiast `SetLength(0)`. | [Encoder.cs](../src/AvroConvert/Features/Serialize/Encoder.cs#L87), [DeflateCodec.cs](../src/AvroConvert/AvroObjectServices/FileHeader/Codec/DeflateCodec.cs), [SnappyCodec.cs](../src/AvroConvert/AvroObjectServices/FileHeader/Codec/SnappyCodec.cs) |
| 8 | **`Serialize` zwraca `resultStream.ToArray()`** – finalna kopia całego wyniku; brak przeciążenia piszącego do `IBufferWriter<byte>`/`Stream` użytkownika bez pośredniego `MemoryStream`. | [AvroConvert.Serialize.cs](../src/AvroConvert/AvroConvert.Serialize.cs#L29) |
| 9 | **Odczyt kolekcji: `List<T>` → `Activator.CreateInstance(T[])` + `CopyTo`; `HashSet<T>` przez `dynamic`**; `Map` przez `dynamic result.Add(key, value)` per wpis (DLR call-site cache + boxing). | [Read/Array.cs](../src/AvroConvert/AvroObjectServices/Read/Resolvers/Array.cs#L100), [Read/Map.cs](../src/AvroConvert/AvroObjectServices/Read/Resolvers/Map.cs#L28) |
| 10 | **Cache schematów wyłączony, gdy podano `AvroConvertOptions`** – `Schema.Create` buduje schemat refleksyjnie przy każdym wywołaniu. | [Schema.cs](../src/AvroConvert/AvroObjectServices/BuildSchema/Schema.cs#L28) |
| 11 | **`WriteResolver`/`Resolver` tworzone per wywołanie `Serialize`/`Deserialize`** – graf delegatów zapisu budowany od nowa dla każdego wywołania (nie jest cache'owany per `(Type, options)`), `Resolver` ma własne słowniki cache (`_accessorDictionary`, `_readStepsDictionary`), które giną po wywołaniu. | [Encoder.cs](../src/AvroConvert/Features/Serialize/Encoder.cs#L63), [Read/Record.cs](../src/AvroConvert/AvroObjectServices/Read/Resolvers/Record.cs) |
| 12 | **Dwie gałęzie `#if NET6_0_OR_GREATER` / `#else`** w `Writer`/`Reader`; gałąź ns2.0 alokuje `byte[]` per string/float/double. Po przejściu na sam `net10.0` gałąź `#else` znika. | [Writer.cs](../src/AvroConvert/AvroObjectServices/Write/Writer.cs#L80), [Reader.cs](../src/AvroConvert/AvroObjectServices/Read/Reader.cs#L165) |
| 13 | **Per blok: `new byte[blockSize]` + `new byte[16]` na sync marker + `SequenceEqual` (LINQ).** | [Reader.Extensions.cs](../src/AvroConvert/AvroObjectServices/Read/Reader.Extensions.cs#L55) |
| 14 | **Nagłówek: `schema.ToString()` serializuje schemat do JSON przez Newtonsoft przy każdym `Serialize`.** | [Encoder.cs](../src/AvroConvert/Features/Serialize/Encoder.cs#L60) |
| 15 | **`Map` przy zapisie: `entry.Key.ToString()`** per wpis, iteracja po niegenerycznym `IDictionary` (boxing `DictionaryEntry`). | [Write/Map.cs](../src/AvroConvert/AvroObjectServices/Write/Resolvers/Map.cs#L36) |

### 3.3 Drobne / higiena

- `new Random()` per `Encoder` dla sync markera (lepiej `RandomNumberGenerator.Fill` lub `Random.Shared`).
- `FastMember.TypeAccessor` używa `Hashtable` + `lock` zamiast `ConcurrentDictionary`.
- Cache w `Resolver` kluczowane `type.GetHashCode()` (`int`) – kolizje hashy typów są możliwe, klucz powinien być `Type`.
- `ExpandoObject` → `ToDictionary` z `StringComparer.InvariantCultureIgnoreCase` per rekord.
- Wyjątki jako control-flow w `Write<S>` (`Convert.ChangeType` w `try/catch`) i `catch (Exception)` + rethrow z interpolowanym stringiem per wartość w `Resolver.Resolve` (kosztowne przy błędach, blokuje inlining zawsze).
- Zależność od Newtonsoft.Json tylko do parsowania/serializacji schematu i `JObject` – ciężka (ok. 700 KB) jak na tę rolę.

### 3.4 Co już jest dobrze i warto zachować

- `ConcurrentDictionary<Type, TypeSchema>` jako cache schematów.
- IL-emit (FastMember) i Expression trees – właściwy kierunek, tylko za mało "wyspecjalizowane".
- `FormatterServices.GetUninitializedObject` do tworzenia instancji.
- `stackalloc`/`ArrayPool` dla stringów ≤512 B na net6.0.
- `OpenDeserializer<T>` (DeserializeByLine) – model strumieniowy bez `Array.Resize`.
- Istniejące benchmarki (`tests/CoreBenchmarks`, `tests/PerformanceBenchmark`) z `MemoryDiagnoser`.

## 4. Docelowa architektura

Zasada: **wszystko, co zależy od typu i schematu, policz raz; w runtime wykonuj tylko silnie typowane delegaty na `Span<byte>`.**

### 4.1 Warstwa binarna – `ref struct` zamiast `Stream`

```csharp
internal ref struct AvroWriter
{
    private IBufferWriter<byte> _output;   // ArrayBufferWriter / PipeWriter / pooled
    private Span<byte> _span;
    private int _pos;

    public void WriteLong(long v)        // zigzag varint do _span, bez wirtualnych wywołań
    public void WriteString(ReadOnlySpan<char> s)  // Utf8 bezpośrednio do bufora
    public void WriteDouble(double v)    // BinaryPrimitives.WriteInt64LittleEndian
}

internal ref struct AvroReader
{
    private ReadOnlySpan<byte> _buffer;  // cały blok po dekompresji
    private int _pos;

    public long ReadLong()               // pętla po span, bez ReadByte
    public string ReadString()           // Encoding.UTF8.GetString(span) lub string.Create
    public ReadOnlySpan<byte> ReadBytesSpan()
}
```

- Jeden blok = jeden `ReadOnlySpan<byte>`; brak `MemoryStream`, brak `Array.Resize`.
- `IBufferWriter<byte>` pozwala pisać do `ArrayPool`, `PipeWriter`, `Stream` użytkownika (przez adapter) bez finalnego `ToArray()`.
- Jeden target `net10.0` – koniec `#if NET6_0_OR_GREATER`, gałęzie `#else` z `BitConverter.GetBytes`/`new byte[]` znikają.

### 4.2 Silnie typowane serializery per `(Type, Schema, Options)`

#### Dlaczego nie "wszystko na Expression trees"

Expression trees to mechanizm generowania kodu, nie źródło szybkości. Biblioteka już ich używa (`Write/Record.cs`, `Read/Array.cs`), a mimo to boxuje każdą wartość, bo generowana sygnatura to `Func<object, string, object>`. Po `Compile()` powstaje `DynamicMethod` → JIT → kod tej samej jakości co z ręcznego C#; o wyniku decyduje **kształt** wygenerowanego kodu (typowane parametry, brak lookupów po nazwie), nie narzędzie.

| Źródło zysku | Czy Expression trees to załatwiają? |
|---|---|
| Brak boxingu (typowane `Action<AvroWriter, T>`) | Tak, jeśli sygnatura jest typowana |
| Dostęp do pól bez lookupu po nazwie | Tak |
| Dispatch po schemacie raz zamiast per wartość | Tak |
| `Array.Resize` O(n²) przy odczycie bloków (#5) | Nie – logika bufora |
| `Stream.ReadByte/WriteByte` per bajt (#6) | Nie – warstwa I/O |
| Kopie `ToArray()` w kodekach (#7) | Nie |
| `Cast<object>().ToList()` w kolekcjach (#4) | Nie – wystarczy generyczny kod |

Ograniczenia drzew istotne dla tego projektu:
- **Brak JIT → interpreter.** Na Blazor WASM (AvroConvertOnline!), iOS i NativeAOT `Expression.Compile()` przełącza się na interpreter – 10–100× wolniej. Dlatego docelowo source generator, a drzewa tylko jako fallback.
- **`ref struct` / `Span<byte>` / `stackalloc`** – słabo wspierane w drzewach; warstwa binarna ma być ręczna, drzewa tylko wołają `writer.WriteLong(x.Age)`.
- Koszt pierwszego wywołania 1–10 ms per typ; stack trace `lambda_method(Closure, …)` bez debugowalności.

Decyzja: **trzy warstwy, każda innym narzędziem**:
1. Warstwa binarna (varint, UTF-8, bloki, kodeki, pooling) – ręczny kod na `Span<byte>`/`IBufferWriter<byte>`.
2. Prymitywy, kolekcje, słowniki, unie, typy logiczne – ręczne generyczne `WriteFn<T>`/`ReadFn<T>`, składane raz per schemat w graf delegatów.
3. Rekordy (kształt zależny od typu użytkownika) – Expression trees generujące **typowaną** `Action<AvroWriter, T>` wołającą warstwę 2; dostęp do prywatnych pól przez `UnsafeAccessor`; ten sam kształt emituje później source generator dla `[AvroSerializable]`, drzewa zostają dla typów bez atrybutu i `dynamic`.

```csharp
internal delegate void WriteFn<T>(ref AvroWriter w, T value);
internal delegate T ReadFn<T>(ref AvroReader r);

internal static class AvroSerializer<T>
{
    // zbudowane raz; klucz cache: (typeof(T), writerSchemaFingerprint, readerSchemaFingerprint, optionsKey)
    public static readonly WriteFn<T> Write;
    public static readonly ReadFn<T>  Read;
}
```

Budowa delegatów – jedna z dwóch dróg:

| Podejście | Zalety | Wady |
|---|---|---|
| **Expression trees / `ILGenerator` w runtime** (jak dziś, ale typowane) | Brak zmian po stronie użytkownika, działa dla `dynamic`/`ExpandoObject` i typów bez atrybutu | Nie działa pod NativeAOT/trimming (interpreter); koszt kompilacji przy pierwszym użyciu (~ms per typ) |
| **Source generator (`[AvroSerializable]`)** | Zero refleksji w runtime, AOT/trimming-friendly, najszybszy start, czytelny wygenerowany kod | Wymaga atrybutu na modelach; `dynamic`/anonimowe typy nadal przez fallback |

Rekomendacja: **oba** – generator jako ścieżka główna (4.x), kompilacja Expression/IL jako fallback dla typów bez atrybutu (4.0). Delegaty generowane dla rekordu to zwykły kod w stylu:

```csharp
static void Write(ref AvroWriter w, User u)
{
    w.WriteString(u.Name);
    w.WriteLong(u.Age);          // bez boxingu, bez lookupu po nazwie
    if (u.Address is null) w.WriteLong(0); else { w.WriteLong(1); Write(ref w, u.Address); }
    WriteList(ref w, u.Offerings, WriteOffering);
}
```

- Pola rozwiązywane **w czasie budowy delegatu** (po nazwie/aliasie/naming policy) → w runtime brak stringów.
- Ewolucja schematu (writer ≠ reader): projekcja liczona raz → delegat odczytu zawiera gotowe `Skip` dla pól nadmiarowych i `default` dla brakujących. `Skipper` zostaje, ale wywoływany z prekompilowanej listy.
- Unie: `switch` po indeksie gałęzi do delegatów gałęzi, wyznaczony raz.
- Kolekcje: `WriteList<TItem>(ref AvroWriter, IReadOnlyList<TItem>/T[]/IEnumerable<TItem>, WriteFn<TItem>)` – rozpoznanie `ICollection<T>.Count` vs `IEnumerable<T>` raz; przy nieznanym `Count` zapis do bufora tymczasowego lub w porcjach z licznikiem (format Avro na to pozwala – bloki o znanym rozmiarze).
- Słowniki: `IDictionary<string,TValue>`/`IReadOnlyDictionary` iterowane generycznie, bez `DictionaryEntry` i `ToString()`.
- Odczyt kolekcji: od razu `T[]`/`List<T>`/`HashSet<T>` przez wygenerowany kod, bez `Activator`/`dynamic`.

### 4.3 Pipeline bloków i kodeki

- `Encoder`: jeden `ArrayBufferWriter<byte>`/pooled bufor na blok, `Clear()` zamiast `new MemoryStream()`; kodek dostaje `ReadOnlySpan<byte>` i pisze do `IBufferWriter<byte>`; brak `ToArray()` po drodze.
- `Decoder`: czytanie bloków strumieniowo – dekompresja do pooled bufora, dekodowanie rekordów z tego bufora, bufor wraca do poola; `Deserialize<List<T>>` tylko dopisuje elementy do wynikowej listy. To eliminuje O(n²) i LOH.
- Kodeki: `DeflateStream`/`GZipStream`/`BrotliEncoder` (wbudowane; `BrotliSharpLib` usunięty) działające na `IBufferWriter`; Snappy – `IronSnappy` ma API na `Span`; zstandard – `ZstdSharp.Port`.
- Sync marker: porównanie `span.SequenceEqual` bez alokacji.
- Nagłówek: cache `schema.ToString()` razem ze schematem.

### 4.4 Cache i konfiguracja

- `AvroConvertOptions` dostaje stabilny klucz (hash konwerterów, naming policy, number handling) → cache schematów i serializerów działa także z opcjami.
- Globalny cache `(Type, optionsKey) → (TypeSchema, schemaJson, WriteFn)` i `(Type, writerSchemaFingerprint, optionsKey) → ReadFn`; fingerprint schematu writera z nagłówka (np. SHA-256 lub Avro CRC-64 fingerprint) zamiast parsowania JSON przy każdym `Deserialize`.
- Parsowanie schematu z JSON: `System.Text.Json` (`Utf8JsonReader`) zamiast Newtonsoft; `JObject` w API publicznym (`Json2Avro`/`Avro2Json`) do zastąpienia `JsonNode`/`JsonElement` – to zmiana łamiąca, więc osobny major.

### 4.5 API publiczne – rozszerzenia bez łamania

```csharp
byte[] Serialize<T>(T obj)                                  // istniejące, wewnątrz pooled bufor + jedna kopia na wynik
void   Serialize<T>(T obj, IBufferWriter<byte> output)      // zero kopii
void   Serialize<T>(T obj, Stream output)                   // istniejące semantyki
T      Deserialize<T>(ReadOnlySpan<byte> avro)              // bez MemoryStream
T      Deserialize<T>(ReadOnlyMemory<byte> avro)
IAsyncEnumerable<T> DeserializeAsync<T>(Stream)             // dla dużych plików
```

Headless (`SerializeHeadless`/`DeserializeHeadless`) korzysta z tych samych `WriteFn<T>`/`ReadFn<T>` bez warstwy bloków.

## 5. Plan wdrożenia

### Faza 0 – pomiar (przed zmianami)
- Rozszerzyć `tests/PerformanceBenchmark` o scenariusze: płaski rekord z prymitywami; rekord z listą 1 k elementów; słownik; zagnieżdżone rekordy 3 poziomy; plik 100 bloków. Baseline zmierzyć na obecnym kodzie **po** migracji na net10.0 (ta sama platforma przed i po przepisaniu).
- Zapisać baseline (`Mean`, `Allocated`, `Gen0/1/2`) do `docs/benchmarks/`.
- Profil alokacji (dotMemory / `dotnet-counters` + `dotnet-trace`) na scenariuszu z GrandeBenchmark, żeby potwierdzić udział boxingu vs kopii buforów.

### Faza 1 – szybkie wygrane, bez zmiany architektury (niskie ryzyko)
1. `Decoder.Read`: pre-alokacja lub lista bloków zamiast `Array.Resize` (#5).
2. `Encoder`: `_memoryChunk.SetLength(0)` zamiast `new MemoryStream()`; kodeki na `GetBuffer()`/`TryGetBuffer` zamiast `ToArray()` (#7).
3. `Write/Array.cs`: `ICollection.Count`/`ICollection<T>.Count` zamiast `Cast<object>().ToList()`; iteracja po `IEnumerable` bez kopii (#4).
4. `Write/Record.cs`: usunąć refleksyjny fallback przy `null`; getter zwraca także informację "pole istnieje" (#3).
5. Cache `schema.ToString()`; `Random.Shared`/`RandomNumberGenerator` (#14).
6. Usunięcie `#if`/`#else` w `Writer`/`Reader` po migracji na net10.0 (#12).
7. Sync marker i `ReadFloat` bez alokacji (#13, #6 częściowo).
8. Cache schematów także z `AvroConvertOptions` (klucz z opcji) (#10).

Oczekiwany efekt: ok. 30–40 % mniej alokacji, 15–25 % szybciej; brak zmian API.

### Faza 2 – typowany rdzeń (średnie ryzyko)
1. `AvroWriter`/`AvroReader` jako `ref struct` na `Span<byte>`/`IBufferWriter<byte>` (#6).
2. Generyczne `WriteFn<T>`/`ReadFn<T>` budowane Expression trees; rekordy, prymitywy, kolekcje, słowniki, unie, typy logiczne (#1, #2, #3, #9).
3. Globalny cache serializerów per `(Type, schemaFingerprint, optionsKey)` (#11).
4. Strumieniowy `Decoder` bez składania bloków.
5. Stary `Resolver`/`WriteResolver` jako fallback dla `dynamic`/`ExpandoObject`/`JObject`.

Oczekiwany efekt łącznie: 3–5× mniej alokacji, 2–4× szybciej na rekordach z prymitywami.

### Faza 3 – source generator (4.x)
1. `AvroConvert.Generators` (Roslyn incremental generator) emitujący `WriteFn<T>`/`ReadFn<T>` dla `[AvroSerializable]`.
2. Zielony NativeAOT/trimming dla ścieżki generowanej; `[RequiresDynamicCode]` na ścieżce Expression trees / `dynamic`.

(Zmiana targetów na `net10.0` i zastąpienie Newtonsoft przez System.Text.Json wchodzą już do 4.0 – patrz `V4ReleasePlan.md` §4.)

## 6. Ryzyka i kompatybilność

- **Semantyka `null`/union/nullable** – obecny kod ma sporo przypadków brzegowych (aliasy, `AvroUnionAttribute`, `NullableSchemaAttribute`, naming policy, `AvroNumberHandling`). Testy komponentowe w `tests/AvroConvertTests` muszą przejść w 100 % przed i po każdej fazie; warto dołożyć testy round-trip z Apache.Avro jako wyrocznią binarną.
- **`dynamic` / `ExpandoObject` / `JObject`** – pozostają na starej ścieżce; nie optymalizować.
- **Konwertery użytkownika (`IAvroConverter`)** – API operuje na `IWriter`/`IReader`; `ref struct` nie da się przekazać przez interfejs klasowy → potrzebny adapter lub nowy interfejs generyczny `IAvroConverter<T>` z metodami na `ref AvroWriter`. Stary interfejs zostaje jako wolniejsza ścieżka.
- **`Convert.ChangeType` w `Write<S>`** toleruje dziś niezgodności typów (np. `int` do schematu `long`). Typowane delegaty muszą te konwersje wyznaczyć w czasie budowy (jawnie wspierana lista promocji Avro: int→long→float→double), inaczej regresja funkcjonalna.
- **FastMember** – po fazie 2 używany tylko przez fallback; można zostawić jako projekt lub usunąć w fazie 3.
- **Pamięć vs CPU przy kompresji** – kodeki strumieniowe na `IBufferWriter` wymagają, by biblioteki (IronSnappy, BrotliSharpLib) miały API na `Span`; na netstandard2.0 Brotli pozostaje przez `BrotliSharpLib` (byte[]).

## 7. Metryki sukcesu

| Scenariusz (200 rekordów × 200 zagnieżdżonych, jak GrandeBenchmark) | Dziś | Cel po fazie 1 | Cel po fazie 2 |
|---|---|---|---|
| Allocated (Avro_Default) | 76,6 MB | ≤ 50 MB | ≤ 20 MB |
| Mean | 175 ms | ≤ 140 ms | ≤ 70 ms |
| Gen2 collections | 3 | ≤ 1 | 0 |
| Serialize płaskiego rekordu (10 pól) | – (zmierzyć) | – | 0 alokacji poza wynikowym `byte[]` |
| Deserialize 100-blokowego pliku | – (zmierzyć) | brak LOH | brak LOH, stała pamięć robocza |

Każda faza kończy się porównaniem z baseline w `docs/benchmarks/` i pełnym przebiegiem testów komponentowych.
