# Download Workflow

Download fetches Confluence pages and converts them back into Markdown files on your local filesystem.

---

## Basic Download

=== "Bash"

    ```bash
    dotnet run --project src/ConfluenceSynkMD -- \
      --mode Download \
      --path ./output \
      --conf-space DEV \
      --conf-parent-id 12345
    ```

=== "PowerShell"

    ```powershell
    dotnet run --project src/ConfluenceSynkMD -- `
      --mode Download `
      --path ./output `
      --conf-space DEV `
      --conf-parent-id 12345
    ```

=== "CMD"

    ```cmd
    dotnet run --project src/ConfluenceSynkMD -- ^
      --mode Download ^
      --path .\output ^
      --conf-space DEV ^
      --conf-parent-id 12345
    ```

This downloads all child pages under page `12345` in space `DEV` and saves them as `.md` files in `./output`.

### Docker Download

=== "Bash"

    ```bash
    docker run --rm -it \
      -e CONFLUENCE__BASEURL=https://yoursite.atlassian.net \
      -e CONFLUENCE__AUTHMODE=Basic \
      -e CONFLUENCE__USEREMAIL=user@example.com \
      -e CONFLUENCE__APITOKEN=your-token \
      -v ${PWD}:/workspace \
      confluencesynkmd \
      --mode Download \
      --path /workspace/output \
      --conf-space DEV \
      --conf-parent-id 12345
    ```

    ???+ tip "Path Hint"
        - Keep `-v ${PWD}:/workspace` unchanged.
        - For download, `--path` points to the target folder inside the container (for example `/workspace/output`).
        - `${PWD}` is your current local directory.

=== "PowerShell"

    ```powershell
    docker run --rm -it `
      -e CONFLUENCE__BASEURL=https://yoursite.atlassian.net `
      -e CONFLUENCE__AUTHMODE=Basic `
      -e CONFLUENCE__USEREMAIL=user@example.com `
      -e CONFLUENCE__APITOKEN=your-token `
      -v ${PWD}:/workspace `
      confluencesynkmd `
      --mode Download `
      --path /workspace/output `
      --conf-space DEV `
      --conf-parent-id 12345
    ```

    ???+ tip "Path Hint"
        - Keep `-v ${PWD}:/workspace` unchanged.
        - For download, `--path` points to the target folder inside the container (for example `/workspace/output`).
        - `${PWD}` is your current local directory.

=== "CMD"

    ```cmd
    docker run --rm -it ^
      -e CONFLUENCE__BASEURL=https://yoursite.atlassian.net ^
      -e CONFLUENCE__AUTHMODE=Basic ^
      -e CONFLUENCE__USEREMAIL=user@example.com ^
      -e CONFLUENCE__APITOKEN=your-token ^
      -v %cd%:/workspace ^
      confluencesynkmd ^
      --mode Download ^
      --path /workspace/output ^
      --conf-space DEV ^
      --conf-parent-id 12345
    ```

    ???+ tip "Path Hint"
        - Keep `-v %cd%:/workspace` unchanged.
        - For download, `--path` points to the target folder inside the container (for example `/workspace/output`).
        - `%cd%` is your current local directory.

---

## Download by Root Page Title

Instead of using a numeric page ID, you can specify the root page by title:

=== "Bash"

    ```bash
    dotnet run --project src/ConfluenceSynkMD -- \
      --mode Download \
      --path ./output \
      --conf-space DEV \
      --root-page "My Documentation"
    ```

=== "PowerShell"

    ```powershell
    dotnet run --project src/ConfluenceSynkMD -- `
      --mode Download `
      --path ./output `
      --conf-space DEV `
      --root-page "My Documentation"
    ```

=== "CMD"

    ```cmd
    dotnet run --project src/ConfluenceSynkMD -- ^
      --mode Download ^
      --path .\output ^
      --conf-space DEV ^
      --root-page "My Documentation"
    ```

---

## Directory Structure Reconstruction

If pages were originally uploaded with `--keep-hierarchy`, ConfluenceSynkMD restores the exact directory structure on download using `source-path` metadata that was embedded during upload.

**Example output:**

```
output/
├── getting-started.md
├── guides/
│   ├── setup.md
│   └── advanced.md
└── api/
    └── endpoints.md
```

---

## Download Limitations

!!! warning "Important Limitations"
    - **No incremental download** — Download always fetches the full subtree; there is no delta sync
    - **Markdown fidelity** — Not all Confluence macros have a Markdown equivalent. Complex macro formatting may be simplified
    - **Attachments** — Images and attachments referenced by pages are downloaded alongside the Markdown files

---

## Typical Workflows

### Initial Migration

=== "Bash"

    ```bash
    # Download all pages from a Confluence space
    dotnet run --project src/ConfluenceSynkMD -- \
      --mode Download \
      --path ./migrated-docs \
      --conf-space TEAM \
      --conf-parent-id 98765
    ```

=== "PowerShell"

    ```powershell
    # Download all pages from a Confluence space
    dotnet run --project src/ConfluenceSynkMD -- `
      --mode Download `
      --path ./migrated-docs `
      --conf-space TEAM `
      --conf-parent-id 98765
    ```

=== "CMD"

    ```cmd
    REM Download all pages from a Confluence space
    dotnet run --project src/ConfluenceSynkMD -- ^
      --mode Download ^
      --path .\migrated-docs ^
      --conf-space TEAM ^
      --conf-parent-id 98765
    ```

Then review and edit the generated Markdown, commit to Git, and use **Upload** for ongoing sync.

### Sync Back Changes

If someone edits a page directly in Confluence:

=== "Bash"

    ```bash
    # Download the latest state
    dotnet run --project src/ConfluenceSynkMD -- \
      --mode Download \
      --path ./docs \
      --conf-space DEV \
      --root-page "My Documentation"
    ```

=== "PowerShell"

    ```powershell
    # Download the latest state
    dotnet run --project src/ConfluenceSynkMD -- `
      --mode Download `
      --path ./docs `
      --conf-space DEV `
      --root-page "My Documentation"
    ```

=== "CMD"

    ```cmd
    REM Download the latest state
    dotnet run --project src/ConfluenceSynkMD -- ^
      --mode Download ^
      --path .\docs ^
      --conf-space DEV ^
      --root-page "My Documentation"
    ```

Then commit the updated `.md` files to your repository.
