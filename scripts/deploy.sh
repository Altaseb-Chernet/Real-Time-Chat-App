#!/bin/bash
set -e

echo "Building Docker image..."
docker build -t chatapplication:latest -f scripts/Dockerfile .

echo "Deploying with Docker Compose..."
docker-compose -f scripts/docker-compose.yml up -d

echo "Deployment complete."
