# Quick Deployment Guide - No Card Needed

This is the shortest path to a public URL with no subscription, no trial, and no credit card: **self-host the app on your own machine and expose it with a free tunnel**.

---

## ✅ Fastest Free Path

### What you need
- A Windows PC, laptop, or spare home server you can leave on
- Docker Desktop installed
- Your existing repository cloned locally
- A free Cloudflare account, or Tailscale account if you prefer Funnel

### 10-Minute Setup

1. **Start the stack locally**
   ```bash
   docker-compose -f scripts/docker-compose.yml up
   ```

2. **Verify the app works on localhost**
   - Open the API in your browser
   - Confirm Swagger loads
   - Send a test chat message

3. **Install a free tunnel client**
   - Use Cloudflare Tunnel (`cloudflared`) or Tailscale Funnel

4. **Expose your local API**
   - Point the tunnel to your local API port
   - Publish the public hostname

5. **Keep it running**
   - Leave the machine on
   - Make Docker and the tunnel start with Windows

---

## 🔧 Local Environment Variables

Use these values for the local Docker network:

```env
ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=chatapp;Username=chatuser;Password=chatpassword;Include Error Detail=true
RedisSettings__ConnectionString=redis:6379
RabbitMqSettings__Host=rabbitmq
JwtSettings__Secret=CreateAStrongSecretHere123!@#
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:5000
```

---

## 🌐 Public Access Options

| Option | Card Required | Cost | Notes |
|--------|---------------|------|-------|
| **Cloudflare Tunnel** | No | Free | Best choice for a public URL |
| **Tailscale Funnel** | No | Free for personal use | Good if you already use Tailscale |
| Railway / Render / Fly.io | Usually yes | Not fully free | Not a fit for your requirement |

---

## ✅ Mini Checklist

- [ ] Docker stack starts locally
- [ ] Swagger works on localhost
- [ ] SignalR chat works on localhost
- [ ] Tunnel points to the API port
- [ ] Public URL loads from another network
- [ ] PostgreSQL data is backed up

---

## ⚠️ Important

This is free and requires no card, but it is **self-hosted**. If your machine is off, the app is offline.

If you want, the next step is a precise Windows setup for Cloudflare Tunnel with commands you can copy and run.
