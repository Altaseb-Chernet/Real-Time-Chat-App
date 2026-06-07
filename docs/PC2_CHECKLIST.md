# ✅ PC2 (Classmate) - What You Need & How to Verify

## What PC2 Needs (Prerequisites)

```
┌─────────────────────────────────────────────┐
│ MUST HAVE on PC2:                           │
├─────────────────────────────────────────────┤
│ ✅ Docker Desktop (same as PC1)             │
│    Download: docker.com                    │
│    Must be: Running (check system tray)    │
│                                             │
│ ✅ Git (to clone repository)                │
│    Or: Just copy folder from PC1 (USB)     │
│                                             │
│ ✅ Internet Browser (Chrome/Edge)           │
│    To open: http://localhost:5000          │
│                                             │
│ ✅ Terminal/PowerShell                      │
│    Already on Windows                      │
│                                             │
│ ✅ PC1's IP Address                         │
│    Ask PC1: ipconfig → IPv4 Address        │
│    Example: 192.168.1.100                  │
│                                             │
│ ✅ Network Connection                       │
│    Both PCs on SAME lab WiFi/Ethernet      │
│    NOT different networks!                 │
│                                             │
└─────────────────────────────────────────────┘
```

---

## Step 0: Check Network Connection First

**On PC2, verify you can reach PC1:**

```powershell
ping 192.168.1.100
```

**Expected output:**
```
Reply from 192.168.1.100: bytes=32 time=5ms TTL=64
Reply from 192.168.1.100: bytes=32 time=4ms TTL=64
Reply from 192.168.1.100: bytes=32 time=5ms TTL=64

Ping statistics:
    Packets: Sent = 3, Received = 3, Lost = 0 (0% loss)
```

**If you see this:** ✅ Network is good!

**If connection times out:** ❌ Check:
- Same WiFi network?
- Firewall blocking?
- PC1 is on?

---

## Step 1: Have Repository on PC2

**Option A: Clone from GitHub**
```bash
git clone https://github.com/Altaseb-Chernet/Real-Time-Chat-App.git
cd Real-Time-Chat-App/ChatApplication
```

**Option B: Copy from PC1**
- Plug USB into PC1
- Copy entire `ChatApplication` folder
- Plug USB into PC2
- Paste folder

Both work! Cloning is faster if internet is good.

---

## Step 2: Update Connection Strings (CRITICAL!)

**Open file:** `scripts/docker-compose.pc2-replica.yml`

**Find and replace ALL occurrences of `PC1_IP` with PC1's actual IP**

### Method 1: VS Code (Easiest)

```
1. Open VS Code
2. Open file: scripts/docker-compose.pc2-replica.yml
3. Press: Ctrl+H (Find and Replace)
4. Find: PC1_IP
5. Replace with: 192.168.1.100 (actual IP)
6. Click: Replace All
7. Save: Ctrl+S
```

### Method 2: Command Line

```powershell
# PowerShell on PC2
$ip = "192.168.1.100"  # PC1's IP
(Get-Content scripts/docker-compose.pc2-replica.yml) -replace 'PC1_IP', $ip | Set-Content scripts/docker-compose.pc2-replica.yml
```

### Verify It's Updated

Open the file and check:
```yaml
# Should have real IP now:
- ConnectionStrings__DefaultConnection=Host=192.168.1.100;Port=5433;...
- RabbitMqSettings__Host=192.168.1.100
```

If still shows `PC1_IP` → NOT updated yet!

---

## Step 3: Start Docker Services on PC2

```bash
# Navigate to project
cd ChatApplication

# Start docker-compose
docker-compose -f scripts/docker-compose.pc2-replica.yml up -d
```

**Wait 2-3 minutes!** (PostgreSQL replica initialization takes time)

---

## Step 4: Verify Everything Started

### Check Docker Containers

```powershell
docker-compose -f scripts/docker-compose.pc2-replica.yml ps
```

**Expected output:**
```
NAME                STATUS              PORTS
chatapplication-api-1                Up 1 minute         0.0.0.0:5000->5000/tcp
chatapplication-postgres-1           Up 1 minute         0.0.0.0:5433->5432/tcp
chatapplication-redis-1              Up 1 minute         0.0.0.0:6379->6379/tcp
```

**All running?** ✅ Good!

**Any stopped?** ❌ Check logs:
```powershell
docker logs chatapplication-postgres-1 -f
```

---

## Step 5: Check PostgreSQL Replica Connected

### See if PC2's PostgreSQL Connected to PC1

```powershell
# On PC2, check replica status
docker exec -it chatapplication-postgres-1 pg_isready -h localhost
```

**Expected output:**
```
localhost:5432 - accepting connections
```

### See if Replication Streaming

```powershell
# On PC1, run:
docker exec -it chatapplication-postgres-1 psql -U postgres -d postgres -c "SELECT * FROM pg_stat_replication;"
```

**Expected output (on PC1):**
```
 usename          | client_addr   | state     | lsn
------------------+---------------+-----------+--------
 replication_user | 192.168.1.101 | streaming | 0/3000
```

**This means:** ✅ PC2 is connected and streaming!

---

## Step 6: Open Blazor Chat on PC2

**In browser, go to:**
```
http://localhost:5000
```

**Should see:**
```
ChatApplication
[Login screen / Home page]
[Theme: Light/Dark]
[Logo]
```

**If appears:** ✅ API is working!

**If doesn't load:** ❌ Check:
```powershell
docker logs chatapplication-api-1 -f
```

---

## Now Test Real-Time Sync!

### Test 1: See PC1's Existing Messages

1. **On PC1:** Log in, create a message "Hello from PC1"
2. **On PC2:** Log in, check messages
3. ✅ Should see PC1's message instantly!

### Test 2: Send from PC2, See on PC1

1. **On PC2:** Send message "Hello from PC2 - can you see this?"
2. **On PC1:** Check chat
3. ✅ Message appears instantly!

### Test 3: Check Database Sync

**On PC2, query database:**
```powershell
docker exec -it chatapplication-postgres-1 psql -U postgres -d chatapp -c "SELECT id, content, created_at FROM messages ORDER BY created_at DESC LIMIT 5;"
```

**On PC1, run same query:**
```powershell
docker exec -it chatapplication-postgres-1 psql -U postgres -d chatapp -c "SELECT id, content, created_at FROM messages ORDER BY created_at DESC LIMIT 5;"
```

**Both should show:**
```
                   id                   |      content       |     created_at
───────────────────────────────────────┼──────────────────┼──────────────────
 12345678-1234-1234-1234-123456789012 | Hello from PC2   | 2024-01-15 14:05:32
 87654321-4321-4321-4321-210987654321 | Hello from PC1   | 2024-01-15 14:05:20
```

✅ **SAME data on both PCs!**

---

## What to Check & Report Back

### ✅ Everything Working:

```
1. Docker containers running:
   docker-compose ps
   ✅ All 3 containers UP

2. Network connection:
   ping 192.168.1.100
   ✅ Getting replies (0% packet loss)

3. Replication active:
   Check pg_stat_replication on PC1
   ✅ Shows PC2 connected, "streaming" state

4. Real-time messaging:
   Send message on PC1
   ✅ Appears on PC2 within 100ms

   Send message on PC2
   ✅ Appears on PC1 within 100ms

5. Database sync:
   Query same table on both PCs
   ✅ Both show identical data

6. API accessible:
   http://localhost:5000
   ✅ Page loads, can login
```

---

## Troubleshooting Checklist

### ❌ "Can't reach PC1"
```
[ ] Check: ping 192.168.1.100
[ ] Check: PC1 is online (ask classmate)
[ ] Check: Same WiFi network
[ ] Check: Firewall settings
[ ] Check: IP address is correct
```

### ❌ "Docker containers not starting"
```
[ ] Check: Docker Desktop running (system tray)
[ ] Check: docker-compose.pc2-replica.yml has correct IP
[ ] Check: Enough disk space
[ ] Run: docker-compose logs
```

### ❌ "API won't load (http://localhost:5000)"
```
[ ] Check: API container running (docker ps)
[ ] Check: docker logs chatapplication-api-1
[ ] Check: Port 5000 not used by something else
[ ] Check: Firewall allowing port 5000
```

### ❌ "PostgreSQL replica not connecting to PC1"
```
[ ] Check: PC1's PostgreSQL running
[ ] Check: docker-compose.pc2-replica.yml has PC1_IP updated
[ ] Check: docker logs chatapplication-postgres-1
[ ] Check: Network connection to PC1 works (ping)
```

### ❌ "Messages not syncing"
```
[ ] Check: Both APIs running
[ ] Check: Both PostgreSQL running
[ ] Check: Replication active: SELECT * FROM pg_stat_replication;
[ ] Wait 2 seconds (replication lag)
[ ] Refresh page
[ ] If still not synced: Restart docker-compose
```

---

## Commands to Have Ready

```bash
# Check Docker status
docker-compose -f scripts/docker-compose.pc2-replica.yml ps

# View logs
docker logs chatapplication-postgres-1 -f
docker logs chatapplication-api-1 -f
docker logs chatapplication-redis-1 -f

# Query database
docker exec -it chatapplication-postgres-1 psql -U postgres -d chatapp -c "SELECT COUNT(*) FROM messages;"

# Check replication (on PC1)
docker exec -it chatapplication-postgres-1 psql -U postgres -d postgres -c "SELECT * FROM pg_stat_replication;"

# Check if ready (on PC2)
docker exec -it chatapplication-postgres-1 pg_isready -h localhost

# Stop services
docker-compose -f scripts/docker-compose.pc2-replica.yml down

# Restart services
docker-compose -f scripts/docker-compose.pc2-replica.yml restart

# Clean up and start fresh (⚠️ deletes data)
docker-compose -f scripts/docker-compose.pc2-replica.yml down -v
docker-compose -f scripts/docker-compose.pc2-replica.yml up -d
```

---

## Success Criteria Checklist

**✅ You'll know it's working when:**

- [ ] PC2's docker containers all running
- [ ] Can ping PC1 (network good)
- [ ] API accessible at http://localhost:5000
- [ ] Can see PC1's existing messages on PC2
- [ ] Send message on PC1 → appears on PC2 instantly
- [ ] Send message on PC2 → appears on PC1 instantly
- [ ] Both databases have identical data
- [ ] Replication shows "streaming" state

**All checked?** 🎉 You have a **real distributed system!**

---

## Demo For Professor

```
LIVE DEMO (5 minutes):
═════════════════════

1. Show replication status:
   SELECT * FROM pg_stat_replication;
   "PC2 is connected and streaming"

2. Open 2 browsers side-by-side:
   PC1: http://localhost:5000
   PC2: http://192.168.1.101:5000

3. Send message on PC1:
   "Hello from PC1"
   ↓
   Appears on PC2 instantly ✅

4. Send message on PC2:
   "Hello from PC2"
   ↓
   Appears on PC1 instantly ✅

5. Query both databases:
   Both have identical data ✅

6. Show replication lag:
   "Under 100 milliseconds, real-time sync"

RESULT: A+ Project ✅
```

---

## If Something Goes Wrong

**Don't panic! Common fixes:**

1. **Restart everything:**
   ```bash
   docker-compose -f scripts/docker-compose.pc2-replica.yml restart
   ```

2. **Check logs for errors:**
   ```bash
   docker logs chatapplication-postgres-1 | grep ERROR
   ```

3. **Verify IP one more time:**
   - On PC1: `ipconfig`
   - Update PC2's docker-compose.yml
   - Restart

4. **Nuclear option (delete and restart):**
   ```bash
   docker-compose -f scripts/docker-compose.pc2-replica.yml down -v
   docker-compose -f scripts/docker-compose.pc2-replica.yml up -d
   ```

5. **Ask for help:**
   - Share: `docker logs` output
   - Share: `docker ps` output
   - Share: Your IP addresses

---

## Final Checklist Before Demo Day

- [ ] PC1's IP address written down
- [ ] PC2's docker-compose updated with PC1's IP
- [ ] Both PCs tested and working
- [ ] Network connection verified (ping works)
- [ ] Messaging works both directions
- [ ] Database data synced
- [ ] Replication status verified
- [ ] Screenshots captured (for presentation)
- [ ] Practiced demo flow
- [ ] Backup plan if tech fails (have screenshots ready)

**You're ready! 🚀**
