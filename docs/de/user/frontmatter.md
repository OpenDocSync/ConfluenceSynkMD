# Frontmatter & Metadaten

ConfluentSynkMD unterstützt YAML-Frontmatter und Inline-HTML-Kommentare zur Steuerung von Seiten-Metadaten auf Dokumentebene.

---

## YAML-Frontmatter

Fügen Sie YAML-Frontmatter am Anfang Ihrer Markdown-Datei hinzu:

```yaml
---
title: Mein Seitentitel
tags:
  - dokumentation
  - api
space_key: TEAM
generated_by: Auto-generiert aus CI
---
```

### Unterstützte Felder

| Feld | Typ | Beschreibung |
|---|---|---|
| `title` | `string` | Seitentitel in Confluence (überschreibt die erste H1-Überschrift) |
| `tags` | `string[]` | Labels/Tags für die Confluence-Seite |
| `space_key` | `string` | Ziel-Confluence-Space für dieses Dokument überschreiben |
| `generated_by` | `string` | Generated-By-Marker für dieses Dokument überschreiben |
| `confluence_page_id` | `string` | Explizite Confluence Page ID zum Aktualisieren einer bestehenden Seite |

---

## Inline-HTML-Kommentare

Alternativ zum YAML-Frontmatter können Inline-HTML-Kommentare verwendet werden:

```html
<!-- confluence-page-id: 12345 -->
<!-- confluence-space-key: TEAM -->
```

---

## Per-Dokument Space-Key-Override

=== "Frontmatter"

    ```yaml
    ---
    space_key: TEAM
    ---
    ```

=== "HTML-Kommentar"

    ```html
    <!-- confluence-space-key: TEAM -->
    ```

---

## Titel-Auflösung

Seitentitel werden in folgender Priorität aufgelöst:

1. `title`-Feld im YAML-Frontmatter
2. Erste H1-Überschrift (`# Überschrift`) im Dokument
3. Dateiname (ohne Erweiterung)

!!! tip
    Verwenden Sie `--skip-title-heading`, um die erste H1 aus dem Seiteninhalt zu entfernen, wenn sie als Titel verwendet wird.

---

## Labels / Tags

Tags aus dem Frontmatter werden als Confluence-Labels angewendet:

```yaml
---
tags:
  - release-notes
  - v2
---
```
