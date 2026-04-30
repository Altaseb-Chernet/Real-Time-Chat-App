# ChatApplication

A real-time chat application built with ASP.NET Core, SignalR, Redis, and RabbitMQ.

## Projects

- `ChatApplication.API` — REST API and SignalR hubs
- `ChatApplication.Core` — Business logic and domain models
- `ChatApplication.Infrastructure` — Data access, caching, messaging
- `ChatApplication.Shared` — Shared DTOs, enums, and response models

## Getting Started

```bash
docker-compose -f scripts/docker-compose.yml up
```

See [Deployment Guide](docs/Deployment_Guide.md) for more details.
