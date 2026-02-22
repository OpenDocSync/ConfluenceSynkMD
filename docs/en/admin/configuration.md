# Configuration & Environment

ConfluentSynkMD reads connection settings from **environment variables** and/or **CLI flags**.

---

## Configuration Priority

Settings are resolved in the following order (highest priority first):

1. **CLI flags** — `--conf-base-url`, `--conf-auth-mode`, `--conf-user-email`, `--conf-api-token`, `--conf-bearer-token`, `--api-version`, `--headers`
2. **Environment variables** — `CONFLUENCE__*`
3. **Default values** — Built-in defaults

---

## Environment Variables

| Variable | Description | Default |
|---|---|---|
| `CONFLUENCE__BASEURL` | Confluence Cloud URL (e.g. `https://yoursite.atlassian.net`) | — (required) |
| `CONFLUENCE__AUTHMODE` | Authentication mode: `Basic` or `Bearer` | `Basic` |
| `CONFLUENCE__USEREMAIL` | Atlassian account email (Basic Auth) | — |
| `CONFLUENCE__APITOKEN` | Atlassian API Token (Basic Auth) | — |
| `CONFLUENCE__BEARERTOKEN` | OAuth 2.0 access token (Bearer Auth) | — |
| `CONFLUENCE__OPTIMIZEIMAGES` | Downscale images before upload | `true` |
| `CONFLUENCE__MAXIMAGEWIDTH` | Maximum width for optimized images (px) | `1280` |
| `CONFLUENCE__APIPATH` | API path prefix (`/wiki` for Cloud, `""` for Data Center) | `/wiki` |
| `CONFLUENCE__APIVERSION` | REST API version (`v1` or `v2`) | `v2` |

---

## CLI Credential Flags

All credential fields can be overridden via CLI flags:

| Flag | Overrides | Description |
|---|---|---|
| `--conf-base-url` | `CONFLUENCE__BASEURL` | Confluence Cloud base URL |
| `--conf-auth-mode` | `CONFLUENCE__AUTHMODE` | Authentication mode: `Basic` or `Bearer` |
| `--conf-user-email` | `CONFLUENCE__USEREMAIL` | User email (Basic Auth) |
| `--conf-api-token` | `CONFLUENCE__APITOKEN` | API token (Basic Auth) |
| `--conf-bearer-token` | `CONFLUENCE__BEARERTOKEN` | Bearer token (OAuth 2.0) |

---

## Naming Convention

Environment variables use the `__` (double underscore) separator, which .NET maps to configuration sections:

```
CONFLUENCE__BASEURL  →  Confluence:BaseUrl
CONFLUENCE__APITOKEN →  Confluence:ApiToken
```

This follows the standard .NET configuration pattern for `IConfiguration`.

---

## Optional: `.env` File

For local convenience during development, you can store variables in a `.env` file and source it manually:

=== "Bash"

    ```bash
    export $(cat .env | grep -v '^#' | xargs)
    ```

=== "PowerShell"

    ```powershell
    Get-Content .env | Where-Object { $_ -notmatch '^\s*#' -and $_ -match '=' } |
        ForEach-Object { $k,$v = $_ -split '=',2; [System.Environment]::SetEnvironmentVariable($k.Trim(),$v.Trim()) }
    ```

=== "CMD"

    ```cmd
    for /f "usebackq tokens=1,* delims==" %%A in (".env") do @if not "%%A"=="" if not "%%A:~0,1"=="#" set "%%A=%%B"
    ```

!!! warning
    Never commit `.env` files to version control. The `.gitignore` already excludes `.env`.

!!! note
    The tool does **not** auto-load `.env` files. You must source the file yourself or use environment variables / CLI flags directly.

---

## Image Optimization

Control image processing with:

| Variable | Default | Description |
|---|---|---|
| `CONFLUENCE__OPTIMIZEIMAGES` | `true` | Enable/disable image optimization |
| `CONFLUENCE__MAXIMAGEWIDTH` | `1280` | Max width in pixels (height scales proportionally) |

Images are optimized _before_ upload to reduce Confluence storage and improve page load times.

---

## API Path and Version

| Setting | Cloud | Data Center |
|---|---|---|
| `CONFLUENCE__APIPATH` | `/wiki` | `""` (empty) |
| `CONFLUENCE__APIVERSION` | `v2` | `v1` |

!!! note
    ConfluentSynkMD is primarily tested against Confluence Cloud with API v2. Data Center support is not officially guaranteed.
