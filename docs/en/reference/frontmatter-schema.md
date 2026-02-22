# Frontmatter Schema

Complete specification of YAML frontmatter fields supported by ConfluenceSynkMD.

---

## Overview

ConfluenceSynkMD reads YAML frontmatter from the top of Markdown files to control per-document behavior. Frontmatter is optional — files without it are processed normally.

```yaml
---
title: My Page Title
tags:
  - documentation
  - api
space_key: TEAM
generated_by: Auto-generated from CI pipeline
confluence_page_id: "12345"
---
```

---

## Fields

### `title`

| Property | Value |
|---|---|
| Type | `string` |
| Required | No |
| Default | First H1 heading or filename |

Overrides the Confluence page title. If not set, the title is taken from the first `# Heading` in the document, or the filename (without extension) as a fallback.

---

### `tags`

| Property | Value |
|---|---|
| Type | `string[]` |
| Required | No |
| Default | None |

List of labels applied to the Confluence page. Labels are used for filtering and organizing pages in Confluence.

```yaml
tags:
  - release-notes
  - v2.0
  - public
```

---

### `space_key`

| Property | Value |
|---|---|
| Type | `string` |
| Required | No |
| Default | Value of `--conf-space` CLI flag |

Override the target Confluence space for this specific document. When set, the page is created in the specified space rather than the global CLI space.

---

### `generated_by`

| Property | Value |
|---|---|
| Type | `string` |
| Required | No |
| Default | Value of `--generated-by` CLI flag |

Override the generated-by marker for this document. Supports template placeholders:

| Placeholder | Description |
|---|---|
| `%{filepath}` | Full relative file path |
| `%{filename}` | Filename with extension |
| `%{filedir}` | Directory part of the path |
| `%{filestem}` | Filename without extension |

---

### `confluence_page_id`

| Property | Value |
|---|---|
| Type | `string` |
| Required | No |
| Default | None (auto-detected from write-back comments) |

Explicit Confluence Page ID. When set, upload will update this specific page instead of creating a new one. This field is typically managed automatically via the write-back feature.

---

## Inline HTML Comments

As an alternative to YAML frontmatter, ConfluenceSynkMD also reads inline HTML comments:

```html
<!-- confluence-page-id: 12345 -->
<!-- confluence-space-key: TEAM -->
```

These are primarily used by the write-back feature and can appear anywhere in the document (not just at the top).
