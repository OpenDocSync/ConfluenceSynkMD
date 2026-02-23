# Docker Deployment

The Docker image comes pre-packaged with .NET and the Docker CLI. It uses a **sibling-container architecture** to spawn an official `mermaid-cli` Docker container on demand for rendering Mermaid diagrams.

For full features including diagram rendering, using **Docker Compose** is recommended as it automatically mounts the Docker socket and shares volumes.

!!! danger "Security: Docker socket is host-level privileged"
    Mounting `/var/run/docker.sock` gives this container access to the host Docker daemon, which is effectively root-equivalent on the host.
    Treat this deployment as privileged and only use it in trusted environments.

!!! tip "Prefer non-root runtime with group-based socket access"
    Instead of running the app process as root, run as a fixed user and add the host docker-socket GID with `--group-add`.
    This is the recommended default when docker socket access is required.

## Non-Root Container with Docker Socket Access

=== "Bash"

    ```bash
    # Resolve host docker socket group id
    DOCKER_GID=$(stat -c '%g' /var/run/docker.sock)

    docker run --rm -it \
      --user 1001:1001 \
      --group-add ${DOCKER_GID} \
      -v /var/run/docker.sock:/var/run/docker.sock \
      -v $(pwd)/docs:/workspace/docs:ro \
      -v $(pwd)/mermaid_tmp:/app/mermaid_temp \
      -e TMPDIR=/app/mermaid_temp \
      -e MERMAID_DOCKER_VOLUME=$(pwd)/mermaid_tmp \
      confluencesynkmd \
      --mode Upload \
      --path /workspace/docs \
      --conf-space YOUR_SPACE_KEY \
      --conf-parent-id YOUR_PAGE_ID
    ```

=== "PowerShell"

    ```powershell
    # Linux host with PowerShell shell
    $DOCKER_GID = (stat -c '%g' /var/run/docker.sock)

    docker run --rm -it `
      --user 1001:1001 `
      --group-add $DOCKER_GID `
      -v /var/run/docker.sock:/var/run/docker.sock `
      -v ${PWD}/docs:/workspace/docs:ro `
      -v ${PWD}/mermaid_tmp:/app/mermaid_temp `
      -e TMPDIR=/app/mermaid_temp `
      -e MERMAID_DOCKER_VOLUME=${PWD}/mermaid_tmp `
      confluencesynkmd `
      --mode Upload `
      --path /workspace/docs `
      --conf-space YOUR_SPACE_KEY `
      --conf-parent-id YOUR_PAGE_ID
    ```

=== "Docker Compose"

    ```bash
    export DOCKER_GID=$(stat -c '%g' /var/run/docker.sock)
    docker compose up
    ```

    ```yaml
    services:
      confluencesynk:
        user: "1001:1001"
        group_add:
          - "${DOCKER_GID}"
        volumes:
          - /var/run/docker.sock:/var/run/docker.sock
    ```

If your environment disallows docker socket mounts, disable Mermaid rendering (`--no-render-mermaid`) and do not mount `/var/run/docker.sock`.

---

## Run with Docker Compose (Recommended)

```yaml
# docker-compose.yml example
version: '3.8'

services:
  confluencesynk:
    image: confluencesynkmd
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock
      - ${PWD}/docs:/workspace/docs:ro
      - mermaid_data:/app/mermaid_temp
    environment:
      - CONFLUENCE__BASEURL=...
      - CONFLUENCE__AUTHMODE=Basic
      - CONFLUENCE__USEREMAIL=...
      - CONFLUENCE__APITOKEN=...
      - TMPDIR=/app/mermaid_temp
      - MERMAID_DOCKER_VOLUME=mermaid_data

volumes:
  mermaid_data:
```

```bash
docker compose up
```

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
2. **Runtime stage** — Slim image with .NET runtime and Docker CLI

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
      -v /var/run/docker.sock:/var/run/docker.sock \
      -v $(pwd)/mermaid_tmp:/app/mermaid_temp \
      -e TMPDIR=/app/mermaid_temp \
      -e MERMAID_DOCKER_VOLUME=$(pwd)/mermaid_tmp \
      confluencesynkmd \
      --mode Upload \
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
      -v /var/run/docker.sock:/var/run/docker.sock \
      -v $(pwd)/mermaid_tmp:/app/mermaid_temp \
      -e TMPDIR=/app/mermaid_temp \
      -e MERMAID_DOCKER_VOLUME=$(pwd)/mermaid_tmp \
      confluencesynkmd \
      --mode Download \
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
      -v /var/run/docker.sock:/var/run/docker.sock \
      -v $(pwd)/mermaid_tmp:/app/mermaid_temp \
      -e TMPDIR=/app/mermaid_temp \
      -e MERMAID_DOCKER_VOLUME=$(pwd)/mermaid_tmp \
      confluencesynkmd \
      --mode Upload \
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
      -v /var/run/docker.sock:/var/run/docker.sock \
      -v $(pwd)/mermaid_tmp:/app/mermaid_temp \
      -e TMPDIR=/app/mermaid_temp \
      -e MERMAID_DOCKER_VOLUME=$(pwd)/mermaid_tmp \
      confluencesynkmd \
      --mode Download \
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
      -v /var/run/docker.sock:/var/run/docker.sock `
      -v ${PWD}/mermaid_tmp:/app/mermaid_temp `
      -e TMPDIR=/app/mermaid_temp `
      -e MERMAID_DOCKER_VOLUME=${PWD}/mermaid_tmp `
      confluencesynkmd `
      --mode Upload `
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
      -v /var/run/docker.sock:/var/run/docker.sock `
      -v ${PWD}/mermaid_tmp:/app/mermaid_temp `
      -e TMPDIR=/app/mermaid_temp `
      -e MERMAID_DOCKER_VOLUME=${PWD}/mermaid_tmp `
      confluencesynkmd `
      --mode Download `
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
      -v /var/run/docker.sock:/var/run/docker.sock `
      -v ${PWD}/mermaid_tmp:/app/mermaid_temp `
      -e TMPDIR=/app/mermaid_temp `
      -e MERMAID_DOCKER_VOLUME=${PWD}/mermaid_tmp `
      confluencesynkmd `
      --mode Upload `
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
      -v /var/run/docker.sock:/var/run/docker.sock `
      -v ${PWD}/mermaid_tmp:/app/mermaid_temp `
      -e TMPDIR=/app/mermaid_temp `
      -e MERMAID_DOCKER_VOLUME=${PWD}/mermaid_tmp `
      confluencesynkmd `
      --mode Download `
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
      -v /var/run/docker.sock:/var/run/docker.sock ^
      -v %cd%/mermaid_tmp:/app/mermaid_temp ^
      -e TMPDIR=/app/mermaid_temp ^
      -e MERMAID_DOCKER_VOLUME=%cd%/mermaid_tmp ^
      confluencesynkmd ^
      --mode Upload ^
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
      -v /var/run/docker.sock:/var/run/docker.sock ^
      -v %cd%/mermaid_tmp:/app/mermaid_temp ^
      -e TMPDIR=/app/mermaid_temp ^
      -e MERMAID_DOCKER_VOLUME=%cd%/mermaid_tmp ^
      confluencesynkmd ^
      --mode Download ^
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
      -v /var/run/docker.sock:/var/run/docker.sock ^
      -v %cd%/mermaid_tmp:/app/mermaid_temp ^
      -e TMPDIR=/app/mermaid_temp ^
      -e MERMAID_DOCKER_VOLUME=%cd%/mermaid_tmp ^
      confluencesynkmd ^
      --mode Upload ^
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
      -v /var/run/docker.sock:/var/run/docker.sock ^
      -v %cd%/mermaid_tmp:/app/mermaid_temp ^
      -e TMPDIR=/app/mermaid_temp ^
      -e MERMAID_DOCKER_VOLUME=%cd%/mermaid_tmp ^
      confluencesynkmd ^
      --mode Download ^
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
                -v /var/run/docker.sock:/var/run/docker.sock \
                -v "$PWD/mermaid_tmp:/app/mermaid_temp" \
                -e TMPDIR=/app/mermaid_temp \
                -e MERMAID_DOCKER_VOLUME="$PWD/mermaid_tmp" \
                confluencesynkmd \
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
        - docker build -t confluencesynkmd .
        - |
          docker run --rm \
            -e CONFLUENCE__BASEURL \
            -e CONFLUENCE__AUTHMODE \
            -e CONFLUENCE__USEREMAIL \
            -e CONFLUENCE__APITOKEN \
            -v "$CI_PROJECT_DIR/docs:/workspace/docs:ro" \
            -v /var/run/docker.sock:/var/run/docker.sock \
            -v "$CI_PROJECT_DIR/mermaid_tmp:/app/mermaid_temp" \
            -e TMPDIR=/app/mermaid_temp \
            -e MERMAID_DOCKER_VOLUME="$CI_PROJECT_DIR/mermaid_tmp" \
            confluencesynkmd \
            --mode Upload \
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

## Extending the Image

To add additional diagram renderers, extend the Dockerfile:

```dockerfile
FROM confluencesynkmd AS base

# Add PlantUML
RUN apt-get update && apt-get install -y plantuml

# Add Draw.io export
RUN npm install -g drawio-export
```
