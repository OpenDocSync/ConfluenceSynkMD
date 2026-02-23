# Docker-Deployment

ConfluenceSynkMD verwendet jetzt standardmäßig eine **lokale Mermaid-CLI (`mmdc`) im selben Container** (md2conf-ähnlich).

Das bedeutet:

- Mermaid-Rendering funktioniert **ohne** Mount von `/var/run/docker.sock`.
- Das Image enthält Chromium + Fonts + Mermaid-CLI-Abhängigkeiten.
- Docker Compose bleibt der empfohlene Betriebsweg.

---

## Sicherheitsmodell

!!! success "Sichererer Standardmodus"
    Das Standard-Setup benötigt keinen Zugriff auf den Docker-Daemon des Hosts.

!!! warning "Legacy-Fallback ist weiterhin vorhanden"
    Wenn lokales `mmdc` nicht verfügbar ist, kann ConfluenceSynkMD weiterhin auf Docker-basiertes Mermaid-Rendering zurückfallen.
    Dieser Fallback benötigt `/var/run/docker.sock` und ist als privilegiert zu behandeln.

Wenn Ihre Sicherheitsvorgaben streng sind, lokales `mmdc` aktiv lassen und keine docker.sock-Mounts verwenden.

---

## Ausführen mit Docker Compose (Empfohlen)

```yaml
version: '3.8'

services:
  confluencesynk:
    image: confluencesynkmd
    build:
      context: .
      dockerfile: Dockerfile
    environment:
      - CONFLUENCE__BASEURL=...
      - CONFLUENCE__AUTHMODE=Basic
      - CONFLUENCE__USEREMAIL=...
      - CONFLUENCE__APITOKEN=...
      - MERMAID_MMDC_COMMAND=mmdc
```

```bash
docker compose up
```

---

## Image bauen

=== "Bash"

    ```bash
    docker build -t confluencesynkmd .
    ```

=== "PowerShell"

    ```powershell
    docker build -t confluencesynkmd .
    ```

=== "CMD"

    ```cmd
    docker build -t confluencesynkmd .
    ```

Das Dockerfile nutzt einen **Multi-Stage-Build**:

1. **Build-Stage** — .NET SDK kompiliert die Anwendung
2. **Runtime-Stage** — .NET Runtime + lokale Mermaid-Rendering-Abhängigkeiten

---

## Ausführen

=== "Bash"

    ```bash
    # Upload
    docker run --rm -it \
      -e CONFLUENCE__BASEURL \
      -e CONFLUENCE__AUTHMODE \
      -e CONFLUENCE__USEREMAIL \
      -e CONFLUENCE__APITOKEN \
      -e MERMAID_MMDC_COMMAND=mmdc \
      -v $(pwd)/docs:/workspace/docs:ro \
      confluencesynkmd \
      --mode Upload \
      --path /workspace/docs \
      --conf-space IHR_SPACE_KEY \
      --conf-parent-id IHRE_PAGE_ID

    # Download
    docker run --rm -it \
      -e CONFLUENCE__BASEURL \
      -e CONFLUENCE__AUTHMODE \
      -e CONFLUENCE__USEREMAIL \
      -e CONFLUENCE__APITOKEN \
      -e MERMAID_MMDC_COMMAND=mmdc \
      -v $(pwd)/output:/workspace/output \
      confluencesynkmd \
      --mode Download \
      --path /workspace/output \
      --conf-space IHR_SPACE_KEY \
      --conf-parent-id IHRE_PAGE_ID
    ```

=== "PowerShell"

    ```powershell
    # Upload
    docker run --rm -it `
      -e CONFLUENCE__BASEURL `
      -e CONFLUENCE__AUTHMODE `
      -e CONFLUENCE__USEREMAIL `
      -e CONFLUENCE__APITOKEN `
      -e MERMAID_MMDC_COMMAND=mmdc `
      -v ${PWD}/docs:/workspace/docs:ro `
      confluencesynkmd `
      --mode Upload `
      --path /workspace/docs `
      --conf-space IHR_SPACE_KEY `
      --conf-parent-id IHRE_PAGE_ID

    # Download
    docker run --rm -it `
      -e CONFLUENCE__BASEURL `
      -e CONFLUENCE__AUTHMODE `
      -e CONFLUENCE__USEREMAIL `
      -e CONFLUENCE__APITOKEN `
      -e MERMAID_MMDC_COMMAND=mmdc `
      -v ${PWD}/output:/workspace/output `
      confluencesynkmd `
      --mode Download `
      --path /workspace/output `
      --conf-space IHR_SPACE_KEY `
      --conf-parent-id IHRE_PAGE_ID
    ```

=== "CMD"

    ```cmd
    REM Upload
    docker run --rm -it ^
      -e CONFLUENCE__BASEURL ^
      -e CONFLUENCE__AUTHMODE ^
      -e CONFLUENCE__USEREMAIL ^
      -e CONFLUENCE__APITOKEN ^
      -e MERMAID_MMDC_COMMAND=mmdc ^
      -v %cd%/docs:/workspace/docs:ro ^
      confluencesynkmd ^
      --mode Upload ^
      --path /workspace/docs ^
      --conf-space IHR_SPACE_KEY ^
      --conf-parent-id IHRE_PAGE_ID

    REM Download
    docker run --rm -it ^
      -e CONFLUENCE__BASEURL ^
      -e CONFLUENCE__AUTHMODE ^
      -e CONFLUENCE__USEREMAIL ^
      -e CONFLUENCE__APITOKEN ^
      -e MERMAID_MMDC_COMMAND=mmdc ^
      -v %cd%/output:/workspace/output ^
      confluencesynkmd ^
      --mode Download ^
      --path /workspace/output ^
      --conf-space IHR_SPACE_KEY ^
      --conf-parent-id IHRE_PAGE_ID
    ```

---

## Mount-Strategien

| Mount | Anwendungsfall |
|---|---|
| `-v $(pwd)/docs:/workspace/docs:ro` | Bevorzugt für Upload (Minimalrechte) |
| `-v $(pwd)/docs:/workspace/docs:ro` + zusätzliche Mounts | Für Assets außerhalb von docs |
| `-v $(pwd):/workspace` | Fallback mit vollständigem Workspace-Mount |

!!! tip "Pfade mit Leerzeichen"
    Mount-Pfade mit Leerzeichen korrekt quotieren, z. B. PowerShell: `-v "${PWD}/my docs:/workspace/docs:ro"`.

---

## Legacy-Docker-Fallback (Optional)

Nur verwenden, wenn lokales `mmdc` nicht verfügbar ist.

!!! danger "Privilegierter Modus"
    Das Mounten von `/var/run/docker.sock` ist effektiv host-root-äquivalent.

Erforderliche Variablen im Fallback-Modus:

- `MERMAID_DOCKER_IMAGE`
- `MERMAID_DOCKER_VOLUME`
- optional `MERMAID_USE_PUPPETEER_CONFIG=true`

Wenn Sie diesen Modus vermeiden möchten, `mmdc` verfügbar halten und keine docker.sock-Mounts verwenden.
