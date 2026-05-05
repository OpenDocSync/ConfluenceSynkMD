# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

Round-trip content fidelity fixes uncovered by an end-to-end test against a
real Confluence Cloud instance. Four classes of bug previously caused silent
data loss or document corruption on the Markdown ↔ Confluence Storage Format
path. All four are now fixed with regression tests.

### Fixed

- **Soft line breaks no longer drop their separator on upload.** A plain
  newline between two source lines inside a Markdown paragraph
  (`"published\nDocker image"`) used to render as `<p>publishedDocker image</p>`
  in Confluence — the words touched, the page text was unreadable. The custom
  `LineBreakInlineRenderer` now emits a literal newline for soft breaks; hard
  breaks (two trailing spaces or trailing backslash) continue to emit `<br/>`.
  Regression test names the exact word-mash from the round-trip reproducer.
- **Code blocks containing the literal `]]>` no longer corrupt the Storage
  Format.** XSLT, generated XML, and templating output frequently include
  `]]>`, which used to terminate the surrounding CDATA section early and
  produce invalid XHTML that Confluence rejected. The escape splits the
  payload across two adjacent CDATA sections (`]]]]><![CDATA[>`) on upload;
  the download path now resolves both placeholders so the Markdown comes back
  byte-equivalent.
- **Plain Markdown blockquotes round-trip as blockquotes, not as `[!NOTE]`
  alerts.** `> Just a quote` previously became a Confluence `info` macro on
  upload and round-tripped back as `> [!NOTE]\n> Just a quote` — the
  semantics changed silently on every cycle. The upload path now emits a
  standard HTML `<blockquote>`, the download path converts it back to `> `
  prefixed Markdown lines. The `--use-panel` flag still affects how explicit
  GitHub/GitLab alerts render and no longer overloads onto plain quotes.
- **Inline formatting inside link text survives the round-trip.** The download
  transform used `el.TextContent` for anchor bodies, which collapsed
  `[**bold**](url)` to `[bold](url)` — silent loss of the bold marker on the
  second download cycle. The transform now recurses into the anchor's
  children, preserving `<strong>`, `<em>`, and `<code>` inside link text.
- **GitHub-style alerts now use Markdig's typed `AlertBlock` node directly.**
  The previous detector scanned the first inline for `[!TYPE]`, but
  `UseAdvancedExtensions()` already strips the marker line during parsing, so
  every `> [!NOTE]` actually fell through to the (then-incorrect) plain-quote
  fallback and happened to produce the right output by accident. Mapping
  `AlertBlock.Kind` directly is correct by construction.

## [0.1.0] - 2026-05-05

The first published, signed, multi-arch release. ConfluenceSynkMD now ships
as a single Docker image with the full diagram toolchain preinstalled — no
host-side Node, Java, TeX Live, or drawio installs required.

### Added

- **Full diagram toolchain in the runtime image.** The Docker image ships
  Node.js 22 + `@mermaid-js/mermaid-cli` + Chromium (Mermaid), Java +
  `plantuml` (PlantUML), TeX Live + Ghostscript (LaTeX), and drawio-desktop
  + Xvfb (Draw.io). `--render-mermaid`, `--render-drawio`, `--render-plantuml`,
  and `--render-latex` work out of the box.
- **Container entrypoint with shared Xvfb display server.** `entrypoint.sh`
  starts a single `Xvfb :99` for the container's lifetime so drawio-desktop
  (Electron) can run headless without restarting the display server per
  diagram. Polls `xdpyinfo` for up to 5 seconds before exec'ing dotnet to
  close the startup race.
- **`doctor` subcommand.** Runtime self-test that renders a small canary
  diagram via every external-process renderer and probes the configured
  Confluence instance via `IConfluenceHealthCheck`. Prints a green/red
  checklist; exits 0 if every check passed, 1 otherwise. The release
  workflow uses `doctor --renderers-only` as the smoke gate before pushing
  any tag to GHCR.
- **`init` subcommand.** First-run wizard. Reads `CONFLUENCE__*` env vars,
  prompts for whatever is missing (refusing under `--non-interactive` or
  when stdin is redirected, with a single-line error naming the missing
  vars), validates against Confluence via `IConfluenceHealthCheck`, and
  writes `.confluencesynkmd.json` (or `--config-out PATH`).
- **`IConfluenceHealthCheck` service.** Lightweight reachability + auth
  probe. Hits `GET /wiki/api/v2/spaces?limit=1` with a 10s default timeout;
  falls back to v1 on 404; classifies responses into Healthy / NotConfigured
  / AuthError / UpstreamError / NetworkError. Consumed by both `doctor`
  and `init`.
- **`RendererCommandResolver` shared helper.** Parses renderer command
  env vars (`DRAWIO_CMD`, `PLANTUML_CMD`, `LATEX_PDFLATEX_CMD`,
  `LATEX_GHOSTSCRIPT_CMD`) as shell-style command-plus-args strings.
  Single-binary names (the previous contract) and multi-token invocations
  (e.g. `xvfb-run -a drawio --no-sandbox --disable-gpu`) are both supported.
- **Release workflow** at `.github/workflows/release-container.yml`.
  Triggered on `v*.*.*` tag push: builds amd64 with `--load`, smoke-tests
  via `doctor --renderers-only` BEFORE pushing, then builds + pushes
  multi-arch (linux/amd64 + linux/arm64) to GHCR with provenance + SBOM
  attestations. Signs the manifest list with cosign keyless OIDC and
  immediately re-runs `cosign verify` to prove the user-facing
  verification snippet works against the signed bytes.
- **Emergency yank workflow** at `.github/workflows/release-yank.yml`.
  Manual `workflow_dispatch` only; requires a written `reason` input that
  lands in the step summary as an audit-log entry. Tag-only delete via
  `gh api`; pinned digest pulls remain valid.
- **GitHub Action wrapper** at `action.yml`. Composite action consuming
  the published image. Inputs: `subcommand`, `path`, `conf-space`,
  `conf-parent-id`, `keep-hierarchy`, `skip-update`, `image`, `extra-args`.
  Mounts the workspace read-only for `upload`, writable for
  `download`/`local`. Forwards `CONFLUENCE__*` env vars to the container.
- **`DRAWIO_VERSION` Dockerfile pin** tracked monthly by
  `.github/workflows/docker-manual-pin-check.yml` alongside the existing
  `MERMAID_CLI_VERSION` and `NODEJS_MAJOR` pins. The refresh PR body
  surfaces arm64 `.deb` availability for the candidate version.
- **`CliMigrationCheck`** detects users running the legacy `--mode <Value>`,
  `--mode=Value`, `--mode:Value`, or bare `--local` syntax at startup and
  prints an inline migration table before exiting with code 2. No silent
  failures for users on the v0 CLI shape.
- **`.gitattributes`** forcing LF line endings on `.sh`, `Dockerfile`, and
  YAML files so Windows contributors do not introduce CRLF in
  Linux-target files.

### Changed

- **CLI shape: subcommand tree.** `--mode Upload|Download|LocalExport`
  was replaced by three top-level subcommands (`upload`, `download`, `local`)
  alongside `doctor` and `init`. This matches the idiomatic
  System.CommandLine pattern. Pre-1.0 makes the breaking change cheap;
  the migration check above keeps users informed.
- **`LatexRenderer` calls Ghostscript directly.** The PDF→PNG rasterization
  step previously delegated to ImageMagick's `convert`, which itself wraps
  Ghostscript. Calling `gs` directly drops the ImageMagick dependency from
  the runtime image (~80 MB) and removes the need for the `policy.xml`
  workaround for ImageMagick's PDF read defaults. Adds
  `LATEX_PDFLATEX_CMD` and `LATEX_GHOSTSCRIPT_CMD` env-var overrides.
- **README headline** rewritten around the published Docker image. The new
  Quick Start is three commands (`doctor`, `upload`, done). Also gains a
  "What ships in the default image" coherence table and a GitHub Action
  snippet.

### Removed

- **`--mode` flag** (replaced by subcommands above).
- **`--local` flag** (replaced by the `local` subcommand).
- **ImageMagick** from the runtime image (Ghostscript handles PDF→PNG
  natively; no functionality lost). The previously documented
  `policy.xml` workaround is no longer required.

### Security

- **Cosign-signed multi-arch image.** Every `v*.*.*` release publishes a
  manifest-list digest signed by cosign keyless OIDC against the GitHub
  Actions workflow that built it. SBOM (`docker/build-push-action`
  `sbom: true`) and SLSA provenance (`provenance: mode=max`) attestations
  are attached to the OCI manifest. Verify with the snippet pinned in
  the release notes.

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

### Known Issues

- **Draw.io rendering inside the published Docker image is unreliable in v0.1.0.** drawio-desktop is installed and the binary runs, but its container-headless behavior (Electron + Xvfb + dbus combo running as an unprivileged user) does not produce export output reliably. Symptom: `confluencesynkmd doctor` shows `[FAIL] Draw.io` with `Error: input file/directory not found`. The smoke gate built into the release workflow (`doctor --renderers-only`) deliberately skips drawio for this reason — it is reported as `[ OK ] Draw.io  (skipped under --renderers-only ...)`. **For v0.1.0, do not rely on automatic `--render-drawio` in the published image.** Mermaid, PlantUML, and LaTeX work correctly. Tracked for v0.1.1.

### Forward-only fix policy

If a published `:0.1.x` release contains a critical bug, the project ships
a `:0.1.x+1` patch and updates the moving `:0.1` tag. Pinned digest pulls
(`docker pull ghcr.io/opendocsync/confluencesynkmd@sha256:...`) are never
deleted. For security-grade incidents only (leaked credentials, actively
dangerous regressions), the `release-yank.yml` workflow deletes a tag
(not a digest). Routine bugs are fixed forward.

### Image tag scheme

For pre-1.0 releases, `metadata-action` emits exact-version (`0.1.0`),
minor-track (`0.1`), and `sha-<short>` tags. `latest`, the major-track
(`0`), and (`1`) are intentionally NOT created until v1.0.0 ships.
