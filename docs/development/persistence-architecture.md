# Persistence Architecture - IRN

**Status:** 📋 Planning Phase
**Data:** 2025-10-03
**Versão:** 1.0

---

## Visão Geral

Este documento define a arquitetura de persistência para o Interoperable Research Node (IRN), substituindo o armazenamento in-memory atual por soluções apropriadas de persistência:

1. **Redis Cache** - Para Sessions e Channels (dados temporários com TTL)
2. **PostgreSQL** - Para Node Registry (dados permanentes)

---

## Análise do Estado Atual (In-Memory)

### 1. Node Registry Service
**Localização:** `Bioteca.Prism.Service/Services/Node/NodeRegistryService.cs`

**Armazenamento Atual:**
```csharp
private readonly Dictionary<string, RegisteredNode> _nodes = new();
private readonly Dictionary<string, RegisteredNode> _nodesByCertificate = new();
private readonly object _lock = new();
```

**Dados Armazenados:**
- `RegisteredNode` entities (NodeId, Certificate, Status, etc.)
- Indexed by: NodeId (primary) e CertificateFingerprint (secondary)
- Acesso concorrente protegido por lock

**Operações:**
- `GetNodeAsync(nodeId)` - Busca por ID
- `GetNodeByCertificateAsync(fingerprint)` - Busca por certificado
- `RegisterNodeAsync(request)` - Criação/atualização
- `UpdateNodeStatusAsync(nodeId, status)` - Atualização de status
- `GetAllNodesAsync()` - Listagem completa
- `UpdateLastAuthenticationAsync(nodeId)` - Atualização de timestamp

**Problema Atual:**
- ❌ Dados perdidos ao reiniciar aplicação
- ❌ Não funciona em múltiplas instâncias
- ❌ Sem histórico de mudanças
- ❌ Sem auditoria

---

### 2. Session Service
**Localização:** `Bioteca.Prism.Service/Services/Session/SessionService.cs`

**Armazenamento Atual:**
```csharp
private readonly ConcurrentDictionary<string, SessionData> _sessions = new();
private readonly ConcurrentDictionary<string, Queue<DateTime>> _requestHistory = new();
```

**Dados Armazenados:**
- `SessionData` (SessionToken, NodeId, ChannelId, TTL, AccessLevel)
- Request history para rate limiting (últimos 60 segundos)

**Operações:**
- `CreateSessionAsync()` - Criação de sessão (TTL padrão: 1 hora)
- `ValidateSessionAsync(token)` - Validação e conversão para SessionContext
- `RenewSessionAsync(token)` - Renovação de TTL
- `RevokeSessionAsync(token)` - Invalidação (logout)
- `GetNodeSessionsAsync(nodeId)` - Sessões de um nó
- `GetSessionMetricsAsync(nodeId)` - Métricas agregadas
- `CleanupExpiredSessionsAsync()` - Limpeza de expirados
- `RecordRequestAsync(token)` - Rate limiting (60 req/min)

**Características:**
- ✅ Expiração automática (TTL)
- ✅ Rate limiting com sliding window
- ❌ Não funciona em múltiplas instâncias
- ❌ Sessões perdidas ao reiniciar

---

### 3. Channel Store
**Localização:** `Bioteca.Prism.Data/Cache/Channel/ChannelStore.cs`

**Armazenamento Atual:**
```csharp
private static readonly ConcurrentDictionary<string, ChannelContext> _channels = new();
```

**Dados Armazenados:**
- `ChannelContext` (ChannelId, SymmetricKey, Role, TTL)
- Chave simétrica AES-256-GCM (32 bytes)
- TTL padrão: 30 minutos

**Operações:**
- `AddChannel(channelId, context)` - Adiciona canal
- `GetChannel(channelId)` - Busca e valida TTL
- `RemoveChannel(channelId)` - Remove e limpa chave (segurança)
- `IsChannelValid(channelId)` - Valida existência e TTL

**Características:**
- ✅ Expiração automática (TTL)
- ✅ Limpeza segura de chaves (Array.Clear)
- ❌ Não funciona em múltiplas instâncias
- ❌ Canais perdidos ao reiniciar

---

## Arquitetura Proposta

### Stack Tecnológica

1. **Redis** (Distributed Cache)
   - Versão: Redis 7.2+
   - Biblioteca: `StackExchange.Redis` 2.7+
   - Uso: Sessions, Channels, Rate Limiting

2. **PostgreSQL** (Relational Database)
   - Versão: PostgreSQL 15+
   - ORM: Entity Framework Core 8.0+
   - Uso: Node Registry, Audit Logs

3. **Connection Pooling**
   - Npgsql connection pooling para PostgreSQL
   - StackExchange.Redis connection multiplexing

---

## Parte 1: Redis Cache (Sessions + Channels)

### Por que Redis?

✅ **TTL Nativo**: Expiração automática de chaves (não precisa de cleanup manual)
✅ **Atômico**: Operações atômicas para rate limiting (INCR, EXPIRE)
✅ **Distribuído**: Funciona em múltiplas instâncias (shared state)
✅ **Rápido**: In-memory, latência <1ms
✅ **Sliding Window**: Suporte a sorted sets para rate limiting preciso

### Estratégia de Cache

#### 1. Sessions (TTL: 1 hora padrão)

**Key Pattern:**
```
session:{sessionToken}             → SessionData (JSON)
session:node:{nodeId}:sessions     → Set de session tokens
session:ratelimit:{sessionToken}   → Sorted Set (timestamps)
```

**Estruturas Redis:**

```redis
# Session data (Hash)
HSET session:abc-123-def
  nodeId "node-001"
  channelId "channel-xyz"
  accessLevel "2"  # Admin
  createdAt "2025-10-03T12:00:00Z"
  expiresAt "2025-10-03T13:00:00Z"
  requestCount "42"

EXPIRE session:abc-123-def 3600

# Node sessions index (Set)
SADD session:node:node-001:sessions "abc-123-def" "ghi-456-jkl"
EXPIRE session:node:node-001:sessions 86400  # 24h (maior que TTL de sessão)

# Rate limiting (Sorted Set - score = timestamp Unix)
ZADD session:ratelimit:abc-123-def 1696339200.123 "req-1"
ZADD session:ratelimit:abc-123-def 1696339201.456 "req-2"
EXPIRE session:ratelimit:abc-123-def 120  # 2 minutos
```

**Operações:**

```csharp
// Create Session
await redis.HashSetAsync($"session:{token}", new HashEntry[]
{
    new("nodeId", nodeId),
    new("channelId", channelId),
    // ... outros campos
});
await redis.KeyExpireAsync($"session:{token}", ttl);
await redis.SetAddAsync($"session:node:{nodeId}:sessions", token);

// Validate Session
var exists = await redis.KeyExistsAsync($"session:{token}");
var data = await redis.HashGetAllAsync($"session:{token}");

// Renew Session (extend TTL)
await redis.KeyExpireAsync($"session:{token}", newTtl);

// Revoke Session
await redis.KeyDeleteAsync($"session:{token}");
await redis.SetRemoveAsync($"session:node:{nodeId}:sessions", token);

// Rate Limiting (Sliding Window)
var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
var windowStart = now - 60; // últimos 60 segundos

// Remove requests antigos
await redis.SortedSetRemoveRangeByScoreAsync(
    $"session:ratelimit:{token}",
    0,
    windowStart);

// Conta requests no último minuto
var count = await redis.SortedSetLengthAsync($"session:ratelimit:{token}");

if (count < 60)
{
    // Adiciona request atual
    await redis.SortedSetAddAsync(
        $"session:ratelimit:{token}",
        Guid.NewGuid().ToString(),
        now);
}
```

---

#### 2. Channels (TTL: 30 minutos padrão)

**Key Pattern:**
```
channel:{channelId}   → ChannelContext (Hash + Binary symmetric key)
```

**Estrutura Redis:**

```redis
# Channel metadata (Hash)
HSET channel:xyz-789-abc
  role "client"
  establishedAt "2025-10-03T12:00:00Z"
  expiresAt "2025-10-03T12:30:00Z"
  remoteNodeId "node-002"

EXPIRE channel:xyz-789-abc 1800

# Symmetric key (Binary - stored separately for security)
SET channel:xyz-789-abc:key <32-byte-binary-key>
EXPIRE channel:xyz-789-abc:key 1800
```

**Operações:**

```csharp
// Add Channel
var key = $"channel:{channelId}";
await redis.HashSetAsync(key, new HashEntry[]
{
    new("role", context.Role.ToString()),
    new("establishedAt", context.EstablishedAt.ToString("O")),
    new("expiresAt", context.ExpiresAt.ToString("O"))
});
await redis.StringSetAsync($"{key}:key", context.SymmetricKey);
await redis.KeyExpireAsync(key, ttl);
await redis.KeyExpireAsync($"{key}:key", ttl);

// Get Channel (with validation)
var exists = await redis.KeyExistsAsync($"channel:{channelId}");
if (exists)
{
    var metadata = await redis.HashGetAllAsync($"channel:{channelId}");
    var symmetricKey = await redis.StringGetAsync($"channel:{channelId}:key");

    // Validate TTL
    var expiresAt = DateTime.Parse(metadata.First(x => x.Name == "expiresAt").Value);
    if (expiresAt > DateTime.UtcNow)
    {
        return new ChannelContext { /* ... */ };
    }
}

// Remove Channel (secure cleanup)
var symmetricKey = await redis.StringGetAsync($"channel:{channelId}:key");
if (symmetricKey.HasValue)
{
    // Clear key from memory before deleting from Redis
    Array.Clear(symmetricKey, 0, symmetricKey.Length);
}
await redis.KeyDeleteAsync($"channel:{channelId}");
await redis.KeyDeleteAsync($"channel:{channelId}:key");
```

---

### Redis Configuration

**appsettings.json:**
```json
{
  "Redis": {
    "ConnectionString": "localhost:6379",
    "InstanceName": "IRN:",
    "DefaultDatabase": 0,
    "ConnectTimeout": 5000,
    "SyncTimeout": 5000,
    "AbortOnConnectFail": false,
    "Ssl": false,
    "AllowAdmin": false
  },
  "Cache": {
    "SessionTtlSeconds": 3600,
    "ChannelTtlSeconds": 1800,
    "RateLimitWindowSeconds": 60,
    "RateLimitMaxRequests": 60
  }
}
```

**Docker Compose:**
```yaml
services:
  redis:
    image: redis:7.2-alpine
    container_name: irn-redis
    ports:
      - "6379:6379"
    volumes:
      - redis-data:/data
    command: redis-server --appendonly yes --maxmemory 256mb --maxmemory-policy allkeys-lru
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 10s
      timeout: 3s
      retries: 3
    networks:
      - irn-network

volumes:
  redis-data:
```

---

## Parte 2: PostgreSQL (Node Registry)

### Por que PostgreSQL?

✅ **ACID**: Transações garantem consistência
✅ **Índices**: Performance em queries complexas
✅ **JSON**: Suporte nativo a JSONB para dados flexíveis (InstitutionDetails, ContactInfo)
✅ **Auditoria**: Triggers e tabelas de histórico
✅ **Full-Text Search**: Busca em NodeName, InstitutionDetails
✅ **Maduro**: Confiável, bem documentado

### Database Schema

```sql
-- =====================================================
-- TABLE: registered_nodes
-- =====================================================
CREATE TABLE registered_nodes (
    -- Primary Key
    node_id VARCHAR(100) PRIMARY KEY,

    -- Node Information
    node_name VARCHAR(255) NOT NULL,
    node_url VARCHAR(500),

    -- Certificate
    certificate TEXT NOT NULL,  -- Base64-encoded X.509 certificate
    certificate_fingerprint VARCHAR(88) NOT NULL UNIQUE,  -- Base64 SHA-256 hash
    certificate_expires_at TIMESTAMP NOT NULL,

    -- Authorization
    status VARCHAR(20) NOT NULL CHECK (status IN ('Pending', 'Authorized', 'Revoked')),
    node_access_level INTEGER NOT NULL CHECK (node_access_level IN (0, 1, 2)),  -- ReadOnly=0, ReadWrite=1, Admin=2

    -- Contact & Institution (JSONB for flexibility)
    contact_info JSONB,  -- { "email": "...", "phone": "...", "responsiblePerson": "..." }
    institution_details JSONB,  -- { "name": "...", "country": "...", "researchAreas": [...] }

    -- Timestamps
    registered_at TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
    last_authenticated_at TIMESTAMP,

    -- Soft Delete
    deleted_at TIMESTAMP,

    -- Indexes
    CONSTRAINT chk_certificate_not_empty CHECK (LENGTH(certificate) > 0)
);

-- Indexes
CREATE INDEX idx_nodes_status ON registered_nodes(status) WHERE deleted_at IS NULL;
CREATE INDEX idx_nodes_access_level ON registered_nodes(node_access_level) WHERE deleted_at IS NULL;
CREATE INDEX idx_nodes_cert_fingerprint ON registered_nodes(certificate_fingerprint) WHERE deleted_at IS NULL;
CREATE INDEX idx_nodes_registered_at ON registered_nodes(registered_at DESC);
CREATE INDEX idx_nodes_institution_name ON registered_nodes((institution_details->>'name')) WHERE deleted_at IS NULL;

-- Full-Text Search
CREATE INDEX idx_nodes_fts ON registered_nodes USING GIN (
    to_tsvector('english',
        COALESCE(node_name, '') || ' ' ||
        COALESCE(institution_details->>'name', '')
    )
) WHERE deleted_at IS NULL;

-- =====================================================
-- TABLE: node_audit_log
-- =====================================================
CREATE TABLE node_audit_log (
    id BIGSERIAL PRIMARY KEY,
    node_id VARCHAR(100) NOT NULL,
    action VARCHAR(50) NOT NULL,  -- 'REGISTERED', 'STATUS_CHANGED', 'UPDATED', 'AUTHENTICATED', 'DELETED'
    old_value JSONB,
    new_value JSONB,
    performed_by VARCHAR(100),  -- Admin NodeId or 'SYSTEM'
    performed_at TIMESTAMP NOT NULL DEFAULT NOW(),
    ip_address INET,
    user_agent TEXT
);

CREATE INDEX idx_audit_node_id ON node_audit_log(node_id);
CREATE INDEX idx_audit_action ON node_audit_log(action);
CREATE INDEX idx_audit_performed_at ON node_audit_log(performed_at DESC);

-- =====================================================
-- TRIGGER: Auto-update updated_at
-- =====================================================
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trigger_update_nodes_timestamp
BEFORE UPDATE ON registered_nodes
FOR EACH ROW
EXECUTE FUNCTION update_updated_at_column();

-- =====================================================
-- TRIGGER: Audit log on changes
-- =====================================================
CREATE OR REPLACE FUNCTION log_node_changes()
RETURNS TRIGGER AS $$
BEGIN
    IF TG_OP = 'INSERT' THEN
        INSERT INTO node_audit_log (node_id, action, new_value, performed_by)
        VALUES (NEW.node_id, 'REGISTERED', row_to_json(NEW), 'SYSTEM');
    ELSIF TG_OP = 'UPDATE' THEN
        IF OLD.status != NEW.status THEN
            INSERT INTO node_audit_log (node_id, action, old_value, new_value, performed_by)
            VALUES (
                NEW.node_id,
                'STATUS_CHANGED',
                jsonb_build_object('status', OLD.status),
                jsonb_build_object('status', NEW.status),
                COALESCE(current_setting('app.current_user', TRUE), 'SYSTEM')
            );
        ELSE
            INSERT INTO node_audit_log (node_id, action, old_value, new_value, performed_by)
            VALUES (NEW.node_id, 'UPDATED', row_to_json(OLD), row_to_json(NEW), 'SYSTEM');
        END IF;
    ELSIF TG_OP = 'DELETE' THEN
        INSERT INTO node_audit_log (node_id, action, old_value, performed_by)
        VALUES (OLD.node_id, 'DELETED', row_to_json(OLD), 'SYSTEM');
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trigger_audit_node_changes
AFTER INSERT OR UPDATE OR DELETE ON registered_nodes
FOR EACH ROW
EXECUTE FUNCTION log_node_changes();
```

### Entity Framework Core Mapping

**Entidade:**
```csharp
// Bioteca.Prism.Domain/Entities/Node/RegisteredNode.cs
public class RegisteredNode
{
    public string NodeId { get; set; } = string.Empty;
    public string NodeName { get; set; } = string.Empty;
    public string? NodeUrl { get; set; }

    public string Certificate { get; set; } = string.Empty;
    public string CertificateFingerprint { get; set; } = string.Empty;
    public DateTime CertificateExpiresAt { get; set; }

    public AuthorizationStatus Status { get; set; }
    public NodeAccessTypeEnum NodeAccessLevel { get; set; }

    public ContactInfo? ContactInfo { get; set; }  // Complex type → JSONB
    public InstitutionDetails? InstitutionDetails { get; set; }  // Complex type → JSONB

    public DateTime RegisteredAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastAuthenticatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}

// Novos tipos complexos para JSONB
public class ContactInfo
{
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? ResponsiblePerson { get; set; }
}

public class InstitutionDetails
{
    public string? Name { get; set; }
    public string? Country { get; set; }
    public List<string>? ResearchAreas { get; set; }
}
```

**DbContext:**
```csharp
// Bioteca.Prism.Data/PrismDbContext.cs
public class PrismDbContext : DbContext
{
    public DbSet<RegisteredNode> RegisteredNodes { get; set; }
    public DbSet<NodeAuditLog> NodeAuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RegisteredNode>(entity =>
        {
            entity.ToTable("registered_nodes");

            entity.HasKey(e => e.NodeId);
            entity.Property(e => e.NodeId).HasColumnName("node_id");
            entity.Property(e => e.NodeName).HasColumnName("node_name").IsRequired();

            // JSONB columns
            entity.Property(e => e.ContactInfo)
                .HasColumnName("contact_info")
                .HasColumnType("jsonb");

            entity.Property(e => e.InstitutionDetails)
                .HasColumnName("institution_details")
                .HasColumnType("jsonb");

            // Enum mapping
            entity.Property(e => e.Status)
                .HasColumnName("status")
                .HasConversion<string>();  // Store as VARCHAR

            entity.Property(e => e.NodeAccessLevel)
                .HasColumnName("node_access_level")
                .HasConversion<int>();  // Store as INTEGER

            // Timestamps
            entity.Property(e => e.RegisteredAt)
                .HasColumnName("registered_at")
                .HasDefaultValueSql("NOW()");

            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at")
                .HasDefaultValueSql("NOW()");

            // Soft delete query filter
            entity.HasQueryFilter(e => e.DeletedAt == null);

            // Indexes (já definidos no SQL, mas úteis para migrations)
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CertificateFingerprint).IsUnique();
        });
    }
}
```

---

## Comparação: In-Memory vs Redis vs PostgreSQL

| Aspecto | In-Memory | Redis | PostgreSQL |
|---------|-----------|-------|------------|
| **Persistência** | ❌ Volatil | ✅ Opcional (AOF/RDB) | ✅ Durável (ACID) |
| **Multi-Instance** | ❌ Não funciona | ✅ Compartilhado | ✅ Compartilhado |
| **TTL Automático** | ❌ Manual cleanup | ✅ Nativo | ❌ Manual (ou TTL extension) |
| **Performance (read)** | ~10 ns | ~0.5 ms | ~5-20 ms |
| **Performance (write)** | ~50 ns | ~1 ms | ~10-50 ms |
| **Escalabilidade** | 🟡 Limited by RAM | 🟢 Sharding/Clustering | 🟢 Replication/Partitioning |
| **Queries Complexas** | ❌ Limitado | 🟡 Básico (Lua scripts) | ✅ SQL completo |
| **Auditoria** | ❌ Não | ❌ Não (requer app) | ✅ Triggers nativos |
| **Transações** | 🟡 Locks | 🟡 Atômico (single key) | ✅ ACID (multi-row) |
| **Custo Operacional** | 🟢 Nenhum | 🟡 Médio | 🔴 Alto |

---

## Próximos Passos

Aguardando definições sobre:
1. Configuração de Redis (local vs cloud vs Docker)
2. Configuração de PostgreSQL (local vs Docker vs cloud)
3. Priorização de implementação (Redis primeiro ou PostgreSQL primeiro?)
4. Estratégia de migração (big bang ou gradual?)

Posso criar:
- Plano de implementação por fases
- Scripts de migração de dados
- Testes de performance
- Documentação de deployment
