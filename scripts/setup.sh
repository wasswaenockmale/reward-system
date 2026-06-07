#!/bin/bash
# ─────────────────────────────────────────────────────────────────────────────
# This script sets up the development environment for the Reward System.
# ─────────────────────────────────────────────────────────────────────────────

set -e  # Exit on any error

GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${BLUE}======================================${NC}"
echo -e "${BLUE}   Reward System — Local Setup        ${NC}"
echo -e "${BLUE}======================================${NC}"

# Check Docker is running
if ! docker info > /dev/null 2>&1; then
  echo "Docker is not running. Please start Docker Desktop and try again."
  exit 1
fi

echo -e "\n${GREEN} Docker is running${NC}"

# Check docker compose (v2 syntax)
if ! docker compose version > /dev/null 2>&1; then
  echo "Docker Compose v2 not found. Please update Docker Desktop."
  exit 1
fi

echo -e "${GREEN} Docker Compose is available${NC}"

echo -e "\n${YELLOW} Step 1: Pulling base images (first run may take a few minutes)...${NC}"
docker compose pull --ignore-buildable 2>/dev/null || true

echo -e "\n${YELLOW} Step 2: Building all service images...${NC}"
docker compose build --parallel

echo -e "\n${YELLOW} Step 3: Starting infrastructure (databases + RabbitMQ)...${NC}"
docker compose up -d postgres-auth postgres-user postgres-reward postgres-wallet rabbitmq

echo -e "\n${YELLOW} Waiting for databases to be healthy...${NC}"
sleep 8

echo -e "\n${YELLOW} Step 4: Starting all services...${NC}"
docker compose up -d

echo -e "\n${YELLOW} Waiting for services to start...${NC}"
sleep 10

echo -e "\n${GREEN}======================================${NC}"
echo -e "${GREEN}   System is up!                   ${NC}"
echo -e "${GREEN}======================================${NC}"

echo -e "\n ${BLUE}Service URLs:${NC}"
echo -e "  API Gateway     →  http://localhost:5000"
echo -e "  Auth Service    →  http://localhost:5001/swagger"
echo -e "  User Service    →  http://localhost:5002/swagger"
echo -e "  Reward Service  →  http://localhost:5003/swagger"
echo -e "  Wallet Service  →  http://localhost:5004/swagger"
echo -e "  Notification    →  http://localhost:5005/swagger"
echo -e "  RabbitMQ UI     →  http://localhost:15672  (guest / guest)"

echo -e "\n ${BLUE}Quick test (copy & paste):${NC}"
echo -e '  # 1. Register a user'
echo -e '  curl -X POST http://localhost:5000/api/auth/register \'
echo -e '    -H "Content-Type: application/json" \'
echo -e '    -d '"'"'{"email":"test@example.com","password":"pass123","firstName":"John","lastName":"Doe"}'"'"

echo -e '\n  # 2. Login and save token'
echo -e '  TOKEN=$(curl -s -X POST http://localhost:5000/api/auth/login \'
echo -e '    -H "Content-Type: application/json" \'
echo -e '    -d '"'"'{"email":"test@example.com","password":"pass123"}'"'"' | python3 -c "import sys,json; print(json.load(sys.stdin)['"'"'accessToken'"'"'])")'

echo -e '\n  # 3. Assign points (purchase trigger)'
echo -e '  curl -X POST http://localhost:5000/api/rewards/assign \'
echo -e '    -H "Authorization: Bearer $TOKEN" \'
echo -e '    -H "Content-Type: application/json" \'
echo -e '    -d '"'"'{"triggerEvent":"purchase","purchaseAmount":60.00}'"'"

echo -e '\n  # 4. Redeem 500 points for $5.00 wallet credit'
echo -e '  curl -X POST http://localhost:5000/api/rewards/redeem \'
echo -e '    -H "Authorization: Bearer $TOKEN" \'
echo -e '    -H "Content-Type: application/json" \'
echo -e '    -d '"'"'{"pointsToRedeem":500}'"'"

echo -e "\n To stop everything: ${YELLOW}docker compose down${NC}"
echo -e "🗑  To wipe databases:  ${YELLOW}docker compose down -v${NC}\n"
