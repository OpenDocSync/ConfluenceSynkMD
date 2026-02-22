# Upload-Workflow

Upload konvertiert Ihre lokalen Markdown-Dateien in das Confluence Storage Format (XHTML) und veröffentlicht sie als Seiten in Ihrem Confluence Space.

---

## Einfacher Upload

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

Dies lädt alle `.md`-Dateien aus `./docs` als Unterseiten der Seite `12345` im Space `DEV` hoch.

---

## Hierarchischer Upload

Standardmäßig erhält ConfluenceSynkMD Ihre lokale Verzeichnisstruktur als Eltern-Kind-Seitenbaum (`--keep-hierarchy` ist standardmäßig `true`).

```
docs/
├── getting-started.md      → Kind der Elternseite
├── guides/
│   ├── setup.md            → Kind der "guides"-Indexseite
│   └── advanced.md         → Kind der "guides"-Indexseite
└── api/
    └── endpoints.md        → Kind der "api"-Indexseite
```

Um alle Seiten **flach** unter die Wurzelseite zu legen:

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

## Root-Page verwenden

Statt `--conf-parent-id` können Sie eine Wurzelseite per Titel angeben. Existiert sie nicht, wird sie erstellt:

=== "Bash"

    ```bash
    --root-page "Meine Dokumentation"
    ```

=== "PowerShell"

    ```powershell
    --root-page "Meine Dokumentation"
    ```

=== "CMD"

    ```cmd
    --root-page "Meine Dokumentation"
    ```

---

## Unveränderte Seiten überspringen

Mit `--skip-update` werden Seiten nicht erneut hochgeladen, deren Inhalt sich nicht geändert hat:

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

---

## Seitentitel-Prefix

Fügen Sie allen hochgeladenen Seitentiteln ein Prefix hinzu:

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

---

## Generated-By-Marker

Standardmäßig fügt ConfluenceSynkMD einen Generated-By-Info-Makro oben auf jeder Seite ein. Anpassen oder deaktivieren:

=== "Bash"

    ```bash
    # Benutzerdefinierte Vorlage
    --generated-by "Auto-generiert aus %{filepath}"

    # Marker deaktivieren
    --generated-by ""
    ```

=== "PowerShell"

    ```powershell
    # Benutzerdefinierte Vorlage
    --generated-by "Auto-generiert aus %{filepath}"

    # Marker deaktivieren
    --generated-by ""
    ```

=== "CMD"

    ```cmd
    REM Benutzerdefinierte Vorlage
    --generated-by "Auto-generiert aus %{filepath}"

    REM Marker deaktivieren
    --generated-by ""
    ```

Platzhalter: `%{filepath}`, `%{filename}`, `%{filedir}`, `%{filestem}`.

---

## Page-ID Write-Back

Nach dem Upload schreibt ConfluenceSynkMD Confluence Page-IDs als HTML-Kommentare zurück in Ihre Markdown-Dateien:

```html
<!-- confluence-page-id: 12345 -->
<!-- confluence-space-key: DEV -->
```

Zum Deaktivieren:

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

## Debug-Modus

Für die Fehlerbehebung bei Konvertierungsproblemen:

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
