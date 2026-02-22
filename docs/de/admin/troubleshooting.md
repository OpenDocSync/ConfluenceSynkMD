# Fehlerbehebung

Häufige Probleme und Lösungen bei der Verwendung von ConfluentSynkMD.

---

## Authentifizierungsfehler

### `401 Unauthorized`

**Ursache**: Ungültige oder abgelaufene Zugangsdaten.

**Lösungen**:

- `CONFLUENCE__BASEURL` überprüfen
- `CONFLUENCE__USEREMAIL` und `CONFLUENCE__APITOKEN` prüfen
- Neuen API-Token generieren unter [id.atlassian.com](https://id.atlassian.com/manage-profile/security/api-tokens)
- Bei Bearer Auth: Token-Ablauf prüfen

### `403 Forbidden`

**Ursache**: Fehlende Berechtigungen für den Ziel-Space.

**Lösungen**:

- Space-Berechtigungen prüfen: **Space-Einstellungen → Berechtigungen**

---

## Space- und Seitenfehler

### `Space not found`

- Space Key überprüfen (nicht den Anzeigenamen)
- Groß-/Kleinschreibung beachten

### `Page not found`

- Page ID überprüfen
- Zugriffsrechte des authentifizierten Benutzers prüfen

---

## Diagramm-Rendering-Probleme

### `mmdc not found`

```bash
npm install -g @mermaid-js/mermaid-cli
mmdc --version
```

### Diagramme leer oder fehlerhaft

- Node.js Version prüfen (22+ erforderlich)
- In Docker: Chromium-Abhängigkeiten prüfen
- `--diagram-output-format png` versuchen

---

## Upload-Probleme

### Seiten werden nicht aktualisiert

- `--skip-update` entfernen
- Content-Hashes prüfen
- `--no-write-back` nicht verwenden

### Doppelte Seiten erstellt

- Page-ID Write-Back aktivieren (ohne `--no-write-back`)
- `<!-- confluence-page-id: ... -->` Kommentare prüfen
- `--loglevel debug` für Details

---

## Logging

```bash
--loglevel debug
```

Logs werden geschrieben nach:

- **Konsole** — Echtzeit-Ausgabe
- **Datei** — `logs/md2conf-{datum}.log` (tägliche Rotation)
