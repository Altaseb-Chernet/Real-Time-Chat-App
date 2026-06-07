# 🎯 Visual Diagrams - Connection & Replication Flow

## Diagram 1: Docker Network Inside One PC

```
┌─ Windows Host (PC1: 192.168.1.100) ──────────────────────┐
│                                                           │
│  Docker Desktop Engine                                   │
│  ┌────────────────────────────────────────────────────┐ │
│  │ Docker Network: "chatapp-network"                 │ │
│  │ (Internal to Docker, managed by Docker)           │ │
│  │                                                    │ │
│  │  ┌──────────────┐   ┌──────────────┐             │ │
│  │  │ postgres     │   │ redis        │             │ │
│  │  │ Container    │   │ Container    │             │ │
│  │  │              │   │              │             │ │
│  │  │ IP: 172.x.x.2│   │ IP: 172.x.x.3│             │ │
│  │  │ Hostname:    │   │ Hostname:    │             │ │
│  │  │ "postgres"   │   │ "redis"      │             │ │
│  │  └──────┬───────┘   └──────┬───────┘             │ │
│  │         │                   │                     │ │
│  │  ┌──────▼────────────────────▼────┐              │ │
│  │  │ api Container                  │              │ │
│  │  │                                │              │ │
│  │  │ ConnectionString=              │              │ │
│  │  │ Host=postgres;Port=5432        │              │ │
│  │  │       ↑                        │              │ │
│  │  │ Docker DNS resolves:           │              │ │
│  │  │ "postgres" → 172.x.x.2         │              │ │
│  │  │                                │              │ │
│  │  │ RedisSettings__ConnectionStr   │              │ │
│  │  │ = redis:6379                   │              │ │
│  │  │   ↑                            │              │ │
│  │  │ Docker DNS resolves:           │              │ │
│  │  │ "redis" → 172.x.x.3            │              │ │
│  │  └────────────────────────────────┘              │ │
│  │                                                    │ │
│  └────────────────────────────────────────────────────┘ │
│                                                           │
│ ┌─ Port Forwarding (from Host to Container) ────────┐   │
│ │                                                    │   │
│ │ Docker Host Listen: 192.168.1.100:5433            │   │
│ │          ↓                                         │   │
│ │ Forward to: Container postgres:5432               │   │
│ │          ↓                                         │   │
│ │ Result: External PC can reach 192.168.1.100:5433  │   │
│ │         and access PC1's PostgreSQL               │   │
│ │                                                    │   │
│ └────────────────────────────────────────────────────┘   │
│                                                           │
└───────────────────────────────────────────────────────────┘
```

---

## Diagram 2: Simple Summary

```
PC1 (Master)             PC2 (Replica)
─────────────            ─────────────
PostgreSQL    ─ WAL ──→  PostgreSQL
  Master        Stream    (Read-only)
  (writes)               
    │                      │
    │ Real-time           │ Real-time
    │ replication         replication
    ↓ (100ms)             ↓ lag
    
All data                All data
synced                  synced

Can WRITE           Can only READ
INSERT/UPDATE        SELECT only
    │                   │
    ├──→ RabbitMQ  ←────┤
        (events)        
        ↓               ↓
      PC1 API      PC2 API
      ↓               ↓
    Browser         Browser
    (localhost)    (classmate)
```

---

## Diagram 3: Connection Strings Simplified

```
CONNECTION STRING = Instructions for API "where to find the database"

┌─────────────────────────────────────────────────────────┐
│ PC1 API                                                 │
├─────────────────────────────────────────────────────────┤
│ Host=postgres;Port=5432                                 │
│         ↑                  ↑                             │
│    Container name      Internal port                    │
│    (Docker DNS         (inside container)               │
│     resolves this)                                      │
│                                                         │
│ Uses: INTERNAL network                                  │
│ Speed: FASTEST (no network overhead)                    │
│ Who can access: Only other containers in same docker   │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│ PC2 API                                                 │
├─────────────────────────────────────────────────────────┤
│ Host=192.168.1.100;Port=5433                            │
│         ↑                       ↑                        │
│    PC1's real IP          Exposed port                  │
│    (on your lab network)  (PC1 listens here)           │
│                                                         │
│ Uses: EXTERNAL network (lab WiFi/ethernet)             │
│ Speed: Medium (network latency ~5-10ms)                │
│ Who can access: Any PC on the lab network              │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│ PC2 PostgreSQL (during initialization)                  │
├─────────────────────────────────────────────────────────┤
│ pg_basebackup -h 192.168.1.100 -p 5433 ...             │
│                      ↑                   ↑              │
│               PC1's IP            Exposed port          │
│                                                         │
│ Uses: EXTERNAL network                                  │
│ Auth: replication_user : replication_password           │
│ Purpose: Copy entire database from PC1 to PC2           │
└─────────────────────────────────────────────────────────┘
```

---

## Diagram 4: Failover - What Each PC Can Do

```
WHEN PC1 IS RUNNING:
════════════════════

PC1:                          PC2:
├─ Write messages ✅          ├─ Read messages ✅
├─ Read messages ✅           ├─ Write messages ✅
└─ Source of truth            └─ Backup for reads
    (DB master)                   (DB replica)


WHEN PC1 DIES:
══════════════

PC1:                          PC2:
├─ Everything ❌              ├─ Read messages ✅
└─ Offline                    ├─ Write messages ❌
                              └─ Replica is read-only


WHEN PC1 COMES BACK:
════════════════════

PC1:                          PC2:
├─ Reads data from disk       ├─ Notices PC1 is alive
├─ Starts normally            ├─ Reconnects
└─ Both can work again        └─ Replication resumes ✅
```

---

## Diagram 5: What Happens to Messages

```
User on PC2 sends: "Hello from PC2"

Timeline:
─────────

T0ms:   Message sent from browser
        └─→ PC2 API receives

T5ms:   PC2 API processes
        ├─ Validates message
        ├─ Creates message ID
        └─ Connects to PC1 database

T10ms:  PC2 API → PC1 Database
        INSERT message...
        └─→ PC1 PostgreSQL master receives

T15ms:  PC1 PostgreSQL:
        ├─ Writes to WAL (Write-Ahead Log)
        ├─ Writes to table
        └─ Sends response to PC2

T20ms:  PC2 API receives confirmation
        ├─ Message accepted
        ├─ Publishes RabbitMQ event
        └─ User sees message (cached)

T20ms:  RabbitMQ subscribers
        ├─ PC1 API receives event
        ├─ PC2 API receives event
        └─ Both broadcast via SignalR

T25ms:  SignalR broadcasts
        ├─ PC1 users' browsers get update
        ├─ PC2 users' browsers get update
        └─ ALL USERS SEE MESSAGE

T30ms:  PostgreSQL Replication
        PC1 → PC2 (background)
        WAL stream: message replicated
        (but users already saw it via SignalR!)

TOTAL LATENCY: ~25-30ms for all users to see ✅
WITHOUT waiting for database replication!
```

---

## Diagram 6: The "Port" Concept

```
Port = Door number to the service

PC1 Windows Host (192.168.1.100):
┌─────────────────────────────────────────────────┐
│ Doors (Ports) listening on the host:            │
│                                                 │
│ Door 5000 ──→ API Service (inside docker)      │
│ Door 5433 ──→ PostgreSQL (inside docker)       │
│ Door 6379 ──→ Redis (inside docker)            │
│ Door 5672 ──→ RabbitMQ (inside docker)         │
│ Door 15672 ──→ RabbitMQ Management             │
│                                                 │
└─────────────────────────────────────────────────┘

From PC2 Lab (192.168.1.101):
    
    "I want to talk to PC1's PostgreSQL"
    │
    ├─→ Connect to: 192.168.1.100:5433
    │               (PC1 host) (PostgreSQL door)
    │
    └─→ Windows opens Door 5433
        └─→ Routes to PostgreSQL container
            └─→ PostgreSQL responds


From API inside Docker (same PC1):
    
    "I want to talk to PostgreSQL"
    │
    ├─→ Connect to: postgres:5432
    │               (container name) (internal port)
    │
    └─→ Docker DNS resolves "postgres"
        └─→ Routes to PostgreSQL container
            └─→ PostgreSQL responds


THE DIFFERENCE:
  From outside (PC2): Use IP:PORT → 192.168.1.100:5433
  From inside (PC1):  Use NAME:PORT → postgres:5432
```

---

## Diagram 7: What PC2 Sees in Database After Replication

```
After pg_basebackup completes, PC2 has:

┌──────────────────────────────────────┐
│ PC2 PostgreSQL Replica               │
│                                      │
│ Users table:                         │
│ ┌─────────────────────────────────┐ │
│ │ id  │ username │ email          │ │
│ ├─────┼──────────┼────────────────┤ │
│ │ 1   │ alice    │ alice@lab.edu  │ │
│ │ 2   │ bob      │ bob@lab.edu    │ │
│ │ 3   │ charlie  │ charlie@lab.edu│ │
│ └─────────────────────────────────┘ │
│                                      │
│ Messages table:                      │
│ ┌────────────────────────────────┐  │
│ │ id │ user_id │ content    │ ts │  │
│ ├────┼─────────┼────────────┼────┤  │
│ │ 1  │ 1       │ Hello all! │ t1 │  │
│ │ 2  │ 2       │ Hi Alice   │ t2 │  │
│ │ 3  │ 3       │ Hey team   │ t3 │  │
│ └────────────────────────────────┘  │
│                                      │
│ ChatRooms table:                     │
│ ┌────────────────────────────────┐  │
│ │ id │ name      │ creator_id    │  │
│ ├────┼───────────┼───────────────┤  │
│ │ 1  │ general   │ 1             │  │
│ │ 2  │ random    │ 2             │  │
│ └────────────────────────────────┘  │
│                                      │
│ Total: 3 users, 42 messages, etc.   │
│                                      │
│ EXACT COPY of PC1 at backup time ✅ │
│                                      │
└──────────────────────────────────────┘

After backup, new data flows via WAL replication:

PC1 (running)              PC2 (replica)
   │                          │
   ├─ Alice sends msg    ──→  ├─ Sees message (100ms lag)
   ├─ Bob creates room   ──→  ├─ Sees new room
   ├─ Charlie joins      ──→  ├─ Sees member added
   │                          │
   (continuous stream)    (continuous update)
```

---

## Quick Reference: Troubleshooting

```
❌ PC2 can't connect to PC1's database
├─ Check: ping 192.168.1.100
│  If fails → WiFi issue
├─ Check: docker ps on PC1
│  If no postgres → start docker-compose
├─ Check: telnet 192.168.1.100 5433
│  If connection refused → PostgreSQL not exposed

❌ Replication not starting
├─ Check: docker logs on PC2
│  Look for: "pg_basebackup" output
├─ Check: PC1 has init-replication.sql executed
├─ Check: replication_user exists on PC1

❌ Messages not syncing
├─ Check: Is PC1 PostgreSQL running?
├─ Check: Is PC2 PostgreSQL replica running?
├─ Check: Do both have same data?
│  SELECT COUNT(*) FROM messages; on both
├─ If different → Replication broken
│  Restart PC2: docker-compose ... restart postgres

❌ PC2 can read but can't write
├─ This is NORMAL for replicas!
├─ PC2 is read-only (streaming replication mode)
├─ All writes must go to PC1
├─ If PC1 dies, cannot write until promoted
```

That's the complete picture of how everything connects! 🎯
