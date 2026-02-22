# CLI-Referenz

Vollständige Referenz aller ConfluentSynkMD-Kommandozeilenoptionen.

---

## Kern-Optionen

| Option | Erforderlich | Standard | Beschreibung |
|---|---|---|---|
| `--mode <Upload\|Download\|LocalExport>` | ✅ | — | Synchronisationsrichtung |
| `--path <Pfad>` | ✅ | — | Lokaler Dateisystempfad zu Markdown-Dateien |
| `--conf-space <Key>` | ✅ | — | Confluence Space Key (z.B. `DEV`) |
| `--conf-parent-id <ID>` | | — | Elternseiten-ID für Unterbaum-Operationen |

---

## Credential-Optionen

Überschreiben Confluence-Verbindungseinstellungen (höhere Priorität als Umgebungsvariablen):

| Option | Standard | Beschreibung |
|---|---|---|
| `--conf-base-url <URL>` | — | Confluence Cloud Base-URL (überschreibt `CONFLUENCE__BASEURL`) |
| `--conf-auth-mode <Basic\|Bearer>` | — | Authentifizierungsmodus (überschreibt `CONFLUENCE__AUTHMODE`) |
| `--conf-user-email <E-Mail>` | — | Benutzer-E-Mail für Basic Auth (überschreibt `CONFLUENCE__USEREMAIL`) |
| `--conf-api-token <Token>` | — | API-Token für Basic Auth (überschreibt `CONFLUENCE__APITOKEN`) |
| `--conf-bearer-token <Token>` | — | Bearer-Token für OAuth 2.0 (überschreibt `CONFLUENCE__BEARERTOKEN`) |

---

## Sync-Steuerung

| Option | Standard | Beschreibung |
|---|---|---|
| `--root-page <Titel>` | — | Wurzelseitentitel (Alternative zu `--conf-parent-id`) |
| `--keep-hierarchy` | `true` | Lokale Verzeichnishierarchie in Confluence beibehalten |
| `--skip-hierarchy` | `false` | Alle Seiten flach unter die Wurzelseite legen |
| `--skip-update` | `false` | Unveränderte Seiten nicht erneut hochladen |
| `--local` | `false` | Nur lokale CSF-Ausgabe, keine API-Aufrufe |
| `--no-write-back` | `false` | Keine Page-ID-Kommentare in Markdown zurückschreiben |
| `--loglevel <Level>` | `info` | Log-Verbosität: `debug`, `info`, `warning`, `error`, `critical` |

---

## API-Einstellungen

| Option | Standard | Beschreibung |
|---|---|---|
| `--api-version <v1\|v2>` | `v2` | Confluence REST API Version |
| `--headers <KEY=VALUE>` | — | Benutzerdefinierte HTTP-Header |

---

## Konverter-Optionen

| Option | Standard | Beschreibung |
|---|---|---|
| `--heading-anchors` | `false` | Anker-Makros vor Überschriften einfügen |
| `--force-valid-url` | `false` | Ungültige URLs bereinigen und escapen |
| `--skip-title-heading` | `false` | Erste H1-Überschrift weglassen |
| `--prefer-raster` | `false` | Rasterbilder statt Vektor bevorzugen |
| `--webui-links` | `false` | Interne `.md`-Links als Confluence Web-UI-URLs rendern |
| `--webui-link-strategy <space-title\|page-id>` | `space-title` | Strategie für Web-UI-Links |
| `--use-panel` | `false` | Panel-Makro statt Info/Note/Warning für Alerts |
| `--force-valid-language` | `false` | Code-Block-Sprachen gegen Confluence-unterstützte validieren |
| `--code-line-numbers` | `false` | Zeilennummern in Code-Block-Makros anzeigen (Alias: `--line-numbers`) |
| `--debug-line-markers` | `false` | Quellzeilennummern in Fehlermeldungen einschließen |
| `--title-prefix <Prefix>` | — | Prefix für alle Seitentitel (z.B. `[AUTO] `) |
| `--generated-by <Wert>` | `MARKDOWN` | Generated-By-Marker. Unterstützt: `%{filepath}`, `%{filename}`, `%{filedir}`, `%{filestem}`. Leer setzen zum Deaktivieren |

---

## Diagramm-Rendering

| Option | Standard | Beschreibung |
|---|---|---|
| `--render-mermaid` | `true` | Mermaid-Codeblöcke als Bilder rendern |
| `--no-render-mermaid` | `false` | Mermaid-Rendering deaktivieren |
| `--render-drawio` | `false` | Draw.io-Rendering aktivieren |
| `--render-plantuml` | `false` | PlantUML-Rendering aktivieren |
| `--render-latex` | `false` | LaTeX-Rendering aktivieren |
| `--diagram-output-format` | `png` | Ausgabeformat: `png` oder `svg` |

---

## Layout-Optionen

| Option | Standard | Beschreibung |
|---|---|---|
| `--layout-image-alignment` | — | Bildausrichtung: `center`, `left`, `right` |
| `--layout-image-max-width <px>` | — | Maximale Bildbreite in Pixeln |
| `--layout-table-width <px>` | — | Tabellenbreite in Pixeln |
| `--layout-table-display-mode` | `responsive` | Tabellenmodus: `responsive` oder `fixed` |
| `--layout-alignment` | — | Inhaltsausrichtung |

---

## Beispiele

### Upload mit Zugangsdaten und häufigen Optionen

=== "Bash"

    ```bash
    dotnet run --project src/ConfluentSynkMD -- \
      --mode Upload \
      --path ./docs \
      --conf-space DEV \
      --root-page "Meine Dokumentation" \
      --conf-base-url https://yoursite.atlassian.net \
      --conf-user-email user@example.com \
      --conf-api-token ihr-token \
      --keep-hierarchy \
      --skip-update \
      --render-mermaid \
      --code-line-numbers \
      --heading-anchors \
      --title-prefix "[AUTO] "
    ```

=== "PowerShell"

    ```powershell
    dotnet run --project src/ConfluentSynkMD -- `
      --mode Upload `
      --path ./docs `
      --conf-space DEV `
      --root-page "Meine Dokumentation" `
      --conf-base-url https://yoursite.atlassian.net `
      --conf-user-email user@example.com `
      --conf-api-token ihr-token `
      --keep-hierarchy `
      --skip-update `
      --render-mermaid `
      --code-line-numbers `
      --heading-anchors `
      --title-prefix "[AUTO] "
    ```

=== "CMD"

    ```cmd
    dotnet run --project src/ConfluentSynkMD -- ^
      --mode Upload ^
      --path .\docs ^
      --conf-space DEV ^
      --root-page "Meine Dokumentation" ^
      --conf-base-url https://yoursite.atlassian.net ^
      --conf-user-email user@example.com ^
      --conf-api-token ihr-token ^
      --keep-hierarchy ^
      --skip-update ^
      --render-mermaid ^
      --code-line-numbers ^
      --heading-anchors ^
      --title-prefix "[AUTO] "
    ```

### Einen Unterbaum herunterladen

=== "Bash"

    ```bash
    dotnet run --project src/ConfluentSynkMD -- \
      --mode Download --path ./output \
      --conf-space DEV --conf-parent-id 12345
    ```

=== "PowerShell"

    ```powershell
    dotnet run --project src/ConfluentSynkMD -- `
      --mode Download --path ./output `
      --conf-space DEV --conf-parent-id 12345
    ```

=== "CMD"

    ```cmd
    dotnet run --project src/ConfluentSynkMD -- ^
      --mode Download --path .\output ^
      --conf-space DEV --conf-parent-id 12345
    ```

### Lokaler Export zum Debuggen

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
