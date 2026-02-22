# Konfiguration & Umgebung

ConfluentSynkMD liest Verbindungseinstellungen aus **Umgebungsvariablen** und/oder **CLI-Flags**.

---

## Konfigurationspriorität

Einstellungen werden in folgender Reihenfolge aufgelöst (höchste Priorität zuerst):

1. **CLI-Flags** — `--conf-base-url`, `--conf-auth-mode`, `--conf-user-email`, `--conf-api-token`, `--conf-bearer-token`, `--api-version`, `--headers`
2. **Umgebungsvariablen** — `CONFLUENCE__*`
3. **Standardwerte** — Eingebaute Defaults

---

## Umgebungsvariablen

| Variable | Beschreibung | Standard |
|---|---|---|
| `CONFLUENCE__BASEURL` | Confluence Cloud URL (z.B. `https://yoursite.atlassian.net`) | — (erforderlich) |
| `CONFLUENCE__AUTHMODE` | Authentifizierungsmodus: `Basic` oder `Bearer` | `Basic` |
| `CONFLUENCE__USEREMAIL` | Atlassian-Account-E-Mail (Basic Auth) | — |
| `CONFLUENCE__APITOKEN` | Atlassian API-Token (Basic Auth) | — |
| `CONFLUENCE__BEARERTOKEN` | OAuth 2.0 Access Token (Bearer Auth) | — |
| `CONFLUENCE__OPTIMIZEIMAGES` | Bilder vor Upload skalieren | `true` |
| `CONFLUENCE__MAXIMAGEWIDTH` | Maximale Bildbreite (px) | `1280` |
| `CONFLUENCE__APIPATH` | API-Pfad-Prefix (`/wiki` für Cloud, `""` für Data Center) | `/wiki` |
| `CONFLUENCE__APIVERSION` | REST-API-Version (`v1` oder `v2`) | `v2` |

---

## CLI-Credential-Flags

Alle Zugangsdaten-Felder können über CLI-Flags überschrieben werden:

| Flag | Überschreibt | Beschreibung |
|---|---|---|
| `--conf-base-url` | `CONFLUENCE__BASEURL` | Confluence Cloud Base-URL |
| `--conf-auth-mode` | `CONFLUENCE__AUTHMODE` | Authentifizierungsmodus: `Basic` oder `Bearer` |
| `--conf-user-email` | `CONFLUENCE__USEREMAIL` | Benutzer-E-Mail (Basic Auth) |
| `--conf-api-token` | `CONFLUENCE__APITOKEN` | API-Token (Basic Auth) |
| `--conf-bearer-token` | `CONFLUENCE__BEARERTOKEN` | Bearer-Token (OAuth 2.0) |

---

## Namenskonvention

Umgebungsvariablen verwenden `__` (doppelter Unterstrich) als Trennzeichen:

```
CONFLUENCE__BASEURL  →  Confluence:BaseUrl
CONFLUENCE__APITOKEN →  Confluence:ApiToken
```

---

## Optional: `.env`-Datei

Für lokalen Komfort während der Entwicklung können Sie Variablen in einer `.env`-Datei speichern und diese manuell laden:

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
    Committen Sie niemals `.env`-Dateien in die Versionsverwaltung.

!!! note
    Das Tool lädt `.env`-Dateien **nicht** automatisch. Laden Sie die Datei selbst oder verwenden Sie Umgebungsvariablen / CLI-Flags direkt.
