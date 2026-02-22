# Download-Workflow

Download ruft Confluence-Seiten ab und konvertiert sie zurück in Markdown-Dateien auf Ihrem lokalen Dateisystem.

---

## Einfacher Download

=== "Bash"

    ```bash
    dotnet run --project src/ConfluenceSynkMD -- \
      --mode Download \
      --path ./output \
      --conf-space DEV \
      --conf-parent-id 12345
    ```

=== "PowerShell"

    ```powershell
    dotnet run --project src/ConfluenceSynkMD -- `
      --mode Download `
      --path ./output `
      --conf-space DEV `
      --conf-parent-id 12345
    ```

=== "CMD"

    ```cmd
    dotnet run --project src/ConfluenceSynkMD -- ^
      --mode Download ^
      --path .\output ^
      --conf-space DEV ^
      --conf-parent-id 12345
    ```

### Docker-Download

=== "Bash"

    ```bash
    docker run --rm -it \
      -e CONFLUENCE__BASEURL=https://yoursite.atlassian.net \
      -e CONFLUENCE__AUTHMODE=Basic \
      -e CONFLUENCE__USEREMAIL=user@example.com \
      -e CONFLUENCE__APITOKEN=ihr-token \
      -v ${PWD}:/workspace \
      confluencesynkmd \
      --mode Download \
      --path /workspace/output \
      --conf-space DEV \
      --conf-parent-id 12345
    ```

    ???+ tip "Pfad-Hinweis"
        - `-v ${PWD}:/workspace` bleibt unverändert.
        - Für Download zeigt `--path` auf den Zielordner im Container (z. B. `/workspace/output`).
        - `${PWD}` ist Ihr aktueller lokaler Ordner.

=== "PowerShell"

    ```powershell
    docker run --rm -it `
      -e CONFLUENCE__BASEURL=https://yoursite.atlassian.net `
      -e CONFLUENCE__AUTHMODE=Basic `
      -e CONFLUENCE__USEREMAIL=user@example.com `
      -e CONFLUENCE__APITOKEN=ihr-token `
      -v ${PWD}:/workspace `
      confluencesynkmd `
      --mode Download `
      --path /workspace/output `
      --conf-space DEV `
      --conf-parent-id 12345
    ```

    ???+ tip "Pfad-Hinweis"
        - `-v ${PWD}:/workspace` bleibt unverändert.
        - Für Download zeigt `--path` auf den Zielordner im Container (z. B. `/workspace/output`).
        - `${PWD}` ist Ihr aktueller lokaler Ordner.

=== "CMD"

    ```cmd
    docker run --rm -it ^
      -e CONFLUENCE__BASEURL=https://yoursite.atlassian.net ^
      -e CONFLUENCE__AUTHMODE=Basic ^
      -e CONFLUENCE__USEREMAIL=user@example.com ^
      -e CONFLUENCE__APITOKEN=ihr-token ^
      -v %cd%:/workspace ^
      confluencesynkmd ^
      --mode Download ^
      --path /workspace/output ^
      --conf-space DEV ^
      --conf-parent-id 12345
    ```

    ???+ tip "Pfad-Hinweis"
        - `-v %cd%:/workspace` bleibt unverändert.
        - Für Download zeigt `--path` auf den Zielordner im Container (z. B. `/workspace/output`).
        - `%cd%` ist Ihr aktueller lokaler Ordner.

---

## Download per Root-Page-Titel

=== "Bash"

    ```bash
    dotnet run --project src/ConfluenceSynkMD -- \
      --mode Download \
      --path ./output \
      --conf-space DEV \
      --root-page "Meine Dokumentation"
    ```

=== "PowerShell"

    ```powershell
    dotnet run --project src/ConfluenceSynkMD -- `
      --mode Download `
      --path ./output `
      --conf-space DEV `
      --root-page "Meine Dokumentation"
    ```

=== "CMD"

    ```cmd
    dotnet run --project src/ConfluenceSynkMD -- ^
      --mode Download ^
      --path .\output ^
      --conf-space DEV ^
      --root-page "Meine Dokumentation"
    ```

---

## Verzeichnisstruktur-Rekonstruktion

Wenn Seiten ursprünglich mit `--keep-hierarchy` hochgeladen wurden, stellt ConfluenceSynkMD die exakte Verzeichnisstruktur beim Download wieder her.

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
    dotnet run --project src/ConfluenceSynkMD -- \
      --mode Download \
      --path ./migrated-docs \
      --conf-space TEAM \
      --conf-parent-id 98765
    ```

=== "PowerShell"

    ```powershell
    dotnet run --project src/ConfluenceSynkMD -- `
      --mode Download `
      --path ./migrated-docs `
      --conf-space TEAM `
      --conf-parent-id 98765
    ```

=== "CMD"

    ```cmd
    dotnet run --project src/ConfluenceSynkMD -- ^
      --mode Download ^
      --path .\migrated-docs ^
      --conf-space TEAM ^
      --conf-parent-id 98765
    ```

### Änderungen zurücksynchronisieren

=== "Bash"

    ```bash
    dotnet run --project src/ConfluenceSynkMD -- \
      --mode Download \
      --path ./docs \
      --conf-space DEV \
      --root-page "Meine Dokumentation"
    ```

=== "PowerShell"

    ```powershell
    dotnet run --project src/ConfluenceSynkMD -- `
      --mode Download `
      --path ./docs `
      --conf-space DEV `
      --root-page "Meine Dokumentation"
    ```

=== "CMD"

    ```cmd
    dotnet run --project src/ConfluenceSynkMD -- ^
      --mode Download ^
      --path .\docs ^
      --conf-space DEV ^
      --root-page "Meine Dokumentation"
    ```
