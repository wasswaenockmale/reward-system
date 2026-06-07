# CONCEPT: Multi-stage Docker build.
# Stage 1 (sdk): compiles the app — uses the large SDK image.
# Stage 2 (runtime): runs the app — uses the small runtime image.
# Final image is ~200MB instead of ~800MB.

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies first (Docker layer caching)
COPY AuthService.csproj .
RUN dotnet restore

# Copy everything else and build
COPY . .
RUN dotnet publish -c Release -o /app/publish

# ── Runtime stage ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Expose port 8080 (ASP.NET Core default in .NET 8)
EXPOSE 8080

ENTRYPOINT ["dotnet", "AuthService.dll"]
