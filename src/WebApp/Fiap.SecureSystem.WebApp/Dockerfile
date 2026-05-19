# =========================
# BUILD STAGE
# =========================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copia os arquivos de projeto primeiro para aproveitar cache de restore
COPY ["src/WebApp/Fiap.SecureSystem.WebApp/Fiap.SecureSystem.WebApp.csproj", "src/WebApp/Fiap.SecureSystem.WebApp/"]

RUN dotnet restore "src/WebApp/Fiap.SecureSystem.WebApp/Fiap.SecureSystem.WebApp.csproj"

# Copia o restante do código
COPY . .

WORKDIR /src/src/WebApp/Fiap.SecureSystem.WebApp
RUN dotnet publish "Fiap.SecureSystem.WebApp.csproj" -c Release -o /app/publish /p:UseAppHost=false

# =========================
# RUNTIME STAGE
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends tzdata \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:8080
ENV TZ=America/Sao_Paulo

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Fiap.SecureSystem.WebApp.dll"]
