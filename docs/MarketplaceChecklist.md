# Marketplace (Paddle) – checklista uruchomienia

Co musisz zrobić **sam** (konta, tożsamość, płatności), w kolejności, która nie blokuje prac nad 4.0. Zmiany w kodzie i na stronie, które z tego wynikają, oznaczono „→ repo”.

Stan 07.10.2026: strona `https://soltechnology.dev` działa (GitHub Pages, HTTPS), cennik Community 0 / Business 189 / Enterprise 1 299 USD. Przyciski zakupu prowadzą na e-mail do czasu podpięcia Paddle.

## 0. Przed założeniem konta Paddle (dziś)

- [ ] **Dane prawne na stronie.** W `sol-technology.github.io/src/config.ts` uzupełnij `legalAddress` i `legalTaxId`; w `src/pages/terms.md` i `privacy.md` zamień `[Street, Postal code City]`, `NIP [●]`, `[provider – to be confirmed]` (dostawca poczty). Paddle Domain Review wymaga pełnej nazwy prawnej JDG w T&C – bez tego odrzucą.
- [ ] **E-mail na domenie.** `contact@soltechnology.dev` i `support@soltechnology.dev` są już w T&C i na stronie. Najprościej: Cloudflare → Email → Email Routing → dodaj oba adresy z przekierowaniem na Gmaila (darmowe, 5 min). Przetestuj wysyłkę **do** nich.
- [ ] **Push strony** (3 commity czekają lokalnie w `sol-technology.github.io`).
- [ ] Avatar organizacji GitHub: `sol-technology.github.io/public/brand/github-avatar.png`.

## 1. Konto Paddle – sandbox (można od razu)

- [ ] Załóż **sandbox**: `https://sandbox-login.paddle.com/signup` (osobne konto od produkcyjnego, bez KYC, bez zatwierdzania domeny).
- [ ] Developer Tools → Authentication → **Client-side token** → skopiuj.
- [ ] Catalog → Products → dodaj produkt **AvroConvert**, tax category: *Standard digital goods* (lub *Software*).
- [ ] Prices (oba `billing_cycle: year`, waluta USD, `quantity 1–1`):
  - `AvroConvert Business` – 189.00
  - `AvroConvert Enterprise` – 1299.00
  - Skopiuj oba `pri_…` id.
- [ ] → repo strony: `src/config.ts` → `paddle.environment = "sandbox"`, `clientToken`, `prices.avroconvertBusiness`, `prices.avroconvertEnterprise`. Po buildzie przyciski otwierają overlay Paddle. Test kartą `4242 4242 4242 4242`.
- [ ] Checkout settings → Default payment link: `https://soltechnology.dev/avroconvert/` (Paddle używa go w e-mailach).

## 2. Konto Paddle – produkcja

- [ ] Załóż konto na `https://www.paddle.com/get-started` jako **sole trader / individual**, kraj Polska, dane JDG (nazwa z CEIDG, adres, NIP). Business Identification dla JDG jest pomijana; Identity Verification = dokument tożsamości.
- [ ] Settings → **Website approval** → zgłoś `soltechnology.dev`. Review 1–7 dni roboczych. Strona spełnia wymagania (opis, cennik, funkcje, T&C/Refund/Privacy w stopce, HTTPS), warunkiem jest pkt 0.
- [ ] Settings → **Payouts**: waluta wypłat **EUR** (brak PLN; USD/GBP też możliwe), konto bankowe EUR z IBAN (Wise / Revolut Business / konto walutowe w banku). Próg wypłaty 100 EUR, wypłata do 15. dnia następnego miesiąca.
- [ ] Settings → Currencies: ceny w USD, włącz lokalne waluty (Paddle przelicza automatycznie).
- [ ] Settings → Tax: zaznacz, że ceny są **netto** (tax exclusive) – zgodnie z tekstem na stronie.
- [ ] Odtwórz produkt i ceny z sandboxa (identyczne nazwy/kwoty); skopiuj produkcyjny client-side token i `pri_…`.
- [ ] → repo strony: `paddle.environment = "production"` + produkcyjne tokeny. Commit, push.
- [ ] Notifications → **Webhook destination**: na razie pomiń (pkt 4), ale zapisz sobie, że tam się to włącza.

## 3. Formalności PL (równolegle z pkt 2)

- [ ] Księgowa: forma opodatkowania (ryczałt – stawka dla licencji oprogramowania do potwierdzenia), rozliczanie przychodu od Paddle (faktura/self-billing na Paddle.com Market Ltd, UK – usługa poza terytorium PL, bez VAT), czy potrzebny VAT-UE.
- [ ] **Wznowienie JDG** w CEIDG – dopiero gdy Domain Review przejdzie i produkcja jest gotowa (ZUS liczy się od dnia wznowienia).
- [ ] Konto EUR z pkt 2 założone na JDG.

## 4. Klucze licencyjne

Klucz jest podpisany (ECDSA P-256), ale nieegzekwowany – biblioteka bez klucza działa w pełni i tylko loguje ostrzeżenie (szczegóły: `LicensingMigrationPlan.md` §5.4). Para kluczy już istnieje: publiczny w `AvroLicense`, prywatny w `~/.config/soltechnology/avroconvert-signing.pem`.

- [ ] Skopiuj `avroconvert-signing.pem` do menedżera haseł (utrata = wszystkie wydane klucze przestają się weryfikować przy ponownym wygenerowaniu).
- [ ] Sprzedaż ręczna (oferty, PO, przelewy, klienci Xabe): w repo Workera `scripts/issue.sh --licensee "Nazwa z faktury" --email kupujacy@firma.com --plan business|enterprise --reference NR-FAKTURY` – ta sama ścieżka co zakup przez Paddle (D1, PDF, mail). Odnowienie: `--id` z poprzedniego klucza, nowe `--expires`. Wymaga sekretu `ADMIN_TOKEN` (patrz README Workera).
- [ ] Przy wsparciu: `dotnet run scripts/verify.cs -- "AVC1.…"` w repo Workera pokazuje, co klient ma w kluczu.

### 4.1 Automat (Cloudflare Worker) – plan

Osobne prywatne repo `Sol-Technology/sol-technology-licensing` (TypeScript, `wrangler`), bo zawiera szablony maili i logikę fulfillmentu; sekrety wyłącznie jako Worker Secrets.

**Przepływ**
1. Strona (`avroconvert.astro`): przed `Paddle.Checkout.open` pole „Licensee (company name as it should appear on the licence)” – wymagane, przekazywane jako `customData.licensee`. Paddle Checkout dodatkowo zbiera dane firmy (`business`) na życzenie klienta, ale pole na stronie jest jedynym pewnym źródłem nazwy.
2. Paddle → `POST /paddle/webhook` na `transaction.completed`. Worker: weryfikacja `Paddle-Signature` (`ts` + `h1` = HMAC-SHA256 sekretu nad `${ts}:${rawBody}`, tolerancja 5 min), idempotencja po `event_id` (D1), mapowanie `items[].price.id` → plan (zmienne `PRICE_BUSINESS`, `PRICE_ENTERPRISE`), `expires` = `billing_period.ends_at`, licencjobiorca = `custom_data.licensee` → `business.name` (API `GET /customers/{id}/businesses/{id}`) → nazwa klienta.
3. Numer licencji: nowy dla nowej subskrypcji; dla `origin = subscription_recurring` ten sam `id` z D1 po `subscription_id`, nowy `expires`.
4. Podpis: WebCrypto `importKey("pkcs8", …, {name:"ECDSA", namedCurve:"P-256"})` + `sign({name:"ECDSA", hash:"SHA-256"})` – daje format P1363, który czyta .NET.
5. D1 `licenses(id, subscription_id, transaction_id, customer_id, email, licensee, plan, issued, expires, key, status)`.
6. Certyfikat PDF (`pdf-lib`): logo, numer, licencjobiorca, plan, okres, odnośnik do `licenses/Commercial.md`. E-mail przez Resend z `licensing@soltechnology.dev` (reply-to `support@`), klucz w treści + PDF w załączniku + instrukcja `AvroConvert.License = AvroLicense.Commercial("…")`, BCC na własną skrzynkę.
7. `adjustment.created` (refund/chargeback) → `status = revoked` w D1 + mail do Ciebie; technicznie nic się nie dzieje (brak egzekucji). `subscription.canceled` → nic; klucz wygasa sam.

**Sekrety**: `PADDLE_WEBHOOK_SECRET`, `PADDLE_API_KEY` (read-only wystarczy), `SIGNING_KEY_PEM` (`wrangler secret put SIGNING_KEY_PEM < ~/.config/soltechnology/avroconvert-signing.pem`), `RESEND_API_KEY`.

**Kroki**
- [ ] Resend: konto, domena `soltechnology.dev` (SPF/DKIM w Cloudflare DNS), adres `licensing@`.
- [x] Repo + Worker: `~/Documents/GitHub/licensing` → `https://github.com/Sol-Technology/sol-technology-licensing` (prywatne; webhook, `POST /admin/issue`, podpis, D1, Resend, certyfikat PDF, testy); wdrożony na `licensing.soltechnology.dev`, klucz z Workera zweryfikowany parserem .NET. Kroki wdrożenia w jego README.
- [ ] Paddle sandbox → Notifications → destination `https://licensing.soltechnology.dev/paddle/webhook` (custom domain Workera), zdarzenia `transaction.completed`, `adjustment.created`; test „Simulate” + prawdziwy zakup testową kartą.
- [ ] Strona: pole licensee + `customData`; tekst „A licence key by e-mail within minutes” już jest.
- [ ] Produkcja: te same kroki z produkcyjnym sekretem webhooka i `PRICE_*`.
- [ ] Etap 2 (opcjonalnie): `GET /licenses/resend?email=` do samodzielnego ponownego wysłania klucza; miesięczny eksport D1 dla księgowej.

## 5. Po starcie

- [ ] Komunikat o zmianie licencji: GitHub Release 4.0.0 + pinned issue + sekcja w README (3.4.x pozostaje CC BY-NC-SA; 4.x PolyForm + Commercial).
- [ ] Klienci z licencją Xabe: zniżka/kod na pierwszy rok (Paddle → Discounts).
- [ ] Paddle → Reports → ustaw miesięczny raport na e-mail dla księgowej.

## Co jest po stronie repo (zrobię ja, na gałęzi `release/4.0`)

Zgodnie z `V4ReleasePlan.md` §4.5: migracja na net10.0 → interop tests → poprawki spec → typowany rdzeń → `System.Text.Json` → licencja PolyForm/Commercial w repo i paczce → `AvroLicense` + generator kluczy → CHANGELOG → publikacja 4.0.0 na nuget.org (właściciel: Ty; paczka z nową ikoną `avroconvert-nuget-128.png`).
