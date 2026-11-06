FROM node:22-bookworm-slim AS node

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
COPY --from=node /usr/local/ /usr/local/
WORKDIR /src
COPY . .
RUN dotnet restore server/fullstackapplication.Server.csproj
RUN dotnet publish server/fullstackapplication.Server.csproj --configuration Release --output /app/publish --no-restore -p:BuildProjectReferences=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 10000
USER $APP_UID
ENTRYPOINT ["dotnet", "fullstackapplication.Server.dll"]
