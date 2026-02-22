# CLI Reference

Complete reference of all ConfluentSynkMD command-line options.

```
ConfluentSynkMD – Markdown ↔ Confluence Synchronization Tool
```

---

## Core Options

| Option | Required | Default | Description |
|---|---|---|---|
| `--mode <Upload\|Download\|LocalExport>` | ✅ | — | Synchronization direction |
| `--path <path>` | ✅ | — | Local filesystem path to Markdown files |
| `--conf-space <key>` | ✅ | — | Confluence Space Key (e.g. `DEV`) |
| `--conf-parent-id <id>` | | — | Parent page ID for subtree operations |

---

## Credential Options

Override Confluence connection settings (takes priority over environment variables):

| Option | Default | Description |
|---|---|---|
| `--conf-base-url <url>` | — | Confluence Cloud base URL (overrides `CONFLUENCE__BASEURL`) |
| `--conf-auth-mode <Basic\|Bearer>` | — | Authentication mode (overrides `CONFLUENCE__AUTHMODE`) |
| `--conf-user-email <email>` | — | User email for Basic auth (overrides `CONFLUENCE__USEREMAIL`) |
| `--conf-api-token <token>` | — | API token for Basic auth (overrides `CONFLUENCE__APITOKEN`) |
| `--conf-bearer-token <token>` | — | Bearer token for OAuth 2.0 (overrides `CONFLUENCE__BEARERTOKEN`) |

---

## Sync Control

| Option | Default | Description |
|---|---|---|
| `--root-page <title>` | — | Root page title (alternative to `--conf-parent-id`; created if not found) |
| `--keep-hierarchy` | `true` | Preserve local directory hierarchy in Confluence |
| `--skip-hierarchy` | `false` | Flatten all pages under the root (overrides `--keep-hierarchy`) |
| `--skip-update` | `false` | Skip uploading pages whose content has not changed |
| `--local` | `false` | Only produce local CSF output, no API calls |
| `--no-write-back` | `false` | Don't write `<!-- confluence-page-id -->` / `<!-- confluence-space-key -->` comments back into Markdown sources |
| `--loglevel <level>` | `info` | Logging verbosity: `debug`, `info`, `warning`, `error`, `critical` |

---

## API Settings

| Option | Default | Description |
|---|---|---|
| `--api-version <v1\|v2>` | `v2` | Confluence REST API version |
| `--headers <KEY=VALUE>` | — | Custom HTTP headers (can specify multiple) |

---

## Converter Options

| Option | Default | Description |
|---|---|---|
| `--heading-anchors` | `false` | Inject anchor macros before headings for deep-linking |
| `--force-valid-url` | `false` | Sanitize and escape invalid URLs |
| `--skip-title-heading` | `false` | Omit the first H1 heading (used as page title) |
| `--prefer-raster` | `false` | Prefer raster images over vector (SVG → PNG) |
| `--webui-links` | `false` | Render internal `.md` links as Confluence Web UI URLs |
| `--webui-link-strategy <space-title\|page-id>` | `space-title` | Strategy for Web UI links |
| `--use-panel` | `false` | Use panel macro instead of info/note/warning for alerts |
| `--force-valid-language` | `false` | Validate code block languages against Confluence-supported set |
| `--code-line-numbers` | `false` | Show line numbers in Confluence code block macros (alias: `--line-numbers`) |
| `--debug-line-markers` | `false` | Include source line numbers in conversion error messages |
| `--title-prefix <prefix>` | — | Prefix prepended to all page titles (e.g. `[AUTO] `) |
| `--generated-by <value>` | `MARKDOWN` | Generated-by marker. Supports: `%{filepath}`, `%{filename}`, `%{filedir}`, `%{filestem}`. Set to empty to disable |

---

## Diagram Rendering

| Option | Default | Description |
|---|---|---|
| `--render-mermaid` | `true` | Render Mermaid code blocks as image attachments |
| `--no-render-mermaid` | `false` | Disable Mermaid rendering |
| `--render-drawio` | `false` | Render Draw.io code blocks as image attachments |
| `--render-plantuml` | `false` | Render PlantUML code blocks as image attachments |
| `--render-latex` | `false` | Render LaTeX code blocks as image attachments |
| `--diagram-output-format` | `png` | Output format for rendered diagrams: `png` or `svg` |

---

## Layout Options

| Option | Default | Description |
|---|---|---|
| `--layout-image-alignment` | — | Image alignment: `center`, `left`, `right` |
| `--layout-image-max-width <px>` | — | Maximum width for images in pixels |
| `--layout-table-width <px>` | — | Table width in pixels |
| `--layout-table-display-mode` | `responsive` | Table display mode: `responsive` or `fixed` |
| `--layout-alignment` | — | Content alignment: `center`, `left`, `right` |

---

## Examples

### Upload with credentials and common options

=== "Bash"

    ```bash
    dotnet run --project src/ConfluentSynkMD -- \
      --mode Upload \
      --path ./docs \
      --conf-space DEV \
      --root-page "My Documentation" \
      --conf-base-url https://yoursite.atlassian.net \
      --conf-user-email user@example.com \
      --conf-api-token your-token \
      --keep-hierarchy \
      --skip-update \
      --render-mermaid \
      --code-line-numbers \
      --heading-anchors \
      --title-prefix "[AUTO] " \
      --loglevel info
    ```

=== "PowerShell"

    ```powershell
    dotnet run --project src/ConfluentSynkMD -- `
      --mode Upload `
      --path ./docs `
      --conf-space DEV `
      --root-page "My Documentation" `
      --conf-base-url https://yoursite.atlassian.net `
      --conf-user-email user@example.com `
      --conf-api-token your-token `
      --keep-hierarchy `
      --skip-update `
      --render-mermaid `
      --code-line-numbers `
      --heading-anchors `
      --title-prefix "[AUTO] " `
      --loglevel info
    ```

=== "CMD"

    ```cmd
    dotnet run --project src/ConfluentSynkMD -- ^
      --mode Upload ^
      --path .\docs ^
      --conf-space DEV ^
      --root-page "My Documentation" ^
      --conf-base-url https://yoursite.atlassian.net ^
      --conf-user-email user@example.com ^
      --conf-api-token your-token ^
      --keep-hierarchy ^
      --skip-update ^
      --render-mermaid ^
      --code-line-numbers ^
      --heading-anchors ^
      --title-prefix "[AUTO] " ^
      --loglevel info
    ```

### Download a subtree

=== "Bash"

    ```bash
    dotnet run --project src/ConfluentSynkMD -- \
      --mode Download \
      --path ./output \
      --conf-space DEV \
      --conf-parent-id 12345
    ```

=== "PowerShell"

    ```powershell
    dotnet run --project src/ConfluentSynkMD -- `
      --mode Download `
      --path ./output `
      --conf-space DEV `
      --conf-parent-id 12345
    ```

=== "CMD"

    ```cmd
    dotnet run --project src/ConfluentSynkMD -- ^
      --mode Download ^
      --path .\output ^
      --conf-space DEV ^
      --conf-parent-id 12345
    ```

### Local export for debugging

=== "Bash"

    ```bash
    dotnet run --project src/ConfluentSynkMD -- \
      --mode Upload \
      --path ./docs \
      --conf-space DEV \
      --local \
      --debug-line-markers \
      --loglevel debug
    ```

=== "PowerShell"

    ```powershell
    dotnet run --project src/ConfluentSynkMD -- `
      --mode Upload `
      --path ./docs `
      --conf-space DEV `
      --local `
      --debug-line-markers `
      --loglevel debug
    ```

=== "CMD"

    ```cmd
    dotnet run --project src/ConfluentSynkMD -- ^
      --mode Upload ^
      --path .\docs ^
      --conf-space DEV ^
      --local ^
      --debug-line-markers ^
      --loglevel debug
    ```

### Custom layout settings

=== "Bash"

    ```bash
    dotnet run --project src/ConfluentSynkMD -- \
      --mode Upload \
      --path ./docs \
      --conf-space DEV \
      --conf-parent-id 12345 \
      --layout-image-alignment center \
      --layout-image-max-width 800 \
      --layout-table-display-mode fixed \
      --layout-table-width 960
    ```

=== "PowerShell"

    ```powershell
    dotnet run --project src/ConfluentSynkMD -- `
      --mode Upload `
      --path ./docs `
      --conf-space DEV `
      --conf-parent-id 12345 `
      --layout-image-alignment center `
      --layout-image-max-width 800 `
      --layout-table-display-mode fixed `
      --layout-table-width 960
    ```

=== "CMD"

    ```cmd
    dotnet run --project src/ConfluentSynkMD -- ^
      --mode Upload ^
      --path .\docs ^
      --conf-space DEV ^
      --conf-parent-id 12345 ^
      --layout-image-alignment center ^
      --layout-image-max-width 800 ^
      --layout-table-display-mode fixed ^
      --layout-table-width 960
    ```
