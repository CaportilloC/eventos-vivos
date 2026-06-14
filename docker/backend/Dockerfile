# ──────────────────────────────────────────────────────────────
# Stage 1: Build / Publish
# ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /build

# Copy project files first (layer caching for restore)
COPY src/backend/EventosVivos.Api/EventosVivos.Api.csproj   src/backend/EventosVivos.Api/
COPY src/backend/EventosVivos.Application/EventosVivos.Application.csproj   src/backend/EventosVivos.Application/
COPY src/backend/EventosVivos.Domain/EventosVivos.Domain.csproj   src/backend/EventosVivos.Domain/
COPY src/backend/EventosVivos.Infrastructure/EventosVivos.Infrastructure.csproj   src/backend/EventosVivos.Infrastructure/
COPY tests/backend/EventosVivos.Tests/EventosVivos.Tests.csproj   tests/backend/EventosVivos.Tests/

RUN dotnet restore src/backend/EventosVivos.Api/EventosVivos.Api.csproj

# Copy all source and publish
COPY . .
WORKDIR /build/src/backend/EventosVivos.Api
RUN dotnet publish -c Release -o /app --no-restore

# ──────────────────────────────────────────────────────────────
# Stage 2: Runtime
# ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "EventosVivos.Api.dll"]
