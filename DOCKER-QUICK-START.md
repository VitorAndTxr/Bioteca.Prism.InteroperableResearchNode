# Docker Quick Start Guide

## TL;DR - Separated Architecture (Recommended)

```bash
# 1. Start persistence services (one-time setup)
docker-compose -f docker-compose.persistence.yml up -d

# 2. Start application services
docker-compose -f docker-compose.application.yml up -d

# 3. View logs
docker-compose -f docker-compose.application.yml logs -f

# 4. Stop applications (data persists)
docker-compose -f docker-compose.application.yml down

# 5. Restart applications after code changes
docker-compose -f docker-compose.application.yml up -d --build
```

## Why Separated Architecture?

✅ **Data Safety**: Running `docker-compose down` on applications won't delete your database or cache data

✅ **Faster Development**: Rebuild/restart applications quickly without touching databases

✅ **Production-Like**: Mimics production where databases run independently from applications

## Quick Commands

### First Time Setup
```bash
# Start all persistence services
docker-compose -f docker-compose.persistence.yml up -d

# Verify services are healthy
docker-compose -f docker-compose.persistence.yml ps

# Start applications
docker-compose -f docker-compose.application.yml up -d
```

### Daily Development
```bash
# Start applications (if persistence is already running)
docker-compose -f docker-compose.application.yml up -d

# View logs
docker logs -f irn-node-a

# Rebuild after code changes
docker-compose -f docker-compose.application.yml up -d --build

# Stop applications
docker-compose -f docker-compose.application.yml down
```

### Access Services
- **Node A Swagger**: http://localhost:5000/swagger
- **Node B Swagger**: http://localhost:5001/swagger
- **pgAdmin**: http://localhost:5050 (admin@prism.com / admin123)

### Database Access
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

### Troubleshooting
```bash
# Check if persistence services are running
docker-compose -f docker-compose.persistence.yml ps

# Check if network exists
docker network ls | grep irn-network

# View all logs
docker-compose -f docker-compose.persistence.yml logs
docker-compose -f docker-compose.application.yml logs

# Restart specific service
docker restart irn-node-a
```

## Legacy Mode (All Services Together)

For quick local development without separation:

```bash
# Start everything
docker-compose up -d

# Stop everything (volumes persist)
docker-compose down

# Rebuild
docker-compose up -d --build
```

## Complete Documentation

See `docs/development/DOCKER-SETUP.md` for comprehensive documentation including:
- Volume management and backups
- Network configuration
- Production deployment
- Complete troubleshooting guide
