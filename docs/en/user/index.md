# User Guide

Welcome to the ConfluentSynkMD User Guide. This section covers everything you need to synchronize Markdown documentation with Confluence.

---

## Sync Modes

ConfluentSynkMD supports three synchronization modes:

| Mode | Direction | Description |
|---|---|---|
| **Upload** | Markdown → Confluence | Convert and upload Markdown files as Confluence pages |
| **Download** | Confluence → Markdown | Fetch Confluence pages and convert them to Markdown |
| **LocalExport** | Markdown → Local XHTML | Convert Markdown to Confluence Storage Format locally (no API calls) |

---

## When to Use Each Mode

### Upload

Use **Upload** when you want to publish your Markdown documentation to Confluence. This is the primary workflow: write in Markdown, push to Confluence.

=== "Bash"

    ```bash
    dotnet run --project src/ConfluentSynkMD -- \
      --mode Upload --path ./docs --conf-space DEV
    ```

=== "PowerShell"

    ```powershell
    dotnet run --project src/ConfluentSynkMD -- `
      --mode Upload --path ./docs --conf-space DEV
    ```

=== "CMD"

    ```cmd
    dotnet run --project src/ConfluentSynkMD -- ^
      --mode Upload --path .\docs --conf-space DEV
    ```

### Download

Use **Download** when you want to pull existing Confluence pages into your local repository as Markdown files. Useful for:

- Initial migration from Confluence → Markdown
- Syncing back changes made directly in Confluence

=== "Bash"

    ```bash
    dotnet run --project src/ConfluentSynkMD -- \
      --mode Download --path ./output --conf-space DEV --conf-parent-id 12345
    ```

=== "PowerShell"

    ```powershell
    dotnet run --project src/ConfluentSynkMD -- `
      --mode Download --path ./output --conf-space DEV --conf-parent-id 12345
    ```

=== "CMD"

    ```cmd
    dotnet run --project src/ConfluentSynkMD -- ^
      --mode Download --path .\output --conf-space DEV --conf-parent-id 12345
    ```

### LocalExport

Use **LocalExport** when you want to preview the Confluence Storage Format (XHTML) output without making any API calls. Useful for debugging or CI pipelines.

=== "Bash"

    ```bash
    dotnet run --project src/ConfluentSynkMD -- \
      --mode Upload --path ./docs --conf-space DEV --local
    ```

=== "PowerShell"

    ```powershell
    dotnet run --project src/ConfluentSynkMD -- `
      --mode Upload --path ./docs --conf-space DEV --local
    ```

=== "CMD"

    ```cmd
    dotnet run --project src/ConfluentSynkMD -- ^
      --mode Upload --path .\docs --conf-space DEV --local
    ```

---

## Key Features

- [Upload Workflow](upload.md) — Detailed upload guide with hierarchy and skip-update
- [Download Workflow](download.md) — Downloading pages with root-page selection
- [Local Export](local-export.md) — Working without Confluence API access
- [Frontmatter & Metadata](frontmatter.md) — Controlling page titles, labels, and space keys
- [Diagram Rendering](diagrams.md) — Mermaid, Draw.io, PlantUML, and LaTeX
- [Finding Confluence IDs](confluence-ids.md) — Locating Space Keys and Page IDs
