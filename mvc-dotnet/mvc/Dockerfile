FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY cqmediator-dotnet/cqmediator/CQMediator.csproj cqmediator-dotnet/cqmediator/
COPY core-dotnet/core/Core.csproj core-dotnet/core/
COPY mvc-dotnet/mvc/WeatherMVC.csproj mvc-dotnet/mvc/

RUN dotnet restore mvc-dotnet/mvc/WeatherMVC.csproj

COPY cqmediator-dotnet/ cqmediator-dotnet/
COPY core-dotnet/ core-dotnet/
COPY mvc-dotnet/ mvc-dotnet/

RUN dotnet publish mvc-dotnet/mvc/WeatherMVC.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "WeatherMVC.dll"]
