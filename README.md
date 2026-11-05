# BookMatch – en säker AI-chattbot för boktips

BookMatch är en webbaserad AI-chattbot som hjälper användare att hitta böcker. Användaren väljer språk, genre och läsnivå och fårs boktips, och kan sedan ställa följdfrågor i samma chatt – till exempel hur en bok har tagits emot av läsare och kritiker.

Projektets fokus ligger inte bara på funktionerna, utan på att göra en AI-tjänst **säker och pålitlig**: skydd mot prompt injection, säker inloggning, validering av input och systematiska tester av hur ofta AI:n hittar på information.

> Boktipsen genereras av AI och kan innehålla fel.

## Funktioner

- Boktips utifrån språk (18 språk), genre och läsnivå
- Följdfrågor i samma chatt, med klickbara förslag
- Svar som strömmas ord för ord med SSE (Server-Sent Events)
- Användarkonton – inloggade får fler frågor och sin senaste chatt sparad
- Radering av sparad chatt
- Enkel, responsiv design

## Säkerhet

Säkerheten bygger på flera lager, så att ett enskilt fel inte räcker för en lyckad attack.

| Hot                               | Skydd                                                                                                                                                                |
| --------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Prompt injection                  | Strukturerad input med vitlista, system prompt med säkerhetsregler, avgränsning av användartext, påminnelse efter varje fråga, historik behandlas som otillförlitlig |
| XSS (Cross-Site Scripting)        | AI-svar visas alltid som text (`textContent`), strikt CSP (Content Security Policy) utan inline-skript                                                               |
| CSRF (Cross-Site Request Forgery) | `SameSite=Strict`-cookies och antiforgery-token i header                                                                                                             |
| Stöld av inloggning               | Cookie-baserad inloggning med `HttpOnly` och `Secure`                                                                                                                |
| Gissning av lösenord              | Lockout efter 5 felaktiga försök, rate limiting per IP-adress                                                                                                        |
| Missbruk och kostnader            | Rate limiting i två nivåer: per IP-adress för ej inloggade, per användare för inloggade                                                                              |
| Åtkomst till andras historik      | Användar-id från inloggningscookien i varje databasfråga, inga id:n i adresser (skydd mot IDOR)                                                                      |
| Läckage av API-nyckel             | Nyckeln finns bara på servern i User Secrets                                                                                                                         |
| Clickjacking                      | `frame-ancestors 'none'` och `X-Frame-Options: DENY`                                                                                                                 |

Personuppgifter: meddelanden loggas aldrig, användaren ser bara sin egen historik och kan radera den.

## Teknik

| Del        | Teknik                                                                                               |
| ---------- | ---------------------------------------------------------------------------------------------------- |
| Backend    | ASP.NET Core (.NET 10), Minimal APIs                                                                 |
| AI         | Google Gemini via `IChatClient` (Microsoft.Extensions.AI) – leverantören kan bytas i konfigurationen |
| Frontend   | HTML, CSS och JavaScript i `wwwroot`, samma ursprung som backend                                     |
| Databas    | SQLite med Entity Framework Core                                                                     |
| Inloggning | ASP.NET Core Identity med cookies                                                                    |
| Tester     | xUnit, SQLite i minnet                                                                               |

````

## Kom igång

### Krav

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- En API-nyckel från [Google AI Studio](https://aistudio.google.com/)
- Verktyget för migrationer:

```bash
dotnet tool install --global dotnet-ef
````

### 1. Klona projektet

```bash
git clone https://github.com/felhu0/BookMatch.git
cd BookMatch
```

### 2. Lägg in API-nyckeln i User Secrets

Nyckeln sparas utanför projektmappen och hamnar aldrig i Git.

```bash
dotnet user-secrets set "Gemini:ApiKey" "DIN-NYCKEL" --project src/BookMatch.Api
```

Modell och endpoint finns i `src/BookMatch.Api/appsettings.json` och kan ändras där.

### 3. Skapa databasen

```bash
dotnet ef database update --project src/BookMatch.Api
```

### 4. Starta appen

Appen måste köras över HTTPS, eftersom inloggningscookien bara skickas över HTTPS.

```bash
dotnet run --project src/BookMatch.Api --launch-profile https
```

Öppna adressen som visas i terminalen, till exempel `https://localhost:7051`.

### 5. Kör testerna

```bash
dotnet test
```

## Testning

- **Enhetstester** för validering, prompt-byggaren (roller, avgränsning och påminnelser) och behörighet för sparad historik
- **Attacklista** med 12 prompt injection-attacker och 16 webbsäkerhetstester – se [`docs/tester/attacklist.md.txt`](docs/tests/attacklist.md.txt)
- **Hallucinationstester** med frågor om påhittade böcker och kontroll av boktips mot en annan AI, Libris och Open Library – se [`docs/tests/hallucinationtests.md.txt`](docs/tests/hallucinationtests.md.txt)

## Begränsningar

- AI:n använder sin egen kunskap och kan ibland ge fel genre, fel detaljer eller fel översatt titel – särskilt för mindre kända böcker och på andra språk än engelska
- Bara den senaste konversationen sparas för inloggade
- Rate limiting sparas i serverns minne och nollställs vid omstart
- Med Geminis gratisnivå kan Google använda meddelanden för att förbättra sina produkter – skriv inga personuppgifter i chatten
