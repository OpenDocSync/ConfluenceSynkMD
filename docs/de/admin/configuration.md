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
