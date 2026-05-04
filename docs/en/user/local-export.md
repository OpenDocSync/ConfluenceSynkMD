# Local Export

Local Export converts Markdown to Confluence Storage Format (XHTML) on your local filesystem without making any API calls. This is useful for previewing, debugging, or CI validation.

---

## Usage

=== "Bash"

    ```bash
    dotnet run --project src/ConfluenceSynkMD -- \
      upload \
      --path ./docs \
      --conf-space DEV \
    ```

=== "PowerShell"

    ```powershell
    dotnet run --project src/ConfluenceSynkMD -- `
      upload `
      --path ./docs `
      --conf-space DEV `
    ```

=== "CMD"

    ```cmd
    dotnet run --project src/ConfluenceSynkMD -- ^
      upload ^
      --path .\docs ^
      --conf-space DEV ^
    ```

!!! note
    The `local` subcommand flag overrides the mode to `LocalExport` regardless of the `<subcommand>` value. The `--conf-space` is still required for link resolution but no API calls are made.

---

## Output

The exported XHTML files are written alongside your Markdown sources. Each `.md` file produces a corresponding `.html` file containing the Confluence Storage Format output.

---

## Use Cases

### Preview Before Upload

Run a local export to inspect the generated XHTML before pushing to Confluence:

=== "Bash"

    ```bash
    dotnet run --project src/ConfluenceSynkMD -- \
      local --path ./docs --conf-space DEV
    ```

=== "PowerShell"

    ```powershell
    dotnet run --project src/ConfluenceSynkMD -- `
      local --path ./docs --conf-space DEV
    ```

=== "CMD"

    ```cmd
    dotnet run --project src/ConfluenceSynkMD -- ^
      local --path .\docs --conf-space DEV
    ```

### CI Pipeline Validation

Add a local export step to your CI pipeline to verify that all Markdown files convert successfully:

```yaml
- name: Validate Confluence conversion
  run: |
    dotnet run --project src/ConfluenceSynkMD -- \
      local --path ./docs --conf-space DEV
```

### Debugging Conversion Issues

Combine with `--debug-line-markers` and `--loglevel debug` for detailed output:

=== "Bash"

    ```bash
    dotnet run --project src/ConfluenceSynkMD -- \
      local --path ./docs --conf-space DEV \
      --debug-line-markers --loglevel debug
    ```

=== "PowerShell"

    ```powershell
    dotnet run --project src/ConfluenceSynkMD -- `
      local --path ./docs --conf-space DEV `
      --debug-line-markers --loglevel debug
    ```

=== "CMD"

    ```cmd
    dotnet run --project src/ConfluenceSynkMD -- ^
      local --path .\docs --conf-space DEV ^
      --debug-line-markers --loglevel debug
    ```
