# ──────────────────────────────────────────────────────────────────────────────
# Stage 1: Build .NET application
# ──────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy solution and project files first (layer caching)
COPY *.slnx ./
COPY src/ConfluentSynkMD/ConfluentSynkMD.csproj src/ConfluentSynkMD/
COPY tests/ConfluentSynkMD.Tests/ConfluentSynkMD.Tests.csproj tests/ConfluentSynkMD.Tests/
RUN dotnet restore src/ConfluentSynkMD/ConfluentSynkMD.csproj

# Copy source and build
COPY src/ src/
RUN dotnet publish src/ConfluentSynkMD/ConfluentSynkMD.csproj -c Release -o /app/publish --no-restore

# ──────────────────────────────────────────────────────────────────────────────
# Stage 2: Runtime image with Node.js + mermaid-cli
# ──────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime

# Install Node.js (LTS) and Chromium dependencies for mermaid-cli/Puppeteer
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
    && apt-get install -y nodejs \
    && rm -rf /var/lib/apt/lists/*

# Install mermaid-cli globally (includes Puppeteer + bundled Chromium)
RUN npm install -g @mermaid-js/mermaid-cli \
    && npx puppeteer browsers install chrome

# Create puppeteer config for container (no-sandbox required when running as root)
WORKDIR /app
RUN echo '{"args": ["--no-sandbox", "--disable-setuid-sandbox"]}' > /app/puppeteer-config.json

COPY --from=build /app/publish .

# Default entrypoint
ENTRYPOINT ["dotnet", "ConfluentSynkMD.dll"]
