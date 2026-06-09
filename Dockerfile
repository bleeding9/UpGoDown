FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/UpGoDown.Api/UpGoDown.Api.csproj UpGoDown.Api/
RUN dotnet restore UpGoDown.Api/UpGoDown.Api.csproj
COPY src/UpGoDown.Api/ UpGoDown.Api/
RUN dotnet publish UpGoDown.Api/UpGoDown.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "UpGoDown.Api.dll"]
