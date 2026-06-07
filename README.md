# Reward Points System — Microservices in C# / .NET 8

A fully working reward points system built as microservices.
Use this project to **learn C# backend concepts** hands-on.

---

## Architecture Overview

```
                        ┌──────────────────────────────────────────┐
                        │           API GATEWAY : 5000             │
                        │     (YARP Reverse Proxy — single door)   │
                        └────┬──────┬──────┬──────┬───────────────┘
                             │      │      │      │
              ┌──────────────┘      │      │      └───────────────┐
              ▼                     ▼      ▼                       ▼
   ┌──────────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐
   │  auth-service    │  │ user-service │  │reward-service│  │wallet-service│
   │  :5001           │  │ :5002        │  │ :5003        │  │ :5004        │
   │  Login / Signup  │  │ Profiles &   │  │ Assign &     │  │ Virtual      │
   │  JWT tokens      │  │ point balance│  │ redeem points│  │ wallet $$$   │
   └──────┬───────────┘  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘
          │ PostgreSQL          │ PostgreSQL        │ PostgreSQL       │ PostgreSQL
          │ (authdb:5433)       │ (userdb:5434)     │ (rewarddb:5435) │ (walletdb:5436)
          └─────────────────────┴──────────┬────────┴──────────────────┘
                                           │ Events (async)
                                    ┌──────▼──────┐
                                    │  RabbitMQ   │
                                    │  :15672     │
                                    └──────┬──────┘
                                           │
                               ┌───────────▼───────────┐
                               │  notification-service  │
                               │  :5005                 │
                               │  Sends SMS/Email/Push  │
                               └────────────────────────┘
```

### How a request flows

1. **User** → sends request to `http://localhost:5000`
2. **API Gateway** → validates JWT, routes to the right service
3. **Service** → runs business logic, queries its own database
4. **Message Broker** → service publishes an event (e.g. `PointsAssigned`)
5. **Notification Service** → listens to the event, sends notification
6. **Response** → returned to the user

---

## Quick Start

### Prerequisites

| Tool | Version | Install |
|------|---------|---------|
| Docker Desktop | Latest | https://www.docker.com/products/docker-desktop |
| .NET SDK | 8.0+ | https://dotnet.microsoft.com/download |

### Run the system

```bash
# Clone / download the project, then:
cd reward-system

# Option A: Use the setup script (recommended for first run)
bash scripts/setup.sh

# Option B: Manual
docker compose up --build
```

All databases are created automatically. All services start automatically.

### Service URLs

| Service | URL | Purpose |
|---------|-----|---------|
| **API Gateway** | http://localhost:5000 | Your single entry point |
| Auth Service | http://localhost:5001/swagger | Login, register |
| User Service | http://localhost:5002/swagger | Profiles, balances |
| Reward Service | http://localhost:5003/swagger | Assign & redeem points |
| Wallet Service | http://localhost:5004/swagger | Virtual wallet |
| Notification | http://localhost:5005/swagger | Event consumer |
| **RabbitMQ UI** | http://localhost:15672 | guest / guest |

---

## Test the Full Flow (copy & paste)

### Step 1 — Register a user
```bash
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"alice@example.com","password":"pass123","firstName":"Alice","lastName":"Smith"}'
```

### Step 2 — Login and capture the token
```bash
TOKEN=$(curl -s -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"alice@example.com","password":"pass123"}' \
  | python3 -c "import sys,json; print(json.load(sys.stdin)['accessToken'])")

echo "Your token: $TOKEN"
```

### Step 3 — Create user profile (links auth → user service)
```bash
# Get your userId from the login response, then:
curl -X POST http://localhost:5002/api/users \
  -H "Content-Type: application/json" \
  -d '{"authUserId":"<your-user-id>","email":"alice@example.com","firstName":"Alice","lastName":"Smith"}'
```

### Step 4 — Assign points via a purchase
```bash
curl -X POST http://localhost:5000/api/rewards/assign \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"triggerEvent":"purchase","purchaseAmount":60.00}'
# Result: 600 points earned (10 pts per $1)
```

### Step 5 — Assign referral bonus
```bash
curl -X POST http://localhost:5000/api/rewards/assign \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"triggerEvent":"referral"}'
# Result: 500 bonus points
```

### Step 6 — Check your balance
```bash
curl http://localhost:5000/api/users/me/balance \
  -H "Authorization: Bearer $TOKEN"
# Should show 1100 points
```

### Step 7 — Redeem 500 points for $5.00
```bash
curl -X POST http://localhost:5000/api/rewards/redeem \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"pointsToRedeem":500}'
# Result: $5.00 credited to virtual wallet
```

### Step 8 — Check wallet balance
```bash
curl http://localhost:5000/api/wallets/me \
  -H "Authorization: Bearer $TOKEN"
# Should show $5.00 balance
```

### Step 9 — View transaction history
```bash
curl http://localhost:5000/api/rewards/transactions \
  -H "Authorization: Bearer $TOKEN"
```

### Step 10 — Watch notifications fire
```bash
# In a separate terminal, watch notification-service logs:
docker compose logs -f notification-service
```

---

## Project Structure

```
reward-system/
├── docker-compose.yml              ← Orchestrates everything
├── api-gateway/
│   ├── ApiGateway.csproj
│   ├── appsettings.json            ← YARP routing config
│   ├── Dockerfile
│   └── src/
│       └── Program.cs              ← Gateway setup
├── services/
│   ├── auth-service/               ← Login, signup, JWT
│   │   ├── AuthService.csproj
│   │   ├── Dockerfile
│   │   ├── appsettings.json
│   │   └── src/
│   │       ├── Program.cs
│   │       ├── controllers/AuthController.cs
│   │       ├── models/User.cs
│   │       ├── data/AuthDbContext.cs
│   │       ├── services/AuthService.cs
│   │       ├── services/TokenService.cs
│   │       └── dtos/AuthDtos.cs
│   ├── user-service/               ← Profiles & point balances
│   ├── reward-service/             ← Core logic: assign & redeem
│   ├── wallet-service/             ← Virtual wallet credits
│   └── notification-service/       ← Event-driven notifications
├── shared/
│   ├── Shared.csproj
│   └── Events/DomainEvents.cs      ← Event contracts (shared)
├── monitoring/
│   └── prometheus/prometheus.yml
└── scripts/
    ├── setup.sh                    ← First-time setup
    └── create-migrations.sh        ← EF Core migrations
```

---

<!-- ## C# Concepts Demonstrated — Where to Find Them

| Concept | File to look at |
|---------|-----------------|
| **async / await** | `AuthService.cs` — every method is async |
| **Interfaces + DI** | `TokenService.cs` — `ITokenService` → `TokenService` |
| **Records (immutable DTOs)** | `AuthDtos.cs` — `public record RegisterRequest(...)` |
| **Primary constructors** | `AuthDbContext.cs` — `(DbContextOptions options)` |
| **EF Core DbContext** | `AuthDbContext.cs`, `RewardDbContext.cs` |
| **LINQ queries** | `RewardService.cs` — `.Where().OrderByDescending().Select()` |
| **JWT generation** | `TokenService.cs` |
| **JWT validation middleware** | `Program.cs` in every service |
| **Typed HttpClient** | `HttpClients.cs` in reward-service |
| **MassTransit publish** | `RewardService.cs` — `await bus.Publish(...)` |
| **MassTransit consume** | `NotificationConsumers.cs` — `IConsumer<T>` |
| **Idempotency pattern** | `WalletService.cs` — duplicate key check |
| **Repository-style data access** | `UserProfileService.cs` |
| **Pattern matching** | `RewardService.cs` — `if (criteria is null)` |
| **Null-conditional operators** | Controllers — `claim?.Value` |
| **YARP Reverse Proxy config** | `api-gateway/appsettings.json` |

--- -->

## Reward Criteria (Earning Rules)

The system ships with three default rules (seeded automatically):

| Event | Points Awarded |
|-------|---------------|
| `purchase` | 10 points per $1 spent (minimum $1) |
| `referral` | 500 flat bonus points |
| `daily_login` | 10 flat bonus points |

**Redemption rate:** 100 points = $1.00 wallet credit

---

## Useful Commands

```bash
# Start everything
docker compose up --build

# Start in background
docker compose up -d --build

# Watch all logs
docker compose logs -f

# Watch one service
docker compose logs -f reward-service

# Stop everything (keeps database data)
docker compose down

# Stop and DELETE all data (fresh start)
docker compose down -v

# Rebuild one service after code change
docker compose up -d --build reward-service

# Open a shell inside a running container
docker compose exec reward-service sh
```

---

## Security Notes (for learning)

- JWT secret is hardcoded in docker-compose.yml — **never do this in production**
- In production: use environment secrets, Azure Key Vault, or AWS Secrets Manager
- The wallet credit endpoint (`POST /api/wallets/credit`) should be protected by
  an internal API key or network policy — not exposed via the gateway
- Passwords are hashed with BCrypt (work factor 11) — this is production-safe

---

## Learning Resources

1. **C# Basics** → Tim Corey on YouTube (free, .NET 8)
2. **Microservices in .NET** → Nick Chapsas — ".NET Microservices" full course
3. **Clean Architecture patterns** → Milan Jovanović on YouTube
4. **EF Core** → https://learn.microsoft.com/ef/core/
5. **MassTransit docs** → https://masstransit.io/documentation/