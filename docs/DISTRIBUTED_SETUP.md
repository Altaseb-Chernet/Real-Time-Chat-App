# 🚀 Local Network Distributed Database Setup (Two PCs - Same Lab)

## Overview

```
PC1 (Your Lab)                          PC2 (Classmate's Lab)
┌──────────────────────────────┐      ┌──────────────────────────────┐
│ PostgreSQL Master            │      │ PostgreSQL Replica (Read)    │
│ - Accepts writes             │      │ - Streams from PC1           │
│ - Replicates to PC2          │◄─────┤ - Real-time sync             │
│                              │      │                              │
│ Redis Primary                │      │ Redis Replica (Read)         │
│ - Cache source               │────►│ - Mirrors PC1's cache        │
│                              │      │                              │
│ RabbitMQ Broker              │      │ (connects to PC1)            │
│ - Message hub                │      │                              │
│                              │      │ API + Blazor Client          │
│ API + Blazor Client          │      │ (same code as PC1)           │
│ (master, handles writes)     │      │                              │
└──────────────────────────────┘      └──────────────────────────────┘
         Connected via: Local Network / WiFi (same lab)
```

---

## Step 1: Find Your Local Network IPs

### On Windows (Both PCs):

Open PowerShell and run:
```powershell
ipconfig
```

Look for:
```
Ethernet adapter Ethernet:
   IPv4 Address. . . . . . . . . . : 192.168.1.100
   
   OR
   
   IPv4 Address. . . . . . . . . . : 10.0.0.50
```

**Example IPs:**
- PC1 (Your Lab): `192.168.1.100`
- PC2 (Classmate's Lab): `192.168.1.101`

### Test Network Connection:

On PC2, ping PC1:
```powershell
ping 192.168.1.100
```

Should show:
```
Reply from 192.168.1.100: bytes=32 time=5ms TTL=64
```

If this fails, check:
- Both on same WiFi/Ethernet?
- No firewall blocking?
- Same subnet (first 3 numbers same)?

---

## Step 2: Setup PC1 (Master - Your Lab)

### 2.1: Clone and navigate
```bash
cd D:\Chat App\ChatApplication
```

### 2.2: Run PC1 Master
```bash
docker-compose -f scripts/docker-compose.pc1-master.yml up -d
```

### 2.3: Verify PC1 is running
```bash
docker-compose -f scripts/docker-compose.pc1-master.yml ps
```

Should show:
```
NAME          STATUS              PORTS
api           Up 2 minutes        0.0.0.0:5000->5000/tcp
postgres      Up 2 minutes        0.0.0.0:5433->5432/tcp
redis         Up 2 minutes        0.0.0.0:6379->6379/tcp
rabbitmq      Up 2 minutes        5672/tcp, 0.0.0.0:15672->15672/tcp
```

### 2.4: Check PostgreSQL is accepting replication
```bash
docker exec -it chatapplication-postgres-1 psql -U postgres -d postgres -c "SELECT * FROM pg_stat_replication;"
```

Should initially show empty (no replicas yet), that's fine.

### 2.5: Open Blazor on PC1
```
http://localhost:5000
```

Create a test message: "Hello from PC1"

---

## Step 3: Setup PC2 (Replica - Classmate's Lab)

### 3.1: Clone repository on PC2
```bash
git clone https://github.com/Altaseb-Chernet/Real-Time-Chat-App.git
cd Real-Time-Chat-App/ChatApplication
```

### 3.2: Edit docker-compose.pc2-replica.yml
Replace all `PC1_IP` with PC1's actual IP (example: `192.168.1.100`)

**Find and replace (use Ctrl+H in VS Code):**

Find: `PC1_IP`
Replace with: `192.168.1.100` (PC1's actual IP from Step 1)

**Result should look like:**
```yaml
- ConnectionStrings__DefaultConnection=Host=192.168.1.100;Port=5433;...
- RabbitMqSettings__Host=192.168.1.100
```

And in the `postgres` service:
```yaml
command:
  - bash -c 'pg_basebackup -h 192.168.1.100 -D ...'
```

### 3.3: Run PC2 Replica
```bash
docker-compose -f scripts/docker-compose.pc2-replica.yml up -d
```

### 3.4: Wait for replication to initialize
This takes 1-2 minutes. Check:
```bash
docker logs chatapplication-postgres-1
```

Look for:
```
LOG: started streaming WAL from primary at 0/3000000 on timeline 1
```

### 3.5: Verify replication on PC1
Go back to PC1 and run:
```bash
docker exec -it chatapplication-postgres-1 psql -U postgres -d postgres -c "SELECT * FROM pg_stat_replication;"
```

Should show:
```
 pid  | usename          | application_name | client_addr   | state       | lsn
------+------------------+------------------+---------------+-------------+--------
 1234 | replication_user | walreceiver      | 192.168.1.101 | streaming   | 0/3000ABC
```

✅ **Replication is working!**

### 3.6: Open Blazor on PC2
```
http://localhost:5000
```

---

## Step 4: Test Real-Time Sync

### Test 1: Write on PC1, Read on PC2

1. **PC1:** Open `http://localhost:5000`
2. **Create a new room** called "Test Room"
3. **Send a message:** "Hello from PC1 - can you see this?"
4. **PC2:** Refresh page or check live (SignalR should push instantly)
5. ✅ **PC2 should see message instantly**

### Test 2: Write on PC2, Read on PC1

1. **PC2:** Send message "Hello from PC2 - can PC1 see this?"
2. **PC1:** Check live or refresh
3. ✅ **PC1 should see message instantly**

### Test 3: Verify Data in Databases

**On PC1:**
```bash
docker exec -it chatapplication-postgres-1 psql -U postgres -d chatapp -c "SELECT id, content, created_at FROM messages ORDER BY created_at DESC LIMIT 5;"
```

**On PC2:**
```bash
docker exec -it chatapplication-postgres-1 psql -U postgres -d chatapp -c "SELECT id, content, created_at FROM messages ORDER BY created_at DESC LIMIT 5;"
```

Should show **same messages** (within 100ms lag for replication).

### Test 4: Verify Redis Replication

**On PC1:**
```bash
docker exec -it chatapplication-redis-1 redis-cli SET test_key "Hello from PC1"
docker exec -it chatapplication-redis-1 redis-cli GET test_key
```

**On PC2:**
```bash
docker exec -it chatapplication-redis-1 redis-cli GET test_key
```

Should return: `"Hello from PC1"`

---

## Step 5: Test Failover (What Happens If PC1 Dies?)

### Simulate PC1 Failure

On PC1, stop the database:
```bash
docker-compose -f scripts/docker-compose.pc1-master.yml stop postgres
```

### PC2 Still Works?

1. **PC2 can still read messages** ✅ (PostgreSQL replica is available)
2. **PC2 can NOT write new messages** ❌ (master is down)
3. **PC1 comes back online** → Writes resume automatically

This demonstrates **failover resilience**!

### Restart PC1
```bash
docker-compose -f scripts/docker-compose.pc1-master.yml start postgres
```

PC2 will automatically reconnect and resync.

---

## Step 6: Monitor Replication Lag

### On PC1, check replication status:
```bash
docker exec -it chatapplication-postgres-1 psql -U postgres -d postgres -c "
SELECT 
  usename, 
  client_addr, 
  state,
  pg_wal_lsn_diff(pg_current_wal_lsn(), replay_lsn) as lag_bytes
FROM pg_stat_replication;
"
```

Output example:
```
 usename          | client_addr   | state     | lag_bytes
------------------+---------------+-----------+----------
 replication_user | 192.168.1.101 | streaming | 0
```

**lag_bytes = 0** means **real-time sync** ✅

---

## Architecture Explained

### What is Replication?

```
Transaction on PC1:
┌──────────────────────────────────────┐
│ INSERT INTO messages (...) VALUES... │
└──────────┬───────────────────────────┘
           │
           ├─→ Write to PostgreSQL Master
           │
           ├─→ Store in WAL (Write-Ahead Logs)
           │
           └─→ Stream to PC2 via network
                    │
                    └─→ PC2 PostgreSQL Replica replays same transaction
                    
Result: Both databases have identical data (sub-100ms lag)
```

### Why Use Replication?

1. **Resilience:** If PC1 dies, PC2 still has all data
2. **Read Performance:** PC2 can serve reads locally (fast)
3. **Writes:** Still centralized on PC1 (avoids conflicts)
4. **Learning:** Shows how real distributed systems work

### SignalR + Replication = Real-Time

```
User sends message on PC2:
└─→ SignalR sends to PC1's API
    └─→ Writes to PC1's PostgreSQL
        └─→ RabbitMQ publishes "MessageCreated" event
            └─→ Both PCs' APIs receive event
                └─→ Both PCs' SignalR hubs broadcast to clients
                    └─→ User sees message instantly on both screens
```

---

## Troubleshooting

### Problem: PC2 Can't Connect to PC1

**Error:** `FATAL: Ident authentication failed for user "postgres"`

**Solution:**
1. Verify IP is correct: `ipconfig` on PC1
2. Test connection: `telnet 192.168.1.100 5433` on PC2
3. Check firewall: Allow port 5433 through Windows Defender

### Problem: Replication Not Starting

**Error:** `pg_basebackup: error: could not connect to server`

**Solution:**
1. Verify PC1's postgres is running: `docker ps` on PC1
2. Check replication user exists: `docker exec -it ... psql -U postgres -l`
3. Verify network: `ping 192.168.1.100` from PC2

### Problem: Messages Appear on PC1 but Not PC2

**Cause:** Replication lag or Redis not replicated

**Solution:**
1. Wait 2-3 seconds (replication lag)
2. Refresh browser on PC2
3. Check PostgreSQL replication: `SELECT * FROM pg_stat_replication;`
4. If not showing, restart PC2: `docker-compose -f scripts/docker-compose.pc2-replica.yml restart`

### Problem: High Replication Lag (lag_bytes > 1000000)

**Cause:** Network congestion or slow disk

**Solution:**
1. Check network: Run `ping -c 100 PC1_IP` and check latency
2. Check disk I/O on PC2: `docker stats`
3. Reduce message frequency temporarily

---

## For Class Presentation

### Show These Metrics:

1. **Replication Status:**
   ```bash
   SELECT * FROM pg_stat_replication;
   ```
   Shows PC2 connected and streaming

2. **Lag Measurement:**
   ```bash
   SELECT pg_wal_lsn_diff(pg_current_wal_lsn(), replay_lsn) as lag_bytes FROM pg_stat_replication;
   ```
   Shows real-time < 100ms

3. **Live Demo:**
   - Send message on PC1
   - Appears on PC2 instantly (show timer)
   - Send message on PC2
   - Appears on PC1 instantly

4. **Failover Demo:**
   - Kill PC1's PostgreSQL
   - Show PC2 still serves reads
   - Show PC2 queues writes (or shows error)
   - Restart PC1
   - Data syncs automatically

---

## What This Demonstrates (For Professor)

✅ **Distributed Systems:**
- Two independent machines
- Network replication
- Real-time synchronization

✅ **Database Replication:**
- Master-Slave architecture
- WAL (Write-Ahead Logs)
- Streaming replication

✅ **Real-Time Communication:**
- SignalR + RabbitMQ event bus
- Cross-instance notifications
- User sees changes instantly

✅ **Resilience:**
- Failover capability
- Data redundancy
- Graceful degradation

✅ **Professional Architecture:**
- Used by: Netflix, Uber, Discord
- Cloud-native patterns
- Scalable foundation

---

## Commands Cheat Sheet

### PC1:
```bash
# Start
docker-compose -f scripts/docker-compose.pc1-master.yml up -d

# Check status
docker-compose -f scripts/docker-compose.pc1-master.yml ps

# View logs
docker logs chatapplication-api-1 -f

# Stop
docker-compose -f scripts/docker-compose.pc1-master.yml down

# Check replication
docker exec -it chatapplication-postgres-1 psql -U postgres -d postgres -c "SELECT * FROM pg_stat_replication;"
```

### PC2:
```bash
# Start (after editing docker-compose.pc2-replica.yml with PC1_IP)
docker-compose -f scripts/docker-compose.pc2-replica.yml up -d

# Check status
docker-compose -f scripts/docker-compose.pc2-replica.yml ps

# View logs
docker logs chatapplication-postgres-1 -f

# Check if replication connected
docker logs chatapplication-postgres-1 | grep "started streaming"
```

---

## Next Steps

1. **Immediate:**
   - Run PC1 first (let it initialize 2 minutes)
   - Run PC2 (let replication initialize 2 minutes)
   - Test messaging both directions

2. **Optional Enhancements:**
   - Add load balancer (Nginx) to distribute reads
   - Add PC3 with another replica
   - Implement automatic failover (Patroni)
   - Add monitoring (Prometheus + Grafana)

3. **For Assignment:**
   - Document setup in README
   - Include screenshots of replication working
   - Add performance metrics (lag, throughput)
   - Show both PCs with messages syncing

---

**You're building a real distributed system! 🎉**
