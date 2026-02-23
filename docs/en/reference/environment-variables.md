# Environment Variables

Complete reference of all environment variables used by ConfluenceSynkMD.

!!! tip "CLI Override"
    All credential variables can also be set via CLI flags (e.g. `--conf-base-url`). CLI flags take priority over environment variables. See the [CLI Reference](cli.md) for details.

---

## Confluence Connection

| Variable | Required | Default | Description |
|---|---|---|---|
| `CONFLUENCE__BASEURL` | ✅ | — | Confluence Cloud URL (e.g. `https://yoursite.atlassian.net`) |
| `CONFLUENCE__AUTHMODE` | | `Basic` | Authentication mode: `Basic` or `Bearer` |

---

## Basic Auth

| Variable | Required | Default | Description |
|---|---|---|---|
| `CONFLUENCE__USEREMAIL` | When `Basic` | — | Atlassian account email |
| `CONFLUENCE__APITOKEN` | When `Basic` | — | Atlassian API Token |

---

## Bearer Auth

| Variable | Required | Default | Description |
|---|---|---|---|
| `CONFLUENCE__BEARERTOKEN` | When `Bearer` | — | OAuth 2.0 access token |

---

## Image Processing

| Variable | Required | Default | Description |
|---|---|---|---|
| `CONFLUENCE__OPTIMIZEIMAGES` | | `true` | Downscale images before upload |
| `CONFLUENCE__MAXIMAGEWIDTH` | | `1280` | Maximum image width in pixels |

---

## API Configuration

| Variable | Required | Default | Description |
|---|---|---|---|
| `CONFLUENCE__APIPATH` | | `/wiki` | API path prefix. Use `/wiki` for Cloud, `""` for Data Center |
| `CONFLUENCE__APIVERSION` | | `v2` | REST API version: `v1` or `v2` |

---

## Mermaid Rendering

| Variable | Required | Default | Description |
|---|---|---|---|
| `MERMAID_MMDC_COMMAND` | | auto (`mmdc` / `mmdc.cmd`) | Optional executable path/name for local Mermaid CLI. If available, local rendering is preferred over Docker. |
| `MERMAID_DOCKER_IMAGE` | Docker fallback only | `ghcr.io/mermaid-js/mermaid-cli/mermaid-cli` | Docker image used when local `mmdc` is unavailable. |
| `MERMAID_DOCKER_VOLUME` | In-container Docker fallback | — | Host-visible shared temp path/volume for sibling container rendering. |
| `MERMAID_USE_PUPPETEER_CONFIG` | | `false` | When `true`, passes a generated Puppeteer config in Docker fallback mode. |

!!! note "Security recommendation"
    Prefer local `mmdc` rendering to avoid mounting `/var/run/docker.sock`.
    If local `mmdc` is not available, you can disable Mermaid rendering with `--no-render-mermaid`.

---

## Naming Convention

Environment variables use `__` (double underscore) as a section separator, following the .NET configuration pattern:

| Environment Variable | .NET Configuration Path |
|---|---|
| `CONFLUENCE__BASEURL` | `Confluence:BaseUrl` |
| `CONFLUENCE__APITOKEN` | `Confluence:ApiToken` |
| `CONFLUENCE__MAXIMAGEWIDTH` | `Confluence:MaxImageWidth` |
