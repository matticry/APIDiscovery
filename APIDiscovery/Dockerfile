FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8082
ENV ASPNETCORE_URLS=http://+:8082
ENV ASPNETCORE_ENVIRONMENT=Development

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

# Crear directorios necesarios
RUN mkdir -p /app/Certificados /app/FacturasXml

# Copiar certificados si existen en el proyecto
COPY --from=build /src/Certificados/ /app/Certificados/

# Dar permisos necesarios a los directorios
RUN chmod -R 755 /app/Certificados /app/FacturasXml

ENTRYPOINT ["dotnet", "APIDiscovery.dll"]