# Redis Persistence Testing Guide

**Status**: Implemented
**Last Updated**: 2025-10-03
**Version**: 1.0

## Overview

Este guia fornece instruções para testar a persistência Redis de sessions e channels no IRN (Interoperable Research Node).

## Arquitetura Redis

### **Instâncias Redis por Nó**

Cada nó IRN possui sua própria instância Redis isolada:

```
┌─────────────────┐      ┌──────────────────┐
│  Redis Node A   │◄─────┤   IRN Node A     │
│  localhost:6379 │      │   localhost:5000 │
│  Password: ...a │      └──────────────────┘
└─────────────────┘

┌─────────────────┐      ┌──────────────────┐
│  Redis Node B   │◄─────┤   IRN Node B     │
│  localhost:6380 │      │   localhost:5001 │
│  Password: ...b │      └──────────────────┘
└─────────────────┘
```

### **Dados Persistidos**

#### **Sessions** (via `RedisSessionStore`)
- **Keys**: `session:{sessionToken}`
- **Type**: Hash (JSON serializado)
- **TTL**: Automático (1 hora padrão)
- **Data**:
  - SessionToken, NodeId, ChannelId
  - CreatedAt, ExpiresAt, LastAccessedAt
  - AccessLevel, RequestCount

#### **Rate Limiting** (sessions)
- **Keys**: `session:ratelimit:{sessionToken}`
- **Type**: Sorted Set (timestamps como scores)
- **TTL**: 1 hora
- **Uso**: Sliding window de 60 requests/minuto

#### **Channels** (via `RedisChannelStore`)
- **Metadata Key**: `channel:{channelId}`
- **Type**: Hash (JSON serializado)
- **Binary Key**: `channel:key:{channelId}`
- **Type**: Binary (chave simétrica AES-256)
- **TTL**: Automático (30 minutos padrão)

## Configuração

### **1. Habilitar Redis (appsettings.json)**

#### **appsettings.NodeA.json**
```json
{
  "Redis": {
    "ConnectionString": "localhost:6379,password=prism-redis-password-node-a,abortConnect=false",
    "EnableRedis": true
  },
  "FeatureFlags": {
    "UseRedisForSessions": true,
    "UseRedisForChannels": true
  }
}
```

#### **appsettings.NodeB.json**
```json
{
  "Redis": {
    "ConnectionString": "localhost:6380,password=prism-redis-password-node-b,abortConnect=false",
    "EnableRedis": true
  },
  "FeatureFlags": {
    "UseRedisForSessions": true,
    "UseRedisForChannels": true
  }
}
```

### **2. Docker Compose**

```bash
# Iniciar todos os serviços (Redis + Nodes)
docker-compose up -d

# Verificar status
docker-compose ps

# Verificar logs do Redis Node A
docker logs -f irn-redis-node-a

# Verificar logs do Redis Node B
docker logs -f irn-redis-node-b

# Parar todos os serviços
docker-compose down

# Remover volumes (limpar dados persistidos)
docker-compose down -v
```

## Testes Manuais

### **Teste 1: Verificar Conexão Redis**

```bash
# Conectar ao Redis Node A
docker exec -it irn-redis-node-a redis-cli -a prism-redis-password-node-a

# Dentro do Redis CLI
PING                    # Deve retornar PONG
INFO stats              # Mostra estatísticas
DBSIZE                  # Quantidade de keys
KEYS *                  # Lista todas as keys (use com cuidado em produção)
exit

# Conectar ao Redis Node B
docker exec -it irn-redis-node-b redis-cli -a prism-redis-password-node-b
PING
exit
```

### **Teste 2: Monitorar Criação de Sessions**

#### **Terminal 1: Monitor Redis Node A**
```bash
docker exec -it irn-redis-node-a redis-cli -a prism-redis-password-node-a MONITOR
```

#### **Terminal 2: Criar Session (via Postman/curl)**
```bash
# Exemplo completo: Phase 1 + 2 + 3 + 4
curl -X POST http://localhost:5000/api/channel/initiate \\
  -H "Content-Type: application/json" \\
  -d '{
    "targetNodeUrl": "http://localhost:5001"
  }'

# Observe no Terminal 1: Comandos SET/HSET para criar session
```

#### **Terminal 3: Inspecionar Session**
```bash
# Listar keys de sessions
docker exec -it irn-redis-node-a redis-cli -a prism-redis-password-node-a KEYS "session:*"

# Ver dados da session (substituir {token} pelo token real)
docker exec -it irn-redis-node-a redis-cli -a prism-redis-password-node-a GET "session:{token}"

# Ver TTL restante (em segundos)
docker exec -it irn-redis-node-a redis-cli -a prism-redis-password-node-a TTL "session:{token}"
```

### **Teste 3: Monitorar Criação de Channels**

#### **Terminal 1: Monitor Redis**
```bash
docker exec -it irn-redis-node-a redis-cli -a prism-redis-password-node-a MONITOR
```

#### **Terminal 2: Criar Channel**
```bash
# Abrir canal via Swagger ou curl
curl -X POST http://localhost:5000/api/channel/open \\
  -H "Content-Type: application/json" \\
  -d '{
    "protocolVersion": "1.0",
    "ephemeralPublicKey": "base64-encoded-key...",
    "keyExchangeAlgorithm": "ECDH-P384",
    "supportedCiphers": ["AES-256-GCM"],
    "nonce": "base64-nonce"
  }'
```

#### **Terminal 3: Inspecionar Channel**
```bash
# Listar keys de channels
docker exec -it irn-redis-node-a redis-cli -a prism-redis-password-node-a KEYS "channel:*"

# Ver metadata do channel
docker exec -it irn-redis-node-a redis-cli -a prism-redis-password-node-a GET "channel:{channelId}"

# Ver se a chave binária existe
docker exec -it irn-redis-node-a redis-cli -a prism-redis-password-node-a EXISTS "channel:key:{channelId}"
```

### **Teste 4: Validar TTL Automático**

```bash
# Criar um channel/session e verificar TTL

# Ver TTL inicial (deve ser ~1800s para channels, ~3600s para sessions)
docker exec -it irn-redis-node-a redis-cli -a prism-redis-password-node-a TTL "channel:{channelId}"

# Esperar alguns segundos e verificar novamente (TTL deve diminuir)
sleep 10
docker exec -it irn-redis-node-a redis-cli -a prism-redis-password-node-a TTL "channel:{channelId}"

# Verificar se a key ainda existe
docker exec -it irn-redis-node-a redis-cli -a prism-redis-password-node-a EXISTS "channel:{channelId}"
```

### **Teste 5: Rate Limiting**

```bash
# Fazer múltiplas requisições autenticadas

# Ver sorted set de rate limiting
docker exec -it irn-redis-node-a redis-cli -a prism-redis-password-node-a ZRANGE "session:ratelimit:{token}" 0 -1 WITHSCORES

# Contar requests na janela (últimos 60 segundos)
docker exec -it irn-redis-node-a redis-cli -a prism-redis-password-node-a ZCARD "session:ratelimit:{token}"
```

## Teste de Persistência (Restart)

### **Cenário: Verificar se dados sobrevivem ao restart**

```bash
# 1. Criar sessions e channels com Redis habilitado
curl -X POST http://localhost:5000/api/testing/...

# 2. Verificar keys no Redis
docker exec -it irn-redis-node-a redis-cli -a prism-redis-password-node-a KEYS "*"

# 3. Reiniciar APENAS o container do Node A (Redis continua rodando)
docker restart irn-node-a

# 4. Aguardar o Node A ficar online novamente
docker logs -f irn-node-a

# 5. Verificar se os dados ainda estão no Redis
docker exec -it irn-redis-node-a redis-cli -a prism-redis-password-node-a KEYS "*"

# 6. Tentar usar uma session existente
curl -X POST http://localhost:5000/api/session/whoami \\
  -H "Content-Type: application/json" \\
  -d '{
    "channelId": "...",
    "sessionToken": "...",
    "timestamp": "2025-10-03T10:00:00Z"
  }'
```

## Teste de Falha Redis

### **Cenário: Comportamento quando Redis cai**

```bash
# 1. Parar APENAS o Redis do Node A
docker stop irn-redis-node-a

# 2. Tentar criar uma nova session (deve falhar graciosamente)
curl -X POST http://localhost:5000/api/node/authenticate \\
  -H "Content-Type: application/json" \\
  -d '{...}'

# 3. Verificar logs do Node A (deve mostrar erro de conexão Redis)
docker logs -f irn-node-a | grep -i redis

# 4. Reiniciar Redis
docker start irn-redis-node-a

# 5. Aguardar reconexão (o RedisConnectionService tenta reconectar automaticamente)
docker logs -f irn-node-a | grep -i "Redis connection"
```

## Comparação In-Memory vs Redis

### **Teste A/B: Verificar Diferença de Comportamento**

#### **Terminal 1: Node A com Redis**
```json
// appsettings.NodeA.json
{
  "FeatureFlags": {
    "UseRedisForSessions": true,
    "UseRedisForChannels": true
  }
}
```

#### **Terminal 2: Node B sem Redis (In-Memory)**
```json
// appsettings.NodeB.json
{
  "FeatureFlags": {
    "UseRedisForSessions": false,
    "UseRedisForChannels": false
  }
}
```

#### **Executar testes idênticos nos dois nós e comparar:**
- Tempo de resposta (Redis pode ser ligeiramente mais lento)
- Comportamento após restart (Redis persiste, in-memory perde dados)
- Visualização de dados (Redis permite inspeção com redis-cli)

## Comandos Redis Úteis

```bash
# Conectar ao Redis
docker exec -it irn-redis-node-a redis-cli -a prism-redis-password-node-a

# Dentro do Redis CLI:

# Listar todas as keys
KEYS *

# Buscar keys por padrão
KEYS session:*
KEYS channel:*

# Ver tipo de uma key
TYPE session:abc123

# Ver conteúdo de uma key (string/hash)
GET session:abc123
HGETALL session:abc123

# Ver TTL restante
TTL session:abc123
PTTL session:abc123  # Em milissegundos

# Deletar uma key
DEL session:abc123

# Flush todos os dados (CUIDADO!)
FLUSHDB

# Estatísticas
INFO stats
INFO memory
INFO keyspace

# Monitorar comandos em tempo real
MONITOR

# Sair
exit
```

## Troubleshooting

### **Erro: "Unable to connect to Redis"**

1. Verificar se o Redis está rodando:
```bash
docker ps | grep redis
```

2. Verificar logs do Redis:
```bash
docker logs irn-redis-node-a
```

3. Testar conexão manual:
```bash
docker exec -it irn-redis-node-a redis-cli -a prism-redis-password-node-a PING
```

4. Verificar se a senha está correta no `appsettings.json`

### **Erro: "WRONGPASS invalid username-password pair"**

- Verificar se a senha no `appsettings.json` corresponde à senha configurada no `docker-compose.yml`

### **Dados não persistem após restart**

- Verificar se o volume `redis-data-node-a` foi criado:
```bash
docker volume ls | grep redis
```

- Verificar se o Redis está usando AOF (Append-Only File):
```bash
docker exec -it irn-redis-node-a redis-cli -a prism-redis-password-node-a CONFIG GET appendonly
# Deve retornar: 1) "appendonly" 2) "yes"
```

### **Logs indicam "abortConnect=false"**

- Isso é esperado! Permite que a aplicação continue funcionando mesmo se o Redis estiver temporariamente indisponível

## Próximos Passos

- [✅] Redis implementado para Sessions e Channels
- [ ] Implementar health check endpoint para verificar status do Redis
- [ ] Implementar fallback automático para in-memory se Redis falhar
- [ ] Adicionar métricas Prometheus para monitorar uso do Redis
- [ ] Implementar Redis Sentinel para alta disponibilidade (produção)

## Referências

- [StackExchange.Redis Documentation](https://stackexchange.github.io/StackExchange.Redis/)
- [Redis Commands Reference](https://redis.io/commands/)
- [Docker Compose Redis Best Practices](https://redis.io/docs/stack/get-started/install/docker/)
