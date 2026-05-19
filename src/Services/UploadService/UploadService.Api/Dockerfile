# =========================
# BUILD STAGE
# =========================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copia os arquivos de projeto primeiro para aproveitar cache de restore
COPY ["src/Shared/Shared.Contracts/Shared.Contracts.csproj", "src/Shared/Shared.Contracts/"]
COPY ["src/Shared/Shared.Kernel/Shared.Kernel.csproj", "src/Shared/Shared.Kernel/"]
COPY ["src/Shared/Shared.Observability/Shared.Observability.csproj", "src/Shared/Shared.Observability/"]

COPY ["src/Services/UploadService/UploadService.Domain/UploadService.Domain.csproj", "src/Services/UploadService/UploadService.Domain/"]
COPY ["src/Services/UploadService/UploadService.Application/UploadService.Application.csproj", "src/Services/UploadService/UploadService.Application/"]
COPY ["src/Services/UploadService/UploadService.Infrastructure/UploadService.Infrastructure.csproj", "src/Services/UploadService/UploadService.Infrastructure/"]
COPY ["src/Services/UploadService/UploadService.Api/UploadService.Api.csproj", "src/Services/UploadService/UploadService.Api/"]

RUN dotnet restore "src/Services/UploadService/UploadService.Api/UploadService.Api.csproj"

# Copia o restante do código
COPY . .

WORKDIR /src/src/Services/UploadService/UploadService.Api
RUN dotnet publish "UploadService.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# =========================
# RUNTIME STAGE
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

RUN apt-get update \
    && apt-get install -y --no-install-recommends tzdata \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:8080
ENV TZ=America/Sao_Paulo

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "UploadService.Api.dll"]
