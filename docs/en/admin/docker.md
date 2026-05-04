# Docker Deployment

The Docker image comes pre-packaged with .NET, Node.js, and mermaid-cli — providing a consistent, portable runtime environment.

---

## Build the Image

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

The Dockerfile uses a **multi-stage build**:

1. **Build stage** — .NET SDK compiles the application
2. **Runtime stage** — Slim image with .NET runtime, Node.js 22, and mermaid-cli

---

## Run

=== "Bash"

    ```bash
    # Upload: inject credentials as environment variables
    docker run --rm -it \
      -e CONFLUENCE__BASEURL \
      -e CONFLUENCE__AUTHMODE \
      -e CONFLUENCE__USEREMAIL \
      -e CONFLUENCE__APITOKEN \
      -v $(pwd)/docs:/workspace/docs:ro \
      confluencesynkmd \
      upload \
      --path /workspace/docs \
      --conf-space YOUR_SPACE_KEY \
      --conf-parent-id YOUR_PAGE_ID

    # Download: separate writable output mount
    docker run --rm -it \
      -e CONFLUENCE__BASEURL \
      -e CONFLUENCE__AUTHMODE \
      -e CONFLUENCE__USEREMAIL \
      -e CONFLUENCE__APITOKEN \
      -v $(pwd)/output:/workspace/output \
      confluencesynkmd \
      download \
      --path /workspace/output \
      --conf-space YOUR_SPACE_KEY \
      --conf-parent-id YOUR_PAGE_ID

    # Upload (CI/CD): inject secrets as runner environment variables
    docker run --rm -it \
      -e CONFLUENCE__BASEURL \
      -e CONFLUENCE__AUTHMODE \
      -e CONFLUENCE__USEREMAIL \
      -e CONFLUENCE__APITOKEN \
      -v $(pwd)/docs:/workspace/docs:ro \
      confluencesynkmd \
      upload \
      --path /workspace/docs \
      --conf-space YOUR_SPACE_KEY \
      --conf-parent-id YOUR_PAGE_ID

    # Download (CI/CD): writable output mount
    docker run --rm -it \
      -e CONFLUENCE__BASEURL \
      -e CONFLUENCE__AUTHMODE \
      -e CONFLUENCE__USEREMAIL \
      -e CONFLUENCE__APITOKEN \
      -v $(pwd)/output:/workspace/output \
      confluencesynkmd \
      download \
      --path /workspace/output \
      --conf-space YOUR_SPACE_KEY \
      --conf-parent-id YOUR_PAGE_ID
    ```

=== "PowerShell"

    ```powershell
    # Upload: inject credentials as environment variables
    docker run --rm -it `
      -e CONFLUENCE__BASEURL `
      -e CONFLUENCE__AUTHMODE `
      -e CONFLUENCE__USEREMAIL `
      -e CONFLUENCE__APITOKEN `
      -v ${PWD}/docs:/workspace/docs:ro `
      confluencesynkmd `
      upload `
      --path /workspace/docs `
      --conf-space YOUR_SPACE_KEY `
      --conf-parent-id YOUR_PAGE_ID

    # Download: separate writable output mount
    docker run --rm -it `
      -e CONFLUENCE__BASEURL `
      -e CONFLUENCE__AUTHMODE `
      -e CONFLUENCE__USEREMAIL `
      -e CONFLUENCE__APITOKEN `
      -v ${PWD}/output:/workspace/output `
      confluencesynkmd `
      download `
      --path /workspace/output `
      --conf-space YOUR_SPACE_KEY `
      --conf-parent-id YOUR_PAGE_ID

    # Upload (CI/CD): inject secrets as runner environment variables
    docker run --rm -it `
      -e CONFLUENCE__BASEURL `
      -e CONFLUENCE__AUTHMODE `
      -e CONFLUENCE__USEREMAIL `
      -e CONFLUENCE__APITOKEN `
      -v ${PWD}/docs:/workspace/docs:ro `
      confluencesynkmd `
      upload `
      --path /workspace/docs `
      --conf-space YOUR_SPACE_KEY `
      --conf-parent-id YOUR_PAGE_ID

    # Download (CI/CD): writable output mount
    docker run --rm -it `
      -e CONFLUENCE__BASEURL `
      -e CONFLUENCE__AUTHMODE `
      -e CONFLUENCE__USEREMAIL `
      -e CONFLUENCE__APITOKEN `
      -v ${PWD}/output:/workspace/output `
      confluencesynkmd `
      download `
      --path /workspace/output `
      --conf-space YOUR_SPACE_KEY `
      --conf-parent-id YOUR_PAGE_ID
    ```

=== "CMD"

    ```cmd
    REM Upload: credentials via environment variables
    docker run --rm -it ^
      -e CONFLUENCE__BASEURL ^
      -e CONFLUENCE__AUTHMODE ^
      -e CONFLUENCE__USEREMAIL ^
      -e CONFLUENCE__APITOKEN ^
      -v %cd%/docs:/workspace/docs:ro ^
      confluencesynkmd ^
      upload ^
      --path /workspace/docs ^
      --conf-space YOUR_SPACE_KEY ^
      --conf-parent-id YOUR_PAGE_ID

    REM Download: separate writable output mount
    docker run --rm -it ^
      -e CONFLUENCE__BASEURL ^
      -e CONFLUENCE__AUTHMODE ^
      -e CONFLUENCE__USEREMAIL ^
      -e CONFLUENCE__APITOKEN ^
      -v %cd%/output:/workspace/output ^
      confluencesynkmd ^
      download ^
      --path /workspace/output ^
      --conf-space YOUR_SPACE_KEY ^
      --conf-parent-id YOUR_PAGE_ID

    REM Upload (CI/CD): inject secrets as runner environment variables
    docker run --rm -it ^
      -e CONFLUENCE__BASEURL ^
      -e CONFLUENCE__AUTHMODE ^
      -e CONFLUENCE__USEREMAIL ^
      -e CONFLUENCE__APITOKEN ^
      -v %cd%/docs:/workspace/docs:ro ^
      confluencesynkmd ^
      upload ^
      --path /workspace/docs ^
      --conf-space YOUR_SPACE_KEY ^
      --conf-parent-id YOUR_PAGE_ID

    REM Download (CI/CD): writable output mount
    docker run --rm -it ^
      -e CONFLUENCE__BASEURL ^
      -e CONFLUENCE__AUTHMODE ^
      -e CONFLUENCE__USEREMAIL ^
      -e CONFLUENCE__APITOKEN ^
      -v %cd%/output:/workspace/output ^
      confluencesynkmd ^
      download ^
      --path /workspace/output ^
      --conf-space YOUR_SPACE_KEY ^
      --conf-parent-id YOUR_PAGE_ID
    ```

!!! warning "Working directory"
    If you mount `${PWD}` / `$(pwd)`, make sure you run from the intended project directory. Prefer mounting only required paths.

---

## Mount Strategies & Working Directory

| Mount | Use case |
|---|---|
| `-v $(pwd)/docs:/workspace/docs:ro` | Preferred for upload: least-privilege docs-only mount |
| `-v $(pwd)/docs:/workspace/docs:ro` + additional mounts (e.g. `-v $(pwd)/img:/workspace/img:ro`) | Use when Markdown references assets outside the docs folder |
| `-v $(pwd):/workspace` | Full workspace mount (fallback), only if many cross-folder references are required |

!!! tip "Paths with spaces"
  Quote mount paths when they contain spaces, e.g. PowerShell: `-v "${PWD}/my docs:/workspace/docs:ro"`.

!!! note "Validation"
  The PowerShell mount syntax with space-containing paths was validated against the Docker image. Validate Bash syntax in your target CI runner.

!!! warning "Security"
  For CI/CD, prefer platform secret stores (GitHub/GitLab protected variables) and inject them at runtime.

---

## Minimal CI Snippets

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

          - name: Build Docker image
            run: docker build -t confluencesynkmd .

          - name: Run upload
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
                confluencesynkmd \
                upload \
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
        - docker build -t confluencesynkmd .
        - |
          docker run --rm \
            -e CONFLUENCE__BASEURL \
            -e CONFLUENCE__AUTHMODE \
            -e CONFLUENCE__USEREMAIL \
            -e CONFLUENCE__APITOKEN \
            -v "$CI_PROJECT_DIR/docs:/workspace/docs:ro" \
            confluencesynkmd \
            upload \
            --path /workspace/docs \
            --conf-space "$CONFLUENCE_SPACE" \
            --conf-parent-id "$CONFLUENCE_PARENT_ID"
      variables:
        CONFLUENCE__AUTHMODE: "Basic"
      # Set as masked/protected CI variables:
      # CONFLUENCE__BASEURL, CONFLUENCE__USEREMAIL, CONFLUENCE__APITOKEN,
      # CONFLUENCE_SPACE, CONFLUENCE_PARENT_ID
    ```

---

## What ships in the default image

The published `ghcr.io/opendocsync/confluencesynkmd:0.1` image preinstalls every renderer claimed by the project:

| Renderer | Backing engine |
|---|---|
| Mermaid | `@mermaid-js/mermaid-cli` + Chromium (Puppeteer) |
| Draw.io | drawio-desktop (Electron, headless via Xvfb-once started by `entrypoint.sh`) |
| PlantUML | `default-jre-headless` + the `plantuml` package |
| LaTeX | TeX Live (`texlive-latex-base` + `texlive-latex-extra` + fonts-recommended) + Ghostscript directly (no ImageMagick) |

Run `docker run --rm ghcr.io/opendocsync/confluencesynkmd:0.1 doctor --renderers-only` to verify all four renderers are healthy in the image you pulled.

## Forward-only fix policy

If a published `:0.1.x` release contains a critical bug, the project ships a `:0.1.x+1` patch and updates the moving `:0.1` tag. Pinned digest pulls (`docker pull ghcr.io/opendocsync/confluencesynkmd@sha256:...`) are never deleted — users who pinned by digest pinned for a reason.

For security-grade incidents only (leaked credentials, actively dangerous regression), the `release-yank.yml` workflow deletes a tag (not a digest) from GHCR. See the workflow file for the decision criteria.
