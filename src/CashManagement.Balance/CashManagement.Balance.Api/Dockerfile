# syntax=docker/dockerfile:1
# Dockerfile multi-stage do serviço de Consolidado (Balance).
# Contexto de build esperado: a RAIZ do repositório, pois a Api referencia os
# projetos Application/Infrastructure e o Directory.Build.props (que fixa net10.0
# e a qualidade) vive na raiz e é importado por MSBuild ao subir a árvore.

ARG SERVICE_DIR=src/CashManagement.Balance

# --- build ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG SERVICE_DIR
WORKDIR /src

# Restaura primeiro só os csproj + props para aproveitar o cache de camadas.
COPY ["Directory.Build.props", "./"]
COPY ["${SERVICE_DIR}/CashManagement.Balance.Api/CashManagement.Balance.Api.csproj", "service/CashManagement.Balance.Api/"]
COPY ["${SERVICE_DIR}/CashManagement.Balance.Application/CashManagement.Balance.Application.csproj", "service/CashManagement.Balance.Application/"]
COPY ["${SERVICE_DIR}/CashManagement.Balance.Infrastructure/CashManagement.Balance.Infrastructure.csproj", "service/CashManagement.Balance.Infrastructure/"]
RUN dotnet restore "service/CashManagement.Balance.Api/CashManagement.Balance.Api.csproj"

# Copia o restante do código do serviço e publica.
COPY ["${SERVICE_DIR}/", "service/"]
RUN dotnet publish "service/CashManagement.Balance.Api/CashManagement.Balance.Api.csproj" \
    -c Release -o /app/publish /p:UseAppHost=false

# --- runtime ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# curl para o healthcheck do compose (a imagem aspnet é enxuta e não o traz).
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# A imagem aspnet:10.0 já traz o usuário non-root "app".
USER app
ENV ASPNETCORE_URLS=http://+:8081
EXPOSE 8081

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CashManagement.Balance.Api.dll"]
