# =========================
# BUILD STAGE
# =========================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Shared/Shared.Contracts/Shared.Contracts.csproj", "src/Shared/Shared.Contracts/"]
COPY ["src/Shared/Shared.Kernel/Shared.Kernel.csproj", "src/Shared/Shared.Kernel/"]
COPY ["src/Shared/Shared.Observability/Shared.Observability.csproj", "src/Shared/Shared.Observability/"]
COPY ["src/Shared/Shared.Security/Shared.Security.csproj", "src/Shared/Shared.Security/"]

COPY ["src/Services/IdentityService/IdentityService.Domain/IdentityService.Domain.csproj", "src/Services/IdentityService/IdentityService.Domain/"]
COPY ["src/Services/IdentityService/IdentityService.Application/IdentityService.Application.csproj", "src/Services/IdentityService/IdentityService.Application/"]
COPY ["src/Services/IdentityService/IdentityService.Infrastructure/IdentityService.Infrastructure.csproj", "src/Services/IdentityService/IdentityService.Infrastructure/"]
COPY ["src/Services/IdentityService/IdentityService.Api/IdentityService.Api.csproj", "src/Services/IdentityService/IdentityService.Api/"]

RUN dotnet restore "src/Services/IdentityService/IdentityService.Api/IdentityService.Api.csproj"

COPY . .

WORKDIR /src/src/Services/IdentityService/IdentityService.Api
RUN dotnet publish "IdentityService.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

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

ENTRYPOINT ["dotnet", "IdentityService.Api.dll"]
