# Upload Workflow

Upload converts your local Markdown files into Confluence Storage Format (XHTML) and publishes them as pages in your Confluence space.

---

## Basic Upload

=== "Bash"

    ```bash
    dotnet run --project src/ConfluenceSynkMD -- \
      --mode Upload \
      --path ./docs \
      --conf-space DEV \
      --conf-parent-id 12345
    ```

=== "PowerShell"

    ```powershell
    dotnet run --project src/ConfluenceSynkMD -- `
      --mode Upload `
      --path ./docs `
      --conf-space DEV `
      --conf-parent-id 12345
    ```

=== "CMD"

    ```cmd
    dotnet run --project src/ConfluenceSynkMD -- ^
      --mode Upload ^
      --path .\docs ^
      --conf-space DEV ^
      --conf-parent-id 12345
    ```

This uploads all `.md` files from `./docs` as child pages under page `12345` in space `DEV`.

---

## Hierarchical Upload

By default, ConfluenceSynkMD preserves your local directory structure as a parent–child page tree in Confluence (`--keep-hierarchy` is `true` by default).

```
docs/
├── getting-started.md      → Child of parent page
├── guides/
│   ├── setup.md            → Child of "guides" index page
│   └── advanced.md         → Child of "guides" index page
└── api/
    └── endpoints.md        → Child of "api" index page
```

To **flatten** all pages under the root (disable hierarchy):

=== "Bash"

    ```bash
    --skip-hierarchy
    ```

=== "PowerShell"

    ```powershell
    --skip-hierarchy
    ```

=== "CMD"

    ```cmd
    --skip-hierarchy
    ```

---

## Using a Root Page

Instead of `--conf-parent-id`, you can specify a root page by title. If the page doesn't exist, it will be created:

=== "Bash"

    ```bash
    dotnet run --project src/ConfluenceSynkMD -- \
      --mode Upload \
      --path ./docs \
      --conf-space DEV \
      --root-page "My Documentation"
    ```

=== "PowerShell"

    ```powershell
    dotnet run --project src/ConfluenceSynkMD -- `
      --mode Upload `
      --path ./docs `
      --conf-space DEV `
      --root-page "My Documentation"
    ```

=== "CMD"

    ```cmd
    dotnet run --project src/ConfluenceSynkMD -- ^
      --mode Upload ^
      --path .\docs ^
      --conf-space DEV ^
      --root-page "My Documentation"
    ```

---

## Skip Unchanged Pages

Use `--skip-update` to avoid re-uploading pages whose content hasn't changed. ConfluenceSynkMD computes content hashes to detect changes:

=== "Bash"

    ```bash
    --skip-update
    ```

=== "PowerShell"

    ```powershell
    --skip-update
    ```

=== "CMD"

    ```cmd
    --skip-update
    ```

This significantly speeds up repeated uploads of large documentation sets.

---

## Page Title Prefix

Add a prefix to all uploaded page titles (useful for distinguishing auto-generated content):

=== "Bash"

    ```bash
    --title-prefix "[AUTO] "
    ```

=== "PowerShell"

    ```powershell
    --title-prefix "[AUTO] "
    ```

=== "CMD"

    ```cmd
    --title-prefix "[AUTO] "
    ```

This produces page titles like `[AUTO] Getting Started`, `[AUTO] Setup Guide`, etc.

---

## Generated-By Marker

By default, ConfluenceSynkMD adds a generated-by info macro at the top of each page. You can customize or disable this:

=== "Bash"

    ```bash
    # Custom template with file path placeholder
    --generated-by "Auto-generated from %{filepath}"

    # Disable the marker entirely
    --generated-by ""
    ```

=== "PowerShell"

    ```powershell
    # Custom template with file path placeholder
    --generated-by "Auto-generated from %{filepath}"

    # Disable the marker entirely
    --generated-by ""
    ```

=== "CMD"

    ```cmd
    REM Custom template with file path placeholder
    --generated-by "Auto-generated from %{filepath}"

    REM Disable the marker entirely
    --generated-by ""
    ```

Supported placeholders: `%{filepath}`, `%{filename}`, `%{filedir}`, `%{filestem}`.

---

## Page-ID Write-Back

After upload, ConfluenceSynkMD writes Confluence Page IDs back into your Markdown source files as HTML comments:

```html
<!-- confluence-page-id: 12345 -->
<!-- confluence-space-key: DEV -->
```

This enables round-trip sync — subsequent uploads will update the existing page rather than creating a new one. To disable:

=== "Bash"

    ```bash
    --no-write-back
    ```

=== "PowerShell"

    ```powershell
    --no-write-back
    ```

=== "CMD"

    ```cmd
    --no-write-back
    ```

---

## Debug Mode

For troubleshooting conversion issues, enable debug line markers:

=== "Bash"

    ```bash
    --debug-line-markers --loglevel debug
    ```

=== "PowerShell"

    ```powershell
    --debug-line-markers --loglevel debug
    ```

=== "CMD"

    ```cmd
    --debug-line-markers --loglevel debug
    ```

This includes source line numbers in error messages, making it easier to locate issues in your Markdown files.
