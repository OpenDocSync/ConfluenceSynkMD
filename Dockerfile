# ──────────────────────────────────────────────────────────────────────────────
# Stage 1: Build .NET application
# ──────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0@sha256:0a506ab0c8aa077361af42f82569d364ab1b8741e967955d883e3f23683d473a AS build
WORKDIR /src

# Copy solution and project files first (layer caching)
COPY *.slnx ./
COPY src/ConfluenceSynkMD/ConfluenceSynkMD.csproj src/ConfluenceSynkMD/
RUN dotnet restore src/ConfluenceSynkMD/ConfluenceSynkMD.csproj

# Copy source and build
COPY src/ src/
RUN dotnet publish src/ConfluenceSynkMD/ConfluenceSynkMD.csproj -c Release -o /app/publish --no-restore

# ──────────────────────────────────────────────────────────────────────────────
# Stage 2: Runtime — full diagram toolchain
# Ships Mermaid (Node + mermaid-cli + Chromium), PlantUML (JRE + plantuml),
# LaTeX (TeX Live + Ghostscript; ImageMagick is intentionally NOT installed —
# LatexRenderer calls `gs -sDEVICE=pngalpha` directly), and drawio-desktop
# (Electron app, runs headless via Xvfb-once started by the entrypoint).
# ──────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0@sha256:52dcfb4225fda614c38ba5997a4ec72cbd5260a624125174416e547ff9eb9b8c AS runtime

ARG NODEJS_MAJOR=22
ARG MERMAID_CLI_VERSION=11.16.0
ARG DRAWIO_VERSION=31.3.2
ARG TARGETARCH

# Dependabot tracks Docker base image updates (FROM).
# NODEJS_MAJOR, MERMAID_CLI_VERSION, and DRAWIO_VERSION are intentionally
# explicit pins refreshed monthly by .github/workflows/docker-manual-pin-check.yml.

ENV PUPPETEER_CACHE_DIR=/app/.cache/puppeteer \
    DISPLAY=:99 \
    DRAWIO_CMD=drawio-headless

# System packages.
# - Chromium runtime libs for Puppeteer (Mermaid renderer).
# - Java + plantuml for the PlantUML renderer.
# - TeX Live + Ghostscript + poppler for the LaTeX renderer (no ImageMagick).
# - xvfb + x11-utils for the headless display server used by drawio-desktop.
# NOTE: Ubuntu/Debian package versions are intentionally unpinned to receive
# upstream security updates on regular image rebuilds. See the project's
# pinning policy in .github/workflows/docker-manual-pin-check.yml.
RUN apt-get update && apt-get install -y --no-install-recommends \
    curl \
    ca-certificates \
    gnupg \
    # Chromium / Puppeteer (Mermaid)
    libnss3 \
    libatk1.0-0t64 \
    libatk-bridge2.0-0t64 \
    libcups2t64 \
    libdrm2 \
    libxkbcommon0 \
    libxcomposite1 \
    libxdamage1 \
    libxfixes3 \
    libxrandr2 \
    libgbm1 \
    libpango-1.0-0 \
    libcairo2 \
    libasound2t64 \
    libxshmfence1 \
    libxss1 \
    fonts-liberation \
    fonts-noto-color-emoji \
    # PlantUML (needs JRE)
    default-jre-headless \
    plantuml \
    # LaTeX rendering: TeX Live + Ghostscript directly (no ImageMagick)
    texlive-latex-base \
    texlive-latex-extra \
    texlive-fonts-recommended \
    ghostscript \
    poppler-utils \
    # Headless display server for drawio-desktop (Electron app).
    # `dbus` provides a temporary session bus via `dbus-run-session` that
    # the shim wraps drawio with — without it, Electron's IPC fails to
    # initialize and drawio's commander parser falls back to a code path
    # that emits "Error: input file/directory not found" with exit 0.
    xvfb \
    x11-utils \
    dbus \
    dbus-x11 \
    && rm -rf /var/lib/apt/lists/*

# Install Node.js LTS from explicitly configured NodeSource APT repository.
RUN install -d -m 0755 /etc/apt/keyrings \
        && curl -fsSL https://deb.nodesource.com/gpgkey/nodesource-repo.gpg.key \
            | gpg --dearmor -o /etc/apt/keyrings/nodesource.gpg \
        && chmod 0644 /etc/apt/keyrings/nodesource.gpg \
        && echo "deb [signed-by=/etc/apt/keyrings/nodesource.gpg] https://deb.nodesource.com/node_${NODEJS_MAJOR}.x nodistro main" \
            > /etc/apt/sources.list.d/nodesource.list \
        && apt-get update \
            && apt-get install -y --no-install-recommends nodejs \
    && rm -rf /var/lib/apt/lists/*

# Install mermaid-cli globally (includes Puppeteer + bundled Chromium).
RUN mkdir -p ${PUPPETEER_CACHE_DIR}
RUN npm install -g @mermaid-js/mermaid-cli@${MERMAID_CLI_VERSION} \
    && npx puppeteer browsers install chrome

# Install drawio-desktop from the pinned upstream .deb.
# amd64 .deb URL pattern is reliable on every release.
# arm64 availability has been inconsistent — the release workflow's
# multi-arch pre-flight catches breakage before publishing the manifest list.
RUN DRAWIO_ARCH=$([ "${TARGETARCH:-amd64}" = "arm64" ] && echo arm64 || echo amd64) \
    && curl -fsSL -o /tmp/drawio.deb \
        "https://github.com/jgraph/drawio-desktop/releases/download/v${DRAWIO_VERSION}/drawio-${DRAWIO_ARCH}-${DRAWIO_VERSION}.deb" \
    && apt-get update \
    && apt-get install -y --no-install-recommends /tmp/drawio.deb \
    && rm -f /tmp/drawio.deb \
    && rm -rf /var/lib/apt/lists/*

# Puppeteer Chromium config for headless container (Mermaid renderer).
WORKDIR /app
RUN echo '{"args": ["--no-sandbox", "--disable-setuid-sandbox"]}' > /app/puppeteer-config.json

# Container entrypoint: starts Xvfb once on :99 for the container's lifetime,
# then exec's dotnet. drawio-desktop inherits DISPLAY=:99 with no per-call
# xvfb-run startup cost. See entrypoint.sh for the full startup contract.
COPY entrypoint.sh /usr/local/bin/entrypoint.sh
RUN chmod +x /usr/local/bin/entrypoint.sh

# drawio-desktop invocation wrapper. Electron flags (--no-sandbox /
# --disable-gpu / --disable-dev-shm-usage) MUST appear before drawio's own
# CLI options or commander.js mis-parses them as positional input paths
# and exits with "Error: input file/directory not found". The shim exec's
# drawio with the Electron flags, then "$@" forwards the renderer's export
# args. ENV DRAWIO_CMD=drawio-headless points DrawioRenderer here.
COPY drawio-headless.sh /usr/local/bin/drawio-headless
RUN chmod +x /usr/local/bin/drawio-headless

COPY --from=build /app/publish .

RUN useradd -m -u 1001 appuser && chown -R appuser:appuser /app
USER appuser

ENTRYPOINT ["/usr/local/bin/entrypoint.sh"]
