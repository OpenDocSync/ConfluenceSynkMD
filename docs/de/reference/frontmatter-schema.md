# Frontmatter-Schema

Vollständige Spezifikation der von ConfluenceSynkMD unterstützten YAML-Frontmatter-Felder.

---

## Übersicht

```yaml
---
title: Mein Seitentitel
tags:
  - dokumentation
  - api
space_key: TEAM
generated_by: Auto-generiert aus CI Pipeline
confluence_page_id: "12345"
---
```

---

## Felder

### `title`

| Eigenschaft | Wert |
|---|---|
| Typ | `string` |
| Erforderlich | Nein |
| Standard | Erste H1-Überschrift oder Dateiname |

Überschreibt den Confluence-Seitentitel.

---

### `tags`

| Eigenschaft | Wert |
|---|---|
| Typ | `string[]` |
| Erforderlich | Nein |
| Standard | Keine |

Labels für die Confluence-Seite.

---

### `space_key`

| Eigenschaft | Wert |
|---|---|
| Typ | `string` |
| Erforderlich | Nein |
| Standard | Wert von `--conf-space` |

Ziel-Confluence-Space für dieses Dokument überschreiben.

---

### `generated_by`

| Eigenschaft | Wert |
|---|---|
| Typ | `string` |
| Erforderlich | Nein |
| Standard | Wert von `--generated-by` |

Platzhalter: `%{filepath}`, `%{filename}`, `%{filedir}`, `%{filestem}`.

---

### `confluence_page_id`

| Eigenschaft | Wert |
|---|---|
| Typ | `string` |
| Erforderlich | Nein |
| Standard | Keine |

Explizite Confluence Page ID zum Aktualisieren einer bestehenden Seite.

---

## Inline-HTML-Kommentare

```html
<!-- confluence-page-id: 12345 -->
<!-- confluence-space-key: TEAM -->
```

Diese werden primär vom Write-Back-Feature verwendet.

---

## Titel-Auflösung

1. `title`-Feld im YAML-Frontmatter
2. Erste H1-Überschrift im Dokument
3. Dateiname (ohne Erweiterung)
