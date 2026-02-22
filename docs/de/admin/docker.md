# Docker-Deployment

Das Docker-Image enthält .NET, Node.js und mermaid-cli — eine konsistente, portable Laufzeitumgebung.

---

## Image bauen

=== "Bash"

    ```bash
    docker build -t confluentsynkmd .
    ```

=== "PowerShell"

    ```powershell
    docker build -t confluentsynkmd .
    ```

=== "CMD"

    ```cmd
    docker build -t confluentsynkmd .
    ```

---

## Ausführen

=== "Bash"

    ```bash
    # Upload (lokale Entwicklung, optional): mit --env-file
    # Befehl im Verzeichnis mit .env ausführen
    docker run --rm -it \
      --env-file ./.env \
      -v $(pwd)/docs:/workspace/docs:ro \
      confluentsynkmd \
      --mode Upload \
      --path /workspace/docs \
      --conf-space IHR_SPACE_KEY \
      --conf-parent-id IHRE_PAGE_ID

    # Download (lokale Entwicklung, optional): separater schreibbarer Output-Mount
    docker run --rm -it \
      --env-file ./.env \
      -v $(pwd)/output:/workspace/output \
      confluentsynkmd \
      --mode Download \
      --path /workspace/output \
      --conf-space IHR_SPACE_KEY \
      --conf-parent-id IHRE_PAGE_ID

    # Upload (CI/CD empfohlen): Secrets als Runner-Umgebungsvariablen injizieren
    docker run --rm -it \
      -e CONFLUENCE__BASEURL \
      -e CONFLUENCE__AUTHMODE \
      -e CONFLUENCE__USEREMAIL \
      -e CONFLUENCE__APITOKEN \
      -v $(pwd)/docs:/workspace/docs:ro \
      confluentsynkmd \
      --mode Upload \
      --path /workspace/docs \
      --conf-space IHR_SPACE_KEY \
      --conf-parent-id IHRE_PAGE_ID

    # Download (CI/CD empfohlen): schreibbarer Output-Mount
    docker run --rm -it \
      -e CONFLUENCE__BASEURL \
      -e CONFLUENCE__AUTHMODE \
      -e CONFLUENCE__USEREMAIL \
      -e CONFLUENCE__APITOKEN \
      -v $(pwd)/output:/workspace/output \
      confluentsynkmd \
      --mode Download \
      --path /workspace/output \
      --conf-space IHR_SPACE_KEY \
      --conf-parent-id IHRE_PAGE_ID
    ```

=== "PowerShell"

    ```powershell
    # Upload (lokale Entwicklung, optional): mit --env-file
    # Befehl im Verzeichnis mit .env ausführen
    docker run --rm -it `
      --env-file ./.env `
      -v ${PWD}/docs:/workspace/docs:ro `
      confluentsynkmd `
      --mode Upload `
      --path /workspace/docs `
      --conf-space IHR_SPACE_KEY `
      --conf-parent-id IHRE_PAGE_ID

    # Download (lokale Entwicklung, optional): separater schreibbarer Output-Mount
    docker run --rm -it `
      --env-file ./.env `
      -v ${PWD}/output:/workspace/output `
      confluentsynkmd `
      --mode Download `
      --path /workspace/output `
      --conf-space IHR_SPACE_KEY `
      --conf-parent-id IHRE_PAGE_ID

    # Upload (CI/CD empfohlen): Secrets als Runner-Umgebungsvariablen injizieren
    docker run --rm -it `
      -e CONFLUENCE__BASEURL `
      -e CONFLUENCE__AUTHMODE `
      -e CONFLUENCE__USEREMAIL `
      -e CONFLUENCE__APITOKEN `
      -v ${PWD}/docs:/workspace/docs:ro `
      confluentsynkmd `
      --mode Upload `
      --path /workspace/docs `
      --conf-space IHR_SPACE_KEY `
      --conf-parent-id IHRE_PAGE_ID

    # Download (CI/CD empfohlen): schreibbarer Output-Mount
    docker run --rm -it `
      -e CONFLUENCE__BASEURL `
      -e CONFLUENCE__AUTHMODE `
      -e CONFLUENCE__USEREMAIL `
      -e CONFLUENCE__APITOKEN `
      -v ${PWD}/output:/workspace/output `
      confluentsynkmd `
      --mode Download `
      --path /workspace/output `
      --conf-space IHR_SPACE_KEY `
      --conf-parent-id IHRE_PAGE_ID
    ```

=== "CMD"

    ```cmd
    REM Upload (lokale Entwicklung, optional): mit --env-file
    REM Befehl im Verzeichnis mit .env ausführen
    docker run --rm -it ^
      --env-file ./.env ^
      -v %cd%/docs:/workspace/docs:ro ^
      confluentsynkmd ^
      --mode Upload ^
      --path /workspace/docs ^
      --conf-space IHR_SPACE_KEY ^
      --conf-parent-id IHRE_PAGE_ID

    REM Download (lokale Entwicklung, optional): separater schreibbarer Output-Mount
    docker run --rm -it ^
      --env-file ./.env ^
      -v %cd%/output:/workspace/output ^
      confluentsynkmd ^
      --mode Download ^
      --path /workspace/output ^
      --conf-space IHR_SPACE_KEY ^
      --conf-parent-id IHRE_PAGE_ID

    REM Upload (CI/CD empfohlen): Secrets als Runner-Umgebungsvariablen injizieren
    docker run --rm -it ^
      -e CONFLUENCE__BASEURL ^
      -e CONFLUENCE__AUTHMODE ^
      -e CONFLUENCE__USEREMAIL ^
      -e CONFLUENCE__APITOKEN ^
      -v %cd%/docs:/workspace/docs:ro ^
      confluentsynkmd ^
      --mode Upload ^
      --path /workspace/docs ^
      --conf-space IHR_SPACE_KEY ^
      --conf-parent-id IHRE_PAGE_ID

    REM Download (CI/CD empfohlen): schreibbarer Output-Mount
    docker run --rm -it ^
      -e CONFLUENCE__BASEURL ^
      -e CONFLUENCE__AUTHMODE ^
      -e CONFLUENCE__USEREMAIL ^
      -e CONFLUENCE__APITOKEN ^
      -v %cd%/output:/workspace/output ^
      confluentsynkmd ^
      --mode Download ^
      --path /workspace/output ^
      --conf-space IHR_SPACE_KEY ^
      --conf-parent-id IHRE_PAGE_ID
    ```

!!! warning "Arbeitsverzeichnis"
    Wenn Sie `${PWD}` / `$(pwd)` mounten, muss der Befehl im gewünschten Projektverzeichnis ausgeführt werden. Bevorzugen Sie gezielte Mounts nur für benötigte Pfade.

---

## Mount-Strategien & Arbeitsverzeichnis

| Mount | Anwendungsfall |
|---|---|
| `-v $(pwd)/docs:/workspace/docs:ro` | Empfohlen für Upload: Docs-only mit Minimalrechten |
| `-v $(pwd)/docs:/workspace/docs:ro` + zusätzliche Mounts (z. B. `-v $(pwd)/img:/workspace/img:ro`) | Wenn Markdown auf Assets außerhalb von `docs` verweist |
| `-v $(pwd):/workspace` | Vollständiger Workspace-Mount (Fallback), nur bei vielen pfadübergreifenden Referenzen |

!!! tip "Pfade mit Leerzeichen"
  Mount-Pfade mit Leerzeichen müssen korrekt quotiert werden, z. B. PowerShell: `-v "${PWD}/my docs:/workspace/docs:ro"`.

!!! note "Validierung"
  Die PowerShell-Mount-Syntax mit Pfaden inklusive Leerzeichen wurde gegen das Docker-Image geprüft. Bash-Syntax im Ziel-CI-Runner separat validieren.

!!! warning "Sicherheit"
    Für CI/CD bevorzugt Secrets des CI-Systems (GitHub/GitLab geschützte Variablen) zur Laufzeit injizieren. `--env-file` primär für lokale Entwicklung nutzen.

---

## Minimale CI-Snippets

=== "GitHub Actions"

    ```yaml
    name: Confluence Sync

    on:
      workflow_dispatch:

    jobs:
      upload:
        runs-on: ubuntu-latest
        steps:
          - uses: actions/checkout@v4

          - name: Docker Image bauen
            run: docker build -t confluentsynkmd .

          - name: Upload ausführen
            env:
              CONFLUENCE__BASEURL: ${{ secrets.CONFLUENCE__BASEURL }}
              CONFLUENCE__AUTHMODE: Basic
              CONFLUENCE__USEREMAIL: ${{ secrets.CONFLUENCE__USEREMAIL }}
              CONFLUENCE__APITOKEN: ${{ secrets.CONFLUENCE__APITOKEN }}
            run: |
              docker run --rm \
                -e CONFLUENCE__BASEURL \
                -e CONFLUENCE__AUTHMODE \
                -e CONFLUENCE__USEREMAIL \
                -e CONFLUENCE__APITOKEN \
                -v "$PWD/docs:/workspace/docs:ro" \
                confluentsynkmd \
                --mode Upload \
                --path /workspace/docs \
                --conf-space "${{ vars.CONFLUENCE_SPACE }}" \
                --conf-parent-id "${{ vars.CONFLUENCE_PARENT_ID }}"
    ```

=== "GitLab CI"

    ```yaml
    stages: [upload]

    confluence_upload:
      stage: upload
      image: docker:27
      services:
        - docker:27-dind
      variables:
        DOCKER_HOST: tcp://docker:2375
        DOCKER_TLS_CERTDIR: ""
      script:
        - docker build -t confluentsynkmd .
        - |
          docker run --rm \
            -e CONFLUENCE__BASEURL \
            -e CONFLUENCE__AUTHMODE \
            -e CONFLUENCE__USEREMAIL \
            -e CONFLUENCE__APITOKEN \
            -v "$CI_PROJECT_DIR/docs:/workspace/docs:ro" \
            confluentsynkmd \
            --mode Upload \
            --path /workspace/docs \
            --conf-space "$CONFLUENCE_SPACE" \
            --conf-parent-id "$CONFLUENCE_PARENT_ID"
      variables:
        CONFLUENCE__AUTHMODE: "Basic"
      # Als masked/protected CI variables setzen:
      # CONFLUENCE__BASEURL, CONFLUENCE__USEREMAIL, CONFLUENCE__APITOKEN,
      # CONFLUENCE_SPACE, CONFLUENCE_PARENT_ID
    ```

---

## Image erweitern

Um zusätzliche Diagramm-Renderer hinzuzufügen:

```dockerfile
FROM confluentsynkmd AS base

# PlantUML hinzufügen
RUN apt-get update && apt-get install -y plantuml

# Draw.io Export hinzufügen
RUN npm install -g drawio-export
```
