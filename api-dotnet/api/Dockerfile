FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY cqmediator-dotnet/cqmediator/CQMediator.csproj cqmediator-dotnet/cqmediator/
COPY core-dotnet/core/Core.csproj core-dotnet/core/
COPY api-dotnet/api/WeatherAPI.csproj api-dotnet/api/

RUN dotnet restore api-dotnet/api/WeatherAPI.csproj

COPY cqmediator-dotnet/ cqmediator-dotnet/
COPY core-dotnet/ core-dotnet/
COPY api-dotnet/ api-dotnet/

RUN dotnet publish api-dotnet/api/WeatherAPI.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "WeatherAPI.dll"]
