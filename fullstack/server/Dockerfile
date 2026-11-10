FROM node:22-bookworm-slim AS node

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
COPY --from=node /usr/local/ /usr/local/
WORKDIR /src
COPY . .
RUN dotnet restore server/fullstackapplication.Server.csproj
RUN dotnet publish server/fullstackapplication.Server.csproj --configuration Release --output /app/publish --no-restore -p:BuildProjectReferences=false

FROM build AS migrate
RUN dotnet tool install --global dotnet-ef --version 9.0.22
ENV PATH="$PATH:/root/.dotnet/tools"
ENTRYPOINT ["dotnet", "ef"]

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 10000
HEALTHCHECK --interval=10s --timeout=3s --start-period=30s --retries=12 CMD curl -fsS http://127.0.0.1:10000/ || exit 1
USER $APP_UID
ENTRYPOINT ["dotnet", "fullstackapplication.Server.dll"]
