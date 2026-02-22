# CI/CD Integration

Automate Markdown ↔ Confluence synchronization with CI/CD pipelines.

---

## GitHub Actions

### Upload on Push

Automatically upload documentation to Confluence whenever changes are pushed to `main`:

```yaml
name: Sync Docs to Confluence

on:
  push:
    branches: [main]
    paths: ['docs/**']

jobs:
  sync:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: '22'

      - name: Install mermaid-cli
        run: npm install -g @mermaid-js/mermaid-cli

      - name: Build ConfluentSynkMD
        run: dotnet build

      - name: Upload to Confluence
        env:
          CONFLUENCE__BASEURL: ${{ secrets.CONFLUENCE_BASEURL }}
          CONFLUENCE__AUTHMODE: Basic
          CONFLUENCE__USEREMAIL: ${{ secrets.CONFLUENCE_EMAIL }}
          CONFLUENCE__APITOKEN: ${{ secrets.CONFLUENCE_TOKEN }}
        run: |
          dotnet run --project src/ConfluentSynkMD -- \
            --mode Upload \
            --path ./docs \
            --conf-space ${{ vars.CONFLUENCE_SPACE }} \
            --root-page "Auto-Synced Documentation" \
            --skip-update \
            --title-prefix "[CI] "
```

### Required Secrets

Add these to your GitHub repository under **Settings → Secrets and variables → Actions**:

| Secret | Description |
|---|---|
| `CONFLUENCE_BASEURL` | e.g. `https://yoursite.atlassian.net` |
| `CONFLUENCE_EMAIL` | Atlassian account email |
| `CONFLUENCE_TOKEN` | Atlassian API token |

| Variable | Description |
|---|---|
| `CONFLUENCE_SPACE` | Target space key |

---

## Docker-Based Pipeline

Use the Docker image directly for simpler CI setups:

```yaml
  sync:
    runs-on: ubuntu-latest
    container:
      image: confluentsynkmd:latest
    steps:
      - uses: actions/checkout@v4
      - name: Upload
        env:
          CONFLUENCE__BASEURL: ${{ secrets.CONFLUENCE_BASEURL }}
          CONFLUENCE__USEREMAIL: ${{ secrets.CONFLUENCE_EMAIL }}
          CONFLUENCE__APITOKEN: ${{ secrets.CONFLUENCE_TOKEN }}
        run: |
          dotnet ConfluentSynkMD.dll \
            --mode Upload --path ./docs \
            --conf-space ${{ vars.CONFLUENCE_SPACE }}
```

---

## Validate Without Uploading

Use `--local` mode in PR checks to validate that Markdown files convert successfully without making API calls:

```yaml
  validate:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - run: dotnet build
      - name: Validate conversion
        run: |
          dotnet run --project src/ConfluentSynkMD -- \
            --mode Upload --path ./docs \
            --conf-space DUMMY --local
```

---

## Tips

- Use `--skip-update` to avoid re-uploading unchanged pages
- Use `--title-prefix "[CI] "` to distinguish auto-generated pages
- Use `--no-write-back` in CI to prevent the pipeline from modifying source files
- Set `--loglevel warning` in CI for cleaner output
