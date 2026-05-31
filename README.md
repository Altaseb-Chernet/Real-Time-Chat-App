# ChatApplication

ChatApplication is a **distributed real-time chat system** built with ASP.NET Core, SignalR, Redis, RabbitMQ, and PostgreSQL. It demonstrates core distributed system patterns including service separation, asynchronous messaging, caching layers, and event-driven architecture to handle concurrent users and high-throughput message delivery across multiple services.

## What This Repository Contains

This solution is organized so each project has a focused responsibility:

- `ChatApplication.API` exposes the REST endpoints and SignalR hubs used by the client.
- `ChatApplication.Core` holds the business rules, domain models, and application logic.
- `ChatApplication.Infrastructure` contains data access, caching, logging, and messaging integrations.
- `ChatApplication.Shared` provides DTOs, enums, and response contracts shared across projects.
- `ChatApplication.Client` is the browser client that consumes the API and connects to SignalR.

## Distributed System Architecture

This system is built as a distributed architecture where:

- **API Service** handles authentication, room management, and message persistence; can scale horizontally behind a load balancer.
- **SignalR Hub** manages real-time bidirectional communication and presence broadcasts to connected clients; scales using Redis backplane for cross-instance messaging.
- **Message Queue (RabbitMQ)** decouples message processing from ingestion, enabling async workflows for notifications, archiving, and other background tasks.
- **Cache Layer (Redis)** reduces database load by caching user profiles, room metadata, and session data; shared across all API instances.
- **Database (PostgreSQL)** is the single source of truth for all persisted chat data, user accounts, and room metadata.

Each service runs independently and communicates via well-defined protocols (HTTP REST, WebSocket SignalR, AMQP messaging), allowing them to scale and fail independently.

## Local Run

The fastest way to start the full stack locally is with Docker Compose:

```bash
docker-compose -f scripts/docker-compose.yml up
```

That starts the API together with PostgreSQL, Redis, and RabbitMQ. The API is exposed on port `5000`, PostgreSQL on `5433`, Redis on `6379`, and RabbitMQ management on `15672`.

## Prerequisites

- Docker Desktop
- A recent .NET SDK if you want to run or debug the projects directly outside containers
- Optional Cloudinary settings if you want to test media upload flows

## Helpful Docs

- [API Documentation](docs/API_Documentation.md) for the available endpoints and SignalR hubs.
- [Deployment Guide](docs/Deployment_Guide.md) for production deployment details.
- [Quick Deployment Guide](docs/QUICK_DEPLOYMENT.md) for the fastest public-access setup.

dotnet run --project src/ChatApplication.API/ChatApplication.API.csproj


dotnet clean ChatApplication.sln
dotnet restore ChatApplication.sln
dotnet build ChatApplication.sln
dotnet run --project src/ChatApplication.API/ChatApplication.API.csproj