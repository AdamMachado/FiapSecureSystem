# =========================
# BUILD STAGE
# =========================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copia os arquivos de projeto primeiro para aproveitar cache de restore
COPY ["src/Shared/Shared.Contracts/Shared.Contracts.csproj", "src/Shared/Shared.Contracts/"]
COPY ["src/Shared/Shared.Kernel/Shared.Kernel.csproj", "src/Shared/Shared.Kernel/"]
COPY ["src/Shared/Shared.Observability/Shared.Observability.csproj", "src/Shared/Shared.Observability/"]

COPY ["src/Services/ProcessingService/ProcessingService.Domain/ProcessingService.Domain.csproj", "src/Services/ProcessingService/ProcessingService.Domain/"]
COPY ["src/Services/ProcessingService/ProcessingService.Application/ProcessingService.Application.csproj", "src/Services/ProcessingService/ProcessingService.Application/"]
COPY ["src/Services/ProcessingService/ProcessingService.Infrastructure/ProcessingService.Infrastructure.csproj", "src/Services/ProcessingService/ProcessingService.Infrastructure/"]
COPY ["src/Services/ProcessingService/ProcessingService.Worker/ProcessingService.Worker.csproj", "src/Services/ProcessingService/ProcessingService.Worker/"]

RUN dotnet restore "src/Services/ProcessingService/ProcessingService.Worker/ProcessingService.Worker.csproj"

# Copia o restante do código
COPY . .

WORKDIR /src/src/Services/ProcessingService/ProcessingService.Worker

RUN dotnet publish "ProcessingService.Worker.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# =========================
# RUNTIME STAGE
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends tzdata \
    && rm -rf /var/lib/apt/lists/*

ENV TZ=America/Sao_Paulo

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "ProcessingService.Worker.dll"]
