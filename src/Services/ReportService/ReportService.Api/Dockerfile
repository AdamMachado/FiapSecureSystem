# =========================
# BUILD STAGE
# =========================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copia os arquivos de projeto primeiro para aproveitar cache de restore
COPY ["src/Shared/Shared.Contracts/Shared.Contracts.csproj", "src/Shared/Shared.Contracts/"]
COPY ["src/Shared/Shared.Kernel/Shared.Kernel.csproj", "src/Shared/Shared.Kernel/"]
COPY ["src/Shared/Shared.Observability/Shared.Observability.csproj", "src/Shared/Shared.Observability/"]
COPY ["src/Shared/Shared.Security/Shared.Security.csproj", "src/Shared/Shared.Security/"]

COPY ["src/Services/ReportService/ReportService.Domain/ReportService.Domain.csproj", "src/Services/ReportService/ReportService.Domain/"]
COPY ["src/Services/ReportService/ReportService.Application/ReportService.Application.csproj", "src/Services/ReportService/ReportService.Application/"]
COPY ["src/Services/ReportService/ReportService.Infrastructure/ReportService.Infrastructure.csproj", "src/Services/ReportService/ReportService.Infrastructure/"]
COPY ["src/Services/ReportService/ReportService.Api/ReportService.Api.csproj", "src/Services/ReportService/ReportService.Api/"]

RUN dotnet restore "src/Services/ReportService/ReportService.Api/ReportService.Api.csproj"

# Copia o restante do código
COPY . .

WORKDIR /src/src/Services/ReportService/ReportService.Api
RUN dotnet publish "ReportService.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# =========================
# RUNTIME STAGE
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "ReportService.Api.dll"]
