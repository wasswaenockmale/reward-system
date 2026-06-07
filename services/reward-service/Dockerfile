# Build context: repo root (set in docker-compose.yml)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy shared project first (referenced by reward-service)
COPY shared/ shared/

# Copy reward-service
COPY services/reward-service/ services/reward-service/

WORKDIR /src/services/reward-service
RUN dotnet restore RewardService.csproj
RUN dotnet publish RewardService.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "RewardService.dll"]
