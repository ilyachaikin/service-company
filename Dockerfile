# ── Stage 1: сборка фронтенда ──────────────────────────────────────────────
FROM node:20-alpine AS frontend

# WORKDIR должен совпадать со структурой проекта,
# чтобы vite outDir (../ServiceCompany.Api/wwwroot) разрешился правильно
WORKDIR /app/src/ServiceCompany.Frontend

COPY src/ServiceCompany.Frontend/package*.json ./
RUN npm install

COPY src/ServiceCompany.Frontend/ ./
RUN chmod +x node_modules/.bin/* && npm run build
# → файлы окажутся в /app/src/ServiceCompany.Api/wwwroot/


# ── Stage 2: сборка бэкенда ────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /app

COPY ServiceCompany.sln ./
COPY src/ ./src/

# Кладём собранный фронтенд в wwwroot бэкенда
COPY --from=frontend /app/src/ServiceCompany.Api/wwwroot ./src/ServiceCompany.Api/wwwroot/

RUN dotnet publish src/ServiceCompany.Api/ServiceCompany.Api.csproj \
    -c Release \
    -o /publish \
    --no-self-contained


# ── Stage 3: финальный образ (только runtime, без SDK) ─────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

WORKDIR /app

COPY --from=build /publish ./

# Папка для загружаемых файлов (акты выполненных работ)
RUN mkdir -p /app/uploads

EXPOSE 8888
ENV ASPNETCORE_URLS=http://+:8888
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "ServiceCompany.Api.dll"]
