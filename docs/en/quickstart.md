# Quick Start

Get ConfluenceSynkMD up and running in under 5 minutes.

---

## Prerequisites

| Requirement | Version | Notes |
|---|---|---|
| .NET SDK | 10.0+ | [Download](https://dotnet.microsoft.com/download) |
| Node.js | 22+ | Required for Mermaid diagram rendering |
| Docker | Latest | Optional — recommended for consistent environments |

---

## Option 1: Build from Source

=== "Bash"

    ```bash
    # Clone the repository
    git clone https://github.com/OpenDocSync/ConfluenceSynkMD.git
    cd ConfluenceSynkMD

    # Build
    dotnet build
    ```

=== "PowerShell"

    ```powershell
    # Clone the repository
    git clone https://github.com/OpenDocSync/ConfluenceSynkMD.git
    Set-Location ConfluenceSynkMD

    # Build
    dotnet build
    ```

=== "CMD"

    ```cmd
    REM Clone the repository
    git clone https://github.com/OpenDocSync/ConfluenceSynkMD.git
    cd ConfluenceSynkMD

    REM Build
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

## Configure Credentials

Credentials can be provided via **environment variables** or **CLI flags**.

### Environment Variables

Set `CONFLUENCE__*` variables in your shell:

=== "Bash"

    ```bash
    export CONFLUENCE__BASEURL=https://yoursite.atlassian.net
    export CONFLUENCE__AUTHMODE=Basic
    export CONFLUENCE__USEREMAIL=your-email@example.com
    export CONFLUENCE__APITOKEN=your-api-token-here
    ```

=== "PowerShell"

    ```powershell
    $env:CONFLUENCE__BASEURL = "https://yoursite.atlassian.net"
    $env:CONFLUENCE__AUTHMODE = "Basic"
    $env:CONFLUENCE__USEREMAIL = "your-email@example.com"
    $env:CONFLUENCE__APITOKEN = "your-api-token-here"
    ```

=== "CMD"

    ```cmd
    set CONFLUENCE__BASEURL=https://yoursite.atlassian.net
    set CONFLUENCE__AUTHMODE=Basic
    set CONFLUENCE__USEREMAIL=your-email@example.com
    set CONFLUENCE__APITOKEN=your-api-token-here
    ```

### CLI Flags

Pass credentials directly as command-line arguments:

=== "Bash"

    ```bash
    --conf-base-url https://yoursite.atlassian.net \
    --conf-auth-mode Basic \
    --conf-user-email your-email@example.com \
    --conf-api-token your-api-token-here
    ```

=== "PowerShell"

    ```powershell
    --conf-base-url https://yoursite.atlassian.net `
    --conf-auth-mode Basic `
    --conf-user-email your-email@example.com `
    --conf-api-token your-api-token-here
    ```

=== "CMD"

    ```cmd
    --conf-base-url https://yoursite.atlassian.net ^
    --conf-auth-mode Basic ^
    --conf-user-email your-email@example.com ^
    --conf-api-token your-api-token-here
    ```

!!! tip "API Token"
    Generate an API token at [id.atlassian.com/manage-profile/security/api-tokens](https://id.atlassian.com/manage-profile/security/api-tokens).

---

## Your First Upload

Upload a folder of Markdown files to Confluence:

### .NET

=== "Bash"

    ```bash
    dotnet run --project src/ConfluenceSynkMD -- \
      --mode Upload \
      --path ./my-docs \
      --conf-space YOUR_SPACE_KEY \
      --conf-parent-id YOUR_PAGE_ID
    ```

=== "PowerShell"

    ```powershell
    dotnet run --project src/ConfluenceSynkMD -- `
      --mode Upload `
      --path ./my-docs `
      --conf-space YOUR_SPACE_KEY `
      --conf-parent-id YOUR_PAGE_ID
    ```

=== "CMD"

    ```cmd
    dotnet run --project src/ConfluenceSynkMD -- ^
      --mode Upload ^
      --path .\my-docs ^
      --conf-space YOUR_SPACE_KEY ^
      --conf-parent-id YOUR_PAGE_ID
    ```

### Docker

=== "Bash"

    ```bash
    docker run --rm -it \
      -e CONFLUENCE__BASEURL=https://yoursite.atlassian.net \
      -e CONFLUENCE__AUTHMODE=Basic \
      -e CONFLUENCE__USEREMAIL=user@example.com \
      -e CONFLUENCE__APITOKEN=your-token \
      -v ${PWD}:/workspace \
      confluencesynkmd \
      --mode Upload \
      --path /workspace/my-docs \
      --conf-space YOUR_SPACE_KEY \
      --conf-parent-id YOUR_PAGE_ID
    ```

        ???+ tip "Path Hint"
                - Keep `-v ${PWD}:/workspace` unchanged.
                - Only change the suffix in `--path` (for example `/workspace/docs`, `/workspace/my-docs`).
                - `${PWD}` is your current local directory.

=== "PowerShell"

    ```powershell
    docker run --rm -it `
      -e CONFLUENCE__BASEURL=https://yoursite.atlassian.net `
      -e CONFLUENCE__AUTHMODE=Basic `
      -e CONFLUENCE__USEREMAIL=user@example.com `
      -e CONFLUENCE__APITOKEN=your-token `
      -v ${PWD}:/workspace `
      confluencesynkmd `
      --mode Upload `
      --path /workspace/my-docs `
      --conf-space YOUR_SPACE_KEY `
      --conf-parent-id YOUR_PAGE_ID
    ```

        ???+ tip "Path Hint"
                - Keep `-v ${PWD}:/workspace` unchanged.
                - Only change the suffix in `--path` (for example `/workspace/docs`, `/workspace/my-docs`).
                - `${PWD}` is your current local directory.

=== "CMD"

    ```cmd
    docker run --rm -it ^
      -e CONFLUENCE__BASEURL=https://yoursite.atlassian.net ^
      -e CONFLUENCE__AUTHMODE=Basic ^
      -e CONFLUENCE__USEREMAIL=user@example.com ^
      -e CONFLUENCE__APITOKEN=your-token ^
      -v %cd%:/workspace ^
      confluencesynkmd ^
      --mode Upload ^
      --path /workspace/my-docs ^
      --conf-space YOUR_SPACE_KEY ^
      --conf-parent-id YOUR_PAGE_ID
    ```

        ???+ tip "Path Hint"
                - Keep `-v %cd%:/workspace` unchanged.
                - Only change the suffix in `--path` (for example `/workspace/docs`, `/workspace/my-docs`).
                - `%cd%` is your current local directory.

#### Set Docker Paths Correctly

The most common issue is mixing up `-v` (volume mount) and `--path`.

- **Keep unchanged:** The container base path `/workspace` stays the same as long as you use `-v ...:/workspace`.
- **Change this:** Only adjust the part **after** `/workspace` in `--path` to match your local folder.
- **Where values come from:** `${PWD}` (Bash/PowerShell) and `%cd%` (CMD) are your current local directory.

| Local start directory | Mount | `--path` inside container |
|---|---|---|
| Project root with a `docs` subfolder | `-v ${PWD}:/workspace` | `--path /workspace/docs` |
| You are already inside the `docs` folder | `-v ${PWD}:/workspace` | `--path /workspace` |
| Mount only `docs` explicitly | `-v ${PWD}/docs:/workspace` | `--path /workspace` |

If the right side of `-v` is not `/workspace`, `--path` must use that same container base path.

---

## Your First Download

Download Confluence pages back to Markdown:

### .NET

=== "Bash"

    ```bash
    dotnet run --project src/ConfluenceSynkMD -- \
      --mode Download \
      --path ./output \
      --conf-space YOUR_SPACE_KEY \
      --conf-parent-id YOUR_PAGE_ID
    ```

=== "PowerShell"

    ```powershell
    dotnet run --project src/ConfluenceSynkMD -- `
      --mode Download `
      --path ./output `
      --conf-space YOUR_SPACE_KEY `
      --conf-parent-id YOUR_PAGE_ID
    ```

=== "CMD"

    ```cmd
    dotnet run --project src/ConfluenceSynkMD -- ^
      --mode Download ^
      --path .\output ^
      --conf-space YOUR_SPACE_KEY ^
      --conf-parent-id YOUR_PAGE_ID
    ```

### Docker

=== "Bash"

    ```bash
    docker run --rm -it \
      -e CONFLUENCE__BASEURL=https://yoursite.atlassian.net \
      -e CONFLUENCE__AUTHMODE=Basic \
      -e CONFLUENCE__USEREMAIL=user@example.com \
      -e CONFLUENCE__APITOKEN=your-token \
      -v ${PWD}:/workspace \
      confluencesynkmd \
      --mode Download \
      --path /workspace/output \
      --conf-space YOUR_SPACE_KEY \
      --conf-parent-id YOUR_PAGE_ID
    ```

        ???+ tip "Path Hint"
                - Keep `-v ${PWD}:/workspace` unchanged.
                - For download, `--path` points to the target folder inside the container, for example `/workspace/output`.
                - `${PWD}` is your current local directory.

=== "PowerShell"

    ```powershell
    docker run --rm -it `
      -e CONFLUENCE__BASEURL=https://yoursite.atlassian.net `
      -e CONFLUENCE__AUTHMODE=Basic `
      -e CONFLUENCE__USEREMAIL=user@example.com `
      -e CONFLUENCE__APITOKEN=your-token `
      -v ${PWD}:/workspace `
      confluencesynkmd `
      --mode Download `
      --path /workspace/output `
      --conf-space YOUR_SPACE_KEY `
      --conf-parent-id YOUR_PAGE_ID
    ```

        ???+ tip "Path Hint"
                - Keep `-v ${PWD}:/workspace` unchanged.
                - For download, `--path` points to the target folder inside the container, for example `/workspace/output`.
                - `${PWD}` is your current local directory.

=== "CMD"

    ```cmd
    docker run --rm -it ^
      -e CONFLUENCE__BASEURL=https://yoursite.atlassian.net ^
      -e CONFLUENCE__AUTHMODE=Basic ^
      -e CONFLUENCE__USEREMAIL=user@example.com ^
      -e CONFLUENCE__APITOKEN=your-token ^
      -v %cd%:/workspace ^
      confluencesynkmd ^
      --mode Download ^
      --path /workspace/output ^
      --conf-space YOUR_SPACE_KEY ^
      --conf-parent-id YOUR_PAGE_ID
    ```

        ???+ tip "Path Hint"
                - Keep `-v %cd%:/workspace` unchanged.
                - For download, `--path` points to the target folder inside the container, for example `/workspace/output`.
                - `%cd%` is your current local directory.

---

## What's Next?

- **[User Guide](user/index.md)** — Learn about all sync modes and features
- **[CLI Reference](reference/cli.md)** — See all 40+ command-line options
- **[Admin Guide](admin/index.md)** — Deploy with Docker, configure authentication
