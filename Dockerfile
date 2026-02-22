# ──────────────────────────────────────────────────────────────────────────────
# Stage 1: Build .NET application
# ──────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0@sha256:0a506ab0c8aa077361af42f82569d364ab1b8741e967955d883e3f23683d473a AS build
WORKDIR /src

# Copy solution and project files first (layer caching)
COPY *.slnx ./
COPY src/ConfluentSynkMD/ConfluentSynkMD.csproj src/ConfluentSynkMD/
RUN dotnet restore src/ConfluentSynkMD/ConfluentSynkMD.csproj

# Copy source and build
COPY src/ src/
RUN dotnet publish src/ConfluentSynkMD/ConfluentSynkMD.csproj -c Release -o /app/publish --no-restore

# ──────────────────────────────────────────────────────────────────────────────
# Stage 2: Runtime image with Node.js + mermaid-cli
# ──────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0@sha256:52dcfb4225fda614c38ba5997a4ec72cbd5260a624125174416e547ff9eb9b8c AS runtime

ARG NODEJS_DEB_VERSION=22.22.0-1nodesource1
ARG MERMAID_CLI_VERSION=11.12.0
# Dependabot tracks Docker base image updates (FROM).
# ARG and apt package pins below are not auto-updated by Dependabot and require periodic manual refresh.

# Install Node.js (LTS) and Chromium dependencies for mermaid-cli/Puppeteer
# NOTE: Package versions are pinned for reproducible builds.
# When updating base image digests, refresh these versions together.
RUN apt-get update && apt-get install -y --no-install-recommends \
    curl=8.5.0-2ubuntu10.6 \
    ca-certificates=20240203 \
    gnupg=2.4.4-2ubuntu17.4 \
    # Chromium dependencies for Puppeteer headless rendering
    libnss3=2:3.98-1build1 \
    libatk1.0-0t64=2.52.0-1build1 \
    libatk-bridge2.0-0t64=2.52.0-1build1 \
    libcups2t64=2.4.7-1.2ubuntu7.9 \
    libdrm2=2.4.125-1ubuntu0.1~24.04.1 \
    libxkbcommon0=1.6.0-1build1 \
    libxcomposite1=1:0.4.5-1build3 \
    libxdamage1=1:1.1.6-1build1 \
    libxfixes3=1:6.0.0-2build1 \
    libxrandr2=2:1.5.2-2build1 \
    libgbm1=25.2.8-0ubuntu0.24.04.1 \
    libpango-1.0-0=1.52.1+ds-1build1 \
    libcairo2=1.18.0-3build1 \
    libasound2t64=1.2.11-1ubuntu0.2 \
    libxshmfence1=1.3-1build5 \
    libxss1=1:1.2.3-1build3 \
    fonts-liberation=1:2.1.5-3 \
    fonts-noto-color-emoji=2.047-0ubuntu0.24.04.1 \
    && rm -rf /var/lib/apt/lists/*

# Install Node.js 22 LTS
RUN curl -fsSL https://deb.nodesource.com/setup_22.x | bash - \
    && apt-get install -y nodejs=${NODEJS_DEB_VERSION} \
    && rm -rf /var/lib/apt/lists/*

# Install mermaid-cli globally (includes Puppeteer + bundled Chromium)
RUN npm install -g @mermaid-js/mermaid-cli@${MERMAID_CLI_VERSION} \
    && npx puppeteer browsers install chrome

# Create puppeteer config for container
WORKDIR /app
RUN echo '{"args": ["--no-sandbox", "--disable-setuid-sandbox"]}' > /app/puppeteer-config.json

COPY --from=build /app/publish .

RUN useradd -m -u 1001 appuser && chown -R appuser:appuser /app
USER appuser

# Default entrypoint
ENTRYPOINT ["dotnet", "ConfluentSynkMD.dll"]
