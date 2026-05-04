# Lokaler Export

Lokaler Export konvertiert Markdown in das Confluence Storage Format (XHTML) auf Ihrem lokalen Dateisystem, ohne API-Aufrufe durchzuführen.

---

## Verwendung

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
    Das `local` subcommand-Flag überschreibt den Modus auf `LocalExport`. Der `--conf-space` wird weiterhin für die Link-Auflösung benötigt, aber es werden keine API-Aufrufe durchgeführt.

---

## Anwendungsfälle

### Vorschau vor dem Upload

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

### CI-Pipeline-Validierung

```yaml
- name: Confluence-Konvertierung validieren
  run: |
    dotnet run --project src/ConfluenceSynkMD -- \
      local --path ./docs --conf-space DEV
```

### Debugging

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
