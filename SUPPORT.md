# Support

## Getting Help

- Questions and usage help: open a GitHub Discussion (recommended)
- Bugs and reproducible issues: open a GitHub Issue
- Security concerns: see `SECURITY.md` and use private reporting channels

## Before Opening an Issue

- Confirm you are using the latest `main` branch or latest release
- Check existing issues/discussions for duplicates
- Include your OS, .NET SDK version, command used, and logs/error output

## FAQ (Placeholder)

- **Q:** Does this support Confluence Data Center?
  **A:** Cloud is the primary target. Data Center may partially work but is not officially supported.

- **Q:** Why are some diagrams not rendered?
  **A:** Some diagram modes require external binaries (for example `mmdc`, `plantuml`, `drawio-desktop`, TeX Live + Ghostscript). The published Docker image (`ghcr.io/opendocsync/confluencesynkmd:0.1`) ships every renderer preinstalled — run `docker run --rm ghcr.io/opendocsync/confluencesynkmd:0.1 doctor --renderers-only` to verify.

- **Q:** Why was my page not updated?
  **A:** Check `--skip-update`, content hash behavior, and write-back metadata (`confluence-page-id`).
