# OrbitChat Distributed Architecture

This document explains the architecture of the distributed Chat Application to help you present it to your lecturer. The application is built using a modern **microservices-oriented** approach designed for scalability, real-time communication, and fault tolerance.

## Architecture Overview

The application utilizes a distributed system composed of several interconnected containerized services. We use **Docker Compose** to orchestrate these services, ensuring they run in a consistent, reproducible environment across development and production.

### 1. ASP.NET Core Web API (The Application Server)
This is the core backend service built with .NET 10.0. It exposes RESTful endpoints for CRUD operations and user authentication while handling business logic.
- **Statelessness**: The API itself is stateless. Authentication is handled via JWT (JSON Web Tokens), meaning any instance of the API can authenticate a user without relying on server-side session memory. This makes it trivial to scale horizontally by adding more instances behind a load balancer.
- **Clean Architecture**: The codebase is separated into `API`, `Core`, `Infrastructure`, `Client` (Blazor WASM), and `Shared` libraries, enforcing a strict separation of concerns.

### 2. SignalR & Redis (Real-Time Communication & Backplane)
Real-time chat functionality relies on **SignalR** to maintain active WebSocket connections with the Blazor WebAssembly client.
- **The Problem**: In a distributed system with multiple API instances, User A might be connected to Server 1, and User B to Server 2. If User A sends a message to User B, Server 1 doesn't know about User B's connection.
- **The Solution (Redis Backplane)**: We use **Redis** as a distributed backplane. When Server 1 receives a message for User B, it publishes the message to the Redis backplane. Server 2, subscribed to Redis, receives this event and forwards the message to User B over its WebSocket connection. Redis also serves as a high-performance, distributed cache.

### 3. PostgreSQL (Persistent Storage)
Relational data (Users, Rooms, Messages) is stored in a containerized **PostgreSQL** database. Entity Framework Core is used as the ORM (Object-Relational Mapper) to interact with the database, applying migrations dynamically on startup.

### 4. RabbitMQ (Message Broker / Asynchronous Processing)
**RabbitMQ** is integrated as an Advanced Message Queuing Protocol (AMQP) broker.
- **Decoupling**: Instead of having the API perform heavy processing synchronously (which could block the thread and reduce API responsiveness), tasks can be published to a RabbitMQ queue.
- **Event-Driven Architecture**: This allows background worker services (or other microservices) to consume these messages asynchronously, creating a resilient, event-driven ecosystem.

### 5. Cloudinary (Cloud Object Storage)
Instead of storing user-uploaded media (images, videos, documents) on the local filesystem of the Docker container—which is ephemeral and would be lost if the container dies—we integrate with **Cloudinary**.
- Media files are uploaded persistently to the cloud, returning a URL that is saved in PostgreSQL. This ensures media files are highly available and distributed via a CDN (Content Delivery Network).

## Summary for the Lecturer
*"The chat application employs a distributed, containerized architecture to ensure horizontal scalability and real-time reliability. We decoupled the persistent storage into PostgreSQL, utilized Redis as a distributed backplane to sync WebSocket connections across multiple server instances, integrated RabbitMQ to support asynchronous message brokering, and pushed media storage to Cloudinary to keep the application nodes stateless. The entire stack is orchestrated using Docker Compose."*
