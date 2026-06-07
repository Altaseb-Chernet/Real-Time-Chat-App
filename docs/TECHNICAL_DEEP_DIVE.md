# 🔧 Technical Deep Dive: How Replication Works Between PCs

## Part 1: Network Architecture & Container Networking

### The Basic Setup

```
┌─────────────────────────────────────────┐
│ PC1 (Your Lab)                          │
│ IP: 192.168.1.100                       │
│                                         │
│ ┌─────────────────────────────────────┐ │
│ │ Docker Desktop                      │ │
│ │                                     │ │
│ │ ┌──────────────┐                    │ │
│ │ │ PostgreSQL   │ Port 5433:5432     │ │
│ │ │ Container    │─────────────────────┼─┼──┐
│ │ │              │ (exposed to host)   │ │  │
│ │ └──────────────┘                    │ │  │
│ │       ▲                             │ │  │
│ │       │ (via docker network)        │ │  │
│ │       │                             │ │  │
│ │ ┌──────────────┐                    │ │  │
│ │ │ API          │ Port 5000:5000     │ │  │
│ │ │ Container    │─────────────────────┼─┼──┐
│ │ │ (localhost)  │                    │ │  │
│ │ └──────────────┘                    │ │  │
│ │       ▲                             │ │  │
│ │       │ Connects via hostname       │ │  │
│ │       │ "postgres:5432"             │ │  │
│ │       │ (resolved by docker)        │ │  │
│ │                                     │ │  │
│ └─────────────────────────────────────┘ │  │
│                                         │  │
└─────────────────────────────────────────┘  │
          ▲                                    │
          │                                    │
          │ (Exposed ports on HOST)            │
          │ 192.168.1.100:5433                 │
          │ 192.168.1.100:5000                 │
          │                                    │
          ├────────────────────────────────────┘
          │
          │ (Over network cable/WiFi)
          │
┌─────────────────────────────────────────┐
│ PC2 (Classmate's Lab)                   │
│ IP: 192.168.1.101                       │
│                                         │
│ ┌─────────────────────────────────────┐ │
│ │ Docker Desktop                      │ │
│ │                                     │ │
│ │ ┌──────────────┐                    │ │
│ │ │ PostgreSQL   │ Port 5433:5432     │ │
│ │ │ Container    │                    │ │
│ │ │ (Replica)    │                    │ │
│ │ └──────────────┘                    │ │
│ │       ▲                             │ │
│ │       │ Connects to PC1 via:        │ │
│ │       │ 192.168.1.100:5433          │ │
│ │       │ (TCP connection)            │ │
│ │                                     │ │
│ │ ┌──────────────┐                    │ │
│ │ │ API          │ Port 5000:5000     │ │
│ │ │ Container    │                    │ │
│ │ │ (reads local)│                    │ │
│ │ │ (writes PC1) │                    │ │
│ │ └──────────────┘                    │ │
│ │       ▲                             │ │
│ │       │ Writes go to:               │ │
│ │       │ 192.168.1.100:5433          │ │
│ │       │ (PC1's master)              │ │
│ │                                     │ │
│ └─────────────────────────────────────┘ │
│                                         │
└─────────────────────────────────────────┘
```

---

## Part 2: Connection Strings - The Key!

### How Containers Communicate

#### PC1: API Container → PostgreSQL Container

```yaml
# Inside PC1's API container
ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=chatapp;Username=postgres;Password=altaseb
                                       ▲          ▲
                                       │          └─ Internal port
                                       └─ Container hostname (DNS name)

# How it works:
# - Docker creates a network "chatapp-network"
# - PostgreSQL container joins this network with hostname "postgres"
# - API container can reach it via "postgres:5432" (docker DNS resolution)
# - No IP needed, docker handles the networking
```

#### PC1: External Access (from PC2)

```yaml
# In PC1's docker-compose.yml
postgres:
  image: postgres:15
  ports:
    - "5433:5432"  # ← CRITICAL!
                   #   Host port 5433 (exposed to network)
                   #   Container port 5432 (internal)
```

**What "5433:5432" means:**
```
Docker Host (Windows)
├─ PC1's Docker Desktop (runs all containers)
│  ├─ Container Port 5432 (internal to container)
│  └─ Mapped to Host Port 5433
│
├─ Windows listens on 192.168.1.100:5433
│  (visible to entire network)
│
└─ Any PC on network can connect:
   - telnet 192.168.1.100 5433
   - psql -h 192.168.1.100 -p 5433 -U postgres
```

---

## Part 3: PostgreSQL Replication - Step by Step

### What is WAL (Write-Ahead Log)?

```
PostgreSQL Master (PC1):

Every transaction:
┌───────────────────────────┐
│ INSERT message WHERE ...  │
└───────┬───────────────────┘
        │
        ├─→ First: Write to WAL (Write-Ahead Log)
        │   File: /var/lib/postgresql/data/pg_wal/000000010000000000000001
        │   Format: Binary log of all changes
        │   Size: ~16MB per file
        │
        ├─→ Second: Apply to database
        │   Actual data modification
        │
        └─→ Third: Stream to Replicas
            Protocol: Streaming Replication Protocol
            Format: Binary WAL segments
            Destination: All connected replicas
```

### The Replication Process

```
PC1 (Master)                          PC2 (Replica)
─────────────────────────────────────────────────────

User sends message:
1. API receives message
2. API writes to PC1's PostgreSQL Master
   INSERT INTO messages (...) VALUES (...)
   └─→ Generated transaction ID (LSN)
       Example: 0/3000ABC

3. PostgreSQL writes to WAL
   WAL file: /pg_wal/000000010000000000000001
   
4. PostgreSQL applies to database
   Data is now in PC1's table

5. PostgreSQL sends to replicas
   ┌──────────────────────────┐
   │ Replication Connection   │
   │ PC1:5433 → PC2:5433      │
   │ (TCP stream)             │
   └──────────────────────────┘
                │
                ├─→ Sends: "LSN 0/3000ABC"
                │
                ├─→ Sends: WAL file segment
                │   (binary data)
                │
                └─→ PC2 receives and applies
                    (30-100ms lag)

6. PC2 receives WAL
   PC2's PostgreSQL Replica:
   ├─→ Receives binary WAL
   ├─→ Applies same transaction
   ├─→ Table now has same data
   └─→ Ready to serve reads

7. Both databases identical
   SELECT * FROM messages;
   PC1 result = PC2 result ✅
```

---

## Part 4: Connection String Explained

### PC1: Master Configuration

```yaml
# docker-compose.pc1-master.yml
services:
  api:
    environment:
      ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=chatapp;Username=postgres;Password=altaseb
      # ▲ Uses INTERNAL hostname "postgres"
      # Why? API container and postgres container are on same docker network
      # Docker DNS resolves "postgres" to the postgres container
      # Same reason browsers use "localhost" instead of "127.0.0.1"

  postgres:
    environment:
      POSTGRES_INITDB_ARGS: |
        -c wal_level=replica
        -c max_wal_senders=3
        -c max_replication_slots=3
        # ▲ These enable replication
    ports:
      - "5433:5432"  # ← Exposed to network so PC2 can connect
```

### PC2: Replica Configuration

```yaml
# docker-compose.pc2-replica.yml
services:
  api:
    environment:
      # IMPORTANT: Writes go to PC1 (not local)
      ConnectionStrings__DefaultConnection=Host=192.168.1.100;Port=5433;Database=chatapp;Username=postgres;Password=altaseb
      #                                    ▲ PC1's IP address
      #                                    ▲ Exposed port
      #                                    ▲ Can be reached from outside docker
      
      # Reads can use local cache (Redis)
      RedisSettings__ConnectionString=redis:6379  # ← Local
      
      # Messaging goes to PC1 broker
      RabbitMqSettings__Host=192.168.1.100
      #                     ▲ PC1's IP
  
  postgres:
    environment:
      PGUSER: replication_user
      PGPASSWORD: replication_password
    # ▲ These credentials must match PC1's replication user
    
    entrypoint: |
      bash -c '
      if [ ! -f /var/lib/postgresql/data/PG_VERSION ]; then
        echo "Initializing replica from PC1..."
        # pg_basebackup copies entire database from PC1
        pg_basebackup -h 192.168.1.100 -D /var/lib/postgresql/data -U replication_user -v -P -W -R
        # ▲ PC1's IP
        # ▲ User: replication_user
        # ▲ Password: replication_password (from PGPASSWORD env)
      fi
      exec docker-entrypoint.sh postgres
      '
```

---

## Part 5: The First Sync - pg_basebackup

### What Happens When PC2 Starts

```
PC2 PostgreSQL Container starts:
│
├─→ Check if database already exists
│   ├─ If YES: Skip initialization (database already replicated)
│   └─ If NO: Proceed with pg_basebackup
│
├─→ Execute pg_basebackup command:
│   pg_basebackup -h 192.168.1.100 -D /var/lib/postgresql/data -U replication_user -v -P -W -R
│   
│   Breaking it down:
│   - -h 192.168.1.100: Connect to PC1's PostgreSQL
│   - -D /var/lib/postgresql/data: Store here
│   - -U replication_user: Use this user
│   - -v: Verbose output
│   - -P: Show progress
│   - -W: Prompt for password (uses PGPASSWORD env)
│   - -R: Create recovery.conf (streaming replication)
│
├─→ PC2 Sends:
│   "Hello PC1, I want to replicate. Accept me?"
│   [Credentials: replication_user:replication_password]
│
├─→ PC1 Checks:
│   "Is replication_user real? Does password match?"
│   ├─ YES: Proceed
│   └─ NO: Reject (connection refused)
│
├─→ If authenticated:
│   PC1 sends entire database to PC2
│   ├─ Step 1: All data files (~100MB for your chat app)
│   ├─ Step 2: All WAL files (for consistency)
│   └─ Step 3: Current WAL position (start streaming from here)
│   Duration: 1-5 minutes depending on size + network speed
│
├─→ PC2 Receives:
│   ├─ Creates files in /var/lib/postgresql/data/
│   ├─ Now has EXACT COPY of PC1's database
│   └─ recovery.conf tells PostgreSQL: "Act as replica"
│
├─→ PC2 Starts Streaming Replication:
│   "OK, I have the data. Now send me every change!"
│   PC1: "Got it. From now on, I'll stream WAL to you"
│   └─→ Continuous TCP connection established
│
└─→ Now synchronized!
    PC1 and PC2 have identical data
    Any change on PC1 → streamed to PC2 (<100ms)
```

---

## Part 6: Connection String Summary - Simple Version

```
┌─────────────────────────────────────────────────────────┐
│ RULE: Use HOSTNAME for SAME NETWORK                     │
│       Use IP ADDRESS for DIFFERENT NETWORK              │
└─────────────────────────────────────────────────────────┘

PC1 API talking to PC1 PostgreSQL:
  Host: postgres         (same docker network)
  Port: 5432            (internal port)

PC2 API talking to PC1 PostgreSQL:
  Host: 192.168.1.100   (different PC, need IP)
  Port: 5433            (exposed port)

PC2 PostgreSQL initializing from PC1:
  Host: 192.168.1.100   (different PC, need IP)
  Port: 5433            (exposed port)
  User: replication_user (special user for replication)
```

---

## Part 7: What Happens When PC1 Fails

### Scenario 1: PC1's PostgreSQL Dies (Container Stops)

```
Timeline:
─────────

T0: PC1's PostgreSQL running normally
    PC2's PostgreSQL streaming from PC1
    Both have same data

T1: PC1's PostgreSQL crashes
    docker stop chatapplication-postgres-1
    
    PC2 notices connection lost:
    [ERROR] connection to server lost
    Connection refused

T2: PC2 tries to reconnect (auto-retry)
    ├─ Every 5 seconds: "Can I connect to PC1:5433?"
    ├─ PC1 not responding: FAIL
    └─ PC2 keeps trying (forever)

T3: PC1 comes back online
    docker start chatapplication-postgres-1
    
    PC2 detects connection available:
    "PC1 is back! Reconnecting..."
    
    PC1 sends: "Here's the WAL you missed"
    PC2 applies: All missed transactions
    
    Result: Perfect sync, no data loss

T4: Both fully synchronized again
    ├─ PC2: "I have all your data"
    └─ PC1: "Good, I'm streaming again"
```

### Scenario 2: PC1's API Dies (But Database Lives)

```
PC1 PostgreSQL: RUNNING ✅
PC1 API:        STOPPED ❌
PC2 PostgreSQL: RUNNING ✅
PC2 API:        RUNNING ✅

Users on PC2:
├─ Can READ messages ✅ (from local replica)
├─ Can WRITE messages ⏳ (PC2 API tries to connect to PC1 DB)
│  └─ Connection fails: PC1 API not proxying writes
│
PC2 API error:
"Cannot connect to 192.168.1.100:5433"
Wait... PostgreSQL IS running on PC1
"Connection succeeded!"
├─ Can write to PC1's database ✅
└─ Messages sync automatically ✅

Result: PC2 works fully!
Both APIs and PostgreSQL must be down to break writes.
```

### Scenario 3: PC2 Fails

```
PC1: Unaffected ✅
├─ PostgreSQL running
├─ API running
├─ Users can read/write
└─ PC2 no longer replicated

PC2: Down ❌
├─ No data loss
├─ All data still on PC1
└─ When PC2 comes back: Auto-resyncs

When PC2 restarts:
├─ PostgreSQL container starts
├─ Checks if database exists: NO (docker volume was deleted or data lost)
├─ Runs pg_basebackup from PC1
└─ Re-syncs all data
```

### Scenario 4: Network Connection Lost

```
PC1 and PC2 suddenly disconnected:
├─ PC1: PostgreSQL running, API running
├─ PC2: PostgreSQL running, API running
└─ Network: Cable unplugged / WiFi lost

PC1 behavior:
├─ Users can read/write ✅
├─ Replication status: replication_user disconnected
└─ But PC1 keeps running (continues generating WAL)

PC2 behavior:
├─ Users can READ messages ✅ (from local replica at time of disconnect)
├─ Users can WRITE messages ✅ (to PC2's "cache")
│  └─ But writes NOT replicated to PC1
├─ PC2's data diverges from PC1 ❌
└─ Connection attempts: "Can't reach PC1"

After network reconnects:
├─ PC2 tries to reconnect
├─ PC1 sends: "Here's what you missed"
├─ PC2 applies all missing transactions
└─ Fully in sync again ✅

IMPORTANT: Writes done during disconnect are lost!
This is the trade-off of master-slave replication.
```

---

## Part 8: Standalone Mode - PC2 Without PC1

### Can PC2 Work Alone?

```
YES and NO. It depends:

┌───────────────────────────────────────┐
│ YES - PC2 Can READ                    │
├───────────────────────────────────────┤
│ ├─ PostgreSQL replica is FULLY        │
│ │  populated with data from PC1       │
│ │                                     │
│ ├─ Run: SELECT * FROM messages;       │
│ │  Returns: All messages              │
│ │                                     │
│ └─ SignalR + API can serve reads      │
│    (as long as PC2 Docker running)    │
└───────────────────────────────────────┘

┌───────────────────────────────────────┐
│ NO - PC2 Cannot WRITE                 │
├───────────────────────────────────────┤
│ ├─ PostgreSQL replica is read-only    │
│ │  (streaming replication requirement)│
│ │                                     │
│ ├─ Try: INSERT INTO messages (...);   │
│ │  ERROR: cannot execute INSERT      │
│ │  in a read-only transaction        │
│ │                                     │
│ └─ PC2 API will fail on write attempts│
└───────────────────────────────────────┘

How to make PC2 "Master" (Optional - Advanced):

If PC1 dies and you want PC2 to accept writes:
1. Stop PostgreSQL on PC2
2. Run: pg_ctl promote -D /var/lib/postgresql/data
3. Restart PostgreSQL
4. PC2 is now MASTER (can write)

Command in container:
  docker exec -it chatapplication-postgres-1 pg_ctl promote -D /var/lib/postgresql/data

Result:
  - PC2 becomes master
  - PC2 can accept writes ✅
  - PC1 (if it comes back) won't automatically resync
  - You have 2 conflicting masters ❌ (bad)

This is "failover" - only do if you're sure PC1 won't come back.
```

---

## Part 9: Real Scenario - Step by Step

### Scenario: Exam Day Demo

```
SETUP MORNING:
═════════════

9:00 AM: You arrive at lab, start PC1
  docker-compose -f scripts/docker-compose.pc1-master.yml up -d
  Wait 2 minutes for everything to start

9:02 AM: Check status
  docker ps
  ✅ api, postgres, redis, rabbitmq all running

9:05 AM: Classmate arrives, starts PC2
  docker-compose -f scripts/docker-compose.pc2-replica.yml up -d
  [Has already updated docker-compose.pc2-replica.yml with PC1's IP]

9:07 AM: PC2 initializing PostgreSQL replica
  docker logs chatapplication-postgres-1 -f
  [See: "started streaming WAL from primary"]

9:08 AM: Everything ready
  Both PCs can access chat app
  ✅ http://localhost:5000 on PC1
  ✅ http://localhost:5000 on PC2


DEMO TIME (In Front of Professor):
════════════════════════════════════

9:10 AM: Show replication status
  docker exec -it chatapplication-postgres-1 psql -U postgres -d postgres -c "SELECT * FROM pg_stat_replication;"
  
  Output:
   usename          | client_addr   | state     | lag_bytes
  ------------------+---------------+-----------+----------
   replication_user | 192.168.1.101 | streaming | 0
  
  Professor sees: "PC2 is connected and streaming"

9:11 AM: Send message on PC1
  Login as "Alice"
  Type: "Hello from PC1"
  Click Send

9:11:01 AM: Check PC2 immediately
  Browser: http://PC2_IP:5000
  Message appears instantly ✅
  
  Professor sees: "Real-time sync working"

9:12 AM: Send message from PC2
  Login as "Bob"
  Type: "Hello from PC2"
  Click Send

9:12:01 AM: Check PC1 immediately
  Browser: http://localhost:5000
  Message appears instantly ✅
  
  Professor sees: "Bidirectional sync"

9:13 AM: Query both databases
  PC1: docker exec -it chatapplication-postgres-1 psql -U postgres -d chatapp -c "SELECT COUNT(*) FROM messages;"
  Output: 2

  PC2: docker exec -it chatapplication-postgres-1 psql -U postgres -d chatapp -c "SELECT COUNT(*) FROM messages;"
  Output: 2
  
  Professor sees: "Both databases identical"

9:14 AM: Simulate failure
  PC1: docker-compose -f scripts/docker-compose.pc1-master.yml stop postgres
  
  Try to write on PC2:
  ├─ Can still read ✅
  ├─ Try to write: ERROR ❌
  │  "Connection refused: 192.168.1.100:5433"
  └─ Because master is down
  
  Professor sees: "Graceful degradation"

9:15 AM: Restart PC1
  docker-compose -f scripts/docker-compose.pc1-master.yml start postgres
  
  Check replication again:
  SELECT * FROM pg_stat_replication;
  Output: Shows streaming again
  
  Professor sees: "Automatic recovery"

RESULT: A+ Demonstration ✅
```

---

## Part 10: Connection String Cheat Sheet

```
┌────────────────────────────────────────────────────────────┐
│ WHEN PC1 POSTGRESQL IS UP:                                 │
├────────────────────────────────────────────────────────────┤
│                                                            │
│ PC1 API writes to PC1 DB:                                 │
│   Host=postgres;Port=5432;...                             │
│   (internal, docker network)                              │
│                                                            │
│ PC2 API writes to PC1 DB:                                 │
│   Host=192.168.1.100;Port=5433;...                        │
│   (network, exposed port)                                 │
│                                                            │
│ PC2 reads from PC2 local replica:                         │
│   (handled by connection pooling, same host)              │
│   Reads might go to local cache (Redis)                   │
│                                                            │
└────────────────────────────────────────────────────────────┘

┌────────────────────────────────────────────────────────────┐
│ WHEN PC1 POSTGRESQL IS DOWN:                               │
├────────────────────────────────────────────────────────────┤
│                                                            │
│ PC1 API writes: FAIL ❌                                   │
│   Cannot reach: postgres:5432                             │
│   (Container not running)                                 │
│                                                            │
│ PC2 API writes: FAIL ❌                                   │
│   Cannot reach: 192.168.1.100:5433                        │
│   (Master is down)                                        │
│                                                            │
│ PC2 API reads from local replica: SUCCESS ✅             │
│   PostgreSQL replica still running                        │
│   Has all data from last successful sync                  │
│                                                            │
│ But: "What if my PC2 changes are written locally?"        │
│   PC2's replica is READ-ONLY                              │
│   Cannot execute INSERT/UPDATE/DELETE                     │
│   ERROR: read-only transaction                            │
│                                                            │
└────────────────────────────────────────────────────────────┘
```

---

## Part 11: Ports Explained

```
┌─────────────────────────────────────────────────────────┐
│ Port Mapping Reference                                  │
├─────────────────────────────────────────────────────────┤
│                                                         │
│ PostgreSQL:                                             │
│   Internal (inside container): 5432                     │
│   PC1 expose (outside): 5433:5432                       │
│   PC2 expose (outside): 5433:5432                       │
│                                                         │
│   From PC1 API to PC1 DB:  localhost:5432               │
│   From PC2 API to PC1 DB:  192.168.1.100:5433           │
│   From PC2 replica init:    192.168.1.100:5433          │
│                                                         │
│ API:                                                    │
│   Internal (inside container): 5000                     │
│   PC1 expose (outside): 5000:5000                       │
│   PC2 expose (outside): 5000:5000                       │
│                                                         │
│   From browser to PC1 API: http://localhost:5000        │
│   From browser to PC2 API: http://PC2_IP:5000           │
│                                                         │
│ Redis:                                                  │
│   Internal (inside container): 6379                     │
│   PC1 expose (outside): 6379:6379                       │
│                                                         │
│   From PC1 API to Redis: redis:6379 (docker network)    │
│   From PC2 API to Redis: redis:6379 (local replica)     │
│                                                         │
│ RabbitMQ:                                               │
│   Internal (inside container): 5672                     │
│   PC1 expose (outside): 5672:5672                       │
│                                                         │
│   From PC1 API to RabbitMQ: rabbitmq:5672               │
│   From PC2 API to RabbitMQ: 192.168.1.100:5672          │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

---

## Part 12: Troubleshooting Decision Tree

```
Problem: PC2 can't connect to PC1

├─→ Check 1: Network connection
│   ping 192.168.1.100
│   ├─ YES (replies): Network OK ✅
│   └─ NO (timeout): WiFi/cable issue ❌
│       Solution: Check same network, same WiFi password
│
├─→ Check 2: Docker port exposed
│   On PC1: docker ps
│   Look for: 0.0.0.0:5433->5432/tcp
│   ├─ YES: Exposed ✅
│   └─ NO: docker-compose issue ❌
│       Solution: Verify docker-compose.pc1-master.yml
│
├─→ Check 3: PostgreSQL running on PC1
│   On PC1: telnet 192.168.1.100 5433
│   ├─ Connected (cursor appears): PostgreSQL up ✅
│   └─ Connection refused: PostgreSQL down ❌
│       Solution: docker-compose -f ... up
│
├─→ Check 4: Replication user exists
│   On PC1: docker exec -it chatapplication-postgres-1 psql -U postgres -l
│   ├─ Shows replication_user: User exists ✅
│   └─ No replication_user: Init not run ❌
│       Solution: Check init-replication.sql was executed
│
├─→ Check 5: pg_basebackup command
│   On PC2: docker logs chatapplication-postgres-1 | grep basebackup
│   ├─ Shows "pg_basebackup": Attempted ✅
│   └─ No mention: Not running ❌
│       Solution: Check docker-compose.pc2-replica.yml entrypoint
│
└─→ Check 6: Connection string
    On PC2: grep "PC1_IP" docker-compose.pc2-replica.yml
    ├─ Has actual IP (192.168.1.100): Configured ✅
    └─ Still says "PC1_IP": Not updated ❌
        Solution: Replace all PC1_IP with actual IP
```

---

## Part 13: Advanced - PostgreSQL Replication Details

### How WAL Streaming Works

```
PostgreSQL Master (PC1)                PostgreSQL Replica (PC2)
─────────────────────────────────────────────────────────────

1. WAL Writer Process
   ├─ Every transaction generates WAL
   ├─ Writes to /pg_wal/ directory
   ├─ File names: 000000010000000000000001, 000000010000000000000002
   └─ Each file: 16MB

2. WAL Sender Process (NEW)
   ├─ Monitors pg_wal/ directory
   ├─ When new WAL file complete:
   │  ├─ Sends to all replicas
   │  ├─ Or sends immediately (streaming)
   │  └─ Waits for acknowledge
   └─ Handles backpressure

3. Network Stream
   ├─ TCP connection: PC1:5433 → PC2:5433
   ├─ Protocol: PostgreSQL Replication Protocol
   ├─ Data: Binary WAL bytes
   ├─ Frequency: Continuous (streaming)
   └─ Speed: Network limited

4. WAL Receiver Process (on Replica)
   ├─ Receives WAL bytes from PC1
   ├─ Writes to local pg_wal/
   ├─ Acknowledges receipt
   └─ Continuous process

5. Crash Recovery Process (on Replica)
   ├─ Reads WAL files
   ├─ Replays transactions
   ├─ Updates database tables
   └─ Stays in sync
```

### Replication Slots

```
What are replication slots?

Problem:
  PC2 is slow reading WAL
  PC1 deletes old WAL files
  PC2 tries to read: File not found ❌
  Replication breaks

Solution: Replication Slots
  PC2 says: "I'm reading WAL position 0/3000000"
  PC1 says: "OK, I'll keep WAL until you read it"
  PC1 doesn't delete old WAL files
  └─→ PC2 can catch up safely

In docker-compose.pc1-master.yml:
  max_replication_slots=3
  ├─ Allows 3 replicas
  ├─ Each gets a slot
  └─ PC1 keeps WAL for each

View slots:
  SELECT * FROM pg_replication_slots;
  
  Output:
   slot_name  | slot_type  | active | restart_lsn
  ────────────────────────────────────────────────
   walreceiver | physical  | t      | 0/3000000
  
  active=t means: PC2 is using this slot
```

---

## Summary Table

```
┌──────────────────┬─────────────────────┬──────────────────────┐
│ Component        │ PC1                 │ PC2                  │
├──────────────────┼─────────────────────┼──────────────────────┤
│ PostgreSQL       │ MASTER              │ REPLICA (RO)         │
│ Purpose          │ Accept writes       │ Replicate reads      │
│ Can write?       │ YES ✅              │ NO ❌                │
│ Can read?        │ YES ✅              │ YES ✅               │
│ Connection from  │ postgres:5432       │ 192.168.1.100:5433  │
│ Exposed port     │ 5433:5432           │ 5433:5432            │
│                  │                     │                      │
│ Redis            │ PRIMARY             │ REPLICA              │
│ Cache sync       │ Source of truth     │ Mirrors PC1          │
│ Connection from  │ redis:6379          │ redis:6379 (local)   │
│                  │                     │                      │
│ RabbitMQ         │ BROKER              │ Connects to PC1      │
│ Purpose          │ Message hub         │ (not local)          │
│ Connection from  │ rabbitmq:5672       │ 192.168.1.100:5672   │
│                  │                     │                      │
│ API              │ Sends to master DB  │ Reads local, writes  │
│                  │ Publishes to broker │ master, broker PC1   │
│ Port exposed     │ 5000:5000           │ 5000:5000            │
│                  │                     │                      │
│ Failure impact   │ Everything stops    │ Can read, can't      │
│ if this fails    │ writing             │ write new messages   │
└──────────────────┴─────────────────────┴──────────────────────┘
```

---

## When PC1 Fails - Communication Flow

```
BEFORE FAILURE:
─────────────

User sends message on PC2:
  PC2 API ──→ PC1 PostgreSQL (write)
      │
      └──→ PC1 RabbitMQ (publish event)
          └──→ PC2 RabbitMQ consumer
              └──→ PC2 SignalR hub
                  └──→ Browser (user sees message)


AFTER PC1 FAILS:
────────────────

User tries to send message on PC2:
  PC2 API ──→ TRIES: PC1 PostgreSQL (write)
      │
      └──→ Cannot connect: 192.168.1.100:5433
          PC1 PostgreSQL is DOWN
          ERROR: Connection refused
          Message NOT written ❌

User can still read messages:
  PC2 API ──→ PC2 PostgreSQL Replica
      │
      └──→ Read data ✅
          (data from before failure)
```

This is why we emphasize: **PC1 is critical for writes, PC2 is backup for reads.**

---

## Understanding Your Demo Value

Your professor will see you understand:

✅ **How Docker networking works**
   (Internal hostnames vs external IPs)

✅ **How database replication works**
   (WAL, streaming, master-slave)

✅ **How connection strings route traffic**
   (To master for writes, to replicas for reads)

✅ **How systems fail gracefully**
   (PC2 still serves reads when PC1 is down)

✅ **Professional architecture patterns**
   (Used by Netflix, Uber, Discord)

This is MUCH more impressive than just having 2 Docker containers! 🎉
