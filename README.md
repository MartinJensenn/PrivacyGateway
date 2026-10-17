# Privacy Gateway – POC

Et proof of concept på et "beskyttelseslag" mellem brugeren og en generativ AI-tjeneste.
Idéen er simpel: før en tekst bliver sendt til AI, bliver den scannet for personoplysninger,
saniteret og tjekket op mod nogle regler. Hvis teksten indeholder noget, der ikke må sendes
afsted, bliver den blokeret.

Projektet er min hovedopgave på Datamatikeruddannelsen (5. semester).

---

## Hvorfor det her projekt?

Mange organisationer vil gerne bruge generativ AI til fx udkast, opsummeringer og
klassificering. Problemet er, at de tekster, man arbejder med, tit indeholder
personoplysninger (PII) – og dem bør man ikke bare sende ukritisk til en ekstern AI-tjeneste.

Jeg ville undersøge, hvordan man kan placere et lag imellem, der reducerer risikoen for datalæk,
uden at man helt skal give afkald på at bruge AI.

### Problemformulering

> Hvordan kan man implementere en simpel Privacy Gateway i .NET, der reducerer risikoen ved
> brug af generativ AI ved at detektere og sanitere personoplysninger samt håndhæve policies
> (allow/block), før data sendes til en AI-model?

---

## Hvordan det virker

Hver request går igennem en pipeline i backend:

```
Bruger (Web UI)
      │
      ▼
.NET Web API
  1. Scan     → finder PII med regex (EMAIL, PHONE, CPR)
  2. Sanitér  → erstatter fund med tokens, fx <EMAIL_1>, <CPR_1>
  3. Policy   → CPR fundet? BLOCK. Ellers ALLOW.
  4. AI-kald  → kun hvis ALLOW: den saniterede tekst sendes til AI
  5. Audit    → beslutningen logges
      │
      ▼
Svar tilbage til brugeren (før/efter + AI-svar)
```

### Eksempel

**Input:**
```
Hej, kan du opsummere sagen? Kunden hedder Jens og kan kontaktes på
jens@example.com eller 12 34 56 78.
```

**Efter sanitering:**
```
Hej, kan du opsummere sagen? Kunden hedder Jens og kan kontaktes på
<EMAIL_1> eller <PHONE_1>.
```

**Policy:** `ALLOW` → teksten sendes til AI, og svaret vises i UI'et.

Hvis teksten i stedet indeholder et CPR-lignende nummer (fx `010190-1234`), bliver resultatet
`BLOCK`, og der bliver ikke sendt noget til AI.

> Al data i projektet er syntetisk testdata – der bruges ingen rigtige personoplysninger.

---

## Features

- **PII-scan** af e-mail, telefonnummer og CPR-lignende mønstre
- **Sanitering**, hvor fund erstattes med nummererede tokens
- **Policy-motor** med to udfald: `ALLOW` eller `BLOCK`
- **AI-kald kun fra backend** – API-nøglen ligger aldrig i frontend
- **Audit log**, der gemmer tidspunkt, policy-resultat og hvilke PII-typer der blev fundet
  (uden den rå tekst som standard)
- **Web UI**, hvor man indsætter tekst og ser før/efter samt AI'ens svar

---

## Teknologier

- .NET [version] – Web API
- [Frontend-teknologi, fx Blazor / React / ren HTML+JS]
- [AI-tjeneste, fx OpenAI / Azure OpenAI]
- [Testframework, fx xUnit]

---

## Kom i gang

### Krav
- .NET SDK [version]
- En API-nøgle til [AI-tjeneste]

### Opsætning

1. Klon repoet:
```bash
   git clone https://github.com/[brugernavn]/[repo].git
   cd [repo]
```

2. Sæt API-nøglen som user secret (så den ikke ender i koden eller på GitHub):
```bash
   cd [sti/til/api-projekt]
   dotnet user-secrets set "[AI:ApiKey]" "din-nøgle-her"
```

3. Start backend:
```bash
   dotnet run
```

4. Åbn UI'et på `[http://localhost:xxxx]`.

### Kør tests
```bash
dotnet test
```

---

## Audit log

For hver request gemmes:

| Felt | Eksempel |
|------|----------|
| Tidspunkt | 2026-10-17 12:00:00 |
| Policy-resultat | `ALLOW` / `BLOCK` |
| Fundne PII-typer | `EMAIL, PHONE` |

Den rå tekst bliver **ikke** gemt som standard – loggen skal kunne dokumentere beslutningen
uden selv at blive et sted, hvor følsomme data ligger.

---

## Afgrænsning

Det her er et proof of concept, ikke et færdigt produkt. Jeg har bevidst holdt det simpelt:

- Detektion er baseret på regex, så den er ikke perfekt – den kan både overse ting og give
  falske positiver.
- Der er kun to policy-udfald (`ALLOW` og `BLOCK`). Et godkendelsesflow er fravalgt i MVP'en.
- Fokus er på at vise dataflowet og sikkerhedsprincipperne, ikke på at fange alt.

---

## Succeskriterier

- [ ] Tekster med e-mail, telefon og CPR bliver korrekt flagget og saniteret
- [ ] Tekster med CPR bliver blokeret
- [ ] Tekster uden CPR kan sendes til AI i saniteret form og give et svar
- [ ] Audit-loggen dokumenterer beslutningen for hver request

---

## Mulige udvidelser

- Godkendelsesflow (`REQUIRE_APPROVAL`) for bestemte datatyper
- Flere PII-typer (navne, adresser, kontonumre)
- Bedre detektion, fx med NER/ML i stedet for kun regex
- Re-identifikation, så tokens kan sættes tilbage i AI-svaret
- Konfigurerbare policies

---

## Om projektet

Udviklet af **[Dit navn]** som hovedopgave på Datamatikeruddannelsen, [Uddannelsessted], 2026.
