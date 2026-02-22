# Schnellstart

ConfluenceSynkMD in unter 5 Minuten einrichten und loslegen.

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
    git clone https://github.com/OpenDocSync/ConfluenceSynkMD.git
    cd ConfluenceSynkMD

    # Bauen
    dotnet build
    ```

=== "PowerShell"

    ```powershell
    # Repository klonen
    git clone https://github.com/OpenDocSync/ConfluenceSynkMD.git
    Set-Location ConfluenceSynkMD

    # Bauen
    dotnet build
    ```

=== "CMD"

    ```cmd
    REM Repository klonen
    git clone https://github.com/OpenDocSync/ConfluenceSynkMD.git
    cd ConfluenceSynkMD

    REM Bauen
    dotnet build
    ```

## Option 2: Docker

=== "Bash"

    ```bash
    docker build -t confluencesynkmd .
    ```

=== "PowerShell"

    ```powershell
    docker build -t confluencesynkmd .
    ```

=== "CMD"

    ```cmd
    docker build -t confluencesynkmd .
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

---

## Ihr erster Upload

Laden Sie einen Ordner mit Markdown-Dateien nach Confluence hoch:

### .NET

=== "Bash"

    ```bash
    dotnet run --project src/ConfluenceSynkMD -- \
      --mode Upload \
      --path ./my-docs \
      --conf-space IHR_SPACE_KEY \
      --conf-parent-id IHRE_PAGE_ID
    ```

=== "PowerShell"

    ```powershell
    dotnet run --project src/ConfluenceSynkMD -- `
      --mode Upload `
      --path ./my-docs `
      --conf-space IHR_SPACE_KEY `
      --conf-parent-id IHRE_PAGE_ID
    ```

=== "CMD"

    ```cmd
    dotnet run --project src/ConfluenceSynkMD -- ^
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
      confluencesynkmd \
      --mode Upload \
      --path /workspace/my-docs \
      --conf-space IHR_SPACE_KEY \
      --conf-parent-id IHRE_PAGE_ID
    ```

    ???+ tip "Pfad-Hinweis"
        - `-v ${PWD}:/workspace` bleibt unverändert.
        - Passen Sie nur den `--path`-Suffix an (z. B. `/workspace/docs`, `/workspace/my-docs`).
        - `${PWD}` ist Ihr aktueller lokaler Ordner.

=== "PowerShell"

    ```powershell
    docker run --rm -it `
      -e CONFLUENCE__BASEURL=https://yoursite.atlassian.net `
      -e CONFLUENCE__AUTHMODE=Basic `
      -e CONFLUENCE__USEREMAIL=user@example.com `
      -e CONFLUENCE__APITOKEN=ihr-token `
      -v ${PWD}:/workspace `
      confluencesynkmd `
      --mode Upload `
      --path /workspace/my-docs `
      --conf-space IHR_SPACE_KEY `
      --conf-parent-id IHRE_PAGE_ID
    ```

    ???+ tip "Pfad-Hinweis"
        - `-v ${PWD}:/workspace` bleibt unverändert.
        - Passen Sie nur den `--path`-Suffix an (z. B. `/workspace/docs`, `/workspace/my-docs`).
        - `${PWD}` ist Ihr aktueller lokaler Ordner.

=== "CMD"

    ```cmd
    docker run --rm -it ^
      -e CONFLUENCE__BASEURL=https://yoursite.atlassian.net ^
      -e CONFLUENCE__AUTHMODE=Basic ^
      -e CONFLUENCE__USEREMAIL=user@example.com ^
      -e CONFLUENCE__APITOKEN=ihr-token ^
      -v %cd%:/workspace ^
      confluencesynkmd ^
      --mode Upload ^
      --path /workspace/my-docs ^
      --conf-space IHR_SPACE_KEY ^
      --conf-parent-id IHRE_PAGE_ID
    ```

    ???+ tip "Pfad-Hinweis"
        - `-v %cd%:/workspace` bleibt unverändert.
        - Passen Sie nur den `--path`-Suffix an (z. B. `/workspace/docs`, `/workspace/my-docs`).
        - `%cd%` ist Ihr aktueller lokaler Ordner.

#### Docker-Pfade richtig setzen

Die häufigste Fehlerquelle ist die Kombination aus `-v` (Volume-Mount) und `--path`.

- **Unverändert lassen:** Der Container-Pfad `/workspace` bleibt gleich, solange Sie `-v ...:/workspace` verwenden.
- **Anpassen:** Nur der Teil **hinter** `/workspace` in `--path` wird an Ihren lokalen Ordner angepasst.
- **Wertquelle:** `${PWD}` (Bash/PowerShell) bzw. `%cd%` (CMD) ist Ihr aktueller lokaler Ordner.

| Lokaler Startordner | Mount | `--path` im Container |
|---|---|---|
| Projektordner mit Unterordner `docs` | `-v ${PWD}:/workspace` | `--path /workspace/docs` |
| Sie stehen bereits im Ordner `docs` | `-v ${PWD}:/workspace` | `--path /workspace` |
| Explizit nur `docs` mounten | `-v ${PWD}/docs:/workspace` | `--path /workspace` |

Wenn `-v` rechts nicht `/workspace` ist, muss `--path` auf denselben Container-Basispfad zeigen.

---

## Ihr erster Download

Laden Sie Confluence-Seiten als Markdown herunter:

### .NET

=== "Bash"

    ```bash
    dotnet run --project src/ConfluenceSynkMD -- \
      --mode Download \
      --path ./output \
      --conf-space IHR_SPACE_KEY \
      --conf-parent-id IHRE_PAGE_ID
    ```

=== "PowerShell"

    ```powershell
    dotnet run --project src/ConfluenceSynkMD -- `
      --mode Download `
      --path ./output `
      --conf-space IHR_SPACE_KEY `
      --conf-parent-id IHRE_PAGE_ID
    ```

=== "CMD"

    ```cmd
    dotnet run --project src/ConfluenceSynkMD -- ^
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
      confluencesynkmd \
      --mode Download \
      --path /workspace/output \
      --conf-space IHR_SPACE_KEY \
      --conf-parent-id IHRE_PAGE_ID
    ```

    ???+ tip "Pfad-Hinweis"
        - `-v ${PWD}:/workspace` bleibt unverändert.
        - Für Download zeigt `--path` auf den Zielordner im Container, z. B. `/workspace/output`.
        - `${PWD}` ist Ihr aktueller lokaler Ordner.

=== "PowerShell"

    ```powershell
    docker run --rm -it `
      -e CONFLUENCE__BASEURL=https://yoursite.atlassian.net `
      -e CONFLUENCE__AUTHMODE=Basic `
      -e CONFLUENCE__USEREMAIL=user@example.com `
      -e CONFLUENCE__APITOKEN=ihr-token `
      -v ${PWD}:/workspace `
      confluencesynkmd `
      --mode Download `
      --path /workspace/output `
      --conf-space IHR_SPACE_KEY `
      --conf-parent-id IHRE_PAGE_ID
    ```

    ???+ tip "Pfad-Hinweis"
        - `-v ${PWD}:/workspace` bleibt unverändert.
        - Für Download zeigt `--path` auf den Zielordner im Container, z. B. `/workspace/output`.
        - `${PWD}` ist Ihr aktueller lokaler Ordner.

=== "CMD"

    ```cmd
    docker run --rm -it ^
      -e CONFLUENCE__BASEURL=https://yoursite.atlassian.net ^
      -e CONFLUENCE__AUTHMODE=Basic ^
      -e CONFLUENCE__USEREMAIL=user@example.com ^
      -e CONFLUENCE__APITOKEN=ihr-token ^
      -v %cd%:/workspace ^
      confluencesynkmd ^
      --mode Download ^
      --path /workspace/output ^
      --conf-space IHR_SPACE_KEY ^
      --conf-parent-id IHRE_PAGE_ID
    ```

    ???+ tip "Pfad-Hinweis"
        - `-v %cd%:/workspace` bleibt unverändert.
        - Für Download zeigt `--path` auf den Zielordner im Container, z. B. `/workspace/output`.
        - `%cd%` ist Ihr aktueller lokaler Ordner.

---

## Nächste Schritte

- **[Benutzerhandbuch](user/index.md)** — Alle Sync-Modi und Funktionen kennenlernen
- **[CLI-Referenz](reference/cli.md)** — Alle 40+ Kommandozeilen-Optionen
- **[Admin-Handbuch](admin/index.md)** — Docker-Deployment, Authentifizierung konfigurieren
