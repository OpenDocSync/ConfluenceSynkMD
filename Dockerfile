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
# ──────────────────────────────────────────────────────────────────────────────
# Stage 2: Runtime image
# ──────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0@sha256:52dcfb4225fda614c38ba5997a4ec72cbd5260a624125174416e547ff9eb9b8c AS runtime

# Install local Mermaid rendering dependencies (md2conf-like approach):
# - Node.js + npm + mermaid-cli (mmdc)
# - Chromium + fonts for headless rendering
RUN set -eux; \
    apt-get update; \
    apt-get install -y --no-install-recommends \
        ca-certificates \
        chromium \
        fonts-dejavu-core \
        fonts-freefont-ttf \
        fonts-inconsolata \
        fonts-linuxlibertine \
        fonts-noto-cjk \
        fonts-noto-color-emoji \
        nodejs \
        npm; \
    npm install -g @mermaid-js/mermaid-cli@11.12.0; \
    rm -rf /var/lib/apt/lists/*

# Configure mermaid-cli to use system Chromium inside container.
ENV CHROME_BIN=/usr/bin/chromium \
    PUPPETEER_SKIP_DOWNLOAD=true

WORKDIR /app
COPY --from=build /app/publish .

# Security note:
# - Run as non-root by default.
# - Mermaid rendering runs locally via mmdc; no docker.sock mount is required.
RUN groupadd -g 1001 appgroup \
    && useradd -m -u 1001 -g appgroup -s /usr/sbin/nologin appuser \
    && chown -R appuser:appgroup /app
USER appuser

# Default entrypoint
ENTRYPOINT ["dotnet", "ConfluenceSynkMD.dll"]
