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

# Install Docker CLI to allow spawning sibling containers for Mermaid rendering.
# Version is pinned for reproducibility and should be reviewed periodically for security updates.
ARG DOCKER_CLI_VERSION=25.0.5
RUN set -eux; \
    apt-get update; \
    apt-get install -y --no-install-recommends curl ca-certificates; \
    arch="$(uname -m)"; \
    docker_tgz="docker-${DOCKER_CLI_VERSION}.tgz"; \
    docker_url="https://download.docker.com/linux/static/stable/${arch}"; \
    curl -fsSL -o "/tmp/${docker_tgz}" "${docker_url}/${docker_tgz}"; \
    curl -fsSL -o "/tmp/${docker_tgz}.sha256" "${docker_url}/${docker_tgz}.sha256"; \
    cd /tmp; \
    sha256sum -c "${docker_tgz}.sha256"; \
    tar -xzC /usr/local/bin --strip-components=1 -f "/tmp/${docker_tgz}" docker/docker; \
    rm -f "/tmp/${docker_tgz}" "/tmp/${docker_tgz}.sha256"; \
    rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .

# Security note:
# - Run as non-root by default.
# - Access to /var/run/docker.sock remains root-equivalent and must be granted explicitly.
# - Use `--group-add <docker-gid>` at runtime to allow controlled socket access.
RUN groupadd -g 1001 appgroup \
    && useradd -m -u 1001 -g appgroup -s /usr/sbin/nologin appuser \
    && chown -R appuser:appgroup /app
USER appuser

# Default entrypoint
ENTRYPOINT ["dotnet", "ConfluenceSynkMD.dll"]
