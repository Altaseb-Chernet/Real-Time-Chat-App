# Deployment Guide

## Table of Contents
1. [Local Development](#local-development)
2. [Free Deployment Options](#free-deployment-options)
3. [Recommended: Railway.app](#recommended-railwayapp)
4. [Alternative: Render.com](#alternative-rendercom)
5. [Alternative: Fly.io](#alternative-flyio)
6. [Environment Variables](#environment-variables)
7. [Troubleshooting](#troubleshooting)

---

## Prerequisites
- Docker and Docker Compose (for local testing)
- .NET 8 SDK (for local development)
- Git repository (GitHub/GitLab)
- Free account on chosen platform

## Local Development
```bash
docker-compose -f scripts/docker-compose.yml -f scripts/docker-compose.dev.yml up
```

---

## Free Deployment Options Comparison

| Platform | Free Tier | Best For | Database | Cache | Queue |
|----------|-----------|----------|----------|-------|-------|
| **Railway.app** | $5/month credits | Full-stack apps | ✅ PostgreSQL | ✅ Redis | ✅ RabbitMQ |
| **Render.com** | Limited free | Simple apps | ✅ PostgreSQL | ✅ Redis | ⚠️ Manual |
| **Fly.io** | $3/month + credits | Containerized apps | ✅ PostgreSQL | ✅ Redis | ⚠️ Manual |
| **Azure Free** | $200 for 30 days | Enterprise users | ✅ Supported | ✅ Supported | ✅ Supported |
| **GCP Free Tier** | Always free (limits) | Learning/testing | ✅ Supported | ✅ Supported | ✅ Supported |
| **Oracle Cloud** | Always free (2 VM) | Long-term projects | ✅ Supported | ✅ Supported | ✅ Supported |

---

## Recommended: Railway.app

**Why Railway?** Best free tier for full-stack apps, easy deployment, supports all your dependencies.

### Step-by-Step Roadmap

#### Phase 1: Preparation (30 minutes)

**1.1 Create Railway Account**
- Visit https://railway.app
- Sign up with GitHub account (recommended for easy integration)
- Connect your GitHub repository

**1.2 Prepare Repository**
```bash
# Ensure your repo is clean and pushed
git add .
git commit -m "Prepare for Railway deployment"
git push origin main
```

**1.3 Add Railway Configuration Files**

Create `.railway/config.yml` in your project root:
```yaml
builder: nixpacks
variables:
  - name: NIXPACKS_BUILD_CMD
    value: "dotnet publish -c Release -o /app/publish"
```

Create `Dockerfile` if not present (or update the existing one):
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY ["src/ChatApplication.API/ChatApplication.API.csproj", "src/ChatApplication.API/"]
COPY ["src/ChatApplication.Core/ChatApplication.Core.csproj", "src/ChatApplication.Core/"]
COPY ["src/ChatApplication.Infrastructure/ChatApplication.Infrastructure.csproj", "src/ChatApplication.Infrastructure/"]
COPY ["src/ChatApplication.Shared/ChatApplication.Shared.csproj", "src/ChatApplication.Shared/"]
COPY ["src/ChatApplication.Client/ChatApplication.Client.csproj", "src/ChatApplication.Client/"]

RUN dotnet restore "src/ChatApplication.API/ChatApplication.API.csproj"

COPY . .
RUN dotnet publish "src/ChatApplication.API/ChatApplication.API.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "ChatApplication.API.dll"]
```

#### Phase 2: Deploy Services (20 minutes)

**2.1 Log in to Railway Dashboard**
- Go to https://railway.app/dashboard
- Click "New Project"

**2.2 Add PostgreSQL Database**
- Click "+ Add"
- Select "PostgreSQL"
- Railway auto-generates connection string (save it)

**2.3 Add Redis**
- Click "+ Add"
- Select "Redis"
- Railway auto-generates connection string

**2.4 Add RabbitMQ**
- Click "+ Add"
- Select "RabbitMQ"
- Railway auto-generates connection

**2.5 Deploy Your App**
- Click "+ Add"
- Select "GitHub Repo"
- Select your ChatApplication repo
- Choose deployment branch (main)
- Railway auto-detects .NET project

#### Phase 3: Configure Environment Variables (10 minutes)

**3.1 In Railway Dashboard, set these variables:**

```
ConnectionStrings__DefaultConnection = ${{ Postgres.DATABASE_URL }}
RedisSettings__ConnectionString = ${{ Redis.REDIS_URL }}
RabbitMqSettings__Host = ${{ RabbitMQ.RABBITMQ_HOST }}
RabbitMqSettings__Username = ${{ RabbitMQ.RABBITMQ_DEFAULT_USER }}
RabbitMqSettings__Password = ${{ RabbitMQ.RABBITMQ_DEFAULT_PASS }}
JwtSettings__Secret = YourSecureJwtSecretHere123!@#
ASPNETCORE_ENVIRONMENT = Production
ASPNETCORE_URLS = http://+:8080
```

**3.2 Verify Service Links**
- Railway automatically injects environment variables
- Services automatically discover each other

#### Phase 4: Deploy & Monitor (Automatic)

**4.1 Deployment**
- Push to your repo → Railway auto-builds & deploys
- Monitor logs in Railway dashboard
- Get public URL (e.g., `https://chat-app-prod.up.railway.app`)

**4.2 Database Migration**
```bash
# Connect via Railway CLI
railway run dotnet ef database update
```

**4.3 Access Your App**
- Frontend: `https://your-railway-url.up.railway.app`
- Swagger API: `https://your-railway-url.up.railway.app/swagger`

---

## Alternative: Render.com

**Why Render?** Easy free tier, good documentation, simple deployment.

### Deployment Roadmap (45 minutes)

#### Step 1: Account Setup
- Visit https://render.com
- Sign up with GitHub
- Connect repository

#### Step 2: Deploy PostgreSQL
- Create → PostgreSQL Database
- Name: `chat-db`
- Plan: Free tier
- Copy connection string

#### Step 3: Deploy Redis
- Create → Redis
- Name: `chat-redis`
- Plan: Free tier
- Copy connection string

#### Step 4: Deploy Web Service
- Create → Web Service
- Connect to your GitHub repo
- Build command: `dotnet publish -c Release -o /app/publish`
- Start command: `dotnet ChatApplication.API.dll`

#### Step 5: Environment Variables
Add in Render dashboard:
```
ConnectionStrings__DefaultConnection=your-postgres-url
RedisSettings__ConnectionString=your-redis-url
JwtSettings__Secret=YourSecret123!
ASPNETCORE_ENVIRONMENT=Production
```

#### Step 6: Deploy
- Click "Deploy"
- Monitor logs
- Get public URL

---

## Alternative: Fly.io

**Why Fly.io?** Excellent for containerized apps, global deployment, good free credits.

### Deployment Roadmap (1 hour)

#### Step 1: Setup
```bash
# Install Fly CLI
curl https://fly.io/install.sh | sh

# Login
fly auth login

# Create app
fly launch --name chat-app --builder dockerfile
```

#### Step 2: Configure fly.toml
```toml
app = "chat-app"
primary_region = "iad"

[build]
  dockerfile = "Dockerfile"

[env]
  ASPNETCORE_ENVIRONMENT = "Production"
  ASPNETCORE_URLS = "http://+:8080"

[[services]]
  protocol = "tcp"
  internal_port = 8080
  processes = ["app"]
  
  [services.concurrency]
    type = "connections"
    hard_limit = 1000
    soft_limit = 500

  [[services.ports]]
    port = 80
    handlers = ["http"]
    force_https = true

  [[services.ports]]
    port = 443
    handlers = ["tls", "http"]
```

#### Step 3: Add Services with Fly Postgres
```bash
# Create PostgreSQL
fly postgres create --name chat-db

# Create Redis
fly redis create --name chat-redis
```

#### Step 4: Link Services
```bash
fly postgres attach chat-db
fly redis attach chat-redis
```

#### Step 5: Set Secrets
```bash
fly secrets set JwtSettings__Secret="YourSecret123!"
fly secrets set RabbitMqSettings__Host="rabbitmq-service"
```

#### Step 6: Deploy
```bash
fly deploy
fly status
fly logs
```

---

## Free Tier Limitations & Costs

| Platform | Free Tier | Auto-Sleep? | Notes |
|----------|-----------|------------|-------|
| Railway | $5/month | No | Enough for small project for ~2-3 months |
| Render | Very limited | Yes (after 15 min) | Great for demo/testing |
| Fly.io | $3/month + credits | No | Best for long-term |
| Azure | $200/30 days | No | Expires after 30 days |
| GCP | Always free tier | No | Limited resources |
| Oracle Cloud | 2 VMs always free | No | Best for VMs |

---

## Production Checklist

Before deploying to production:

- [ ] Update `appsettings.Production.json`
- [ ] Set strong JWT secret
- [ ] Enable HTTPS/SSL
- [ ] Configure CORS properly
- [ ] Set up database backups
- [ ] Configure logging/monitoring
- [ ] Test SignalR connections
- [ ] Test real-time chat functionality
- [ ] Set up CI/CD pipeline
- [ ] Monitor resource usage

---

## Environment Variables

### Required Variables
| Variable | Description | Example |
|----------|-------------|---------|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string | `Server=localhost;Database=chat;` |
| `JwtSettings__Secret` | JWT signing secret (min 32 chars) | `YourVerySecureSecretKey123!@#$%` |
| `RedisSettings__ConnectionString` | Redis connection string | `localhost:6379` |
| `RabbitMqSettings__Host` | RabbitMQ hostname | `localhost` |
| `ASPNETCORE_ENVIRONMENT` | Environment name | `Production` |
| `ASPNETCORE_URLS` | URL binding | `http://+:8080` |

### Optional Variables
| Variable | Description | Default |
|----------|-------------|---------|
| `RabbitMqSettings__Username` | RabbitMQ username | `guest` |
| `RabbitMqSettings__Password` | RabbitMQ password | `guest` |
| `RedisSettings__Password` | Redis password | (empty) |
| `Logging__LogLevel__Default` | Log level | `Information` |

---

## Troubleshooting

### Build Failures
```bash
# Check .NET version compatibility
dotnet --version  # Should be 8.0+

# Clear build cache
dotnet clean
rm -rf bin obj

# Try local build first
dotnet publish -c Release
```

### Database Connection Issues
```bash
# Verify connection string format
# PostgreSQL: Server=host;Port=5432;Database=chat;Username=user;Password=pass;

# Test connection
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "your-connection-string"
dotnet ef database validate
```

### SignalR Real-Time Issues
- Ensure WebSocket support is enabled on hosting platform
- Check CORS configuration allows SignalR connections
- Verify Redis is properly configured for Scale-Out

### Memory/Resource Issues
- Monitor deployment dashboard
- Upgrade to paid tier if hitting limits
- Consider splitting services into microservices

---

## Recommended Deployment Path for This Project

**For Development/Demo:** Render.com (easiest, free tier)
**For Production:** Railway.app ($5/month covers full stack)
**For Enterprise:** Azure (with initial credits) or GCP

Start with Render for testing, migrate to Railway when ready for production.
