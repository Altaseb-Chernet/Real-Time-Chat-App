# ChatApplication

ChatApplication is a real-time chat platform built with ASP.NET Core, SignalR, Redis, RabbitMQ, and PostgreSQL. It is designed to support live message delivery, user presence, and scalable background messaging while keeping the application split into clear API, core, infrastructure, and shared layers.

## What This Repository Contains

This solution is organized so each project has a focused responsibility:

- `ChatApplication.API` exposes the REST endpoints and SignalR hubs used by the client.
- `ChatApplication.Core` holds the business rules, domain models, and application logic.
- `ChatApplication.Infrastructure` contains data access, caching, logging, and messaging integrations.
- `ChatApplication.Shared` provides DTOs, enums, and response contracts shared across projects.
- `ChatApplication.Client` is the browser client that consumes the API and connects to SignalR.

## How It Works

The API handles authentication, room management, message persistence, and real-time delivery. SignalR is used for instant updates so clients see new messages and presence changes without polling. Redis supports fast shared caching, RabbitMQ handles asynchronous messaging workflows, and PostgreSQL stores the application data.

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
