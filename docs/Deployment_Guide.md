# Deployment Guide

## Table of Contents
1. [Local Development](#local-development)
2. [Completely Free, No-Card Deployment](#completely-free-no-card-deployment)
3. [Free Deployment Options Comparison](#free-deployment-options-comparison)
4. [Recommended For Your Constraint](#recommended-for-your-constraint-self-hosted--cloudflare-tunnel)
5. [Environment Variables](#environment-variables)
6. [Troubleshooting](#troubleshooting)

---

## Prerequisites
- Docker and Docker Compose (for local testing)
- .NET 10 SDK only if you want to build outside Docker; Docker already uses .NET 10 images
- Git repository (GitHub/GitLab)
- Free account on chosen platform

## Local Development
```bash
docker-compose -f scripts/docker-compose.yml -f scripts/docker-compose.dev.yml up
```

---

## Completely Free, No-Card Deployment

If you do not have a credit card and want a deployment path with no subscription, no trial, and no hosted service signup requiring payment details, the practical option is to self-host on hardware you already own and expose it with a free tunnel.

### What this uses
- Your own PC, laptop, or spare home server
- Docker Compose for the API, PostgreSQL, Redis, and RabbitMQ
- Cloudflare Tunnel or Tailscale Funnel to publish the app publicly
- Cloudinary is optional because the API falls back to local uploads if those env vars are absent

### What this avoids
- No managed cloud account with billing attached
- No card-required free tier
- No paid subscription
- No trial expiration

### Roadmap

#### Phase 1: Prepare the host machine
1. Pick a machine that can stay on when the app should be available.
2. Install Docker Desktop.
3. Clone this repository.
4. Verify the stack starts locally with the existing compose files.

#### Phase 2: Start the stack locally
1. Bring up the API, PostgreSQL, Redis, and RabbitMQ.
2. Confirm the API is reachable on its local port.
3. Confirm Swagger and SignalR work on localhost.

#### Phase 3: Expose it publicly for free
1. Create a free Cloudflare account.
2. Install `cloudflared` on the host machine.
3. Create a tunnel to the local API port.
4. Map a public hostname to that tunnel.
5. Point the frontend to the public hostname.

#### Phase 4: Keep it running
1. Set Docker to start on boot.
2. Set the tunnel client to start on boot.
3. Back up the PostgreSQL volume regularly.
4. Watch logs, disk usage, and memory usage.

### Tradeoffs
- Completely free
- No card required
- Uses your current Docker-based architecture
- Availability depends on your own machine and internet connection
- Not ideal for a production service that must be online 24/7

---

## Free Deployment Options Comparison

| Platform | No card required | Best For | Database | Cache | Queue |
|----------|------------------|----------|----------|-------|-------|
| **Self-hosted + Cloudflare Tunnel** | ✅ Yes | Truly free personal deployment | ✅ PostgreSQL in Docker | ✅ Redis in Docker | ✅ RabbitMQ in Docker |
| **Railway.app** | ❌ No | Full-stack apps | ✅ PostgreSQL | ✅ Redis | ✅ RabbitMQ |
| **Render.com** | ❌ No | Simple apps | ✅ PostgreSQL | ✅ Redis | ⚠️ Manual |
| **Fly.io** | ❌ No | Containerized apps | ✅ PostgreSQL | ✅ Redis | ⚠️ Manual |
| **Azure Free** | ❌ No | Enterprise users | ✅ Supported | ✅ Supported | ✅ Supported |
| **GCP Free Tier** | ❌ No | Learning/testing | ✅ Supported | ✅ Supported | ✅ Supported |
| **Oracle Cloud** | ❌ No | Long-term projects | ✅ Supported | ✅ Supported | ✅ Supported |

---

## Recommended For Your Constraint: Self-Hosted + Cloudflare Tunnel

**Why this?** It is the only option that is fully free, requires no card, and works with your current architecture without a rewrite.

### Step-by-Step Roadmap

#### Phase 1: Preparation (30 minutes)

**1.1 Choose the host machine**
- Use a PC, laptop, or spare server that can stay on.
- Make sure Docker has enough disk and memory.
- Keep the machine on a stable network.

**1.2 Prepare the repository**
```bash
# Ensure your repo is clean and pushed
git add .
git commit -m "Prepare for self-hosted deployment"
git push origin main
```

**1.3 Verify local startup**
- Run the compose stack.
- Confirm the API starts without errors.
- Confirm chat and SignalR work locally before exposing anything.

#### Phase 2: Run the app locally (20 minutes)

**2.1 Bring up the containers**
- Start the API, PostgreSQL, Redis, and RabbitMQ containers.
- Keep the stack in one local Docker network.

**2.2 Confirm the endpoints**
- API should answer on localhost.
- Swagger should load.
- The chat client should connect and send messages.

#### Phase 3: Expose it publicly with a free tunnel (15 minutes)

**3.1 Create a free Cloudflare account**
- Cloudflare's free tunnel path normally does not require a card.
- If you already own a domain, point it there.
- If not, use the tunnel hostname for a test deployment.

**3.2 Install `cloudflared`**
- Install the tunnel client on the host machine.
- Authenticate it to your Cloudflare account.

**3.3 Create the tunnel**
- Route the public hostname to your local API port.
- Keep the tunnel as the public entry point.

**3.4 Update the app URLs**
- Point the frontend to the public tunnel URL.
- Keep service-to-service communication on the local Docker network.

#### Phase 4: Make it persistent (10 minutes)

**4.1 Start on boot**
- Configure Docker Desktop or Docker Engine to start with Windows.
- Configure the tunnel client to start with Windows.

**4.2 Protect the data**
- Back up the PostgreSQL volume.
- Store secrets locally outside the repo.

#### Phase 5: Keep it healthy (ongoing)

**5.1 Check it from another network**
- Open the public URL from a phone on mobile data.
- Confirm login, chat, reconnect, and refresh behavior.

**5.2 Watch the host**
- Monitor disk space.
- Monitor RAM usage.
- Check logs if chat disconnects.

---

## Environment Variables

Use local values instead of cloud dashboard variables:

```
ConnectionStrings__DefaultConnection = Host=postgres;Port=5432;Database=chatapp;Username=chatuser;Password=chatpassword;Include Error Detail=true
RedisSettings__ConnectionString = redis:6379
RabbitMqSettings__Host = rabbitmq
JwtSettings__Secret = YourStrongLocalSecretHere
ASPNETCORE_ENVIRONMENT = Production
ASPNETCORE_URLS = http://+:5000
```

## Troubleshooting

### Tunnel failures
- Check the tunnel client status.
- Restart `cloudflared` and confirm the hostname resolves.

### Docker failures
- Check container status with `docker ps`.
- Read logs for the API and dependencies.

### SignalR issues
- Confirm the tunnel uses HTTPS.
- Confirm the frontend points to the public hostname, not localhost.
- Confirm Redis is still running if you use any scale-out behavior.

### Database issues
- Confirm the PostgreSQL volume is mounted.
- Confirm the connection string matches your compose service name.
- Restore from backup if the local disk is damaged.
