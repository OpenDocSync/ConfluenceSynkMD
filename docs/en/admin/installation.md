# Installation

ConfluenceSynkMD can be installed by building from source with the .NET SDK.

---

## Prerequisites

| Requirement | Version | Purpose |
|---|---|---|
| **.NET SDK** | 10.0+ | Build and run the CLI tool |
| **Docker** | Latest | Required for Mermaid diagram rendering |

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

## Docker Requirement for Mermaid

Mermaid rendering relies on the official Mermaid CLI Docker image (`ghcr.io/mermaid-js/mermaid-cli/mermaid-cli`). The ConfluenceSynkMD tool will dynamically start a temporary Docker container to perform the rendering.

Ensure that the **Docker Engine** is installed and the `docker` command is available on your system's `PATH`.

Verify: `docker --version`

---

## Optional: PlantUML, Draw.io

See [Diagram Rendering](../user/diagrams.md) for installing additional diagram tools.

---

## Next Steps

- [Configuration & Environment](configuration.md) — Set up Confluence credentials
- [Docker Deployment](docker.md) — Run in a container with all dependencies
