FROM node:22-alpine AS frontend-build
WORKDIR /source/frontend
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci
COPY frontend/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS dotnet-build
WORKDIR /source
COPY Directory.Build.props JobParser.slnx NuGet.Config ./
COPY src/ ./src/
COPY tests/ ./tests/
COPY config/ ./config/
COPY system-rules/ ./system-rules/
COPY example-templates/ ./example-templates/
COPY demo-fixtures/ ./demo-fixtures/
RUN dotnet restore JobParser.slnx --configfile NuGet.Config
RUN dotnet publish src/App.Api/App.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /publish/api
RUN dotnet publish src/App.GmailSync/App.GmailSync.csproj \
    --configuration Release \
    --no-restore \
    --output /publish/gmail-sync
RUN dotnet publish src/App.DatabaseMigrator/App.DatabaseMigrator.csproj \
    --configuration Release \
    --no-restore \
    --output /publish/database-migrator

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app/api
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_EnableDiagnostics=0
COPY --from=dotnet-build /publish/api/ /app/api/
COPY --from=dotnet-build /publish/gmail-sync/ /app/gmail-sync/
COPY --from=dotnet-build /publish/database-migrator/ /app/database-migrator/
COPY --from=frontend-build /source/frontend/dist/ /app/api/wwwroot/
COPY config/ /app/config/
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "/app/api/App.Api.dll"]
