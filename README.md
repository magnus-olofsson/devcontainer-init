# devcontainer-init (`dci`)

CLI-verktyg för att snabbt sätta upp en devcontainer i ett projekt. Verktyget ansluter till ett GitHub-repo med färdiga templates, låter dig välja en, och laddar ner `devcontainer.json` (och eventuell Dockerfile) till din projektmapp.

## Hur det fungerar

1. Kör `dci` i din projektmapp
2. Verktyget hämtar listan över tillgängliga templates från ditt GitHub-repo
3. Du väljer en template från listan
4. `devcontainer.json` sparas till `.devcontainer/` i din projektmapp
5. Om templaten har en Dockerfile och imagen inte finns lokalt erbjuds du att ladda ner och bygga den
6. Öppna mappen i VS Code och kör **Reopen in Container**

## Template-repots struktur

Skapa ett GitHub-repo (publikt) med följande struktur:

```
my-devcontainer-templates/
└── templates/
    ├── dotnet/
    │   ├── devcontainer.json   # obligatorisk
    │   ├── Dockerfile          # valfri
    │   └── README.md           # valfri — visas i listan vid val
    ├── python/
    │   ├── devcontainer.json
    │   ├── Dockerfile
    │   └── README.md
    └── node/
        └── devcontainer.json
```

- Varje undermapp i `templates/` blir en valbara template
- `devcontainer.json` är obligatorisk
- `Dockerfile` är valfri — om den finns erbjuds användaren att bygga imagen
- `README.md` är valfri — första textraden (ej rubrik) visas som beskrivning i listan

### Exempel på devcontainer.json

```json
{
  "name": "Python 3.12",
  "image": "my-org/devcontainer-python:latest",
  "customizations": {
    "vscode": {
      "extensions": ["ms-python.python"]
    }
  }
}
```

Om `image` anges används det för att kontrollera om Docker-imagen redan finns lokalt.

## Konfiguration

`dci` använder en global config som kompletteras med en valfri lokal override per projekt.

### Global config

Skapas automatiskt vid första körningen. Ligger på:

| OS      | Sökväg                              |
|---------|-------------------------------------|
| Windows | `C:\Users\<användare>\.dci\config.json` |
| macOS   | `/Users/<användare>/.dci/config.json`   |
| Linux   | `/home/<användare>/.dci/config.json`    |

**Exempel:**
```json
{
  "GitHubOwner": "mitt-användarnamn",
  "GitHubRepo": "mina-devcontainer-templates",
  "TemplatesPath": "templates"
}
```

### Lokal config (override per projekt)

Skapa en `.dci.json` i projektmappen för att peka om mot ett annat repo för just det projektet. Alla fält är valfria — bara de som anges skriver över den globala configen.

```json
{
  "GitHubOwner": "annan-org",
  "GitHubRepo": "andra-templates"
}
```

## Bygga och publicera

Kräver [.NET 10 SDK](https://dotnet.microsoft.com/download).

### Bygga

```powershell
dotnet build src/DevcontainerInit/DevcontainerInit.csproj
```

### Publicera som fristående binär

```powershell
dotnet publish src/DevcontainerInit/DevcontainerInit.csproj -c Release -r win-x64
```

Byt ut `win-x64` mot `osx-arm64`, `osx-x64` eller `linux-x64` för andra plattformar.

Binären (`dci.exe` på Windows, `dci` på macOS/Linux) hamnar i:

```
src/DevcontainerInit/bin/Release/net10.0/<rid>/publish/
```

Lägg till den mappen i din `PATH` eller kopiera binären till en mapp som redan finns i `PATH`.

## Att tänka på

**GitHub API-gränser** — `dci` använder GitHubs publika API utan autentisering, vilket ger 60 anrop per timme per IP. För personligt bruk räcker det gott. Verktyget hämtar hela filträdet i ett enda API-anrop och minimerar därmed antalet requests.

**Repot måste vara publikt** — autentisering stöds inte i nuläget.

**Befintlig `.devcontainer/`-mapp** — verktyget skriver över `devcontainer.json` om den redan finns, utan varning.
