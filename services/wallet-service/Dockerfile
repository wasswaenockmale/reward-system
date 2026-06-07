FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY shared/ shared/
COPY services/wallet-service/ services/wallet-service/
WORKDIR /src/services/wallet-service
RUN dotnet restore WalletService.csproj
RUN dotnet publish WalletService.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "WalletService.dll"]
