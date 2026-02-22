# Frontmatter & Metadata

ConfluentSynkMD supports YAML frontmatter and inline HTML comments for controlling page metadata on a per-document basis.

---

## YAML Frontmatter

Add YAML frontmatter at the top of your Markdown file:

```yaml
---
title: My Custom Page Title
tags:
  - documentation
  - api
space_key: TEAM
generated_by: Auto-generated from CI
---

# Page Content Starts Here
```

### Supported Fields

| Field | Type | Description |
|---|---|---|
| `title` | `string` | Page title in Confluence (overrides the first H1 heading) |
| `tags` | `string[]` | Labels/tags applied to the Confluence page |
| `space_key` | `string` | Override the target Confluence space for this document |
| `generated_by` | `string` | Override the global `--generated-by` marker for this document |
| `confluence_page_id` | `string` | Explicit Confluence Page ID for updating an existing page |

---

## Inline HTML Comments

As an alternative to YAML frontmatter, you can use inline HTML comments anywhere in the document:

```html
<!-- confluence-page-id: 12345 -->
<!-- confluence-space-key: TEAM -->
```

These comments are also used by the **write-back** feature after upload. See [Upload Workflow](upload.md#page-id-write-back) for details.

---

## Per-Document Space Key Override

By default, all pages are uploaded to the space specified via `--conf-space`. You can override this per document:

=== "Frontmatter"

    ```yaml
    ---
    space_key: TEAM
    ---
    ```

=== "HTML Comment"

    ```html
    <!-- confluence-space-key: TEAM -->
    ```

When set, the document will be created/updated in the overridden space. The write-back comment reflects the actual space used.

---

## Title Resolution

Page titles are resolved in this order of priority:

1. `title` field in YAML frontmatter
2. First H1 heading (`# Heading`) in the document
3. Filename (without extension)

!!! tip
    Use `--skip-title-heading` to omit the first H1 from the page body when it's used as the title. This avoids a duplicate heading in Confluence.

---

## Labels / Tags

Tags from frontmatter are applied as Confluence labels:

```yaml
---
tags:
  - release-notes
  - v2
---
```

Labels appear in Confluence under the page and can be used for filtering and searching.
