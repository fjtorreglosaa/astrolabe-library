# syntax=docker/dockerfile:1

# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy the manifests first so a source-only change does not invalidate the restore layer.
COPY Directory.Packages.props NuGet.config Astrolabe.slnx ./
COPY src/backend/Astrolabe.Domain/*.csproj          src/backend/Astrolabe.Domain/
COPY src/backend/Astrolabe.Application/*.csproj     src/backend/Astrolabe.Application/
COPY src/backend/Astrolabe.Infrastructure/*.csproj  src/backend/Astrolabe.Infrastructure/
COPY src/backend/Astrolabe.Presentation/*.csproj    src/backend/Astrolabe.Presentation/

RUN dotnet restore src/backend/Astrolabe.Presentation/Astrolabe.Presentation.csproj

COPY src/backend/ src/backend/

RUN dotnet publish src/backend/Astrolabe.Presentation/Astrolabe.Presentation.csproj \
    -c Release -o /app/publish --no-restore

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# The runtime image ships neither curl nor wget, so a container healthcheck has nothing to call.
# Installed before dropping privileges, and the apt lists are removed to keep the layer small.
RUN apt-get update \
    && apt-get install --no-install-recommends -y curl \
    && rm -rf /var/lib/apt/lists/*

# Run unprivileged. The base image ships a non-root 'app' user.
USER app

ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true

EXPOSE 8080

COPY --from=build /app/publish .

HEALTHCHECK --interval=10s --timeout=5s --start-period=20s --retries=10 \
    CMD curl --fail --silent http://localhost:8080/health/ready || exit 1

ENTRYPOINT ["dotnet", "Astrolabe.Presentation.dll"]
