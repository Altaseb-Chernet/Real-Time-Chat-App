# Distributed Real-Time Chat Application

## 🎓 Academic Presentation Documentation

Welcome to the documentation for the Distributed Real-Time Chat Application. This document is designed to explain the architecture, design choices, and distributed systems concepts used in this project. It is written specifically to demonstrate an understanding of distributed systems for academic presentation purposes.

---

## 1. Why is this Project "Distributed"?

In a traditional monolithic application, the Web API, real-time WebSocket connections, database, and cache all live on a single physical machine. While simple, a monolith suffers from **single points of failure** and **scalability limits**. 

This chat application is built as a **Distributed System** spanning across multiple physical machines (e.g., PC1 and PC2). We distributed the application to achieve the following core distributed systems goals:

1. **Horizontal Scalability:** By running the API on both PC1 and PC2, we split the computational load (CPU/RAM) and concurrent WebSocket connections across multiple physical machines. If user traffic increases, we can easily add a PC3.
2. **High Availability (HA) & Fault Tolerance:** If the API instance on PC1 crashes, users can seamlessly connect to the API instance on PC2 and continue chatting without bringing down the entire system.
3. **Real-time State Synchronization:** In a distributed chat, User A might be connected to the API on PC1, while User B is connected to PC2. We implemented a message broker (RabbitMQ) to instantly synchronize events across the network so User A and User B can chat in real-time despite being on different servers.

---

## 2. The Distributed Tools Arsenal

To achieve true distribution, we utilize a stack of specialized tools, each solving a specific distributed systems problem.

### 🟢 RabbitMQ (The SignalR Backplane)
* **The Problem:** SignalR keeps WebSocket connections in memory. If User A connects to PC1, PC2 has no idea User A exists. If User B (on PC2) sends a message to User A, PC2 doesn't know how to deliver it.
* **The Distributed Solution:** We use **RabbitMQ** as a pub/sub message broker. When User B sends a message, PC2 publishes the message to RabbitMQ. RabbitMQ instantly broadcasts this message to *all* nodes (including PC1). PC1 receives it, sees User A is connected locally, and delivers the message. This is known as a **SignalR Backplane**.

### 🔴 Redis (Distributed Caching & Data Protection)
* **The Problem:** In a distributed system, if PC1 caches user data locally in RAM, PC2 won't have access to it, leading to inconsistent states. Furthermore, encryption keys used for authentication tokens must be identical across nodes, otherwise PC1 will reject a user authenticated by PC2.
* **The Distributed Solution:** **Redis** acts as our centralized, in-memory data store. Both PC1 and PC2 read and write to the same Redis instance. It is used to store Data Protection Keys (ensuring JWTs and sessions are valid across all PCs) and to cache frequently accessed data.

### 🐘 PostgreSQL (Relational Data & Replication)
* **The Problem:** Chat history and user profiles must be persisted reliably. A single database is a single point of failure.
* **The Distributed Solution:** We use PostgreSQL. Our scripts folder includes tools for **Master-Replica Database Replication** (`init-replication.sql`). This allows PC1 to act as the primary write database, while PC2 can act as a read-replica or failover node, ensuring data durability and read-scalability.

### 🐳 Docker & Docker Compose (Container Orchestration)
* **The Problem:** "It works on my machine" is the biggest enemy of distributed systems. PC1 and PC2 might have different OS versions or missing SDKs.
* **The Distributed Solution:** Everything is containerized. Docker ensures that the API, RabbitMQ, Redis, and Postgres run in exactly the same isolated environment regardless of the underlying hardware.

---

## 3. Architecture & Folder Structure Exploration

The codebase is organized into two main domains: the **Application Source Code** (`src/`) and the **Distributed Orchestration** (`scripts/`).

### 📂 `src/` (Clean Architecture)
The application logic is split into decoupled layers to maintain clean code boundaries:
* **`ChatApplication.API/`**: The distributed entry point. Hosts the REST endpoints and the SignalR Hubs. It configures the connections to RabbitMQ and Redis.
* **`ChatApplication.Client/`**: The Blazor WebAssembly frontend. Runs entirely in the user's browser, making HTTP and WebSocket calls to the API nodes.
* **`ChatApplication.Core/`**: The domain entities (e.g., `Message`, `AppUser`) and business logic interfaces.
* **`ChatApplication.Infrastructure/`**: The data access layer. Implements Entity Framework Core migrations and interacts directly with PostgreSQL.
* **`ChatApplication.Shared/`**: Data Transfer Objects (DTOs) shared between the Client and the API.

### 📂 `scripts/` (Distributed Deployment Scripts)
This folder is the heart of the distributed deployment. It contains the files necessary to orchestrate the network across multiple PCs.

* **`docker-compose.pc1-master.yml`**: 
  * Run on the Primary Machine (PC1).
  * Spins up the foundational distributed infrastructure: PostgreSQL (Database), Redis (Cache), and RabbitMQ (Message Broker).
  * Also spins up **API Node 1** (`scripts-api-1`), configuring it to connect to localhost infrastructure.
* **`docker-compose.pc2-replica.yml`**:
  * Run on the Secondary Machine (PC2).
  * Spins up **API Node 2**.
  * Crucially, the environment variables in this file are configured to point to **PC1's IP Address** rather than localhost. This forces PC2 to join the distributed cluster managed by PC1.
* **`Dockerfile`**:
  * A multi-stage build script that compiles the C# code, publishes the Blazor client, and creates a lightweight Alpine Linux container for the API.
* **`init-replication.sql`**:
  * A PostgreSQL initialization script that configures the master database to accept replication connections from replica nodes, setting up the logical replication user.

---

## 4. How the Distributed Flow Works (Example Scenario)

1. **Deployment:** The teacher starts the infrastructure and API on PC1. Then, starts the API on PC2.
2. **Connection:** Student A opens the app and their network routes them to **PC1**. Student B opens the app and routes to **PC2**. Both see the same chat rooms because both APIs query the same PostgreSQL database.
3. **Messaging:** Student A sends a private message to Student B.
4. **Processing:** PC1 receives the HTTP request and saves the message to PostgreSQL.
5. **Broadcasting:** PC1 tells its SignalR Hub to send the message to Student B. PC1 realizes Student B is *not* connected to PC1. 
6. **The Backplane Magic:** PC1 pushes the event to RabbitMQ. 
7. **Delivery:** RabbitMQ broadcasts the event. PC2 receives it, sees Student B *is* connected via WebSockets, and pushes the real-time notification to Student B's browser.

**Result:** A seamless, real-time chat experience powered by a robust distributed system!