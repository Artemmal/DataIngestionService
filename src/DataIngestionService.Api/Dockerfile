FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/DataIngestionService.Api/DataIngestionService.Api.csproj src/DataIngestionService.Api/

RUN dotnet restore src/DataIngestionService.Api/DataIngestionService.Api.csproj

COPY . .

RUN dotnet publish src/DataIngestionService.Api/DataIngestionService.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "DataIngestionService.Api.dll"]