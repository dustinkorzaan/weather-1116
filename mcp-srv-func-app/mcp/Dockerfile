# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY cqmediator-dotnet/cqmediator/CQMediator.csproj cqmediator-dotnet/cqmediator/
COPY core-dotnet/core/Core.csproj core-dotnet/core/
COPY mcp-srv-func-app/mcp/WeatherMcpSrvFuncApp.csproj mcp-srv-func-app/mcp/

RUN dotnet restore mcp-srv-func-app/mcp/WeatherMcpSrvFuncApp.csproj

COPY cqmediator-dotnet/ cqmediator-dotnet/
COPY core-dotnet/ core-dotnet/
COPY mcp-srv-func-app/ mcp-srv-func-app/

RUN dotnet publish mcp-srv-func-app/mcp/WeatherMcpSrvFuncApp.csproj -c Release -o /home/site/wwwroot

# Functions host runtime (.NET 10 isolated worker)
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated10.0
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true

WORKDIR /home/site/wwwroot
COPY --from=build /home/site/wwwroot .
