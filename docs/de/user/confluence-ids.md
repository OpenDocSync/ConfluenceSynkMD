# Confluence-IDs finden

ConfluentSynkMD benötigt einen **Space Key** und optional eine **Page ID**, um zu wissen, wohin Seiten hochgeladen/heruntergeladen werden sollen.

---

## Space Key

Der `--conf-space`-Wert ist der **Space Key** — eine kurze Kennung, nicht der Anzeigename.

### So finden Sie ihn

1. Navigieren Sie zu Ihrem Confluence Space → **Space-Einstellungen**
2. Der Space Key wird auf der Einstellungsseite angezeigt (z.B. `MFS`, `DEV`, `DOCS`)
3. Oder extrahieren Sie ihn aus der URL:

```
https://yoursite.atlassian.net/wiki/spaces/MFS/...
                                          ^^^
                                       Space Key
```

!!! important "Persönliche Spaces"
    Persönliche Spaces haben lange Keys, die mit `~` beginnen, gefolgt von einer Account-ID.

---

## Page ID

Die `--conf-parent-id` ist die **numerische ID** einer existierenden Confluence-Seite.

### So finden Sie sie

1. Öffnen Sie die Seite in Confluence
2. Extrahieren Sie sie aus der URL:

```
.../pages/123456/Meine+Seite
          ^^^^^^
         Page ID
```

3. Oder klicken Sie auf das **Seitenmenü (⋯)** → **Seiteninformationen**

---

## Per-Dokument Space-Key-Override

Siehe [Frontmatter & Metadaten](frontmatter.md) für Details.

---

## Root-Page statt Page ID verwenden

```bash
--root-page "Meine Dokumentation"
```

Falls die Seite nicht existiert, wird sie als neue Top-Level-Seite im Space erstellt.
