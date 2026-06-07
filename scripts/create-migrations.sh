#!/bin/bash
# ─────────────────────────────────────────────────────────────────────────────
# Run this script to create EF Core migrations for all services.
# Run from the root of the project: bash scripts/create-migrations.sh
#
# CONCEPT: EF Core migrations are C# files that describe database changes.
# "dotnet ef migrations add" generates the migration.
# "dotnet ef database update" applies it.
# In this project, migrations are applied automatically on startup (db.Database.Migrate()).
# ─────────────────────────────────────────────────────────────────────────────

set -e

echo "Creating EF Core migrations..."

# Auth Service
echo "► auth-service..."
cd services/auth-service
dotnet ef migrations add InitialCreate \
  --project AuthService.csproj \
  --startup-project AuthService.csproj \
  --output-dir src/data/Migrations
cd ../..

# User Service
echo "► user-service..."
cd services/user-service
dotnet ef migrations add InitialCreate \
  --project UserService.csproj \
  --startup-project UserService.csproj \
  --output-dir src/data/Migrations
cd ../..

# Reward Service
echo "► reward-service..."
cd services/reward-service
dotnet ef migrations add InitialCreate \
  --project RewardService.csproj \
  --startup-project RewardService.csproj \
  --output-dir src/data/Migrations
cd ../..

# Wallet Service
echo "► wallet-service..."
cd services/wallet-service
dotnet ef migrations add InitialCreate \
  --project WalletService.csproj \
  --startup-project WalletService.csproj \
  --output-dir src/data/Migrations
cd ../..

echo "✅ All migrations created!"
echo ""
echo "NOTE: Migrations are applied automatically when each service starts."
echo "You do NOT need to run 'dotnet ef database update' manually."
