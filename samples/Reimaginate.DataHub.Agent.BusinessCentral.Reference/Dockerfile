FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish Reimaginate.DataHub.Agent.BusinessCentral.Reference.csproj \
    --configuration Release \
    --output /app/publish \
    --no-self-contained

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "Reimaginate.DataHub.Agent.BusinessCentral.Reference.dll", "--worker"]
