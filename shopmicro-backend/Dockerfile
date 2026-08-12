# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/Api/Api.csproj ./Api/
RUN dotnet restore ./Api/Api.csproj

COPY src/Api/. ./Api/
WORKDIR /src/Api
RUN dotnet publish -c Release -o /app/publish

# ---------- Runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Mantém a aplicação sem privilégios e prepara o diretório persistente com
# permissão de escrita para uploads locais.
RUN adduser --disabled-password --gecos "" appuser \
    && mkdir -p /app/uploads \
    && chown appuser:appuser /app/uploads
USER appuser

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Api.dll"]
