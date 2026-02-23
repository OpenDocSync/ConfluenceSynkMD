# Umgebungsvariablen

Vollständige Referenz aller von ConfluenceSynkMD verwendeten Umgebungsvariablen.

!!! tip "CLI-Override"
    Alle Credential-Variablen können auch über CLI-Flags gesetzt werden (z.B. `--conf-base-url`). CLI-Flags haben Vorrang vor Umgebungsvariablen. Siehe [CLI-Referenz](cli.md) für Details.

---

## Confluence-Verbindung

| Variable | Erforderlich | Standard | Beschreibung |
|---|---|---|---|
| `CONFLUENCE__BASEURL` | ✅ | — | Confluence Cloud URL (z.B. `https://yoursite.atlassian.net`) |
| `CONFLUENCE__AUTHMODE` | | `Basic` | Authentifizierungsmodus: `Basic` oder `Bearer` |

---

## Basic Auth

| Variable | Erforderlich | Standard | Beschreibung |
|---|---|---|---|
| `CONFLUENCE__USEREMAIL` | Bei `Basic` | — | Atlassian-Account-E-Mail |
| `CONFLUENCE__APITOKEN` | Bei `Basic` | — | Atlassian API-Token |

---

## Bearer Auth

| Variable | Erforderlich | Standard | Beschreibung |
|---|---|---|---|
| `CONFLUENCE__BEARERTOKEN` | Bei `Bearer` | — | OAuth 2.0 Access Token |

---

## Bildverarbeitung

| Variable | Standard | Beschreibung |
|---|---|---|
| `CONFLUENCE__OPTIMIZEIMAGES` | `true` | Bilder vor Upload skalieren |
| `CONFLUENCE__MAXIMAGEWIDTH` | `1280` | Maximale Bildbreite in Pixeln |

---

## API-Konfiguration

| Variable | Standard | Beschreibung |
|---|---|---|
| `CONFLUENCE__APIPATH` | `/wiki` | API-Pfad-Prefix |
| `CONFLUENCE__APIVERSION` | `v2` | REST API Version |

---

## Mermaid-Rendering

| Variable | Erforderlich | Standard | Beschreibung |
|---|---|---|---|
| `MERMAID_MMDC_COMMAND` | | auto (`mmdc` / `mmdc.cmd`) | Optionaler Pfad/Name für lokale Mermaid CLI. Wenn verfügbar, wird lokales Rendering gegenüber Docker bevorzugt. |
| `MERMAID_DOCKER_IMAGE` | Nur Docker-Fallback | `ghcr.io/mermaid-js/mermaid-cli/mermaid-cli` | Docker-Image für den Fallback, wenn lokales `mmdc` nicht verfügbar ist. |
| `MERMAID_DOCKER_VOLUME` | Bei Docker-Fallback im Container | — | Host-sichtbarer Shared-Temp-Pfad/Volume für Sibling-Container-Rendering. |
| `MERMAID_USE_PUPPETEER_CONFIG` | | `false` | Wenn `true`, wird im Docker-Fallback eine erzeugte Puppeteer-Konfiguration übergeben. |

!!! note "Sicherheitsempfehlung"
    Bevorzuge lokales `mmdc`-Rendering, um das Mounten von `/var/run/docker.sock` zu vermeiden.
    Falls lokales `mmdc` nicht verfügbar ist, kann Mermaid-Rendering mit `--no-render-mermaid` deaktiviert werden.

---

## Namenskonvention

Umgebungsvariablen verwenden `__` (doppelter Unterstrich) als Trennzeichen:

| Umgebungsvariable | .NET-Konfigurationspfad |
|---|---|
| `CONFLUENCE__BASEURL` | `Confluence:BaseUrl` |
| `CONFLUENCE__APITOKEN` | `Confluence:ApiToken` |
| `CONFLUENCE__MAXIMAGEWIDTH` | `Confluence:MaxImageWidth` |
