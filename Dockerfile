# Build web assets
FROM node:20-bullseye AS web-build

WORKDIR /src/Grayjay.Desktop.Web
COPY vendor/Grayjay.Desktop/Grayjay.Desktop.Web/ .
RUN npm ci && npm run build

# Publish .NET app (self-contained, single file)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS publish

WORKDIR /src

COPY src/Grayjay.Desktop.Server/Grayjay.Desktop.Server.csproj Grayjay.Desktop.Server/
COPY vendor/Grayjay.Desktop/Grayjay.ClientServer/Grayjay.ClientServer.csproj Grayjay.ClientServer/
COPY vendor/Grayjay.Desktop/Grayjay.Engine/Grayjay.Engine/Grayjay.Engine.csproj Grayjay.Engine/Grayjay.Engine/
COPY vendor/Grayjay.Desktop/FUTO.MDNS/FUTO.MDNS/FUTO.MDNS.csproj FUTO.MDNS/FUTO.MDNS/
COPY vendor/Grayjay.Desktop/SyncServer/SyncClient/SyncClient.csproj SyncServer/SyncClient/
COPY vendor/Grayjay.Desktop/SyncServer/SyncShared/SyncShared.csproj SyncServer/SyncShared/
COPY vendor/Grayjay.Desktop/SyncServer/FUTO.MDNS/FUTO.MDNS/FUTO.MDNS.csproj SyncServer/FUTO.MDNS/FUTO.MDNS/

RUN dotnet restore Grayjay.Desktop.Server/Grayjay.Desktop.Server.csproj

COPY src/Grayjay.Desktop.Server/ Grayjay.Desktop.Server/
COPY vendor/Grayjay.Desktop/Grayjay.ClientServer/ Grayjay.ClientServer/
COPY vendor/Grayjay.Desktop/Grayjay.Engine/ Grayjay.Engine/
COPY vendor/Grayjay.Desktop/FUTO.MDNS/ FUTO.MDNS/
COPY vendor/Grayjay.Desktop/SyncServer/ SyncServer/

# Fail early if required native artifacts are still Git LFS pointer files
RUN set -eu; \
  files="\
    Grayjay.ClientServer/deps/linux-x64/FUTO.Updater.Client \
    Grayjay.ClientServer/deps/linux-x64/ffmpeg \
    Grayjay.ClientServer/deps/linux-x64/libsteam_api.so \
  "; \
  for f in $files; do \
    if [ ! -f "$f" ]; then \
      echo "ERROR: missing required file: $f" >&2; exit 1; \
    fi; \
    first="$(head -n 1 "$f" || true)"; \
    if [ "$first" = "version https://git-lfs.github.com/spec/v1" ]; then \
      echo "ERROR: $f is a Git LFS pointer; run 'git lfs pull' on the host before docker build." >&2; \
      exit 1; \
    fi; \
  done

COPY --from=web-build /src/Grayjay.Desktop.Web/dist ./Grayjay.Desktop.Web/dist

RUN \
  dotnet publish Grayjay.Desktop.Server/Grayjay.Desktop.Server.csproj \
    -c Release \
    -r linux-x64 \
    -p:PublishSingleFile=true \
    -p:SelfContained=true \
    -o /app/publish && \
  mkdir -p /app/publish/wwwroot && \
  cp -r Grayjay.Desktop.Web/dist /app/publish/wwwroot/web

# Runtime image
FROM --platform=linux/amd64 debian:12-slim AS runtime

ENV \
  DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 \
  ASPNETCORE_URLS=http://0.0.0.0:11338 \
  GRAYJAY_HTTP_PROXY_PORT=11339 \
  GRAYJAY_CASTING_PORT=11340

RUN \
  apt-get update && \
  apt-get install -y --no-install-recommends \
    ca-certificates \
    libnss3 \
    libx11-xcb1 \
    libxcb-dri3-0 \
    libxcomposite1 \
    libxdamage1 \
    libxfixes3 \
    libdrm2 \
    libgbm1 \
    libxrandr2 \
    libasound2 \
    libatk1.0-0 \
    libatk-bridge2.0-0 \
    libcups2 \
    libpango-1.0-0 \
    libxshmfence1 \
    fonts-liberation && \
  rm -rf /var/lib/apt/lists/* && \
  printf '#!/bin/sh\nprintf "xdg-open stub (no GUI): %s\\n" "$*" >&2\nexit 0\n' >/usr/local/bin/xdg-open && \
  chmod +x /usr/local/bin/xdg-open

WORKDIR /app

COPY --from=publish /app/publish .

RUN rm -f /app/Portable

EXPOSE 11338 11339 11340

CMD ["./Grayjay", "--server"]
