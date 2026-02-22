# Installation

ConfluenceSynkMD can be installed by building from source with the .NET SDK.

---

## Prerequisites

| Requirement | Version | Purpose |
|---|---|---|
| **.NET SDK** | 10.0+ | Build and run the CLI tool |
| **Node.js** | 22+ | Required for Mermaid diagram rendering |
| **npm** | Latest | Install mermaid-cli |

---

## Build from Source

=== "Bash"

    ```bash
    # Clone the repository
    git clone https://github.com/OpenDocSync/ConfluenceSynkMD.git
    cd ConfluenceSynkMD

    # Restore dependencies
    dotnet restore

    # Build
    dotnet build
    ```

=== "PowerShell"

    ```powershell
    # Clone the repository
    git clone https://github.com/OpenDocSync/ConfluenceSynkMD.git
    Set-Location ConfluenceSynkMD

    # Restore dependencies
    dotnet restore

    # Build
    dotnet build
    ```

=== "CMD"

    ```cmd
    REM Clone the repository
    git clone https://github.com/OpenDocSync/ConfluenceSynkMD.git
    cd ConfluenceSynkMD

    REM Restore dependencies
    dotnet restore

    REM Build
    dotnet build
    ```

### Run Directly

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

### Publish Standalone Binary

Create a self-contained executable:

=== "Bash"

    ```bash
    dotnet publish src/ConfluenceSynkMD/ConfluenceSynkMD.csproj \
      -c Release \
      -o ./publish \
      --self-contained true \
      -r linux-x64    # or win-x64, osx-arm64
    ```

=== "PowerShell"

    ```powershell
    dotnet publish src/ConfluenceSynkMD/ConfluenceSynkMD.csproj `
      -c Release `
      -o ./publish `
      --self-contained true `
      -r linux-x64    # or win-x64, osx-arm64
    ```

=== "CMD"

    ```cmd
    dotnet publish src/ConfluenceSynkMD/ConfluenceSynkMD.csproj ^
      -c Release ^
      -o .\publish ^
      --self-contained true ^
      -r linux-x64
    ```

The `./publish/ConfluenceSynkMD` binary can be copied to any machine without requiring .NET to be installed.

---

## Install Mermaid CLI

Mermaid rendering requires `@mermaid-js/mermaid-cli`:

=== "Bash"

    ```bash
    npm install -g @mermaid-js/mermaid-cli
    ```

=== "PowerShell"

    ```powershell
    npm install -g @mermaid-js/mermaid-cli
    ```

=== "CMD"

    ```cmd
    npm install -g @mermaid-js/mermaid-cli
    ```

Verify: `mmdc --version`

---

## Optional: PlantUML, Draw.io

See [Diagram Rendering](../user/diagrams.md) for installing additional diagram tools.

---

## Next Steps

- [Configuration & Environment](configuration.md) — Set up Confluence credentials
- [Docker Deployment](docker.md) — Run in a container with all dependencies
