# Installation

ConfluenceSynkMD kann durch Bauen aus dem Quellcode mit dem .NET SDK installiert werden.

---

## Voraussetzungen

| Anforderung | Version | Zweck |
|---|---|---|
| **.NET SDK** | 10.0+ | CLI-Tool bauen und ausführen |
| **Docker** | Aktuell | Für Mermaid-Diagramm-Rendering |

---

## Aus Quellcode bauen

=== "Bash"

    ```bash
    git clone https://github.com/OpenDocSync/ConfluenceSynkMD.git
    cd ConfluenceSynkMD
    dotnet restore
    dotnet build
    ```

=== "PowerShell"

    ```powershell
    git clone https://github.com/OpenDocSync/ConfluenceSynkMD.git
    Set-Location ConfluenceSynkMD
    dotnet restore
    dotnet build
    ```

=== "CMD"

    ```cmd
    git clone https://github.com/OpenDocSync/ConfluenceSynkMD.git
    cd ConfluenceSynkMD
    dotnet restore
    dotnet build
    ```

### Direkt ausführen

=== "Bash"

    ```bash
    dotnet run --project src/ConfluenceSynkMD -- --help
    ```

=== "PowerShell"

    ```powershell
    dotnet run --project src/ConfluenceSynkMD -- --help
    ```

=== "CMD"

    ```cmd
    dotnet run --project src/ConfluenceSynkMD -- --help
    ```

### Eigenständige Binärdatei erstellen

=== "Bash"

    ```bash
    dotnet publish src/ConfluenceSynkMD/ConfluenceSynkMD.csproj \
      -c Release -o ./publish --self-contained true -r linux-x64
    ```

=== "PowerShell"

    ```powershell
    dotnet publish src/ConfluenceSynkMD/ConfluenceSynkMD.csproj `
      -c Release -o ./publish --self-contained true -r linux-x64
    ```

=== "CMD"

    ```cmd
    dotnet publish src/ConfluenceSynkMD/ConfluenceSynkMD.csproj ^
      -c Release -o .\publish --self-contained true -r linux-x64
    ```

---

## Docker-Voraussetzung für Mermaid

Das Rendering von Mermaid-Diagrammen verwendet das offizielle Mermaid CLI Docker-Image (`ghcr.io/mermaid-js/mermaid-cli/mermaid-cli`). ConfluenceSynkMD startet dynamisch einen temporären Docker-Container für diese Aufgabe.

Stellen Sie sicher, dass die **Docker Engine** installiert ist und der Befehl `docker` in Ihrem `PATH` verfügbar ist.

Überprüfen: `docker --version`

---

## Nächste Schritte

- [Konfiguration & Umgebung](configuration.md) — Confluence-Zugangsdaten einrichten
- [Docker-Deployment](docker.md) — In einem Container mit allen Abhängigkeiten ausführen
