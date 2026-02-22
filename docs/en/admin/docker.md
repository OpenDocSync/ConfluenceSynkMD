# Docker Deployment

The Docker image comes pre-packaged with .NET, Node.js, and mermaid-cli — providing a consistent, portable runtime environment.

---

## Build the Image

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

The Dockerfile uses a **multi-stage build**:

1. **Build stage** — .NET SDK compiles the application
2. **Runtime stage** — Slim image with .NET runtime, Node.js 22, and mermaid-cli

---

## Run

=== "Bash"

    ```bash
    # Upload (local development, optional): use --env-file
    # Run from the directory that contains .env
    docker run --rm -it \
      --env-file ./.env \
      -v $(pwd)/docs:/workspace/docs:ro \
      confluentsynkmd \
      --mode Upload \
      --path /workspace/docs \
      --conf-space YOUR_SPACE_KEY \
      --conf-parent-id YOUR_PAGE_ID

    # Download (local development, optional): separate writable output mount
    docker run --rm -it \
      --env-file ./.env \
      -v $(pwd)/output:/workspace/output \
      confluentsynkmd \
      --mode Download \
      --path /workspace/output \
      --conf-space YOUR_SPACE_KEY \
      --conf-parent-id YOUR_PAGE_ID

    # Upload (CI/CD recommended): inject secrets as runner environment variables
    docker run --rm -it \
      -e CONFLUENCE__BASEURL \
      -e CONFLUENCE__AUTHMODE \
      -e CONFLUENCE__USEREMAIL \
      -e CONFLUENCE__APITOKEN \
      -v $(pwd)/docs:/workspace/docs:ro \
      confluentsynkmd \
      --mode Upload \
      --path /workspace/docs \
      --conf-space YOUR_SPACE_KEY \
      --conf-parent-id YOUR_PAGE_ID

    # Download (CI/CD recommended): writable output mount
    docker run --rm -it \
      -e CONFLUENCE__BASEURL \
      -e CONFLUENCE__AUTHMODE \
      -e CONFLUENCE__USEREMAIL \
      -e CONFLUENCE__APITOKEN \
      -v $(pwd)/output:/workspace/output \
      confluentsynkmd \
      --mode Download \
      --path /workspace/output \
      --conf-space YOUR_SPACE_KEY \
      --conf-parent-id YOUR_PAGE_ID
    ```

=== "PowerShell"

    ```powershell
    # Upload (local development, optional): use --env-file
    # Run from the directory that contains .env
    docker run --rm -it `
      --env-file ./.env `
      -v ${PWD}/docs:/workspace/docs:ro `
      confluentsynkmd `
      --mode Upload `
      --path /workspace/docs `
      --conf-space YOUR_SPACE_KEY `
      --conf-parent-id YOUR_PAGE_ID

    # Download (local development, optional): separate writable output mount
    docker run --rm -it `
      --env-file ./.env `
      -v ${PWD}/output:/workspace/output `
      confluentsynkmd `
      --mode Download `
      --path /workspace/output `
      --conf-space YOUR_SPACE_KEY `
      --conf-parent-id YOUR_PAGE_ID

    # Upload (CI/CD recommended): inject secrets as runner environment variables
    docker run --rm -it `
      -e CONFLUENCE__BASEURL `
      -e CONFLUENCE__AUTHMODE `
      -e CONFLUENCE__USEREMAIL `
      -e CONFLUENCE__APITOKEN `
      -v ${PWD}/docs:/workspace/docs:ro `
      confluentsynkmd `
      --mode Upload `
      --path /workspace/docs `
      --conf-space YOUR_SPACE_KEY `
      --conf-parent-id YOUR_PAGE_ID

    # Download (CI/CD recommended): writable output mount
    docker run --rm -it `
      -e CONFLUENCE__BASEURL `
      -e CONFLUENCE__AUTHMODE `
      -e CONFLUENCE__USEREMAIL `
      -e CONFLUENCE__APITOKEN `
      -v ${PWD}/output:/workspace/output `
      confluentsynkmd `
      --mode Download `
      --path /workspace/output `
      --conf-space YOUR_SPACE_KEY `
      --conf-parent-id YOUR_PAGE_ID
    ```

=== "CMD"

    ```cmd
    REM Upload (local development, optional): use --env-file
    REM Run from the directory that contains .env
    docker run --rm -it ^
      --env-file ./.env ^
      -v %cd%/docs:/workspace/docs:ro ^
      confluentsynkmd ^
      --mode Upload ^
      --path /workspace/docs ^
      --conf-space YOUR_SPACE_KEY ^
      --conf-parent-id YOUR_PAGE_ID

    REM Download (local development, optional): separate writable output mount
    docker run --rm -it ^
      --env-file ./.env ^
      -v %cd%/output:/workspace/output ^
      confluentsynkmd ^
      --mode Download ^
      --path /workspace/output ^
      --conf-space YOUR_SPACE_KEY ^
      --conf-parent-id YOUR_PAGE_ID

    REM Upload (CI/CD recommended): inject secrets as runner environment variables
    docker run --rm -it ^
      -e CONFLUENCE__BASEURL ^
      -e CONFLUENCE__AUTHMODE ^
      -e CONFLUENCE__USEREMAIL ^
      -e CONFLUENCE__APITOKEN ^
      -v %cd%/docs:/workspace/docs:ro ^
      confluentsynkmd ^
      --mode Upload ^
      --path /workspace/docs ^
      --conf-space YOUR_SPACE_KEY ^
      --conf-parent-id YOUR_PAGE_ID

    REM Download (CI/CD recommended): writable output mount
    docker run --rm -it ^
      -e CONFLUENCE__BASEURL ^
      -e CONFLUENCE__AUTHMODE ^
      -e CONFLUENCE__USEREMAIL ^
      -e CONFLUENCE__APITOKEN ^
      -v %cd%/output:/workspace/output ^
      confluentsynkmd ^
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
    For CI/CD, prefer platform secret stores (GitHub/GitLab protected variables) and inject them at runtime. Use `--env-file` mainly for local development.

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
            run: docker build -t confluentsynkmd .

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
      # Set as masked/protected CI variables:
      # CONFLUENCE__BASEURL, CONFLUENCE__USEREMAIL, CONFLUENCE__APITOKEN,
      # CONFLUENCE_SPACE, CONFLUENCE_PARENT_ID
    ```

---

## Extending the Image

To add additional diagram renderers, extend the Dockerfile:

```dockerfile
FROM confluentsynkmd AS base

# Add PlantUML
RUN apt-get update && apt-get install -y plantuml

# Add Draw.io export
RUN npm install -g drawio-export
```
