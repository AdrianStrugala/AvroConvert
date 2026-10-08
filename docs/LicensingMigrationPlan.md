# Plan migracji licencjonowania AvroConvert

Status: **plan, nic nie jest jeszcze wdrożone w kodzie.** Dokument roboczy. Zakres całego wydania 4.0: `V4ReleasePlan.md`.

## 1. Sytuacja wyjściowa

- Pakiet `AvroConvert` na nuget.org: 5,3 mln pobrań, ~4,4 tys./dzień, właściciel: AdrianStrugala.
- Licencja: CC BY-NC-SA 3.0 (zakaz użycia komercyjnego). Licencja komercyjna była sprzedawana przez Xabe (`https://xabe.net/product/avroconvert/`) – umowa wypowiedziana, link martwy.
- W kodzie **nie ma** żadnego mechanizmu licencyjnego – egzekucja wyłącznie prawna.
- Nagłówki plików: 27 plików z nagłówkiem CC BY-NC-SA + linkiem do Xabe; reszta (w tym `AvroConvert.Serialize.cs`, `Deserialize.cs`) ma nagłówek Apache 2.0 (kod odziedziczony z Apache.Avro / Microsoft.Hadoop.Avro) – te muszą zostać na Apache 2.0 z zachowanym `NOTICE`.
- Pakiety satelitarne `SolTechnology.Avro.Http` (3.0.1) i `SolTechnology.Avro.Kafka` (3.0.0) również wskazują na CC BY-NC-SA i Xabe w csproj.

## 2. Decyzje podjęte

| Obszar | Decyzja |
|---|---|
| Model | Płatny rdzeń; darmowe użycie niekomercyjne i dla małych firm |
| Licencja darmowa | PolyForm Noncommercial 1.0.0 + PolyForm Small Business 1.0.0 |
| Próg darmowego użycia komercyjnego | firma < 100 osób i < 1 mln USD przychodu rocznie (zgodnie z PolyForm Small Business) |
| Licencja komercyjna | roczna, per organizacja, z perpetual fallback (wersje wydane przed końcem okresu – bezterminowo) |
| Egzekucja w kodzie | **brak klucza i walidacji** (decyzja 08.10.2026 po przeglądzie rynku – patrz §5.4). Deklaracja licencji w kodzie (`AvroConvert.License`, model EPPlus/QuestPDF), bez deklaracji jedno ostrzeżenie `Trace` na proces, pełna funkcjonalność |
| Wersja | 4.0.0 (major) – zmiana licencji **oraz** przepisanie rdzenia pod wydajność (fazy 0–2 z `PerformanceRewriteAnalysis.md`) |
| Platforma sprzedaży | **Paddle** (decyzja 06.10.2026; porównanie w §3) |

## 3. Platforma sprzedaży (merchant of record)

Lemon Squeezy odrzucone: po przejęciu przez Stripe (2024) zespół buduje Stripe Managed Payments, wpis CEO ze stycznia 2026 wprost mówi o wolniejszym supporcie i planowanej migracji użytkowników. Zakładanie nowego sklepu na LS = wejście do produktu w fazie sunsetu.

Kandydaci (wszystkie to MoR – rozliczają VAT/sales tax wobec klienta końcowego i wystawiają mu faktury; Ty fakturujesz platformę):

| | Paddle | Polar.sh | Stripe Managed Payments |
|---|---|---|---|
| Dojrzałość | wysoka, od lat standard w dev-tools | młoda, ale aktywnie rozwijana, open source | **dostępne self-service** (stan 06.10.2026: "Start now" → rejestracja w Dashboardzie, aktywacja + ToS; Stripe zastrzega eligibility review). Informacja o waitliście ze stycznia 2026 nieaktualna |
| Prowizja (orientacyjnie, do weryfikacji) | 5% + 0,50 USD (checkout); 3,5% (invoicing/przelew) | ~4% + 0,40 USD | 3,5% **plus** standardowe opłaty Payments (PL: karty EEA 1,5% + 1 zł; międzynarodowe 3,25% + 1 zł, +2% przewalutowanie) → efektywnie ~5% UE, ~7–9% USA |
| Faktury B2B / procurement korporacyjny | bardzo dobre (PO, wire transfer, VAT ID, tryb Invoicing) | podstawowe | słabe: tylko Checkout/Payment Links, brak przelewu na PO; klient widzi jako sprzedawcę **Link** ("Sold through Link", `LINK.COM*` na wyciągu), checkout bez własnej domeny |
| Klucze licencyjne | tak (przez API/webhook) | tak, wbudowane, z API walidacji | brak wbudowanych – własny webhook |
| Subskrypcje roczne | tak | tak | tak (Billing) |
| Weryfikacja sprzedawcy | dłuższa (KYC, strona WWW, polityki) | szybka | KYC Stripe + eligibility review |
| Przyjmuje JDG z Polski | tak | tak | tak (PL na liście supported business locations) |

**Rekomendacja:** Paddle – grupa docelowa to firmy > 100 osób, które kupują przez dział zakupów i chcą faktury z VAT ID, płatności przelewem i PO. Polar.sh jako plan B jeśli weryfikacja w Paddle będzie problemem lub prowizja ma znaczenie. Stripe Managed Payments jest już dostępne, ale dla sprzedaży B2B do korporacji wypada gorzej (Link jako widoczny sprzedawca, brak invoicing/PO, wyższa efektywna prowizja dla klientów spoza UE); sensowne głównie, jeśli chcesz mieć wszystko w jednym koncie Stripe.

Do zrobienia przed wyborem: potwierdzić aktualne prowizje i wymagania KYC na stronach platform; sprawdzić, czy Paddle wymaga działającej strony produktu z polityką zwrotów (zwykle tak – wystarczy sekcja w README/GitHub Pages).

### 3.1 Dystrybucja e-booka na tej samej platformie (zweryfikowane 06.10.2026)

| | Paddle | Polar.sh | Stripe Managed Payments |
|---|---|---|---|
| E-book dozwolony | **Tak** – strona "Sell digital products" wymienia e-booki, kursy, audio; AUP (13.04.2026) ich nie zakazuje | **Warunkowo** – AUP (25.03.2026) wymienia e-booki jako "restricted, require closer review and may not be accepted" | **Tak, wprost** – kategoria "Digital media: e-books, audiobooks…", kod podatkowy `txcd_10302000` |
| Polska jako kraj sprzedawcy | tak | tak | tak (PL na liście supported business locations) |
| Hosting/dostarczanie pliku | brak – fulfillment po stronie sprzedawcy (webhook → podpisany link / e-mail) | wbudowane File Downloads (do 10 GB, podpisane URL-e, SHA-256) | brak – fulfillment po stronie sprzedawcy |
| Prowizja wg MSA/docs | 5% + 0,50 USD (checkout); 3,5% (przelew / invoicing) | nie weryfikowano | 3,5% + opłaty Payments |
| Status | dojrzały, MSA aktualizowane 06.10.2026 | aktywny | dostępny self-service |

Wniosek: **Paddle obsłuży bibliotekę i e-book z jednego konta.** Dostawę e-booka trzeba zbudować samemu (ten sam webhook, który będzie generował klucze licencyjne, może wysyłać link do pliku np. na Cloudflare R2 / S3 z tokenem wygasającym). Polar ma najwygodniejszy fulfillment plików, ale e-book może nie przejść review. Stripe MP ma najczystszą kwalifikację podatkową e-booka, ale dla biblioteki B2B jest słabszy (patrz §3).

Źródła: `paddle.com/solutions/sell-digital-products`, `paddle.com/help/start/intro-to-paddle/what-am-i-not-allowed-to-sell-on-paddle`, `paddle.com/legal/terms` (§3.2), `polar.sh/docs/merchant-of-record/acceptable-use`, `polar.sh/docs/features/benefits/file-downloads`, `docs.stripe.com/payments/managed-payments/eligibility`.

## 4. Kwestie formalne (Polska)

Nie jest to porada prawna – do potwierdzenia z księgową/doradcą.

- **Zawieszona JDG:** w trakcie zawieszenia nie wolno osiągać bieżących przychodów z działalności (art. 25 Prawa przedsiębiorców). Sprzedaż nowych licencji = nowy przychód → wymaga **wznowienia działalności** (CEIDG, online, bezpłatnie).
- **Działalność nierejestrowana** – niedostępna: warunkiem jest brak działalności przez ostatnie 60 miesięcy, a zawieszona JDG to nadal wpis w CEIDG.
- **Przychód z praw majątkowych (art. 18 PIT)** jako osoba fizyczna – ryzykowne: ciągła, zorganizowana sprzedaż subskrypcji ma cechy działalności gospodarczej.
- MoR nie zmienia kwalifikacji po stronie sprzedawcy – nadal jest to przychód z działalności; MoR załatwia tylko VAT/faktury wobec klienta końcowego.
- **ZUS:** przy równoczesnym etacie z wynagrodzeniem ≥ minimalne – z JDG tylko składka zdrowotna. Przy ryczałcie (stawka dla licencji oprogramowania do potwierdzenia: 8,5% lub 12%) zdrowotna jest kwotowa.
- **VAT:** faktura dla platformy (UK/USA) = usługa poza terytorium PL, bez VAT; VAT-UE niewymagany dla kontrahenta spoza UE (Paddle ma podmiot UK – do sprawdzenia, który podmiot fakturować).
- Wznowić JDG dopiero, gdy sklep jest gotowy do startu.
- Umowa z Xabe: upewnić się, że nie zawierała przeniesienia praw ani wyłączności. Klienci z ważną licencją kupioną przez Xabe – honorować (np. darmowy klucz do końca okresu po okazaniu dowodu zakupu).

## 5. Plan zmian w repo

### 5.1 Pliki licencji (root)
- `LICENSE.md` – przegląd: trzy ścieżki (Noncommercial / Small Business / Commercial), próg, link do zakupu, informacja o komponentach Apache 2.0 i `NOTICE`.
- `licenses/PolyForm-Noncommercial-1.0.0.md` – pełny tekst PolyForm Noncommercial 1.0.0.
- `licenses/PolyForm-Small-Business-1.0.0.md` – pełny tekst PolyForm Small Business 1.0.0.
- `licenses/Commercial.md` – warunki licencji komercyjnej (per organizacja, roczna, perpetual fallback, brak gwarancji, ograniczenie odpowiedzialności). Wymaga przejrzenia przez prawnika.
- Źródło tekstów PolyForm: `https://polyformproject.org/licenses/noncommercial/1.0.0/` i `.../small-business/1.0.0/` (pobranie przez `wp-content/uploads/...md` zwróciło HTML – trzeba skopiować ze strony ręcznie lub z repo `polyformproject/polyform-licenses` na GitHubie).

### 5.2 `src/AvroConvert/AvroConvert.csproj`
- `Version` → `4.0.0`.
- Usunąć `PackageLicenseUrl` (deprecated), dodać `PackageLicenseFile=LICENSE.md` i spakować wszystkie pliki licencji.
- `PackageProjectUrl` → GitHub lub strona sklepu (zamiast Xabe).
- `scripts/version.txt` → 4.0.0 (bez `ReleaseDate` – perpetual fallback jest zapisem umownym w `licenses/Commercial.md`, nie mechanizmem w kodzie).

### 5.3 Nagłówki `.cs`
- 27 plików z CC BY-NC-SA: zamienić na nagłówek PolyForm NC / Small Business / Commercial z linkiem do `LICENSE.md`. Lista: `grep -rl "CC BY-NC-SA" src/`.
- Pliki z nagłówkiem Apache 2.0 – bez zmian.

### 5.4 Podpisany klucz bez egzekucji (zrealizowane 08.10.2026)

Przegląd rynku (.NET): ImageSharp / FluentAssertions 8 – czysto prawne; **EPPlus** (PolyForm NC, `ExcelPackage.LicenseContext` / `License.SetNonCommercialPersonal`) i **QuestPDF** (`Settings.License`) – deklaracja bez walidacji; AutoMapper/MediatR, Duende – miękki klucz podpisany; NServiceBus, Syncfusion – twardy klucz/trial; Hangfire Pro – prywatny feed. Antyprzykład: Moq + SponsorLink (telemetria → exodus użytkowników).

Decyzja (08.10, po dyskusji „deklaracja nazwy” vs „klucz”): model AutoMapper/MediatR – klucz podpisany, parsowany, **nieegzekwowany**:
- `AvroConvert.License = AvroLicense.NonCommercial | SmallBusiness | Commercial("AVC1.…")` albo zmienna `AVROCONVERT_LICENSE` (`NonCommercial`, `SmallBusiness` lub sam klucz).
- Klucz: `AVC1.<base64url JSON>.<base64url podpis>`; payload `{id, licensee, product, plan, issued, expires}`; podpis ECDSA P-256/SHA-256 (IEEE P1363) nad ASCII `AVC1.<payload>`. Klucz publiczny (SPKI) wkompilowany w `AvroLicense`; prywatny w `~/.config/soltechnology/avroconvert-signing.pem` (menedżer haseł) i jako sekret Workera. ECDSA zamiast Ed25519, bo .NET ma je wbudowane, a WebCrypto w Cloudflare Workers podpisuje tym samym formatem.
- Biblioteka: ciąg nieparsowalny → `ArgumentException` (literówka ma być głośna); zły podpis albo `expires` wcześniejsze niż `AvroConvert.ReleaseDate` (AssemblyMetadata z csproj) → klucz przyjęty, jedno `Trace.TraceWarning`. Bez deklaracji – jedno ostrzeżenie na proces. Zero ograniczeń funkcji, zero telemetrii. Wygaśnięcie respektuje perpetual fallback z licencji: ostrzega tylko wersja wydana po końcu subskrypcji.
- Numer licencji `AVC-<rok>-<6 znaków Crockford base32>`; ten sam numer przez cały okres subskrypcji (odnowienie = nowy klucz z nowym `expires`, ten sam `id`).
- Wystawianie kluczy wyłącznie w prywatnym repo Workera (`Sol-Technology/sol-technology-licensing`): webhook Paddle albo `POST /admin/issue` (`scripts/issue.sh`) dla sprzedaży ręcznej – jedna ścieżka (D1, PDF, mail). Awaryjnie `scripts/issue-offline.ts` z lokalnym PEM; `scripts/verify.cs` sprawdza klucz prawdziwym parserem .NET. W publicznym repo AvroConvert zostaje tylko weryfikacja.
- Widoczność (model prawny): `PackageLicenseFile`, pliki licencji i `NOTICE` w paczce, opis i release notes w NuGet, sekcja i badge w README, nagłówki `.cs` z `Required Notice`.
- W 5.0 do rozważenia: deklaracja obowiązkowa (wyjątek), jeśli konwersja będzie słaba – API już to umożliwia bez zmian po stronie klientów.

### 5.5 Testy
- `tests/AvroConvertUnitTests/LicenseTests.cs`: parsowanie zmiennej środowiskowej, walidacja nazwy licencjobiorcy, brak wpływu deklaracji na serializację.

### 5.6 Dokumentacja
- README: sekcja License (trzy ścieżki, próg, link do zakupu, jak ustawić klucz), usunąć link Xabe.
- `docs/CHANGELOG.md`: wpis 4.0.0 – zmiana licencji, mechanizm klucza, przepisanie rdzenia (wyniki benchmarków przed/po), nowe przeciążenia API (`IBufferWriter<byte>`, `ReadOnlySpan<byte>`), brak zmian w istniejącym API.
- `docs/Documentation.md`: podsekcja o deklaracji `AvroConvert.License`.
- Pakiety Http/Kafka: zaktualizować `PackageLicenseUrl`/`PackageProjectUrl` w csproj przy następnym wydaniu (bez zmiany modelu – zależą od `AvroConvert`).

### 5.7 Kolejność wdrożenia
1. Wybór platformy i założenie konta (weryfikacja KYC trwa – zacząć najwcześniej).
2. Pliki licencji + `licenses/Commercial.md` (przegląd prawny).
3. Zmiany w kodzie (§5.2–5.5), build, testy.
4. README/CHANGELOG, strona produktu (GitHub Pages wystarczy: cennik, polityka zwrotów, kontakt).
5. Wznowienie JDG.
6. Publikacja 4.0.0 na nuget.org; w opisie starszych wersji nic nie zmieniać (pozostają na CC BY-NC-SA).
7. Komunikat na GitHub (release notes + pinned issue) z wyjaśnieniem zmiany i okresem przejściowym.

## 6. Cennik – decyzja (07.10.2026) po analizie rynku

Punkty odniesienia (cenniki publiczne, USD/rok, per organizacja):

| Biblioteka | Skala | Model | Ceny |
|---|---|---|---|
| Six Labors / ImageSharp (5 bibliotek) | 650 M pobrań | flat wg liczby devów; darmowe < 1 M USD przychodu; Paddle MoR | ≤10 dev ≈ 770 · 11–20 ≈ 1 250 · unlimited ≈ 5 000 |
| Hangfire | setki M | rdzeń LGPL; płatne Pro/Ace + support | 500 · 1 500 · 4 500 |
| MassTransit v9 | 200 M | per linia produktowa, perpetual downgrade | ≈ 4 800 · ≈ 14 400 |
| Dapper Plus (ZZZ) | 250 M łącznie | per developer, perpetual | ≈ 1 400 jednorazowo |

Skala AvroConvert: 5,3 M pobrań (~1 % ImageSharp), jedna niszowa biblioteka, jeden maintainer, darmowa alternatywa Apache.Avro. Klient docelowy (>100 osób) nie rozróżnia 200 od 500 USD/rok – oba poniżej progu zgód zakupowych. Strategia: niskie wejście, skok tylko tam, gdzie dochodzi realny koszt (support). Łatwiej podnieść przy 4.0 niż obniżać.

| Tier | Dla kogo | Cena roczna |
|---|---|---|
| Community | non-commercial, firmy < 100 osób i < 1 M USD | 0 |
| Business | bez limitu developerów, priorytetowy triage | **199 USD** |
| Enterprise | + e-mail support 2 dni rob., faktura/PO, umowa | **1 499 USD** |

Decyzja właściciela (07.10.2026): trzy tiery, bez pośredniego „Team”. Perpetual fallback w każdym płatnym tierze. Rozważyć zniżkę migracyjną dla klientów Xabe. Ceny wdrożone w `sol-technology.github.io/src/config.ts`.

Dwa pliki licencji darmowych są potrzebne: PolyForm Small Business 1.0.0 zawiera **tylko** klauzulę o firmach <100 osób / <1 M USD (zweryfikowano tekst 07.10.2026); użytek osobisty, edukacyjny, badawczy i non-profit obejmuje wyłącznie PolyForm Noncommercial 1.0.0. Na stronie komunikowane jako jeden tier „Community”.

## 7. Otwarte pytania

- Treść `licenses/Commercial.md` – prawnik czy szablon (np. wzorowany na ImageSharp Commercial License / Duende)?
- Czy ostrzeżenie bez klucza ma być możliwe do wyciszenia (np. dla tierów darmowych flagą `AvroLicense.DeclareNoncommercialUse()`)? Zmniejsza irytację małych firm, nie osłabia egzekucji.
- Domena produktu: własna (zalecane, patrz §8.2) czy `adrianstrugala.github.io/AvroConvert`.

## 8. Checklista startowa – Paddle

Źródła: `paddle.com/help/start/account-verification/*`, `paddle.com/legal/terms` (MSA, 06.10.2026).

### 8.1 Jak wygląda weryfikacja w Paddle

Trzy etapy, kolejno:
1. **Domain Review** – przegląd strony, z której będzie uruchamiany checkout. Często automatyczny; ręczny 5–7 dni roboczych.
2. **Business Identification** – **nie dotyczy JDG / osób fizycznych** (tylko spółki).
3. **Identity Verification** – KYC właściciela konta (dokument tożsamości).

Sandbox (`sandbox-login.paddle.com`) działa bez zatwierdzonej domeny – integrację można budować równolegle z weryfikacją.

### 8.2 Co musi być na stronie produktu (wymóg Domain Review)

- Jasny opis produktu i lista funkcji / co wchodzi w zakup.
- Strona z cennikiem (może być zrzut ekranu, jeśli jeszcze nie ma).
- **Terms & Conditions, Refund Policy, Privacy Policy** – dostępne z nawigacji strony.
- W T&C **pełna nazwa prawna JDG** (imię i nazwisko + nazwa firmy z CEIDG) oraz informacja, że Paddle jest resellerem/MoR (MSA §9.2).
- HTTPS, strona live.
- Cennik enterprise jako PDF do okazania na żądanie (jeśli będzie tier z wyceną indywidualną).
- Na domenie tylko produkty sprzedawane przez Paddle (niezwiązane produkty obniżają szansę akceptacji).

Domena: MSA §11.1(iv) wymaga, by sprzedawca **posiadał i operował** zgłoszoną domenę; docs API Checkout Domains: "customers can only purchase from sites you control". Paddle nie zakazuje wprost subdomen `github.io` (zweryfikowano 07.10.2026 – brak takiej reguły w Domain Review, AUP ani MSA), ale:
- `adrianstrugala.github.io` jest własnością GitHubu, operujesz tylko treścią; przy ręcznym review to argument do odrzucenia lub prośby o wyjaśnienia (5–7 dni roboczych na rundę).
- Paddle zatwierdza **domeny, nie ścieżki** – zatwierdzenie `adrianstrugala.github.io` obejmuje wszystkie Twoje project pages, a docs ostrzegają, że "unrelated products" na domenie obniżają szansę akceptacji.
- Sprzedaż licencji B2B spod `github.io` obniża wiarygodność u działów zakupów.
- Apple Pay wymaga pliku w `/.well-known/` na domenie – na GitHub Pages wykonalne, ale na własnej domenie bezproblemowe.

**Decyzja:** hosting na **GitHub Pages wystarcza** (statyczna strona, HTTPS z automatu, zero kosztów), ale pod **własną domeną** (custom domain w ustawieniach Pages + rekord CNAME/A; koszt ~50–70 zł/rok). AvroConvertOnline zostaje na tej samej domenie jako podstrona. Sandbox Paddle nie wymaga zatwierdzonej domeny – integrację można budować przed zakupem domeny.

Wiele produktów na domenie (kolejne biblioteki, e-book): dozwolone i preferowane, o ile **wszystkie sprzedawane są przez Paddle** (ostrzeżenie w docs dotyczy produktów sprzedawanych innym kanałem). Zasady:
- domena **markowa** (SolTechnology), produkty jako ścieżki `/avroconvert`, `/…` – **nie subdomeny** (każda subdomena = osobny Domain Review);
- każdy produkt ma własną stronę z opisem, cennikiem i listą funkcji; jeden zestaw T&C/Refund/Privacy z sekcją "Products";
- w katalogu Paddle osobny produkt i kategoria podatkowa per pozycja (software vs eBook); bundle "Suite" możliwy jako produkt wieloelementowy;
- jeden webhook; payload klucza licencyjnego zawiera pole `product`, wspólny klucz publiczny dla wszystkich bibliotek;
- do pierwszego review wystawić **tylko AvroConvert**; kolejne produkty dodawać po zatwierdzeniu domeny (katalog nie wymaga ponownego review strony).

### 8.3 Dane i dokumenty do konta

- Dane JDG: nazwa, adres, NIP; Paddle fakturuje się self-billing, ale wystawianie własnej faktury dla Paddle (podmiot: Paddle.com Market Ltd, UK, lub Paddle.com Inc. dla transakcji w USA – wg MSA §1.1) pozostaje po Twojej stronie.
- Dokument tożsamości do Identity Verification.
- **Konto do wypłat:** waluty wypłat to USD/EUR/GBP/CAD/AUD – **nie ma PLN**. Potrzebne konto w EUR (SEPA, bez opłaty) albo USD; przelew międzynarodowy poza lokalną siecią kosztuje 15 USD/EUR/GBP, przewalutowanie do 1,5%. Wypłata do 15. dnia następnego miesiąca, próg 100 USD/EUR/GBP.
- Adres e-mail supportu dla klientów (Paddle obsługuje tylko pierwszą linię: faktury, zwroty, płatności; support produktowy jest Twój).

### 8.4 Konfiguracja w dashboardzie Paddle

- Produkty i ceny: Business (roczna subskrypcja), Enterprise (roczna lub przez Invoicing z przelewem – 3,5% zamiast 5% + 0,50 USD), opcjonalnie e-book (one-off).
- Kategoria podatkowa: "Software" / "Standard digital goods" dla biblioteki, "eBook" dla książki (Paddle wymaga wyboru taxable category).
- Waluta rozliczeniowa konta (Payment Currency): EUR lub USD – raz wybrana.
- Notification/webhook endpoint na `transaction.completed` i `adjustment.*` → Cloudflare Worker `licensing.soltechnology.dev` generuje klucz, certyfikat PDF i wysyła e-mail (zrealizowane 08.10.2026; sprzedaż ręczna przez `POST /admin/issue`).
- Checkout: overlay/inline Paddle.js na stronie cennika lub hosted checkout link.

### 8.5 Formalności po stronie PL

- Wznowienie JDG przed pierwszą sprzedażą (nie przed – ZUS liczy się od dnia wznowienia).
- Potwierdzić z księgową: forma opodatkowania (ryczałt – stawka dla licencji oprogramowania), fakturowanie Paddle jako usługi poza terytorium PL ("odwrotne obciążenie" / NP), czy wymagany VAT-UE (Paddle UK = poza UE → raczej nie, ale Paddle Payments Ltd jest w Irlandii – zależy, który podmiot będzie stroną umowy).
- Konto walutowe EUR dla JDG.

### 8.6 Kolejność na najbliższe tygodnie

1. Kupić domenę, postawić stronę produktu (GitHub Pages wystarczy): opis, cennik, T&C, Refund Policy, Privacy Policy, kontakt.
2. Założyć konto Paddle (produkcyjne + sandbox), zgłosić domenę do review, przejść KYC.
3. Równolegle: zmiany w repo wg §5 (licencje, `AvroLicense`, generator kluczy, testy).
4. Skonfigurować produkty/ceny w sandboxie, przetestować checkout i webhook.
5. Założyć konto EUR, ustawić payout.
6. Wznowić JDG.
7. Przełączyć na produkcję, opublikować AvroConvert 4.0.0, komunikat na GitHub.
