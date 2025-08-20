# Build context is the repository root:  docker build -f api/Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first so the layer is cached until a project file changes.
COPY global.json ./
COPY api/Directory.Build.props api/
COPY api/src/GameLog.Domain/GameLog.Domain.csproj api/src/GameLog.Domain/
COPY api/src/GameLog.Application/GameLog.Application.csproj api/src/GameLog.Application/
COPY api/src/GameLog.Infrastructure/GameLog.Infrastructure.csproj api/src/GameLog.Infrastructure/
COPY api/src/GameLog.Api/GameLog.Api.csproj api/src/GameLog.Api/
RUN dotnet restore api/src/GameLog.Api/GameLog.Api.csproj

COPY api/src api/src
RUN dotnet publish api/src/GameLog.Api/GameLog.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app ./

# Data Protection key ring (auth cookies) lives on a volume so restarts keep sessions valid.
RUN mkdir -p /keys && chown $APP_UID:$APP_UID /keys
USER $APP_UID
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "GameLog.Api.dll"]
