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

# Install Node.js (LTS) and Chromium dependencies for mermaid-cli/Puppeteer
# NOTE: Chromium runtime packages below are intentionally unpinned to receive
# distro security updates. This reduces strict bit-for-bit reproducibility.
# For fully reproducible builds, pin package versions or use a Debian snapshot mirror.
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
