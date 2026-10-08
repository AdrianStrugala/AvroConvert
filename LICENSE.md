# AvroConvert License

Copyright (c) 2019–2026 Adrian Strugała (SolTechnology) — https://soltechnology.dev

AvroConvert is **source-available**. You may use it free of charge for noncommercial purposes and
inside small companies; larger companies need a commercial licence. Pick the path that applies to you:

| You are… | Licence | Cost |
|---|---|---|
| An individual, a student, a non-profit, a public institution, or using it for research, hobby or evaluation | [PolyForm Noncommercial 1.0.0](licenses/PolyForm-Noncommercial-1.0.0.md) | Free |
| A company with **fewer than 100 people** (employees + contractors) **and** under **1,000,000 USD** revenue in the prior tax year | [PolyForm Small Business 1.0.0](licenses/PolyForm-Small-Business-1.0.0.md) | Free |
| Any other company or organisation | [SolTechnology Commercial Licence](licenses/Commercial.md) | Annual subscription per organisation — https://soltechnology.dev/avroconvert/ |

The two free licences are offered in parallel: you may rely on whichever one fits your situation. If
neither does, you need the commercial licence to use AvroConvert in production.

Required Notice: Copyright Adrian Strugała (https://soltechnology.dev)

## Declaring your licence in code

AvroConvert does **not** phone home or restrict functionality. It asks you to state which licence you
rely on, so the choice is visible in your code base:

```csharp
AvroConvert.License = AvroLicense.NonCommercial;
// or
AvroConvert.License = AvroLicense.SmallBusiness;
// or
AvroConvert.License = AvroLicense.Commercial("AVC1.…");   // licence key received by e-mail after purchase
```

The same can be set through the `AVROCONVERT_LICENSE` environment variable (`NonCommercial`,
`SmallBusiness` or the key). A commercial key is signed by SolTechnology and carries the licence number,
licensee, plan and subscription end; the library verifies the signature and exposes these values
(`AvroConvert.License.Licensee`, `.ValidUntil`, …) but an invalid or ended key only produces a `Trace`
warning. Without any declaration the library works normally and emits a single `Trace` warning per process.

## Versions

- AvroConvert **4.0 and later** — the terms above.
- AvroConvert **3.x and earlier** — remain under Creative Commons BY-NC-SA 3.0 as published with those versions.

## Third-party components

Parts of the binary encoder/decoder derive from Apache Avro, Copyright The Apache Software Foundation,
licensed under the Apache License 2.0 — see [src/AvroConvert/LICENSE](src/AvroConvert/LICENSE) and
[src/AvroConvert/NOTICE](src/AvroConvert/NOTICE). Files carrying an Apache 2.0 header keep that licence.
