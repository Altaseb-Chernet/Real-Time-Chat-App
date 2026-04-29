# Deployment Guide

## Prerequisites
- Docker and Docker Compose
- .NET 8 SDK (for local development)

## Local Development
```bash
docker-compose -f scripts/docker-compose.yml -f scripts/docker-compose.dev.yml up
```

## Production Deployment
```bash
bash scripts/deploy.sh
```

## Environment Variables
| Variable | Description |
|---|---|
| ConnectionStrings__DefaultConnection | PostgreSQL connection string |
| JwtSettings__Secret | JWT signing secret |
| RedisSettings__ConnectionString | Redis connection string |
| RabbitMqSettings__Host | RabbitMQ host |
