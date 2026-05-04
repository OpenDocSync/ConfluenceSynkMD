# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

This section tracks work toward the upcoming `v0.1.0` release: a portable
Confluence publishing engine packaged as a single multi-arch Docker image
with the full diagram toolchain (Mermaid, Draw.io, PlantUML, LaTeX) baked in.

### Added
- **Full diagram toolchain in the runtime image.** The Docker image now ships
  Java + PlantUML, TeX Live + Ghostscript, and drawio-desktop alongside the
  existing Node + mermaid-cli + Chromium. `--render-drawio`, `--render-plantuml`,
  and `--render-latex` work out of the box without host-side installs.
- **Container entrypoint with shared Xvfb display server.** `entrypoint.sh`
  starts a single `Xvfb :99` for the container's lifetime so drawio-desktop
  (Electron) can run headlessly without restarting the display server per
  diagram.
- **`DRAWIO_VERSION` Dockerfile pin** tracked monthly by
  `.github/workflows/docker-manual-pin-check.yml` alongside the existing
  `MERMAID_CLI_VERSION` and `NODEJS_MAJOR` pins. The refresh PR body surfaces
  arm64 `.deb` availability for the candidate version.
- **Renderer cmd-args parsing.** `DRAWIO_CMD`, `PLANTUML_CMD`,
  `LATEX_PDFLATEX_CMD`, and `LATEX_GHOSTSCRIPT_CMD` env vars now accept
  multi-token values (e.g. `xvfb-run -a drawio --no-sandbox`) via a shared
  `RendererCommandResolver` helper. Single-binary names (the previous contract)
  remain valid.
- **`.gitattributes`** forcing LF line endings on `.sh`, `Dockerfile`, and YAML
  files so contributors on Windows do not accidentally introduce CRLF in
  Linux-target files.

### Changed
- **CLI shape: subcommand tree.** `--mode Upload|Download|LocalExport` was
  replaced by three top-level subcommands: `upload`, `download`, `local`.
  This matches the idiomatic System.CommandLine pattern and clears the way
  for the `doctor` and `init` subcommands shipping later in v0.1.0.
- **`LatexRenderer` calls Ghostscript directly.** The PDF→PNG rasterization
  step previously delegated to ImageMagick's `convert`, which itself wraps
  Ghostscript. Calling `gs` directly drops the ImageMagick dependency from
  the runtime image (~80 MB) and removes the need for the `policy.xml`
  workaround for PDF read defaults.
- **Migration error message** for users hitting the legacy `--mode` / `--local`
  flags now inlines the migration table instead of referencing an external
  changelog entry.

### Removed
- **`--mode` flag** (replaced by subcommands above).
- **`--local` flag** (replaced by the `local` subcommand).
- **ImageMagick** from the runtime image (Ghostscript handles PDF→PNG
  natively; no functionality lost).

### Migration

| Old (`--mode`) syntax        | New (subcommand) syntax |
|------------------------------|--------------------------|
| `--mode Upload`              | `upload`                 |
| `--mode Download`            | `download`               |
| `--mode LocalExport`         | `local`                  |
| `--mode Upload --local`      | `local`                  |

The binary detects the legacy syntax (including `--mode=Upload`,
`--mode:Upload`, and bare `--local`) at startup and prints the migration
table before exiting with code `2`. No silent failures.
