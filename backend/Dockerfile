# syntax=docker/dockerfile:1.7

ARG DOTNET_VERSION=9.0

# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /src

# Solution-level config first so package restore can be cached.
COPY global.json Directory.Build.props Directory.Packages.props nuget.config ./

# Copy only csprojs to maximize layer caching for restore.
COPY src/AgriCure.Api/AgriCure.Api.csproj src/AgriCure.Api/
COPY src/AgriCure.Application/AgriCure.Application.csproj src/AgriCure.Application/
COPY src/AgriCure.Domain/AgriCure.Domain.csproj src/AgriCure.Domain/
COPY src/AgriCure.Infrastructure/AgriCure.Infrastructure.csproj src/AgriCure.Infrastructure/

RUN dotnet restore src/AgriCure.Api/AgriCure.Api.csproj

# Now bring in the rest of the source and publish.
COPY src/ src/

RUN dotnet publish src/AgriCure.Api/AgriCure.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS runtime
WORKDIR /app

# The aspnet:9.0 image ships a non-root `app` user (UID 1654). Use it.
USER app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true

EXPOSE 8080

COPY --from=build --chown=app:app /app/publish ./

ENTRYPOINT ["dotnet", "AgriCure.Api.dll"]
