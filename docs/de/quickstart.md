# Schnellstart

ConfluentSynkMD in unter 5 Minuten einrichten und loslegen.

---

## Voraussetzungen

| Anforderung | Version | Hinweis |
|---|---|---|
| .NET SDK | 10.0+ | [Download](https://dotnet.microsoft.com/download) |
| Node.js | 22+ | Erforderlich für Mermaid-Diagramm-Rendering |
| Docker | Aktuell | Optional — empfohlen für konsistente Umgebungen |

---

## Option 1: Aus Quellcode bauen

=== "Bash"

    ```bash
    # Repository klonen
    git clone https://github.com/YOUR_USERNAME/ConfluentSynkMD.git
    cd ConfluentSynkMD

    # Bauen
    dotnet build
    ```

=== "PowerShell"

    ```powershell
    # Repository klonen
    git clone https://github.com/YOUR_USERNAME/ConfluentSynkMD.git
    Set-Location ConfluentSynkMD

    # Bauen
    dotnet build
    ```

=== "CMD"

    ```cmd
    REM Repository klonen
    git clone https://github.com/YOUR_USERNAME/ConfluentSynkMD.git
    cd ConfluentSynkMD

    REM Bauen
    dotnet build
    ```

## Option 2: Docker

=== "Bash"

    ```bash
    docker build -t confluentsynkmd .
    ```

=== "PowerShell"

    ```powershell
    docker build -t confluentsynkmd .
    ```

=== "CMD"

    ```cmd
    docker build -t confluentsynkmd .
    ```

---

## Zugangsdaten konfigurieren

Zugangsdaten können über **Umgebungsvariablen** oder **CLI-Flags** bereitgestellt werden.

### Umgebungsvariablen

Setzen Sie `CONFLUENCE__*`-Variablen in Ihrer Shell:

=== "Bash"

    ```bash
    export CONFLUENCE__BASEURL=https://yoursite.atlassian.net
    export CONFLUENCE__AUTHMODE=Basic
    export CONFLUENCE__USEREMAIL=ihre-email@example.com
    export CONFLUENCE__APITOKEN=ihr-api-token
    ```

=== "PowerShell"

    ```powershell
    $env:CONFLUENCE__BASEURL = "https://yoursite.atlassian.net"
    $env:CONFLUENCE__AUTHMODE = "Basic"
    $env:CONFLUENCE__USEREMAIL = "ihre-email@example.com"
    $env:CONFLUENCE__APITOKEN = "ihr-api-token"
    ```

=== "CMD"

    ```cmd
    set CONFLUENCE__BASEURL=https://yoursite.atlassian.net
    set CONFLUENCE__AUTHMODE=Basic
    set CONFLUENCE__USEREMAIL=ihre-email@example.com
    set CONFLUENCE__APITOKEN=ihr-api-token
    ```

### CLI-Flags

Übergeben Sie Zugangsdaten direkt als Kommandozeilenargumente:

=== "Bash"

    ```bash
    --conf-base-url https://yoursite.atlassian.net \
    --conf-auth-mode Basic \
    --conf-user-email ihre-email@example.com \
    --conf-api-token ihr-api-token
    ```

=== "PowerShell"

    ```powershell
    --conf-base-url https://yoursite.atlassian.net `
    --conf-auth-mode Basic `
    --conf-user-email ihre-email@example.com `
    --conf-api-token ihr-api-token
    ```

=== "CMD"

    ```cmd
    --conf-base-url https://yoursite.atlassian.net ^
    --conf-auth-mode Basic ^
    --conf-user-email ihre-email@example.com ^
    --conf-api-token ihr-api-token
    ```

!!! tip "API-Token"
    Generieren Sie einen API-Token unter [id.atlassian.com/manage-profile/security/api-tokens](https://id.atlassian.com/manage-profile/security/api-tokens).

!!! note "Optional: `.env`-Datei"
    Für lokalen Komfort können Sie Variablen in einer `.env`-Datei speichern und diese vor dem Start manuell laden. Das Tool lädt `.env`-Dateien **nicht** automatisch.

---

## Ihr erster Upload

Laden Sie einen Ordner mit Markdown-Dateien nach Confluence hoch:

### .NET

=== "Bash"

    ```bash
    dotnet run --project src/ConfluentSynkMD -- \
      --mode Upload \
      --path ./my-docs \
      --conf-space IHR_SPACE_KEY \
      --conf-parent-id IHRE_PAGE_ID
    ```

=== "PowerShell"

    ```powershell
    dotnet run --project src/ConfluentSynkMD -- `
      --mode Upload `
      --path ./my-docs `
      --conf-space IHR_SPACE_KEY `
      --conf-parent-id IHRE_PAGE_ID
    ```

=== "CMD"

    ```cmd
    dotnet run --project src/ConfluentSynkMD -- ^
      --mode Upload ^
      --path .\my-docs ^
      --conf-space IHR_SPACE_KEY ^
      --conf-parent-id IHRE_PAGE_ID
    ```

### Docker

=== "Bash"

    ```bash
    docker run --rm -it \
      -e CONFLUENCE__BASEURL=https://yoursite.atlassian.net \
      -e CONFLUENCE__AUTHMODE=Basic \
      -e CONFLUENCE__USEREMAIL=user@example.com \
      -e CONFLUENCE__APITOKEN=ihr-token \
      -v ${PWD}:/workspace \
      confluentsynkmd \
      --mode Upload \
      --path /workspace/my-docs \
      --conf-space IHR_SPACE_KEY \
      --conf-parent-id IHRE_PAGE_ID
    ```

=== "PowerShell"

    ```powershell
    docker run --rm -it `
      -e CONFLUENCE__BASEURL=https://yoursite.atlassian.net `
      -e CONFLUENCE__AUTHMODE=Basic `
      -e CONFLUENCE__USEREMAIL=user@example.com `
      -e CONFLUENCE__APITOKEN=ihr-token `
      -v ${PWD}:/workspace `
      confluentsynkmd `
      --mode Upload `
      --path /workspace/my-docs `
      --conf-space IHR_SPACE_KEY `
      --conf-parent-id IHRE_PAGE_ID
    ```

=== "CMD"

    ```cmd
    docker run --rm -it ^
      -e CONFLUENCE__BASEURL=https://yoursite.atlassian.net ^
      -e CONFLUENCE__AUTHMODE=Basic ^
      -e CONFLUENCE__USEREMAIL=user@example.com ^
      -e CONFLUENCE__APITOKEN=ihr-token ^
      -v %cd%:/workspace ^
      confluentsynkmd ^
      --mode Upload ^
      --path /workspace/my-docs ^
      --conf-space IHR_SPACE_KEY ^
      --conf-parent-id IHRE_PAGE_ID
    ```

---

## Ihr erster Download

Laden Sie Confluence-Seiten als Markdown herunter:

### .NET

=== "Bash"

    ```bash
    dotnet run --project src/ConfluentSynkMD -- \
      --mode Download \
      --path ./output \
      --conf-space IHR_SPACE_KEY \
      --conf-parent-id IHRE_PAGE_ID
    ```

=== "PowerShell"

    ```powershell
    dotnet run --project src/ConfluentSynkMD -- `
      --mode Download `
      --path ./output `
      --conf-space IHR_SPACE_KEY `
      --conf-parent-id IHRE_PAGE_ID
    ```

=== "CMD"

    ```cmd
    dotnet run --project src/ConfluentSynkMD -- ^
      --mode Download ^
      --path .\output ^
      --conf-space IHR_SPACE_KEY ^
      --conf-parent-id IHRE_PAGE_ID
    ```

### Docker

=== "Bash"

    ```bash
    docker run --rm -it \
      -e CONFLUENCE__BASEURL=https://yoursite.atlassian.net \
      -e CONFLUENCE__AUTHMODE=Basic \
      -e CONFLUENCE__USEREMAIL=user@example.com \
      -e CONFLUENCE__APITOKEN=ihr-token \
      -v ${PWD}:/workspace \
      confluentsynkmd \
      --mode Download \
      --path /workspace/output \
      --conf-space IHR_SPACE_KEY \
      --conf-parent-id IHRE_PAGE_ID
    ```

=== "PowerShell"

    ```powershell
    docker run --rm -it `
      -e CONFLUENCE__BASEURL=https://yoursite.atlassian.net `
      -e CONFLUENCE__AUTHMODE=Basic `
      -e CONFLUENCE__USEREMAIL=user@example.com `
      -e CONFLUENCE__APITOKEN=ihr-token `
      -v ${PWD}:/workspace `
      confluentsynkmd `
      --mode Download `
      --path /workspace/output `
      --conf-space IHR_SPACE_KEY `
      --conf-parent-id IHRE_PAGE_ID
    ```

=== "CMD"

    ```cmd
    docker run --rm -it ^
      -e CONFLUENCE__BASEURL=https://yoursite.atlassian.net ^
      -e CONFLUENCE__AUTHMODE=Basic ^
      -e CONFLUENCE__USEREMAIL=user@example.com ^
      -e CONFLUENCE__APITOKEN=ihr-token ^
      -v %cd%:/workspace ^
      confluentsynkmd ^
      --mode Download ^
      --path /workspace/output ^
      --conf-space IHR_SPACE_KEY ^
      --conf-parent-id IHRE_PAGE_ID
    ```

---

## Nächste Schritte

- **[Benutzerhandbuch](user/index.md)** — Alle Sync-Modi und Funktionen kennenlernen
- **[CLI-Referenz](reference/cli.md)** — Alle 40+ Kommandozeilen-Optionen
- **[Admin-Handbuch](admin/index.md)** — Docker-Deployment, Authentifizierung konfigurieren
