# CI/CD-Integration

Automatisieren Sie die Markdown ↔ Confluence Synchronisation mit CI/CD-Pipelines.

---

## GitHub Actions

### Upload bei Push

Dokumentation automatisch nach Confluence hochladen bei Push auf `main`:

```yaml
name: Docs nach Confluence synchronisieren

on:
  push:
    branches: [main]
    paths: ['docs/**']

jobs:
  sync:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: .NET einrichten
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Node.js einrichten
        uses: actions/setup-node@v4
        with:
          node-version: '22'

      - name: mermaid-cli installieren
        run: npm install -g @mermaid-js/mermaid-cli

      - name: ConfluenceSynkMD bauen
        run: dotnet build

      - name: Nach Confluence hochladen
        env:
          CONFLUENCE__BASEURL: ${{ secrets.CONFLUENCE_BASEURL }}
          CONFLUENCE__AUTHMODE: Basic
          CONFLUENCE__USEREMAIL: ${{ secrets.CONFLUENCE_EMAIL }}
          CONFLUENCE__APITOKEN: ${{ secrets.CONFLUENCE_TOKEN }}
        run: |
          dotnet run --project src/ConfluenceSynkMD -- \
            --mode Upload \
            --path ./docs \
            --conf-space ${{ vars.CONFLUENCE_SPACE }} \
            --root-page "Auto-synchronisierte Dokumentation" \
            --skip-update \
            --title-prefix "[CI] "
```

### Erforderliche Secrets

| Secret | Beschreibung |
|---|---|
| `CONFLUENCE_BASEURL` | z.B. `https://yoursite.atlassian.net` |
| `CONFLUENCE_EMAIL` | Atlassian-Account-E-Mail |
| `CONFLUENCE_TOKEN` | Atlassian API-Token |

---

## Ohne Upload validieren

Verwenden Sie `--local` in PR-Checks zur Validierung ohne API-Aufrufe:

```yaml
- name: Konvertierung validieren
  run: |
    dotnet run --project src/ConfluenceSynkMD -- \
      --mode Upload --path ./docs --conf-space DUMMY --local
```

---

## Tipps

- `--skip-update` — Unveränderte Seiten nicht erneut hochladen
- `--title-prefix "[CI] "` — Auto-generierte Seiten kennzeichnen
- `--no-write-back` — Pipeline-Änderungen an Quelldateien verhindern
- `--loglevel warning` — Saubere CI-Ausgabe
