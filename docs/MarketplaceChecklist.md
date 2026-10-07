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

## 4. Klucze licencyjne (po wydaniu 4.0 z `AvroLicense`)

Do tego czasu klucz nie jest potrzebny – biblioteka 4.0 bez klucza działa w pełni, tylko loguje ostrzeżenie; klienci kupujący wcześniej dostaną klucz e-mailem ręcznie.

- [ ] Wygeneruj parę Ed25519 narzędziem `tools/LicenseKeyGenerator` (powstanie w 4.0); **klucz prywatny poza repo** (menedżer haseł).
- [ ] Ręczny proces na start: Paddle → Transactions → nowy zakup → wygeneruj klucz (`licensee`, `product=avroconvert`, `plan`, `expires`) → wyślij e-mailem z `support@`.
- [ ] Automatyzacja (etap 2): Cloudflare Worker odbiera webhook `transaction.completed` (weryfikacja podpisu `Paddle-Signature`), generuje klucz, wysyła e-mail (Resend / Cloudflare Email Workers). Sekret webhooka i klucz prywatny jako Worker Secrets.

## 5. Po starcie

- [ ] Komunikat o zmianie licencji: GitHub Release 4.0.0 + pinned issue + sekcja w README (3.4.x pozostaje CC BY-NC-SA; 4.x PolyForm + Commercial).
- [ ] Klienci z licencją Xabe: zniżka/kod na pierwszy rok (Paddle → Discounts).
- [ ] Paddle → Reports → ustaw miesięczny raport na e-mail dla księgowej.

## Co jest po stronie repo (zrobię ja, na gałęzi `release/4.0`)

Zgodnie z `V4ReleasePlan.md` §4.5: migracja na net10.0 → interop tests → poprawki spec → typowany rdzeń → `System.Text.Json` → licencja PolyForm/Commercial w repo i paczce → `AvroLicense` + generator kluczy → CHANGELOG → publikacja 4.0.0 na nuget.org (właściciel: Ty; paczka z nową ikoną `avroconvert-nuget-128.png`).
