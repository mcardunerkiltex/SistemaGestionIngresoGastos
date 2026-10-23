# Imagen única para producción: la API .NET sirve también la web Angular (un solo servicio, un solo dominio).
# Se usa en Railway (ver railway.json y docs/publicacion-railway.md). Para desarrollo local sigue docker-compose.yml.

FROM node:24-alpine AS web
WORKDIR /src
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci
# Solo lo que necesita el build: si el contexto trae un node_modules de otra máquina (macOS, Windows),
# no pisa el que instaló npm ci para Linux. No depende de que el .dockerignore haya llegado al servidor.
COPY frontend/angular.json frontend/ngsw-config.json frontend/tsconfig.json frontend/tsconfig.app.json ./
COPY frontend/public/ public/
COPY frontend/src/ src/
RUN npx ng build --configuration production

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS api
WORKDIR /src
COPY backend/Gastos.sln ./
COPY backend/src/Gastos.Domain/*.csproj src/Gastos.Domain/
COPY backend/src/Gastos.Application/*.csproj src/Gastos.Application/
COPY backend/src/Gastos.Infrastructure/*.csproj src/Gastos.Infrastructure/
COPY backend/src/Gastos.Api/*.csproj src/Gastos.Api/
RUN dotnet restore src/Gastos.Api/Gastos.Api.csproj
COPY backend/src/ src/
# Si llegaron carpetas obj/ o bin/ de otra máquina (apuntan a su caché de NuGet, p. ej. /Users/...),
# se descartan y el publish vuelve a restaurar. Es rápido: los paquetes ya quedaron en la capa del restore.
RUN find src -type d \( -name obj -o -name bin \) -prune -exec rm -rf {} + \
    && dotnet publish src/Gastos.Api/Gastos.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=api /app .
COPY --from=web /src/dist/frontend/browser ./wwwroot
ENV ASPNETCORE_ENVIRONMENT=Production \
    Database__MigrateOnStartup=true
USER $APP_UID
# Railway (y otros) indican el puerto en PORT; localmente 8080.
ENTRYPOINT ["sh", "-c", "ASPNETCORE_HTTP_PORTS=${PORT:-8080} exec dotnet Gastos.Api.dll"]
