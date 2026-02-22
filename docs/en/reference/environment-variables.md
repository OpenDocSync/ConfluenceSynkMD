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

## Naming Convention

Environment variables use `__` (double underscore) as a section separator, following the .NET configuration pattern:

| Environment Variable | .NET Configuration Path |
|---|---|
| `CONFLUENCE__BASEURL` | `Confluence:BaseUrl` |
| `CONFLUENCE__APITOKEN` | `Confluence:ApiToken` |
| `CONFLUENCE__MAXIMAGEWIDTH` | `Confluence:MaxImageWidth` |
