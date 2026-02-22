# Download-Workflow

Download ruft Confluence-Seiten ab und konvertiert sie zurück in Markdown-Dateien auf Ihrem lokalen Dateisystem.

---

## Einfacher Download

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

---

## Download per Root-Page-Titel

=== "Bash"

    ```bash
    dotnet run --project src/ConfluentSynkMD -- \
      --mode Download \
      --path ./output \
      --conf-space DEV \
      --root-page "Meine Dokumentation"
    ```

=== "PowerShell"

    ```powershell
    dotnet run --project src/ConfluentSynkMD -- `
      --mode Download `
      --path ./output `
      --conf-space DEV `
      --root-page "Meine Dokumentation"
    ```

=== "CMD"

    ```cmd
    dotnet run --project src/ConfluentSynkMD -- ^
      --mode Download ^
      --path .\output ^
      --conf-space DEV ^
      --root-page "Meine Dokumentation"
    ```

---

## Verzeichnisstruktur-Rekonstruktion

Wenn Seiten ursprünglich mit `--keep-hierarchy` hochgeladen wurden, stellt ConfluentSynkMD die exakte Verzeichnisstruktur beim Download wieder her.

---

## Einschränkungen

!!! warning "Wichtige Einschränkungen"
    - **Kein inkrementeller Download** — Download holt immer den vollständigen Unterbaum
    - **Markdown-Treue** — Nicht alle Confluence-Makros haben ein Markdown-Äquivalent
    - **Anhänge** — Bilder und Anhänge werden zusammen mit den Markdown-Dateien heruntergeladen

---

## Typische Workflows

### Initiale Migration

=== "Bash"

    ```bash
    dotnet run --project src/ConfluentSynkMD -- \
      --mode Download \
      --path ./migrated-docs \
      --conf-space TEAM \
      --conf-parent-id 98765
    ```

=== "PowerShell"

    ```powershell
    dotnet run --project src/ConfluentSynkMD -- `
      --mode Download `
      --path ./migrated-docs `
      --conf-space TEAM `
      --conf-parent-id 98765
    ```

=== "CMD"

    ```cmd
    dotnet run --project src/ConfluentSynkMD -- ^
      --mode Download ^
      --path .\migrated-docs ^
      --conf-space TEAM ^
      --conf-parent-id 98765
    ```

### Änderungen zurücksynchronisieren

=== "Bash"

    ```bash
    dotnet run --project src/ConfluentSynkMD -- \
      --mode Download \
      --path ./docs \
      --conf-space DEV \
      --root-page "Meine Dokumentation"
    ```

=== "PowerShell"

    ```powershell
    dotnet run --project src/ConfluentSynkMD -- `
      --mode Download `
      --path ./docs `
      --conf-space DEV `
      --root-page "Meine Dokumentation"
    ```

=== "CMD"

    ```cmd
    dotnet run --project src/ConfluentSynkMD -- ^
      --mode Download ^
      --path .\docs ^
      --conf-space DEV ^
      --root-page "Meine Dokumentation"
    ```
