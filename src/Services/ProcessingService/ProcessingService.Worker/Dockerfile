# Build context expected at repository root.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore ProcessingService/ProcessingService.Worker/ProcessingService.Worker.csproj
RUN dotnet publish ProcessingService/ProcessingService.Worker/ProcessingService.Worker.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ProcessingService.Worker.dll"]
