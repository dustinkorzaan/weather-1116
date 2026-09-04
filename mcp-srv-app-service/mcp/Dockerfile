FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY cqmediator-dotnet/cqmediator/CQMediator.csproj cqmediator-dotnet/cqmediator/
COPY core-dotnet/core/Core.csproj core-dotnet/core/
COPY mcp-srv-app-service/mcp/WeatherMcpSrvAppService.csproj mcp-srv-app-service/mcp/

RUN dotnet restore mcp-srv-app-service/mcp/WeatherMcpSrvAppService.csproj

COPY cqmediator-dotnet/ cqmediator-dotnet/
COPY core-dotnet/ core-dotnet/
COPY mcp-srv-app-service/ mcp-srv-app-service/

RUN dotnet publish mcp-srv-app-service/mcp/WeatherMcpSrvAppService.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "WeatherMcpSrvAppService.dll"]
