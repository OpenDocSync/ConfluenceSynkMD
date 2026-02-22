# Lokaler Export

Lokaler Export konvertiert Markdown in das Confluence Storage Format (XHTML) auf Ihrem lokalen Dateisystem, ohne API-Aufrufe durchzuführen.

---

## Verwendung

=== "Bash"

    ```bash
    dotnet run --project src/ConfluentSynkMD -- \
      --mode Upload \
      --path ./docs \
      --conf-space DEV \
      --local
    ```

=== "PowerShell"

    ```powershell
    dotnet run --project src/ConfluentSynkMD -- `
      --mode Upload `
      --path ./docs `
      --conf-space DEV `
      --local
    ```

=== "CMD"

    ```cmd
    dotnet run --project src/ConfluentSynkMD -- ^
      --mode Upload ^
      --path .\docs ^
      --conf-space DEV ^
      --local
    ```

!!! note
    Das `--local`-Flag überschreibt den Modus auf `LocalExport`. Der `--conf-space` wird weiterhin für die Link-Auflösung benötigt, aber es werden keine API-Aufrufe durchgeführt.

---

## Anwendungsfälle

### Vorschau vor dem Upload

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

### CI-Pipeline-Validierung

```yaml
- name: Confluence-Konvertierung validieren
  run: |
    dotnet run --project src/ConfluentSynkMD -- \
      --mode Upload --path ./docs --conf-space DEV --local
```

### Debugging

=== "Bash"

    ```bash
    dotnet run --project src/ConfluentSynkMD -- \
      --mode Upload --path ./docs --conf-space DEV \
      --local --debug-line-markers --loglevel debug
    ```

=== "PowerShell"

    ```powershell
    dotnet run --project src/ConfluentSynkMD -- `
      --mode Upload --path ./docs --conf-space DEV `
      --local --debug-line-markers --loglevel debug
    ```

=== "CMD"

    ```cmd
    dotnet run --project src/ConfluentSynkMD -- ^
      --mode Upload --path .\docs --conf-space DEV ^
      --local --debug-line-markers --loglevel debug
    ```
