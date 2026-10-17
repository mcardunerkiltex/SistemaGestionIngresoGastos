# Imagen única para producción: la API .NET sirve también la web Angular (un solo servicio, un solo dominio).
# Se usa en Railway (ver railway.json y docs/publicacion-railway.md). Para desarrollo local sigue docker-compose.yml.

FROM node:24-alpine AS web
WORKDIR /src
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci
COPY frontend/ .
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
RUN dotnet publish src/Gastos.Api/Gastos.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=api /app .
COPY --from=web /src/dist/frontend/browser ./wwwroot
ENV ASPNETCORE_ENVIRONMENT=Production \
    Database__MigrateOnStartup=true
USER $APP_UID
# Railway (y otros) indican el puerto en PORT; localmente 8080.
ENTRYPOINT ["sh", "-c", "ASPNETCORE_HTTP_PORTS=${PORT:-8080} exec dotnet Gastos.Api.dll"]
