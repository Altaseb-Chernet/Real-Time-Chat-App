# Architecture Decisions

## Overview
This application follows a clean architecture pattern with clear separation of concerns.

## Layers
- API: Controllers, Hubs, Middleware
- Core: Business logic, domain models, interfaces
- Infrastructure: Data access, caching, messaging, external services
- Shared: DTOs, enums, common response/request models

## Key Decisions
- SignalR for real-time communication with Redis backplane for horizontal scaling
- RabbitMQ for async event-driven messaging between services
- Redis for distributed caching and session management
- EF Core for data access with repository pattern
