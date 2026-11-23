# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

ARG BUILD_CONFIGURATION=Release

# Copiar primero el csproj para aprovechar la caché de capas en restore
COPY Alquiler-Vehiculos/Alquiler-Vehiculos.Back/Alquiler-Vehiculos.Back.csproj ./Alquiler-Vehiculos/Alquiler-Vehiculos.Back/
RUN dotnet restore Alquiler-Vehiculos/Alquiler-Vehiculos.Back/Alquiler-Vehiculos.Back.csproj

# Copiar el código fuente y publicar
COPY Alquiler-Vehiculos/Alquiler-Vehiculos.Back/ ./Alquiler-Vehiculos/Alquiler-Vehiculos.Back/
RUN dotnet publish Alquiler-Vehiculos/Alquiler-Vehiculos.Back/Alquiler-Vehiculos.Back.csproj \
    -c $BUILD_CONFIGURATION -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# curl para HEALTHCHECK y directorio de logs con permisos para el usuario no-root "app"
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /app/logs \
    && chown -R app:app /app/logs

# Usuario no-root incluido en la imagen oficial aspnet:10.0
USER app

COPY --from=build /app/publish .

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "Alquiler-Vehiculos.Back.dll"]
