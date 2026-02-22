# Finding Confluence IDs

ConfluentSynkMD requires a **Space Key** and optionally a **Page ID** to know where to upload/download pages. This guide shows you how to find them.

---

## Space Key

The `--conf-space` value is the **Space Key** — a short identifier, not the display name.

### How to Find It

1. Navigate to your Confluence space → **Space Settings**
2. The Space Key is shown on the settings page (e.g. `MFS`, `DEV`, `DOCS`)
3. Or extract it from the URL:

```
https://yoursite.atlassian.net/wiki/spaces/MFS/...
                                          ^^^
                                        Space Key
```

!!! important "Personal Spaces"
    Personal spaces have long keys starting with `~` followed by an account ID, e.g. `~ACCOUNT_ID`. Find the key in the URL or via the REST API:

    ```
    GET /wiki/api/v2/spaces
    ```

---

## Page ID

The `--conf-parent-id` is the **numeric ID** of an existing Confluence page.

### How to Find It

1. Open the page in Confluence
2. Extract from the URL:

```
.../pages/123456/My+Page
          ^^^^^^
         Page ID
```

3. Or click the **page menu (⋯)** → **Page Information** — the page ID is shown in the URL

---

## Per-Document Space Key Override

By default, all pages go to the space specified via `--conf-space`. Override per document using frontmatter. See [Frontmatter & Metadata](frontmatter.md) for details.

=== "YAML Frontmatter"

    ```yaml
    ---
    space_key: TEAM
    ---
    ```

=== "HTML Comment"

    ```html
    <!-- confluence-space-key: TEAM -->
    ```

---

## Using Root Page Instead of Page ID

If you don't have a page ID, use `--root-page` with a page title instead:

```bash
--root-page "My Documentation"
```

If the page exists, it will be used as the parent. If it doesn't exist, it will be created as a new top-level page in the space.
