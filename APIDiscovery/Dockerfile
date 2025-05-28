FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8082
ENV ASPNETCORE_URLS=http://+:8082
ENV ASPNETCORE_ENVIRONMENT=Development

# Instalar dependencias para wkhtmltopdf
RUN apt-get update && apt-get install -y \
    wget \
    xfonts-75dpi \
    xfonts-base \
    fontconfig \
    libxrender1 \
    libxext6 \
    libx11-6 \
    libjpeg62-turbo \
    libpng16-16 \
    libssl3 \
    && rm -rf /var/lib/apt/lists/* \
    

# Descargar e instalar wkhtmltopdf
RUN wget -q https://github.com/wkhtmltopdf/packaging/releases/download/0.12.6.1-2/wkhtmltox_0.12.6.1-2.bullseye_amd64.deb \
    && dpkg -i wkhtmltox_0.12.6.1-2.bullseye_amd64.deb || apt-get install -f -y \
    && rm wkhtmltox_0.12.6.1-2.bullseye_amd64.deb

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

# Copiar el archivo .env
COPY --from=build /src/.env /app/.env

# Crear las carpetas necesarias si no existen
RUN mkdir -p /app/Certificados /app/FacturasXml /app/uploads && \
    chmod -R 777 /app/Certificados /app/FacturasXml /app/uploads

USER $APP_UID
ENTRYPOINT ["dotnet", "APIDiscovery.dll"]