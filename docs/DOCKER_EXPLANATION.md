# Docker & Docker Compose Explanation 

understanding the Chat Application's Docker configuration.

---

## 📚 Table of Contents

1. [What is Docker?](#what-is-docker)
2. [What is Docker Compose?](#what-is-docker-compose)
3. [Dockerfile Explained](#dockerfile-explained)
4. [Docker Compose Files Explained](#docker-compose-files-explained)
5. [How It All Works Together](#how-it-all-works-together)
6. [Common Commands](#common-commands)

---

## What is Docker?

### Simple Definition
Think of Docker as a **lightweight package** that contains your entire application along with all its dependencies (libraries, runtime, configuration) bundled into a single unit called a **container**.

### Why Use Docker?
- **Consistency**: Your app runs the same way on your laptop, a colleague's computer, and production servers
- **Isolation**: Each container runs independently without interfering with other applications
- **Easy Deployment**: No need to install complex dependencies on each machine
- **Scalability**: You can easily create multiple instances of your app

### Key Concepts
- **Image**: A blueprint/template for creating containers (like a recipe)
- **Container**: A running instance of an image (like a baked cake from the recipe)
- **Dockerfile**: A text file with instructions on how to build an image

---

## What is Docker Compose?

### Simple Definition
Docker Compose is a tool that lets you define and run **multiple Docker containers** as a single application using a `docker-compose.yml` file.

### Why Use Docker Compose?
- **Multi-Service Setup**: Your Chat App needs a database, cache, message broker, and API server—Docker Compose manages all of them
- **Easy Communication**: Services can talk to each other using service names
- **One Command Startup**: Start all services with a single command: `docker-compose up`
- **Development & Production**: Use different configurations for development and production

---

## Dockerfile Explained

The Dockerfile is a step-by-step recipe to build a Docker image. Here's the Chat Application's Dockerfile:

### Full Dockerfile with Comments

```dockerfile
# ── Stage 1: build ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy ALL project files first for layer-cached restore
COPY ["src/ChatApplication.API/ChatApplication.API.csproj",                       "src/ChatApplication.API/"]
COPY ["src/ChatApplication.Core/ChatApplication.Core.csproj",                     "src/ChatApplication.Core/"]
COPY ["src/ChatApplication.Infrastructure/ChatApplication.Infrastructure.csproj", "src/ChatApplication.Infrastructure/"]
COPY ["src/ChatApplication.Shared/ChatApplication.Shared.csproj",                 "src/ChatApplication.Shared/"]
COPY ["src/ChatApplication.Client/ChatApplication.Client.csproj",                 "src/ChatApplication.Client/"]

RUN dotnet restore "src/ChatApplication.API/ChatApplication.API.csproj"

# Copy all source (preserving the full src/ tree)
COPY src/ src/

# Publish — this also builds the Blazor WASM client and bundles it into wwwroot
RUN dotnet publish "src/ChatApplication.API/ChatApplication.API.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore

# ── Stage 2: runtime ─────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Create uploads directory so local-fallback media works
RUN mkdir -p /app/wwwroot/uploads

COPY --from=build /app/publish .

EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000

ENTRYPOINT ["dotnet", "ChatApplication.API.dll"]
```

### Line-by-Line Explanation

#### **Stage 1: Build Stage**

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
```
- **FROM**: Start with a base image (foundation)
- **mcr.microsoft.com/dotnet/sdk:10.0**: Official Microsoft Docker image with .NET SDK 10.0
- **AS build**: Name this stage "build" so we can reference it later
- Think of it as: "Start with a computer that already has .NET 10 SDK installed"

```dockerfile
WORKDIR /src
```
- **WORKDIR**: Set the working directory inside the container
- This is like `cd /src` in a terminal
- All subsequent commands run from this directory

```dockerfile
COPY ["src/ChatApplication.API/ChatApplication.API.csproj", "src/ChatApplication.API/"]
```
- **COPY**: Copy files from your local machine into the container
- Takes 2 arguments: `[source_on_local_machine, destination_in_container]`
- Copies the `.csproj` files (project files) first
- **Why first?**: Docker caches layers. If source code changes but dependencies don't, it reuses the cache

```dockerfile
RUN dotnet restore "src/ChatApplication.API/ChatApplication.API.csproj"
```
- **RUN**: Execute a command inside the container
- **dotnet restore**: Download all NuGet dependencies for the project
- Like running `npm install` for Node.js projects
- Happens after copying `.csproj` files but before source code

```dockerfile
COPY src/ src/
```
- Copy all source code into the container
- Now the container has everything needed to build the application

```dockerfile
RUN dotnet publish "src/ChatApplication.API/ChatApplication.API.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore
```
- **dotnet publish**: Compile and package the application for production
- **-c Release**: Compile in Release mode (optimized for production)
- **-o /app/publish**: Output the compiled files to `/app/publish`
- **--no-restore**: Don't run restore again (we already did it)
- The Blazor WASM client (frontend) is also built and bundled into `wwwroot` automatically

#### **Stage 2: Runtime Stage**

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
```
- Start a NEW image based on ASP.NET 10.0 (no SDK, lighter weight)
- **Why a new stage?**: The SDK is large (~2GB). We only need the runtime to run the app (~300MB)
- This is called "Multi-stage builds" - very efficient!

```dockerfile
WORKDIR /app
```
- Set working directory to `/app` in this new container

```dockerfile
RUN mkdir -p /app/wwwroot/uploads
```
- Create an `uploads` directory for storing media files
- The `-p` flag means "create parent directories if they don't exist"

```dockerfile
COPY --from=build /app/publish .
```
- **COPY --from=build**: Copy files from the "build" stage (not from local machine)
- Takes the published application from the first stage
- Places it in the current directory (`.` = `/app` because of WORKDIR)

```dockerfile
EXPOSE 5000
```
- **EXPOSE**: Document that this container listens on port 5000
- Doesn't actually expose it (that's docker-compose's job)
- It's metadata for developers

```dockerfile
ENV ASPNETCORE_URLS=http://+:5000
```
- **ENV**: Set an environment variable
- Configures ASP.NET to listen on port 5000
- `+` means "all network interfaces"

```dockerfile
ENTRYPOINT ["dotnet", "ChatApplication.API.dll"]
```
- **ENTRYPOINT**: The command that runs when the container starts
- Runs the compiled application (`ChatApplication.API.dll`)

### Dockerfile Flow Summary

```
┌─────────────────────────────────────┐
│      Stage 1: Build                 │
│  - Start with .NET SDK              │
│  - Copy project files               │
│  - Restore dependencies             │
│  - Copy source code                 │
│  - Compile & publish                │
└─────────────────────────────────────┘
            ↓
┌─────────────────────────────────────┐
│      Stage 2: Runtime               │
│  - Start with .NET Runtime          │
│  - Copy published app from Stage 1  │
│  - Expose port 5000                 │
│  - Set entry point                  │
└─────────────────────────────────────┘
            ↓
        Ready to run! 🚀
```

---

## Docker Compose Files Explained

Docker Compose uses `.yml` (YAML) files to define services. The Chat App has 3 compose files:

### 1. **docker-compose.yml** (Production Configuration)

This is the main configuration for running all services in production.

```yaml
services:
  api:
    build:
      context: ..
      dockerfile: scripts/Dockerfile
    ports:
      - "5000:5000"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:5000
      - ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=chatapp;Username=postgres;Password=altaseb;Include Error Detail=true
      - JwtSettings__Secret=bbd8dab53ffc6dd9ef781cc176a2aaef3ce2e15a292584f3112fac2010bfccee
      - JwtSettings__Issuer=ChatApplication
      - JwtSettings__Audience=ChatApplication
      - JwtSettings__ExpiryMinutes=60
      - RedisSettings__ConnectionString=redis:6379
      - RabbitMqSettings__Host=rabbitmq
      - RabbitMqSettings__Port=5672
      - RabbitMqSettings__Username=guest
      - RabbitMqSettings__Password=guest
      - Cloudinary__CloudName=dlvrwud2y
      - Cloudinary__ApiKey=536731941853732
      - Cloudinary__ApiSecret=Ls8EhXPl8xqhcybYS930AZrpFg0
    depends_on:
      postgres:
        condition: service_healthy
      redis:
        condition: service_started
      rabbitmq:
        condition: service_started
    restart: unless-stopped

  postgres:
    image: postgres:15
    environment:
      POSTGRES_DB: chatapp
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: altaseb
    healthcheck:
      test: ["CMD", "pg_isready", "-U", "postgres"]
      interval: 5s
      timeout: 3s
      retries: 10
    ports:
      - "5433:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
    restart: unless-stopped

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
    restart: unless-stopped

  rabbitmq:
    image: rabbitmq:3-management
    ports:
      - "5672:5672"
      - "15672:15672"
    environment:
      RABBITMQ_DEFAULT_USER: guest
      RABBITMQ_DEFAULT_PASS: guest
    restart: unless-stopped

volumes:
  postgres_data:
```

#### **Understanding Each Service**

##### **API Service** 🚀
The Chat Application backend API

```yaml
services:
  api:
    build:
      context: ..              # Build context is parent directory
      dockerfile: scripts/Dockerfile
```
- **build**: Docker will build an image using the specified Dockerfile
- **context**: The root folder for the build (where COPY commands reference from)
- **dockerfile**: Path to the Dockerfile

```yaml
    ports:
      - "5000:5000"
```
- **ports**: Map ports between container and host machine
- Format: `"host_port:container_port"`
- `5000:5000` means: requests to localhost:5000 → forwarded to container:5000
- You can access the app at `http://localhost:5000`

```yaml
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Host=postgres;...
```
- **environment**: Set environment variables inside the container
- These are like configuration files
- `ConnectionStrings__DefaultConnection=Host=postgres` means:
  - The connection string uses `postgres` as hostname
  - Docker automatically resolves `postgres` to the postgres service IP
  - No need for IP addresses!

```yaml
    depends_on:
      postgres:
        condition: service_healthy
      redis:
        condition: service_started
      rabbitmq:
        condition: service_started
```
- **depends_on**: Define service startup order
- **service_healthy**: Wait until postgres passes healthcheck before starting API
- **service_started**: Just wait for redis and rabbitmq to start (without health check)
- Ensures database is ready before the API tries to connect

```yaml
    restart: unless-stopped
```
- **restart**: Restart policy
- **unless-stopped**: Automatically restart if container crashes (unless manually stopped)

##### **PostgreSQL Service** 🗄️
The database

```yaml
  postgres:
    image: postgres:15
```
- **image**: Use pre-built image instead of building from Dockerfile
- `postgres:15` is the official PostgreSQL 15 image from Docker Hub

```yaml
    healthcheck:
      test: ["CMD", "pg_isready", "-U", "postgres"]
      interval: 5s
      timeout: 3s
      retries: 10
```
- **healthcheck**: Periodically check if the service is healthy
- **test**: Run command `pg_isready` to check if database is ready
- **interval**: Check every 5 seconds
- **timeout**: Wait 3 seconds for response
- **retries**: Try 10 times before marking as unhealthy
- Used by `depends_on: condition: service_healthy` above

```yaml
    volumes:
      - postgres_data:/var/lib/postgresql/data
```
- **volumes**: Persistent storage
- Format: `volume_name:container_path`
- `postgres_data` is defined at the bottom as a named volume
- Data survives even if container is deleted
- Without this, your database would be lost when the container stops!

##### **Redis Service** ⚡
In-memory cache for fast data access

```yaml
  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
```
- Uses official Redis 7 image
- Lightweight `alpine` version (very small)
- Exposes port 6379 for connection

##### **RabbitMQ Service** 📬
Message broker for async communication

```yaml
  rabbitmq:
    image: rabbitmq:3-management
    ports:
      - "5672:5672"      # RabbitMQ port
      - "15672:15672"    # Management UI port
    environment:
      RABBITMQ_DEFAULT_USER: guest
      RABBITMQ_DEFAULT_PASS: guest
```
- Uses official RabbitMQ 3 with management plugin
- Port 5672: RabbitMQ protocol
- Port 15672: Web UI (http://localhost:15672) - username/password: guest/guest

#### **Named Volumes**

```yaml
volumes:
  postgres_data:
```
- Defines a reusable volume that can be referenced in services
- Docker manages the storage automatically
- Data persists between container restarts

---

### 2. **docker-compose.dev.yml** (Development Overlay)

```yaml
version: '3.8'

services:
  api:
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
    volumes:
      - ../src:/app/src
```

#### **What is an "Overlay"?**
This file overrides settings in `docker-compose.yml` for development. When you run:
```bash
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up
```

Both files merge together, with `docker-compose.dev.yml` taking priority.

#### **Changes for Development**

```yaml
environment:
  - ASPNETCORE_ENVIRONMENT=Development
```
- Switches from Production to Development mode
- Enables detailed error messages, logging, etc.

```yaml
volumes:
  - ../src:/app/src
```
- **Volume Binding**: Maps local directory to container directory
- Format: `local_path:container_path`
- `../src` (your local source code) syncs to `/app/src` (in container)
- **Why?**: When you edit code locally, changes appear immediately in the container
- Perfect for development - no rebuilding needed!

---

### 3. **docker-compose.override.yml** (Local Override)

```yaml
services:
  api:
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
```

#### **Purpose**
- Local developer settings that shouldn't be committed to git
- Similar to .dev.yml but individual to each developer
- Automatically applied by Docker Compose (no need to specify with `-f`)

#### **Why Separate Files?**

| File | Purpose | Who Uses | Committed? |
|------|---------|----------|-----------|
| docker-compose.yml | Production config | CI/CD & Production | ✅ Yes |
| docker-compose.dev.yml | Development defaults | Shared team settings | ✅ Yes |
| docker-compose.override.yml | Personal dev setup | Individual developer | ❌ No (.gitignore) |

---

## How It All Works Together

### Step-by-Step Execution

#### **Production Deployment**
```
1. Read docker-compose.yml
   ↓
2. Build Docker image from Dockerfile
   - Stage 1 (Build): Compile everything
   - Stage 2 (Runtime): Prepare lightweight container
   ↓
3. Create containers for each service (API, PostgreSQL, Redis, RabbitMQ)
   ↓
4. Start containers in order (respecting depends_on)
   ↓
5. Services can communicate:
   - API connects to postgres:5432 (DNS resolves to postgres container IP)
   - API connects to redis:6379
   - API connects to rabbitmq:5672
   ↓
6. App runs on http://localhost:5000
```

#### **Development Workflow**
```
1. Read docker-compose.yml (base config)
   ↓
2. Merge docker-compose.dev.yml (development overrides)
   ↓
3. Merge docker-compose.override.yml (personal settings)
   ↓
4. Build Docker image (same as production)
   ↓
5. Create & start containers
   ↓
6. Bind local ./src to container:/app/src
   ↓
7. Edit code locally → Changes sync instantly in container
   ↓
8. Container restarts or hot-reloads automatically
   ↓
9. Test app at http://localhost:5000
```

### Network Communication Diagram

```
┌─────────────────────────────────────────────────────┐
│           Docker Compose Network                    │
│                                                     │
│  ┌──────────────┐   ┌──────────────────────┐       │
│  │   API        │   │  PostgreSQL (postgres)│       │
│  │ :5000        │───→ :5432                │       │
│  │ (ChatApp)    │   │ chatapp database     │       │
│  │              │   │ postgres/altaseb     │       │
│  └──────────────┘   └──────────────────────┘       │
│        │                                            │
│        │            ┌──────────────┐               │
│        ├───────────→│  Redis       │               │
│        │            │  :6379       │               │
│        │            │  Cache       │               │
│        │            └──────────────┘               │
│        │                                            │
│        │            ┌──────────────┐               │
│        └───────────→│  RabbitMQ    │               │
│                     │  :5672       │               │
│                     │  Messages    │               │
│                     └──────────────┘               │
│                                                     │
│  All services use service names for DNS:            │
│  - postgres → resolves to postgres container IP   │
│  - redis → resolves to redis container IP         │
│  - rabbitmq → resolves to rabbitmq container IP   │
└─────────────────────────────────────────────────────┘
```

---

## Common Commands

### Building Images

```bash
# Build the Docker image
docker build -t chatapp:latest -f scripts/Dockerfile .
```
- `-t chatapp:latest`: Tag the image with name "chatapp" and version "latest"
- `-f scripts/Dockerfile`: Path to Dockerfile
- `.`: Build context (current directory)

### Docker Compose Commands

```bash
# Start all services (in foreground - see logs)
docker-compose up

# Start in background (detached mode)
docker-compose up -d

# Start with development settings
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up

# View logs
docker-compose logs -f

# Stop all services
docker-compose down

# Stop and remove volumes (deletes database!)
docker-compose down -v

# View running containers
docker-compose ps

# Execute command in running container
docker-compose exec api bash

# Rebuild image before starting
docker-compose up --build
```

### Development Workflow Commands

```bash
# Start with development settings and log output
docker-compose -f docker-compose.yml -f docker-compose.dev.yml up

# Watch logs from API container
docker-compose logs -f api

# SSH into running API container
docker-compose exec api bash

# Rebuild and restart specific service
docker-compose up -d --build api

# Restart API container (picks up code changes)
docker-compose restart api
```

### Debugging Commands

```bash
# View image layers and size
docker image inspect chatapp:latest

# Check container network settings
docker inspect container_name

# View volume contents
docker volume inspect postgres_data

# Clean up unused images/containers/volumes
docker system prune
```

---

## Key Takeaways for Beginners

| Concept | What It Is | Example |
|---------|-----------|---------|
| **Dockerfile** | Recipe to build an image | Multi-stage build: compile app, then run it |
| **Image** | Blueprint/package | The compiled ChatApplication |
| **Container** | Running instance | Your API running with port 5000 exposed |
| **docker-compose.yml** | Orchestration config | Defines API, Database, Redis, RabbitMQ |
| **Service** | Component in the app | PostgreSQL is a service, Redis is a service |
| **Volume** | Persistent storage | `postgres_data` keeps your database safe |
| **Port Mapping** | Local↔Container | `5000:5000` exposes container port 5000 locally |
| **depends_on** | Startup order | API waits for PostgreSQL to be healthy |
| **Environment Variables** | Configuration | Database credentials, JWT secrets, API keys |

---

## Real-World Analogy

Think of Docker like moving to a new apartment:

- **Dockerfile** = Moving checklist
  - "Gather all your furniture, boxes, and supplies"
  - "Pack everything in a moving truck"
  - "Unload at the new place"

- **Docker Image** = The fully-packed moving truck

- **Docker Container** = Your apartment after moving in with all your stuff

- **docker-compose.yml** = Your apartment complex
  - Your apartment (API)
  - Building maintenance (PostgreSQL database)
  - Security office (Redis cache)
  - Mail room (RabbitMQ messages)
  - All connected and working together

---

## Troubleshooting Quick Reference

| Problem | Cause | Solution |
|---------|-------|----------|
| "Can't connect to database" | PostgreSQL not healthy | Check `docker-compose ps`, wait for health check |
| "Port 5000 already in use" | Another service on port | Change to `"5001:5000"` in docker-compose.yml |
| "Volumes lost after restart" | No volume defined | Add volume: `- postgres_data:/var/lib/postgresql/data` |
| "Code changes not reflecting" | No volume binding | Use docker-compose.dev.yml with `../src:/app/src` |
| "Services can't communicate" | Network issue | Ensure service names are used, not localhost |
| "Image too large" | SDK layer included | Use multi-stage build like our Dockerfile |

---

## Next Steps

1. **Run the application**: `docker-compose up`
2. **Check the app**: Visit http://localhost:5000
3. **View logs**: `docker-compose logs -f api`
4. **Modify code locally**: Edit files in `src/` folder
5. **See changes**: They auto-sync to the container (with dev config)
6. **Access the database**: `docker-compose exec postgres psql -U postgres`
7. **Check RabbitMQ**: Visit http://localhost:15672 (guest/guest)

---

*Last Updated: June 2026*
*Chat Application - Real-Time Chat System*
