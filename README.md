# ConfluenceSynkMD

[![CI](https://github.com/OpenDocSync/ConfluenceSynkMD/actions/workflows/ci.yml/badge.svg)](https://github.com/OpenDocSync/ConfluenceSynkMD/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/OpenDocSync/ConfluenceSynkMD/graph/badge.svg)](https://codecov.io/gh/OpenDocSync/ConfluenceSynkMD)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Docs](https://img.shields.io/badge/docs-GitHub%20Pages-blue?logo=materialformkdocs)](https://opendocsync.github.io/ConfluenceSynkMD/)

**ConfluenceSynkMD** is a .NET 10 CLI tool for bidirectional synchronization of Markdown documentation with Atlassian Confluence Cloud. It converts Markdown into Confluence Storage Format (XHTML) for upload, and Confluence pages back into Markdown for download — preserving directory-based hierarchy, rendering diagrams, and optimizing images.

## Why ConfluenceSynkMD?

Keeping documentation in Markdown (version-controlled, editor-friendly) while publishing to Confluence (company wiki, stakeholder access) usually means manual copy-paste or fragile scripts. **ConfluenceSynkMD** bridges that gap with a single CLI command: write in Markdown, publish to Confluence, download changes back — fully automated and round-trip safe.

---

## Features

- **Bidirectional Sync** — Upload Markdown → Confluence, Download Confluence → Markdown, or Local Export without API calls
- **Hierarchical Pages** — Preserves your local directory structure as a parent–child page tree in Confluence (`--keep-hierarchy`)
- **Diagram Rendering** — Converts Mermaid, Draw.io, PlantUML, and LaTeX code blocks into image attachments
- **Image Optimization** — Automatically downscales and compresses images before upload
- **GitHub Alerts** — Maps `[!NOTE]`, `[!WARNING]`, `[!TIP]`, `[!IMPORTANT]`, `[!CAUTION]` blocks to Confluence macros
- **Frontmatter Support** — Reads YAML frontmatter for page titles, tags/labels, Confluence Page IDs, **per-document `space_key`** override, per-document `generated_by` overrides, and layout options
- **Code Blocks** — Converts fenced code blocks with syntax highlighting, optional line numbers (`--code-line-numbers`), and language validation
- **Attachments** — Uploads local image and file references as Confluence attachments
- **Skip Unchanged** — Detects content hashes to skip re-uploading pages that haven't changed (`--skip-update`)
- **Page-ID Write-Back** — Writes `<!-- confluence-page-id: ... -->` comments back into Markdown sources after upload for round-trip sync
- **Round-Trip Fidelity** — Persists `source-path` metadata during upload, enabling exact directory structure reconstruction on download
- **Collision Safety** — Duplicate title detection and ancestor verification prevent accidental overwrites of unrelated pages
- **Debug Traceability** — `--debug-line-markers` includes source line numbers in conversion error messages
- **Flexible Auth** — Supports both Basic (email + API token) and Bearer (OAuth 2.0) authentication

---

## 📖 Documentation

Full documentation is available at **[opendocsync.github.io/ConfluenceSynkMD](https://opendocsync.github.io/ConfluenceSynkMD/)**.

| Section | Description |
|---|---|
| **Quick Start** | Get up and running in 5 minutes |
| **User Guide** | Upload, download, local export, frontmatter, diagrams |
| **Developer Guide** | Architecture, ETL pipeline, Markdig renderers, contributing |
| **Admin Guide** | Installation, Docker, configuration, authentication, CI/CD |
| **CLI Reference** | All 35+ command-line options documented |

> Available in **English** and **Deutsch**.

---

## Prerequisites

- **.NET 10 SDK** (to build and run locally)
- **Node.js 22+** and **@mermaid-js/mermaid-cli** (for Mermaid rendering)
- **Docker** (recommended for a consistent environment with all dependencies)

---

## Quick Start

The fastest path is the published Docker image — Mermaid, Draw.io, PlantUML, LaTeX, and the .NET runtime are all pre-installed. Three commands and your docs are live in Confluence:

```bash
# 1. Pre-flight: every renderer + Confluence auth in 30 seconds
docker run --rm \
  -e CONFLUENCE__BASEURL=https://yoursite.atlassian.net \
  -e CONFLUENCE__USEREMAIL=you@example.com \
  -e CONFLUENCE__APITOKEN=your-token \
  ghcr.io/opendocsync/confluencesynkmd:0.1 doctor

# 2. Upload your Markdown
docker run --rm \
  -e CONFLUENCE__BASEURL -e CONFLUENCE__USEREMAIL -e CONFLUENCE__APITOKEN \
  -v "$PWD/docs:/workspace/docs:ro" \
  ghcr.io/opendocsync/confluencesynkmd:0.1 \
  upload --path /workspace/docs --conf-space YOUR_SPACE_KEY --conf-parent-id YOUR_PAGE_ID
```

That's it. The default `:0.1` image ships every renderer, so a Markdown file with `mermaid`, `drawio`, `plantuml`, and `latex` code blocks just works.

### What ships in the default image

| Renderer | Engine | Preinstalled |
|---|---|---|
| Mermaid | mermaid-cli + Chromium (Puppeteer) | ✅ |
| Draw.io | drawio-desktop (Electron, headless via Xvfb) | ⚠️ binary present; container-headless export unreliable in v0.1.0 (see [CHANGELOG Known Issues](CHANGELOG.md#known-issues); v0.1.1 follow-up) |
| PlantUML | plantuml + JRE | ✅ |
| LaTeX | TeX Live + Ghostscript (no ImageMagick) | ✅ |

### Other ways to run

```bash
# Build from source (.NET 10 SDK required)
git clone https://github.com/OpenDocSync/ConfluenceSynkMD.git
cd ConfluenceSynkMD
dotnet build

# Local sync subcommands (no Docker)
dotnet run --project src/ConfluenceSynkMD -- upload   --path ./docs --conf-space SPACE --conf-parent-id 12345
dotnet run --project src/ConfluenceSynkMD -- download --path ./out  --conf-space SPACE --conf-parent-id 12345
dotnet run --project src/ConfluenceSynkMD -- local    --path ./docs --conf-space SPACE
dotnet run --project src/ConfluenceSynkMD -- doctor
dotnet run --project src/ConfluenceSynkMD -- init
```

### As a GitHub Action

```yaml
- uses: OpenDocSync/ConfluenceSynkMD@v0.1.0
  with:
    subcommand: upload
    path: docs
    conf-space: ${{ vars.CONFLUENCE_SPACE }}
    conf-parent-id: ${{ vars.CONFLUENCE_PARENT_ID }}
  env:
    CONFLUENCE__BASEURL: ${{ secrets.CONFLUENCE_URL }}
    CONFLUENCE__USEREMAIL: ${{ secrets.CONFLUENCE_EMAIL }}
    CONFLUENCE__APITOKEN: ${{ secrets.CONFLUENCE_TOKEN }}
```

---

## Configuration

The tool reads Confluence connection settings from **environment variables** or **CLI flags**. CLI flags take highest priority.

### Environment Variables

| Variable | Description | Default |
| :--- | :--- | :---: |
| `CONFLUENCE__BASEURL` | Confluence Cloud URL (e.g. `https://yoursite.atlassian.net`) | — |
| `CONFLUENCE__AUTHMODE` | `Basic` or `Bearer` | `Basic` |
| `CONFLUENCE__USEREMAIL` | Atlassian account email (Basic Auth) | — |
| `CONFLUENCE__APITOKEN` | Atlassian API Token (Basic Auth) | — |
| `CONFLUENCE__BEARERTOKEN` | OAuth 2.0 access token (Bearer Auth) | — |
| `CONFLUENCE__OPTIMIZEIMAGES` | Downscale images before upload | `true` |
| `CONFLUENCE__MAXIMAGEWIDTH` | Maximum width for optimized images (px) | `1280` |
| `CONFLUENCE__APIPATH` | API path prefix (`/wiki` for Cloud, empty for Data Center) | `/wiki` |
| `CONFLUENCE__APIVERSION` | REST API version (`v1` or `v2`) | `v2` |

### CLI Credential Flags

Credential settings can also be passed via CLI flags (overrides environment variables):

| Flag | Overrides | Description |
| :--- | :--- | :--- |
| `--conf-base-url` | `CONFLUENCE__BASEURL` | Confluence Cloud base URL |
| `--conf-auth-mode` | `CONFLUENCE__AUTHMODE` | Authentication mode: `Basic` or `Bearer` |
| `--conf-user-email` | `CONFLUENCE__USEREMAIL` | User email (Basic Auth) |
| `--conf-api-token` | `CONFLUENCE__APITOKEN` | API token (Basic Auth) |
| `--conf-bearer-token` | `CONFLUENCE__BEARERTOKEN` | Bearer token (OAuth 2.0) |

---

## CLI Reference

```
ConfluenceSynkMD – Markdown ↔ Confluence Synchronization Tool
```

### Subcommands

ConfluenceSynkMD uses three top-level verbs that pick the synchronization direction:

| Subcommand | Description |
| :--- | :--- |
| `upload` | Upload Markdown documents to Confluence |
| `download` | Download Confluence pages back into Markdown |
| `local` | Produce local Confluence Storage Format output without API calls |

> **Migrating from earlier builds:** the legacy `--mode Upload\|Download\|LocalExport` flag and the standalone `--local` flag were removed in v0.1.0. Run `confluencesynkmd upload`, `download`, or `local` instead. The binary prints a friendly migration hint if it sees the old syntax.

### Core Options

These apply to every subcommand:

| Option | Required | Default | Description |
| :--- | :---: | :---: | :--- |
| `--path <path>` | ✅ | — | Local filesystem path to Markdown files |
| `--conf-space <key>` | ✅ | — | Confluence Space Key (e.g. `DEV`) |
| `--conf-parent-id <id>` | | — | Parent page ID for subtree operations |

### Sync Control

| Option | Default | Description |
| :--- | :---: | :--- |
| `--root-page <title>` | — | Root page title to upload under (alternative to `--conf-parent-id`; created if not found) |
| `--keep-hierarchy` | `true` | Preserve local directory hierarchy in Confluence |
| `--skip-hierarchy` | `false` | Flatten all pages under the root (overrides `--keep-hierarchy`) |
| `--skip-update` | `false` | Skip uploading pages whose content has not changed |
| `--no-write-back` | `false` | Don't write `<!-- confluence-page-id -->` / `<!-- confluence-space-key -->` comments back into Markdown sources after upload |
| `--loglevel <level>` | `info` | Logging verbosity: `debug`, `info`, `warning`, `error`, `critical` |

### Credential Options

| Option | Default | Description |
| :--- | :---: | :--- |
| `--conf-base-url <url>` | — | Confluence Cloud base URL (overrides `CONFLUENCE__BASEURL`) |
| `--conf-auth-mode <Basic\|Bearer>` | — | Authentication mode (overrides `CONFLUENCE__AUTHMODE`) |
| `--conf-user-email <email>` | — | User email for Basic auth (overrides `CONFLUENCE__USEREMAIL`) |
| `--conf-api-token <token>` | — | API token for Basic auth (overrides `CONFLUENCE__APITOKEN`) |
| `--conf-bearer-token <token>` | — | Bearer token for OAuth 2.0 (overrides `CONFLUENCE__BEARERTOKEN`) |

### API Settings

| Option | Default | Description |
| :--- | :---: | :--- |
| `--api-version <v1\|v2>` | `v2` | Confluence REST API version |
| `--headers <KEY=VALUE>` | — | Custom HTTP headers (can specify multiple) |

### Converter Options

| Option | Default | Description |
| :--- | :---: | :--- |
| `--heading-anchors` | `false` | Inject anchor macros before headings for deep-linking |
| `--force-valid-url` | `false` | Sanitize and escape invalid URLs |
| `--skip-title-heading` | `false` | Omit the first H1 heading (used as page title) |
| `--prefer-raster` | `false` | Prefer raster images over vector (SVG → PNG) |
| `--webui-links` | `false` | Render internal `.md` links as Confluence Web UI URLs |
| `--webui-link-strategy <space-title\|page-id>` | `space-title` | Strategy for Web UI links: title-based URL or page-id-based URL (with automatic fallback) |
| `--use-panel` | `false` | Use panel macro instead of info/note/warning for alerts |
| `--force-valid-language` | `false` | Validate code block languages against Confluence-supported set |
| `--code-line-numbers` | `false` | Show line numbers in Confluence code block macros (alias: `--line-numbers`) |
| `--debug-line-markers` | `false` | Include source line numbers in conversion error messages for debugging |
| `--title-prefix <prefix>` | — | Prefix prepended to all page titles (e.g. `[AUTO] `) |
| `--generated-by <value>` | `MARKDOWN` | Generated-by marker rendered as Confluence info macro. Supports template placeholders: `%{filepath}`, `%{filename}`, `%{filedir}`, `%{filestem}`. Can be overridden per-document via frontmatter. Set to empty to disable |

### Diagram Rendering

| Option | Default | Description |
| :--- | :---: | :--- |
| `--render-mermaid` | `true` | Render Mermaid code blocks as image attachments |
| `--no-render-mermaid` | `false` | Disable Mermaid rendering |
| `--render-drawio` | `false` | Render Draw.io code blocks as image attachments |
| `--render-plantuml` | `false` | Render PlantUML code blocks as image attachments |
| `--render-latex` | `false` | Render LaTeX code blocks as image attachments |
| `--diagram-output-format` | `png` | Output format for rendered diagrams: `png` or `svg` |

### Layout Options

| Option | Default | Description |
| :--- | :---: | :--- |
| `--layout-image-alignment` | — | Image alignment: `center`, `left`, `right` |
| `--layout-image-max-width <px>` | — | Maximum width for images in pixels |
| `--layout-table-width <px>` | — | Table width in pixels |
| `--layout-table-display-mode` | `responsive` | Table display mode: `responsive` or `fixed` |
| `--layout-alignment` | — | Content alignment: `center`, `left`, `right` |

---

## Docker Usage

The Docker image comes pre-packaged with .NET, Node.js, and mermaid-cli.

### Build

```bash
docker build -t confluencesynkmd .
```

### Run (PowerShell / Windows)

```powershell
# Upload: inject credentials via environment variables
docker run --rm -it `
  -e CONFLUENCE__BASEURL `
  -e CONFLUENCE__AUTHMODE `
  -e CONFLUENCE__USEREMAIL `
  -e CONFLUENCE__APITOKEN `
  -v ${PWD}/docs:/workspace/docs:ro `
  confluencesynkmd `
  upload `
  --path /workspace/docs `
  --conf-space YOUR_SPACE_KEY `
  --conf-parent-id YOUR_PAGE_ID

# Download: separate writable output mount
docker run --rm -it `
  -e CONFLUENCE__BASEURL `
  -e CONFLUENCE__AUTHMODE `
  -e CONFLUENCE__USEREMAIL `
  -e CONFLUENCE__APITOKEN `
  -v ${PWD}/output:/workspace/output `
  confluencesynkmd `
  download `
  --path /workspace/output `
  --conf-space YOUR_SPACE_KEY `
  --conf-parent-id YOUR_PAGE_ID

# Upload (CI/CD recommended): inject secrets from your pipeline variables
# Example uses environment variables already present in runner context
docker run --rm -it `
  -e CONFLUENCE__BASEURL `
  -e CONFLUENCE__AUTHMODE `
  -e CONFLUENCE__USEREMAIL `
  -e CONFLUENCE__APITOKEN `
  -v ${PWD}/docs:/workspace/docs:ro `
  confluencesynkmd `
  upload `
  --path /workspace/docs `
  --conf-space YOUR_SPACE_KEY `
  --conf-parent-id YOUR_PAGE_ID

# Download (CI/CD recommended): writable output mount
docker run --rm -it `
  -e CONFLUENCE__BASEURL `
  -e CONFLUENCE__AUTHMODE `
  -e CONFLUENCE__USEREMAIL `
  -e CONFLUENCE__APITOKEN `
  -v ${PWD}/output:/workspace/output `
  confluencesynkmd `
  download `
  --path /workspace/output `
  --conf-space YOUR_SPACE_KEY `
  --conf-parent-id YOUR_PAGE_ID
```

> [!IMPORTANT]
> If you mount `${PWD}`, make sure you run the command from the correct project directory. Prefer mounting only the required folders/files.

### Mount Strategies & Working Directory

| Mount | Use case |
| :--- | :--- |
| `-v ${PWD}/docs:/workspace/docs:ro` | Preferred for upload: least-privilege docs-only mount |
| `-v ${PWD}/docs:/workspace/docs:ro` + additional mounts (e.g. `-v ${PWD}/img:/workspace/img:ro`) | Use when Markdown references assets outside the docs folder |
| `-v ${PWD}:/workspace` | Full workspace mount (fallback), only when many cross-folder references are required |

> [!NOTE]
> For CI/CD, prefer secret stores (GitHub/GitLab protected variables).

> [!TIP]
> Paths with spaces should be quoted in shell-specific syntax, e.g. PowerShell: `-v "${PWD}/my docs:/workspace/docs:ro"`.

> [!NOTE]
> The PowerShell mount syntax with space-containing paths was validated against the Docker image. Bash syntax should be validated in your target CI runner.

---

## Finding Confluence IDs

### Space Key

The `--conf-space` value is the **Space Key** (a short identifier), not the display name.

1. Navigate to your Confluence space → **Space Settings**
2. The Space Key is shown on the settings page (e.g. `MFS`, `DEV`)
3. Or extract it from the URL: `https://yoursite.atlassian.net/wiki/spaces/MFS/...` → key is `MFS`

> [!IMPORTANT]
> **Personal spaces** have long keys starting with `~` followed by an account ID, e.g. `~ACCOUNT_ID`. Find the key in the URL or via the REST API: `GET /wiki/api/v2/spaces`.

### Page ID

The `--conf-parent-id` is the numeric ID of an existing Confluence page.

1. Open the page in Confluence
2. Extract from the URL: `…/pages/123456/My+Page` → Page ID is `123456`
3. Or click the **page menu (⋯)** → **Page Information**

### Per-Document Space Key Override

By default, all pages are uploaded to the space specified via `--conf-space`.
You can override this per document using YAML frontmatter or inline HTML comments:

**YAML frontmatter:**
```yaml
---
space_key: TEAM
---
```

**Inline HTML comment:**
```html
<!-- confluence-space-key: TEAM -->
```

When set, the document will be created/updated in the specified space instead of the global one.
The write-back comment (`<!-- confluence-space-key: ... -->`) reflects the actual space used.

---

## Architecture

ConfluenceSynkMD follows an **ETL (Extract-Transform-Load)** pipeline pattern:

```mermaid
graph LR
    subgraph Extract
        A[Markdown Files] -->|Read & Parse| B(DocumentNodes)
        C[Confluence API] -->|Fetch Pages| B
    end
    subgraph Transform
        B -->|Markdig + Custom Renderers| D(Confluence XHTML)
        B -->|AngleSharp + Reverse Mapping| E(Markdown)
    end
    subgraph Load
        D -->|Upload via REST API| F[Confluence Cloud]
        E -->|Write to Disk| G[Local Filesystem]
        D -->|Local Export| G
    end
```

| Layer | Responsibility |
|---|---|
| **Configuration** | CLI parsing, settings records (`ConfluenceSettings`, `ConverterOptions`, `LayoutOptions`) |
| **ETL / Extract** | Markdown file ingestion, Confluence page fetching, frontmatter parsing |
| **ETL / Transform** | Markdown → XHTML conversion (Markdig pipeline with custom renderers), XHTML → Markdown reverse conversion |
| **ETL / Load** | Confluence API upload, filesystem download, local export |
| **Services** | API client, hierarchy resolver, diagram renderers (Mermaid, PlantUML, Draw.io, LaTeX), image optimizer, link resolver |
| **Markdig** | Custom renderers for headings, code blocks, images, links, alerts, tables, etc. |
| **Models** | Domain models (`DocumentNode`, `ConvertedDocument`, `PageInfo`, etc.) |

---

## Limitations

- **Confluence Cloud only** — Tested against Confluence Cloud REST API v2. Data Center / Server may work with `--api-version v1` and `--api-path ""`, but is not officially supported.
- **No incremental download** — Download always fetches the full subtree; there is no delta sync in download mode.
- **Diagram rendering requires external tools when running outside the Docker image** — Mermaid needs Node.js + `@mermaid-js/mermaid-cli`, PlantUML needs Java + the `plantuml` binary, Draw.io needs `drawio-desktop`, LaTeX needs TeX Live + Ghostscript. The published Docker image (`ghcr.io/opendocsync/confluencesynkmd:0.1`) ships every renderer preinstalled.
- **No concurrent uploads** — Pages are uploaded sequentially to respect Confluence API rate limits and parent–child ordering.
- **Markdown fidelity** — Not all Confluence macros have a Markdown equivalent. Download mode maps common structures but may lose macro-specific formatting.
- **Single-space hierarchy** — `--keep-hierarchy` builds the page tree within a single space. Cross-space hierarchies are not supported (though individual documents can target different spaces via frontmatter).

---

## Project Structure

```
ConfluenceSynkMD/
├── src/ConfluenceSynkMD/           # Main application
│   ├── Configuration/             # Settings records (ConfluenceSettings, ConverterOptions, LayoutOptions)
│   ├── ETL/                       # Extract-Transform-Load pipeline
│   │   ├── Core/                  # Pipeline runner, step interfaces, batch context
│   │   ├── Extract/               # Markdown ingestion, Confluence page ingestion
│   │   ├── Transform/             # Markdown→XHTML and XHTML→Markdown conversion
│   │   └── Load/                  # Upload to Confluence, download to filesystem, local export
│   ├── Markdig/                   # Custom Markdig renderers (headings, code, images, links, alerts…)
│   ├── Models/                    # Domain models (DocumentNode, ConvertedDocument, etc.)
│   └── Services/                  # API client, hierarchy resolver, diagram renderers, image optimizer
├── tests/ConfluenceSynkMD.Tests/   # Unit and integration tests (xUnit)
├── Dockerfile                     # Multi-stage Docker build
└── docs/                          # MkDocs documentation and content
```

---

## Development & Testing

```bash
# Build
dotnet build

# Run all tests
dotnet test

# Run with verbose output
dotnet test --verbosity normal
```

The test suite includes unit and integration tests. Diagram rendering integration tests that require external tools (`mmdc`, `plantuml`) are skipped by default.

### Round-Trip / Integration Tests

Round-trip behavior is covered by integration tests in `tests/ConfluenceSynkMD.Tests/Integration`.

```bash
# Run integration tests only
dotnet test --filter "Category=Integration"
```

These tests validate core round-trip guarantees such as heading extraction, path parity checks, and markdown/XHTML transform behavior.

> [!NOTE]
> Some round-trip assertions are fixture-dependent and are skipped automatically when optional local test folders are not present.

---

## Contributing

Contributions are welcome! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines, and read our [Code of Conduct](CODE_OF_CONDUCT.md) before participating.

For security issues, please refer to [SECURITY.md](SECURITY.md).

## Community & Governance

- Contribution guide: [CONTRIBUTING.md](CONTRIBUTING.md)
- Code of Conduct: [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md)
- Security policy: [SECURITY.md](SECURITY.md)
- Support: [SUPPORT.md](SUPPORT.md)
- Changelog: [CHANGELOG.md](CHANGELOG.md)

---

## License

[MIT](LICENSE)
