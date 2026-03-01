# ──────────────────────────────────────────────────────────────────────────────
# Stage 1: Build .NET application
# ──────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0@sha256:e362a8dbcd691522456da26a5198b8f3ca1d7641c95624fadc5e3e82678bd08a AS build
WORKDIR /src

# Copy solution and project files first (layer caching)
COPY *.slnx ./
COPY src/ConfluenceSynkMD/ConfluenceSynkMD.csproj src/ConfluenceSynkMD/
RUN dotnet restore src/ConfluenceSynkMD/ConfluenceSynkMD.csproj

# Copy source and build
COPY src/ src/
RUN dotnet publish src/ConfluenceSynkMD/ConfluenceSynkMD.csproj -c Release -o /app/publish --no-restore

# ──────────────────────────────────────────────────────────────────────────────
# Stage 2: Runtime image with Node.js + mermaid-cli
# ──────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0@sha256:52dcfb4225fda614c38ba5997a4ec72cbd5260a624125174416e547ff9eb9b8c AS runtime

ARG NODEJS_MAJOR=22
ARG MERMAID_CLI_VERSION=11.12.0
ENV PUPPETEER_CACHE_DIR=/app/.cache/puppeteer
# Dependabot tracks Docker base image updates (FROM).
# MERMAID_CLI_VERSION and NODEJS_MAJOR are intentionally explicit and checked in CI.

# Install Node.js (LTS) and Chromium dependencies for mermaid-cli/Puppeteer
# NOTE: Ubuntu archive package versions are intentionally unpinned to receive
# upstream security updates on regular image rebuilds.
RUN apt-get update && apt-get install -y --no-install-recommends \
    curl \
    ca-certificates \
    gnupg \
    # Chromium dependencies for Puppeteer headless rendering
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
    && rm -rf /var/lib/apt/lists/*

# Install Node.js LTS from explicitly configured NodeSource APT repository
RUN install -d -m 0755 /etc/apt/keyrings \
        && curl -fsSL https://deb.nodesource.com/gpgkey/nodesource-repo.gpg.key \
            | gpg --dearmor -o /etc/apt/keyrings/nodesource.gpg \
        && chmod 0644 /etc/apt/keyrings/nodesource.gpg \
        && echo "deb [signed-by=/etc/apt/keyrings/nodesource.gpg] https://deb.nodesource.com/node_${NODEJS_MAJOR}.x nodistro main" \
            > /etc/apt/sources.list.d/nodesource.list \
        && apt-get update \
            && apt-get install -y --no-install-recommends nodejs \
    && rm -rf /var/lib/apt/lists/*

# Install mermaid-cli globally (includes Puppeteer + bundled Chromium)
RUN mkdir -p ${PUPPETEER_CACHE_DIR}
RUN npm install -g @mermaid-js/mermaid-cli@${MERMAID_CLI_VERSION} \
    && npx puppeteer browsers install chrome

# Create puppeteer config for container
WORKDIR /app
RUN echo '{"args": ["--no-sandbox", "--disable-setuid-sandbox"]}' > /app/puppeteer-config.json

COPY --from=build /app/publish .

RUN useradd -m -u 1001 appuser && chown -R appuser:appuser /app
USER appuser

# Default entrypoint
ENTRYPOINT ["dotnet", "ConfluenceSynkMD.dll"]
