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

# Install Docker CLI to allow spawning sibling containers for Mermaid rendering
ARG DOCKER_CLI_VERSION=24.0.9
RUN apt-get update && apt-get install -y --no-install-recommends curl ca-certificates \
    && curl -fsSL https://download.docker.com/linux/static/stable/$(uname -m)/docker-${DOCKER_CLI_VERSION}.tgz | tar -xzC /usr/local/bin --strip-components=1 docker/docker \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .

# It's recommended to run as non-root, but accessing /var/run/docker.sock
# requires specific group permissions which depend on the host machine.
# By default, we leave it as root to ensure docker socket access works seamlessly out of the box.
# If running as non-root is strictly required, users must pass --group-add when running the container.
# RUN useradd -m -u 1001 appuser && chown -R appuser:appuser /app
# USER appuser

# Default entrypoint
ENTRYPOINT ["dotnet", "ConfluenceSynkMD.dll"]
