# Docker Deployment

ConfluenceSynkMD now uses a **local Mermaid CLI (`mmdc`) runtime inside the same container** (md2conf-like).

This means:

- Mermaid rendering works **without** mounting `/var/run/docker.sock`.
- The image includes Chromium + fonts + Mermaid CLI dependencies.
- Docker Compose stays the recommended way to run the stack.

---

## Security Model

!!! success "Default mode is safer"
    The default Docker setup does not require host Docker daemon access.

!!! warning "Legacy fallback still exists"
    If local `mmdc` is unavailable, ConfluenceSynkMD can still fall back to Docker-based Mermaid rendering.
    That fallback requires `/var/run/docker.sock` and should be treated as privileged.

If your baseline is strict, keep local `mmdc` enabled and avoid docker socket mounts entirely.

---

## Run with Docker Compose (Recommended)

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
2. **Runtime stage** — .NET runtime + local Mermaid rendering dependencies

---

## Run

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
      --conf-space YOUR_SPACE_KEY \
      --conf-parent-id YOUR_PAGE_ID

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
      --conf-space YOUR_SPACE_KEY \
      --conf-parent-id YOUR_PAGE_ID
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
      --conf-space YOUR_SPACE_KEY `
      --conf-parent-id YOUR_PAGE_ID

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
      --conf-space YOUR_SPACE_KEY `
      --conf-parent-id YOUR_PAGE_ID
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
      --conf-space YOUR_SPACE_KEY ^
      --conf-parent-id YOUR_PAGE_ID

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
      --conf-space YOUR_SPACE_KEY ^
      --conf-parent-id YOUR_PAGE_ID
    ```

---

## Mount Strategies

| Mount | Use case |
|---|---|
| `-v $(pwd)/docs:/workspace/docs:ro` | Preferred for upload (least privilege) |
| `-v $(pwd)/docs:/workspace/docs:ro` + additional mounts | For assets outside docs |
| `-v $(pwd):/workspace` | Full workspace mount fallback |

!!! tip "Paths with spaces"
    Quote mount paths when they contain spaces, e.g. PowerShell: `-v "${PWD}/my docs:/workspace/docs:ro"`.

---

## Legacy Docker Fallback (Optional)

Only use this if local `mmdc` is unavailable.

!!! danger "Privileged mode"
    Mounting `/var/run/docker.sock` is effectively host-root-equivalent.

Required variables for fallback mode:

- `MERMAID_DOCKER_IMAGE`
- `MERMAID_DOCKER_VOLUME`
- optionally `MERMAID_USE_PUPPETEER_CONFIG=true`

If you do not want this mode, keep `mmdc` available and avoid docker socket mounts.
