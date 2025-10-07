# Docker Setup Guide

This document explains the Docker Compose architecture for the Interoperable Research Node project.

## Architecture Overview

The project uses a **two-layer Docker Compose architecture** to separate stateful persistence services from stateless application services. This ensures data persistence and provides independent lifecycle management.

### Layer 1: Persistence (Stateful Services)
**File**: `docker-compose.persistence.yml`

Contains all data storage services:
- **PostgreSQL Node A** (port 5432) - Node registry database for Node A
- **PostgreSQL Node B** (port 5433) - Node registry database for Node B
- **pgAdmin** (port 5050) - PostgreSQL management UI
- **Redis Node A** (port 6379) - Session/channel cache for Node A
- **Redis Node B** (port 6380) - Session/channel cache for Node B
- **Azurite** (ports 10000-10002) - Azure Storage Emulator for future blob storage

**Key Features**:
- ✅ Data persists across container restarts
- ✅ Named volumes with explicit names (e.g., `irn-postgres-data-node-a`)
- ✅ `restart: unless-stopped` policy for automatic recovery
- ✅ Independent lifecycle from application containers
- ✅ Shared network (`irn-network`) for inter-service communication

### Layer 2: Application (Stateless Services)
**File**: `docker-compose.application.yml`

Contains application services:
- **Node A** (port 5000) - Research Node instance A
- **Node B** (port 5001) - Research Node instance B

**Key Features**:
- ✅ Connects to external `irn-network` created by persistence layer
- ✅ Safe to restart without data loss
- ✅ Depends on persistence services
- ✅ Health checks for service readiness

### Legacy File (Backward Compatibility)
**File**: `docker-compose.yml`

Contains all services in a single file for quick local development and backward compatibility.

**Use Case**: Local development when you don't need strict separation.

---

## Quick Start

### Option 1: Separated Layers (Recommended for Production/Development)

#### 1. Start Persistence Layer (One-Time Setup)
```bash
# Start all persistence services
docker-compose -f docker-compose.persistence.yml up -d

# Verify all services are healthy
docker-compose -f docker-compose.persistence.yml ps

# View logs if needed
docker-compose -f docker-compose.persistence.yml logs -f
```

#### 2. Start Application Layer
```bash
# Start application services
docker-compose -f docker-compose.application.yml up -d

# View logs
docker-compose -f docker-compose.application.yml logs -f

# Follow specific service logs
docker logs -f irn-node-a
docker logs -f irn-node-b
```

#### 3. Restart Application Without Losing Data
```bash
# Stop applications (data is safe)
docker-compose -f docker-compose.application.yml down

# Rebuild and restart applications
docker-compose -f docker-compose.application.yml up -d --build
```

### Option 2: All Services Together (Quick Development)

```bash
# Start everything
docker-compose up -d

# Stop everything (volumes persist by default)
docker-compose down

# Stop and remove volumes (⚠️ DELETES ALL DATA)
docker-compose down -v
```

---

## Common Operations

### Persistence Layer Operations

#### Start Persistence Services
```bash
docker-compose -f docker-compose.persistence.yml up -d
```

#### Stop Persistence Services (Data Preserved)
```bash
docker-compose -f docker-compose.persistence.yml stop
```

#### Stop and Remove Containers (Data Preserved)
```bash
docker-compose -f docker-compose.persistence.yml down
```

#### ⚠️ Delete All Data Volumes
```bash
# WARNING: This deletes ALL data permanently
docker-compose -f docker-compose.persistence.yml down -v
```

#### View Persistence Service Logs
```bash
# All services
docker-compose -f docker-compose.persistence.yml logs -f

# Specific service
docker logs -f irn-postgres-node-a
docker logs -f irn-redis-node-a
```

#### Access Database CLIs
```bash
# PostgreSQL Node A
docker exec -it irn-postgres-node-a psql -U prism_user_a -d prism_node_a_registry

# PostgreSQL Node B
docker exec -it irn-postgres-node-b psql -U prism_user_b -d prism_node_b_registry

# Redis Node A
docker exec -it irn-redis-node-a redis-cli -a prism-redis-password-node-a

# Redis Node B
docker exec -it irn-redis-node-b redis-cli -a prism-redis-password-node-b
```

### Application Layer Operations

#### Start Application Services
```bash
docker-compose -f docker-compose.application.yml up -d
```

#### Rebuild and Restart Applications
```bash
# Rebuild images
docker-compose -f docker-compose.application.yml build --no-cache

# Restart with new images
docker-compose -f docker-compose.application.yml up -d
```

#### Stop Application Services
```bash
docker-compose -f docker-compose.application.yml down
```

#### View Application Logs
```bash
# All applications
docker-compose -f docker-compose.application.yml logs -f

# Specific node
docker logs -f irn-node-a
docker logs -f irn-node-b
```

#### Access Application Shell
```bash
# Node A
docker exec -it irn-node-a /bin/bash

# Node B
docker exec -it irn-node-b /bin/bash
```

---

## Service Endpoints

### Application Endpoints
- **Node A Swagger**: http://localhost:5000/swagger
- **Node B Swagger**: http://localhost:5001/swagger
- **Node A API**: http://localhost:5000
- **Node B API**: http://localhost:5001

### Management Endpoints
- **pgAdmin**: http://localhost:5050
  - Email: `admin@prism.com`
  - Password: `admin123`

### Database Endpoints
- **PostgreSQL Node A**: localhost:5432
  - Database: `prism_node_a_registry`
  - Username: `prism_user_a`
  - Password: `prism_secure_password_2025_a`

- **PostgreSQL Node B**: localhost:5433
  - Database: `prism_node_b_registry`
  - Username: `prism_user_b`
  - Password: `prism_secure_password_2025_b`

### Cache Endpoints
- **Redis Node A**: localhost:6379 (password: `prism-redis-password-node-a`)
- **Redis Node B**: localhost:6380 (password: `prism-redis-password-node-b`)

### Storage Endpoints (Azurite)
- **Blob Service**: http://localhost:10000
- **Queue Service**: http://localhost:10001
- **Table Service**: http://localhost:10002
- **Connection String**:
  ```
  DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://localhost:10000/devstoreaccount1;QueueEndpoint=http://localhost:10001/devstoreaccount1;TableEndpoint=http://localhost:10002/devstoreaccount1;
  ```

---

## Volume Management

### List Volumes
```bash
# All project volumes
docker volume ls | grep irn-

# Expected volumes:
# - irn-postgres-data-node-a
# - irn-postgres-data-node-b
# - irn-pgadmin-data
# - irn-redis-data-node-a
# - irn-redis-data-node-b
# - irn-azurite-data
```

### Backup Volume Data
```bash
# Backup PostgreSQL Node A
docker run --rm -v irn-postgres-data-node-a:/data -v $(pwd):/backup alpine tar czf /backup/postgres-node-a-backup.tar.gz -C /data .

# Backup PostgreSQL Node B
docker run --rm -v irn-postgres-data-node-b:/data -v $(pwd):/backup alpine tar czf /backup/postgres-node-b-backup.tar.gz -C /data .

# Backup Redis Node A
docker run --rm -v irn-redis-data-node-a:/data -v $(pwd):/backup alpine tar czf /backup/redis-node-a-backup.tar.gz -C /data .

# Backup Redis Node B
docker run --rm -v irn-redis-data-node-b:/data -v $(pwd):/backup alpine tar czf /backup/redis-node-b-backup.tar.gz -C /data .
```

### Restore Volume Data
```bash
# Restore PostgreSQL Node A
docker run --rm -v irn-postgres-data-node-a:/data -v $(pwd):/backup alpine sh -c "cd /data && tar xzf /backup/postgres-node-a-backup.tar.gz"

# Restore PostgreSQL Node B
docker run --rm -v irn-postgres-data-node-b:/data -v $(pwd):/backup alpine sh -c "cd /data && tar xzf /backup/postgres-node-b-backup.tar.gz"
```

### Delete Specific Volume (⚠️ Data Loss)
```bash
# Stop services first
docker-compose -f docker-compose.persistence.yml down

# Delete specific volume
docker volume rm irn-postgres-data-node-a
docker volume rm irn-redis-data-node-a
```

---

## Network Management

### Inspect Network
```bash
# View network details
docker network inspect irn-network

# List containers on network
docker network inspect irn-network | grep -A 5 "Containers"
```

### Recreate Network
```bash
# Stop all services
docker-compose -f docker-compose.persistence.yml down
docker-compose -f docker-compose.application.yml down

# Remove network
docker network rm irn-network

# Restart persistence (recreates network)
docker-compose -f docker-compose.persistence.yml up -d
```

---

## Troubleshooting

### Services Won't Start
```bash
# Check if persistence services are running
docker-compose -f docker-compose.persistence.yml ps

# Check network exists
docker network ls | grep irn-network

# View service logs
docker-compose -f docker-compose.persistence.yml logs
docker-compose -f docker-compose.application.yml logs
```

### Database Connection Issues
```bash
# Check PostgreSQL health
docker exec -it irn-postgres-node-a pg_isready -U prism_user_a

# Check database exists
docker exec -it irn-postgres-node-a psql -U prism_user_a -l

# Test connection from application network
docker run --rm --network irn-network postgres:18-alpine \
  psql -h irn-postgres-node-a -U prism_user_a -d prism_node_a_registry -c "SELECT 1;"
```

### Redis Connection Issues
```bash
# Check Redis health
docker exec -it irn-redis-node-a redis-cli -a prism-redis-password-node-a ping

# Test connection from application network
docker run --rm --network irn-network redis:7.2-alpine \
  redis-cli -h irn-redis-node-a -a prism-redis-password-node-a ping
```

### Application Health Check Failures
```bash
# Check application logs
docker logs irn-node-a --tail 100

# Check health endpoint manually
curl -f http://localhost:5000/api/channel/health
curl -f http://localhost:5001/api/channel/health

# Restart application
docker-compose -f docker-compose.application.yml restart node-a
```

### Clean Start (⚠️ Deletes All Data)
```bash
# Stop everything
docker-compose -f docker-compose.application.yml down
docker-compose -f docker-compose.persistence.yml down -v

# Remove network
docker network rm irn-network

# Start fresh
docker-compose -f docker-compose.persistence.yml up -d
docker-compose -f docker-compose.application.yml up -d
```

---

## Development Workflow

### Typical Development Cycle

1. **One-Time Setup**:
   ```bash
   # Start persistence layer (only once)
   docker-compose -f docker-compose.persistence.yml up -d
   ```

2. **Daily Development**:
   ```bash
   # Start applications
   docker-compose -f docker-compose.application.yml up -d

   # Make code changes...

   # Rebuild and restart
   docker-compose -f docker-compose.application.yml up -d --build

   # View logs
   docker-compose -f docker-compose.application.yml logs -f
   ```

3. **End of Day**:
   ```bash
   # Stop applications (data persists)
   docker-compose -f docker-compose.application.yml down

   # Optional: Stop persistence services to free resources
   docker-compose -f docker-compose.persistence.yml stop
   ```

4. **Next Day**:
   ```bash
   # Start persistence if stopped
   docker-compose -f docker-compose.persistence.yml start

   # Start applications
   docker-compose -f docker-compose.application.yml up -d
   ```

---

## Production Deployment

For production deployment, use the separated compose files:

1. **Deploy Persistence Layer First**:
   ```bash
   docker-compose -f docker-compose.persistence.yml up -d
   ```

2. **Deploy Application Layer**:
   ```bash
   docker-compose -f docker-compose.application.yml up -d
   ```

3. **Updates/Rollbacks**:
   ```bash
   # Update application without downtime
   docker-compose -f docker-compose.application.yml pull
   docker-compose -f docker-compose.application.yml up -d --no-deps node-a
   docker-compose -f docker-compose.application.yml up -d --no-deps node-b
   ```

4. **Monitoring**:
   ```bash
   # Check service health
   docker-compose -f docker-compose.persistence.yml ps
   docker-compose -f docker-compose.application.yml ps

   # View logs
   docker-compose -f docker-compose.application.yml logs -f
   ```

---

## Benefits of Separated Architecture

✅ **Data Safety**: Persistence layer runs independently, protecting data from application restarts

✅ **Independent Scaling**: Scale applications without affecting databases

✅ **Faster Iterations**: Rebuild/restart applications quickly without touching data services

✅ **Clear Separation**: Stateful vs stateless services are clearly separated

✅ **Production Ready**: Mimics production architecture where databases are separate from apps

✅ **Resource Management**: Stop applications to free resources while keeping data services running
