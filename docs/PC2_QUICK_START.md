# 🚀 Quick Start - PC2 (Classmate) Setup

## Prerequisites
- [ ] Both PCs connected to same lab network (WiFi or Ethernet)
- [ ] Docker Desktop installed on PC2
- [ ] Repository cloned on PC2

---

## Step 1: Get PC1's IP Address

**Ask your classmate to run on their PC:**
```powershell
ipconfig
```

Find `IPv4 Address` → something like `192.168.1.100`

Write it down: **PC1_IP = _______________**

---

## Step 2: Test Network Connection

Run on PC2:
```powershell
ping 192.168.1.100
```

Should show: `Reply from 192.168.1.100: bytes=32 time=5ms`

If fails → Ask classmate to check WiFi/Network

---

## Step 3: Update docker-compose.pc2-replica.yml

Open file: `scripts/docker-compose.pc2-replica.yml`

**Find all `PC1_IP` and replace with PC1's actual IP**

Search: `PC1_IP` → Replace: `192.168.1.100`

**Lines to change:**
- Line ~30: `ConnectionStrings__DefaultConnection=Host=PC1_IP;...`
- Line ~43: `RabbitMqSettings__Host=PC1_IP`
- Line ~74: `command:` section: `pg_basebackup -h PC1_IP`

---

## Step 4: Start PC2

```bash
cd path\to\Real-Time-Chat-App\ChatApplication

docker-compose -f scripts/docker-compose.pc2-replica.yml up -d
```

Wait 2-3 minutes for database replication to initialize.

---

## Step 5: Verify It Works

Check if containers are running:
```bash
docker-compose -f scripts/docker-compose.pc2-replica.yml ps
```

Open browser: `http://localhost:5000`

---

## Step 6: Test Sync

1. **Create a message on PC1** (ask classmate)
   - Example: "Hello from PC1"

2. **Open PC2's Blazor app** and refresh
   - Should see the message instantly

3. **Send message on PC2**
   - Example: "Hello from PC2"

4. **Check PC1's Blazor app**
   - Should see PC2's message instantly ✅

---

## Troubleshooting

| Problem | Solution |
|---------|----------|
| Containers won't start | Check docker-compose.pc2-replica.yml has correct PC1_IP |
| Can't connect to PC1 | Ping test failed? Check WiFi/network |
| Messages not syncing | Wait 2 more minutes, then restart: `docker-compose -f scripts/docker-compose.pc2-replica.yml down && docker-compose -f scripts/docker-compose.pc2-replica.yml up -d` |
| Port 5000 already in use | Change to `"5001:5000"` in docker-compose |

---

## Stop When Done

```bash
docker-compose -f scripts/docker-compose.pc2-replica.yml down
```

---

**Questions?** Check `DISTRIBUTED_SETUP.md` for full guide!
