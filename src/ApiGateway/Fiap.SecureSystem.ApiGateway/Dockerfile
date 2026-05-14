# =========================
# BUILD STAGE
# =========================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copia os arquivos de projeto primeiro para aproveitar cache de restore
COPY ["src/Shared/Shared.Contracts/Shared.Contracts.csproj", "src/Shared/Shared.Contracts/"]
COPY ["src/Shared/Shared.Kernel/Shared.Kernel.csproj", "src/Shared/Shared.Kernel/"]
COPY ["src/Shared/Shared.Observability/Shared.Observability.csproj", "src/Shared/Shared.Observability/"]

COPY ["src/ApiGateway/Fiap.SecureSystem.ApiGateway/Fiap.SecureSystem.ApiGateway.csproj", "src/ApiGateway/Fiap.SecureSystem.ApiGateway/"]

RUN dotnet restore "src/ApiGateway/Fiap.SecureSystem.ApiGateway/Fiap.SecureSystem.ApiGateway.csproj"

# Copia o restante do código
COPY . .

WORKDIR /src/src/ApiGateway/Fiap.SecureSystem.ApiGateway
RUN dotnet publish "Fiap.SecureSystem.ApiGateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

# =========================
# RUNTIME STAGE
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Fiap.SecureSystem.ApiGateway.dll"]
