# Installation

ConfluenceSynkMD kann durch Bauen aus dem Quellcode mit dem .NET SDK installiert werden.

---

## Voraussetzungen

| Anforderung | Version | Zweck |
|---|---|---|
| **.NET SDK** | 10.0+ | CLI-Tool bauen und ausführen |
| **Node.js** | 22+ | Für Mermaid-Diagramm-Rendering |
| **npm** | Aktuell | mermaid-cli installieren |

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

## Mermaid CLI installieren

=== "Bash"

    ```bash
    npm install -g @mermaid-js/mermaid-cli
    mmdc --version
    ```

=== "PowerShell"

    ```powershell
    npm install -g @mermaid-js/mermaid-cli
    mmdc --version
    ```

=== "CMD"

    ```cmd
    npm install -g @mermaid-js/mermaid-cli
    mmdc --version
    ```

---

## Nächste Schritte

- [Konfiguration & Umgebung](configuration.md) — Confluence-Zugangsdaten einrichten
- [Docker-Deployment](docker.md) — In einem Container mit allen Abhängigkeiten ausführen
