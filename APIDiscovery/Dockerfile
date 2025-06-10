FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8082
ENV ASPNETCORE_URLS=http://+:8082
ENV ASPNETCORE_ENVIRONMENT=Development


RUN apt-get update && apt-get install -y \
    wkhtmltopdf \
    xvfb \
    libfontconfig1 \
    libxrender1 \
    && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["APIDiscovery.csproj", "./"]
RUN dotnet restore "./APIDiscovery.csproj"
COPY . .
RUN dotnet build "APIDiscovery.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "APIDiscovery.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .


COPY --from=build /src/.env /app/.env

# Crear las carpetas necesarias si no existen
RUN mkdir -p /app/Certificados /app/FacturasXml /app/uploads && \
    chmod -R 777 /app/Certificados /app/FacturasXml /app/uploads

USER $APP_UID
ENTRYPOINT ["dotnet", "APIDiscovery.dll"]