FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Gastos.sln ./
COPY src/Gastos.Domain/*.csproj src/Gastos.Domain/
COPY src/Gastos.Application/*.csproj src/Gastos.Application/
COPY src/Gastos.Infrastructure/*.csproj src/Gastos.Infrastructure/
COPY src/Gastos.Api/*.csproj src/Gastos.Api/
RUN dotnet restore src/Gastos.Api/Gastos.Api.csproj
COPY src/ src/
# Descarta obj/ y bin/ de otra máquina, si llegaron, y restaura de nuevo (los paquetes ya están en caché).
RUN find src -type d \( -name obj -o -name bin \) -prune -exec rm -rf {} + \
    && dotnet publish src/Gastos.Api/Gastos.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "Gastos.Api.dll"]
