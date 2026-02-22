# Benutzerhandbuch

Willkommen im ConfluentSynkMD-Benutzerhandbuch. Dieser Bereich deckt alles ab, was Sie zur Synchronisation von Markdown-Dokumentation mit Confluence benötigen.

---

## Synchronisationsmodi

| Modus | Richtung | Beschreibung |
|---|---|---|
| **Upload** | Markdown → Confluence | Markdown-Dateien konvertieren und als Confluence-Seiten hochladen |
| **Download** | Confluence → Markdown | Confluence-Seiten abrufen und in Markdown konvertieren |
| **LocalExport** | Markdown → Lokales XHTML | Markdown lokal in Confluence Storage Format konvertieren (keine API-Aufrufe) |

---

## Wann welchen Modus verwenden?

### Upload

Verwenden Sie **Upload**, wenn Sie Ihre Markdown-Dokumentation auf Confluence publizieren möchten.

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

Verwenden Sie **Download**, um bestehende Confluence-Seiten in Ihr lokales Repository als Markdown-Dateien zu ziehen.

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

Verwenden Sie **LocalExport** für eine Vorschau des Confluence Storage Formats ohne API-Aufrufe.

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

## Funktionen

- [Upload-Workflow](upload.md) — Detaillierte Upload-Anleitung mit Hierarchie und Skip-Update
- [Download-Workflow](download.md) — Seiten herunterladen mit Root-Page-Auswahl
- [Lokaler Export](local-export.md) — Arbeiten ohne Confluence-API-Zugang
- [Frontmatter & Metadaten](frontmatter.md) — Seitentitel, Labels und Space Keys steuern
- [Diagramm-Rendering](diagrams.md) — Mermaid, Draw.io, PlantUML und LaTeX
- [Confluence-IDs finden](confluence-ids.md) — Space Keys und Page IDs ermitteln
