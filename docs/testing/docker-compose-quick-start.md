# Docker Compose Quick Start Guide

**Status**: Updated with Redis Support
**Last Updated**: 2025-10-03
**Version**: 2.0

## Overview

Guia rápido para iniciar e testar o IRN com Docker Compose, incluindo suporte Redis para persistência.

## Arquitetura

```
┌─────────────────────────────────────────────────────────────┐
│                    Docker Network (irn-network)             │
│                                                             │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐     │
│  │ Redis Node A │  │ Redis Node B │  │              │     │
│  │  Port: 6379  │  │  Port: 6380  │  │              │     │
│  └──────┬───────┘  └──────┬───────┘  │              │     │
│         │                 │           │              │     │
│  ┌──────▼───────┐  ┌──────▼───────┐  │              │     │
│  │  IRN Node A  │  │  IRN Node B  │  │              │     │
│  │  Port: 5000  │  │  Port: 5001  │  │              │     │
│  └──────────────┘  └──────────────┘  │              │     │
│                                                             │
└─────────────────────────────────────────────────────────────┘
         ▲                   ▲
         │                   │
    localhost:5000      localhost:5001
```

## Comandos Básicos

### **Iniciar Ambiente Completo**

```bash
# Build e iniciar todos os containers
docker-compose up -d

# Verificar status
docker-compose ps

# Saída esperada:
# NAME              IMAGE                                    STATUS         PORTS
# irn-node-a        interoperableresearchnode-node-a        Up (healthy)   0.0.0.0:5000->8080/tcp
# irn-node-b        interoperableresearchnode-node-b        Up (healthy)   0.0.0.0:5001->8080/tcp
# irn-redis-node-a  redis:7.2-alpine                        Up (healthy)   0.0.0.0:6379->6379/tcp
# irn-redis-node-b  redis:7.2-alpine                        Up (healthy)   0.0.0.0:6380->6379/tcp
```

### **Parar Ambiente**

```bash
# Parar containers (preserva volumes)
docker-compose stop

# Parar e remover containers
docker-compose down

# Parar, remover containers E volumes (limpa dados Redis)
docker-compose down -v
```

### **Rebuild após mudanças no código**

```bash
# Rebuild sem cache
docker-compose build --no-cache

# Rebuild e restart
docker-compose up -d --build
```

## Verificação de Saúde

### **Verificar Logs**

```bash
# Logs de todos os containers
docker-compose logs -f

# Logs de um container específico
docker logs -f irn-node-a
docker logs -f irn-redis-node-a

# Últimas 50 linhas
docker logs --tail 50 irn-node-a

# Filtrar logs por keyword
docker logs irn-node-a 2>&1 | grep -i "error"
docker logs irn-node-a 2>&1 | grep -i "redis"
```

### **Health Checks**

```bash
# Verificar health status
docker-compose ps

# Inspecionar health check de um container
docker inspect --format='{{json .State.Health}}' irn-node-a | ConvertFrom-Json -Depth 10

# Health check manual (Node A)
curl http://localhost:5000/swagger/index.html

# Health check manual (Node B)
curl http://localhost:5001/swagger/index.html
```

### **Verificar Conectividade Redis**

```bash
# Conectar ao Redis Node A
docker exec -it irn-redis-node-a redis-cli -a prism-redis-password-node-a PING
# Esperado: PONG

# Conectar ao Redis Node B
docker exec -it irn-redis-node-b redis-cli -a prism-redis-password-node-b PING
# Esperado: PONG
```

## Testes Rápidos

### **Teste 1: Swagger UI**

1. Abrir navegador:
   - Node A: http://localhost:5000/swagger
   - Node B: http://localhost:5001/swagger

2. Testar endpoint `/api/channel/health` (se existir)

### **Teste 2: Handshake Completo (Phases 1-4)**

```bash
# Executar script de teste end-to-end
bash test-phase4.sh

# Ou executar manualmente via Swagger:
# 1. Node A Swagger → POST /api/channel/initiate → targetNodeUrl: "http://irn-node-b:8080"
# 2. Seguir fluxo Phase 1 → 2 → 3 → 4
```

### **Teste 3: Verificar Dados no Redis**

```bash
# Listar todas as keys no Redis Node A
docker exec -it irn-redis-node-a redis-cli -a prism-redis-password-node-a KEYS "*"

# Listar sessions
docker exec -it irn-redis-node-a redis-cli -a prism-redis-password-node-a KEYS "session:*"

# Listar channels
docker exec -it irn-redis-node-a redis-cli -a prism-redis-password-node-a KEYS "channel:*"

# Ver quantidade de keys
docker exec -it irn-redis-node-a redis-cli -a prism-redis-password-node-a DBSIZE
```

## Cenários de Teste

### **Cenário 1: Teste de Persistência**

```bash
# 1. Iniciar ambiente
docker-compose up -d

# 2. Criar uma session (via Swagger ou script)
# 3. Verificar key no Redis
docker exec -it irn-redis-node-a redis-cli -a prism-redis-password-node-a KEYS "session:*"

# 4. Reiniciar APENAS o Node A (Redis continua rodando)
docker restart irn-node-a

# 5. Aguardar Node A ficar online
timeout 30 bash -c 'until docker logs irn-node-a 2>&1 | grep -q "Now listening on"; do sleep 1; done'

# 6. Verificar se a session ainda existe no Redis
docker exec -it irn-redis-node-a redis-cli -a prism-redis-password-node-a KEYS "session:*"

# 7. Tentar usar a session (deve funcionar!)
```

### **Cenário 2: Teste de Falha Redis**

```bash
# 1. Parar Redis Node A
docker stop irn-redis-node-a

# 2. Verificar logs do Node A (deve mostrar erro de conexão)
docker logs -f irn-node-a | grep -i "redis"

# 3. Tentar criar nova session (deve falhar ou usar fallback in-memory)

# 4. Reiniciar Redis
docker start irn-redis-node-a

# 5. Verificar reconexão nos logs
docker logs -f irn-node-a | grep -i "Redis connection"
```

### **Cenário 3: Comunicação entre Nodes**

```bash
# 1. Verificar network
docker network inspect interoperableresearchnode_irn-network

# 2. Testar conectividade entre containers
docker exec irn-node-a ping -c 3 irn-node-b
docker exec irn-node-a curl -s http://irn-node-b:8080/swagger/index.html

# 3. Executar handshake completo
# (usar Swagger ou test-phase4.sh)
```

## Debugging

### **Entrar em um Container**

```bash
# Abrir shell no Node A
docker exec -it irn-node-a /bin/bash

# Verificar variáveis de ambiente
docker exec irn-node-a env | grep -i redis

# Ver arquivos de configuração
docker exec irn-node-a cat /app/appsettings.json
```

### **Verificar Network**

```bash
# Listar networks
docker network ls

# Inspecionar network do IRN
docker network inspect interoperableresearchnode_irn-network

# Ver IPs dos containers
docker inspect -f '{{.Name}} - {{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}' $(docker ps -q)
```

### **Verificar Volumes**

```bash
# Listar volumes
docker volume ls | grep redis

# Inspecionar volume do Redis Node A
docker volume inspect interoperableresearchnode_redis-data-node-a

# Ver tamanho dos volumes
docker system df -v | grep redis
```

### **Limpar Tudo (Fresh Start)**

```bash
# CUIDADO: Remove TUDO (containers, volumes, networks, imagens)
docker-compose down -v --rmi all

# Rebuild from scratch
docker-compose build --no-cache
docker-compose up -d
```

## Configuração Avançada

### **Habilitar/Desabilitar Redis**

#### **Habilitar Redis** (editar `appsettings.NodeA.json`)
```json
{
  "FeatureFlags": {
    "UseRedisForSessions": true,
    "UseRedisForChannels": true
  }
}
```

#### **Desabilitar Redis** (usar in-memory)
```json
{
  "FeatureFlags": {
    "UseRedisForSessions": false,
    "UseRedisForChannels": false
  }
}
```

**Nota**: Após alterar configurações, rebuild é necessário:
```bash
docker-compose up -d --build
```

### **Alterar Senhas Redis**

Editar `docker-compose.yml`:
```yaml
redis-node-a:
  command: redis-server --appendonly yes --requirepass NOVA_SENHA_AQUI
```

E atualizar `appsettings.NodeA.json`:
```json
{
  "Redis": {
    "ConnectionString": "irn-redis-node-a:6379,password=NOVA_SENHA_AQUI,abortConnect=false"
  }
}
```

## Monitoramento

### **Recursos do Sistema**

```bash
# Ver uso de CPU/memória
docker stats

# Ver uso de CPU/memória de containers específicos
docker stats irn-node-a irn-redis-node-a

# Ver uso de disco
docker system df
```

### **Logs Estruturados**

```bash
# Exportar logs para arquivo
docker-compose logs > logs-$(date +%Y%m%d-%H%M%S).txt

# Logs com timestamps
docker-compose logs -t -f

# Logs desde uma data específica
docker logs --since 2025-10-03T10:00:00 irn-node-a
```

## Troubleshooting Comum

### **Problema: "Container unhealthy"**

```bash
# Ver logs do health check
docker inspect --format='{{json .State.Health}}' irn-node-a

# Testar health check manualmente
curl -v http://localhost:5000/swagger/index.html
```

### **Problema: "Port already in use"**

```bash
# Ver o que está usando a porta 5000
netstat -ano | findstr :5000  # Windows
lsof -i :5000                  # Linux/Mac

# Mudar a porta no docker-compose.yml
ports:
  - "5002:8080"  # Usar porta 5002 ao invés de 5000
```

### **Problema: "Cannot connect to Redis"**

```bash
# Verificar se Redis está rodando
docker ps | grep redis

# Verificar logs do Redis
docker logs irn-redis-node-a

# Testar conexão
docker exec -it irn-redis-node-a redis-cli -a prism-redis-password-node-a PING
```

### **Problema: "Image build failed"**

```bash
# Limpar cache do Docker
docker system prune -a

# Rebuild sem cache
docker-compose build --no-cache
```

## Referências

- [Docker Compose Documentation](https://docs.docker.com/compose/)
- [Redis Docker Image](https://hub.docker.com/_/redis)
- [ASP.NET Core Docker](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/docker/)

## Próximos Passos

- [✅] Redis multi-instância configurado
- [ ] Implementar health check endpoints
- [ ] Adicionar Prometheus/Grafana para métricas
- [ ] Configurar Redis Sentinel (alta disponibilidade)
