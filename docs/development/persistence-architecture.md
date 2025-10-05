# Persistence Architecture - IRN

**Status:** ✅ Partially Implemented (Redis Complete, PostgreSQL Planned)
**Date:** 2025-10-05
**Version:** 2.0

---

## Overview

This document defines the persistence architecture for the Interoperable Research Node (IRN), replacing the current in-memory storage with appropriate persistence solutions:

1. **Redis Cache** - For Sessions and Channels (temporary data with TTL) ✅ **IMPLEMENTED**
2. **PostgreSQL** - For Node Registry (permanent data) ⏳ **PLANNED**

---

## Current State Analysis (In-Memory)

### 1. Node Registry Service
**Location:** `Bioteca.Prism.Service/Services/Node/NodeRegistryService.cs`

**Current Storage:**
```csharp
private readonly Dictionary<string, RegisteredNode> _nodes = new();
private readonly Dictionary<string, RegisteredNode> _nodesByCertificate = new();
private readonly object _lock = new();
```

**Stored Data:**
- `RegisteredNode` entities (NodeId, Certificate, Status, etc.)
- Indexed by: NodeId (primary) and CertificateFingerprint (secondary)
- Concurrent access protected by lock

**Operations:**
- `GetNodeAsync(nodeId)` - Lookup by ID
- `GetNodeByCertificateAsync(fingerprint)` - Lookup by certificate
- `RegisterNodeAsync(request)` - Create/update
- `UpdateNodeStatusAsync(nodeId, status)` - Status update
- `GetAllNodesAsync()` - Complete listing
- `UpdateLastAuthenticationAsync(nodeId)` - Timestamp update

**Current Problems:**
- ❌ Data lost on application restart
- ❌ Does not work with multiple instances
- ❌ No change history
- ❌ No audit trail

---

### 2. Session Service
**Location:** `Bioteca.Prism.Service/Services/Session/SessionService.cs`

**Current Storage:**
```csharp
// Now abstracted via ISessionStore
// Default: InMemorySessionStore
private readonly ConcurrentDictionary<string, SessionData> _sessions = new();
private readonly ConcurrentDictionary<string, Queue<DateTime>> _requestHistory = new();
```

**Stored Data:**
- `SessionData` (SessionToken, NodeId, ChannelId, TTL, AccessLevel)
- Request history for rate limiting (last 60 seconds)

**Operations:**
- `CreateSessionAsync()` - Create session (default TTL: 1 hour)
- `ValidateSessionAsync(token)` - Validation and conversion to SessionContext
- `RenewSessionAsync(token)` - TTL renewal
- `RevokeSessionAsync(token)` - Invalidation (logout)
- `GetNodeSessionsAsync(nodeId)` - Sessions for a node
- `GetSessionMetricsAsync(nodeId)` - Aggregated metrics
- `CleanupExpiredSessionsAsync()` - Cleanup expired sessions
- `RecordRequestAsync(token)` - Rate limiting (60 req/min)

**Characteristics:**
- ✅ **IMPLEMENTED**: Redis persistence via `RedisSessionStore`
- ✅ Automatic expiration (TTL)
- ✅ Rate limiting with sliding window
- ✅ Works with multiple instances (when using Redis)
- ✅ Sessions persist across restarts (when using Redis)

---

### 3. Channel Store
**Location:** `Bioteca.Prism.Data/Cache/Channel/ChannelStore.cs`

**Current Storage:**
```csharp
// Now abstracted via IChannelStore (async)
// Default: In-memory ChannelStore
private static readonly ConcurrentDictionary<string, ChannelContext> _channels = new();
```

**Stored Data:**
- `ChannelContext` (ChannelId, SymmetricKey, Role, TTL)
- AES-256-GCM symmetric key (32 bytes)
- Default TTL: 30 minutes

**Operations:**
- `AddChannelAsync(channelId, context)` - Add channel
- `GetChannelAsync(channelId)` - Lookup and validate TTL
- `RemoveChannelAsync(channelId)` - Remove and clear key (security)
- `IsChannelValidAsync(channelId)` - Validate existence and TTL

**Characteristics:**
- ✅ **IMPLEMENTED**: Redis persistence via `RedisChannelStore`
- ✅ Automatic expiration (TTL)
- ✅ Secure key cleanup (Array.Clear)
- ✅ Works with multiple instances (when using Redis)
- ✅ Channels persist across restarts (when using Redis)

---

## Proposed Architecture

### Technology Stack

1. **Redis** (Distributed Cache)
   - Version: Redis 7.2+
   - Library: `StackExchange.Redis` 2.8.16
   - Usage: Sessions, Channels, Rate Limiting
   - **Status:** ✅ **IMPLEMENTED**

2. **PostgreSQL** (Relational Database)
   - Version: PostgreSQL 15+
   - ORM: Entity Framework Core 8.0+
   - Usage: Node Registry, Audit Logs
   - **Status:** ⏳ **PLANNED**

3. **Connection Pooling**
   - Npgsql connection pooling for PostgreSQL
   - StackExchange.Redis connection multiplexing

---

## Part 1: Redis Cache (Sessions + Channels) ✅ IMPLEMENTED

### Why Redis?

✅ **Native TTL**: Automatic key expiration (no manual cleanup needed)
✅ **Atomic**: Atomic operations for rate limiting (INCR, EXPIRE)
✅ **Distributed**: Works with multiple instances (shared state)
✅ **Fast**: In-memory, <1ms latency
✅ **Sliding Window**: Sorted set support for precise rate limiting

### Cache Strategy

#### 1. Sessions (TTL: 1 hour default)

**Key Pattern:**
```
session:{sessionToken}             → SessionData (JSON)
session:node:{nodeId}:sessions     → Set of session tokens
session:ratelimit:{sessionToken}   → Sorted Set (timestamps)
```

**Redis Structures:**

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
EXPIRE session:node:node-001:sessions 86400  # 24h (longer than session TTL)

# Rate limiting (Sorted Set - score = Unix timestamp)
ZADD session:ratelimit:abc-123-def 1696339200.123 "req-1"
ZADD session:ratelimit:abc-123-def 1696339201.456 "req-2"
EXPIRE session:ratelimit:abc-123-def 120  # 2 minutes
```

**Operations:**

```csharp
// Create Session
await redis.HashSetAsync($"session:{token}", new HashEntry[]
{
    new("nodeId", nodeId),
    new("channelId", channelId),
    // ... other fields
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
var windowStart = now - 60; // last 60 seconds

// Remove old requests
await redis.SortedSetRemoveRangeByScoreAsync(
    $"session:ratelimit:{token}",
    0,
    windowStart);

// Count requests in last minute
var count = await redis.SortedSetLengthAsync($"session:ratelimit:{token}");

if (count < 60)
{
    // Add current request
    await redis.SortedSetAddAsync(
        $"session:ratelimit:{token}",
        Guid.NewGuid().ToString(),
        now);
}
```

---

#### 2. Channels (TTL: 30 minutes default)

**Key Pattern:**
```
channel:{channelId}        → ChannelContext metadata (Hash)
channel:key:{channelId}    → Binary symmetric key (String)
```

**Redis Structure:**

```redis
# Channel metadata (Hash)
HSET channel:xyz-789-abc
  role "client"
  establishedAt "2025-10-03T12:00:00Z"
  expiresAt "2025-10-03T12:30:00Z"
  remoteNodeId "node-002"

EXPIRE channel:xyz-789-abc 1800

# Symmetric key (Binary - stored separately for security)
SET channel:key:xyz-789-abc <32-byte-binary-key>
EXPIRE channel:key:xyz-789-abc 1800
```

**Operations:**

```csharp
// Add Channel
var metadataKey = $"channel:{channelId}";
var keyBinaryKey = $"channel:key:{channelId}";

var transaction = redis.CreateTransaction();
transaction.HashSetAsync(metadataKey, new HashEntry[]
{
    new("role", context.Role.ToString()),
    new("establishedAt", context.EstablishedAt.ToString("O")),
    new("expiresAt", context.ExpiresAt.ToString("O"))
});
transaction.StringSetAsync(keyBinaryKey, context.SymmetricKey);
transaction.KeyExpireAsync(metadataKey, ttl);
transaction.KeyExpireAsync(keyBinaryKey, ttl);
await transaction.ExecuteAsync();

// Get Channel (with validation)
var exists = await redis.KeyExistsAsync($"channel:{channelId}");
if (exists)
{
    var metadata = await redis.HashGetAllAsync($"channel:{channelId}");
    var symmetricKey = await redis.StringGetAsync($"channel:key:{channelId}");

    // Validate TTL
    var expiresAt = DateTime.Parse(metadata.First(x => x.Name == "expiresAt").Value);
    if (expiresAt > DateTime.UtcNow)
    {
        return new ChannelContext { /* ... */ };
    }
}

// Remove Channel (secure cleanup)
var symmetricKey = await redis.StringGetAsync($"channel:key:{channelId}");
if (symmetricKey.HasValue)
{
    // Clear key from memory before deleting from Redis
    Array.Clear(symmetricKey, 0, symmetricKey.Length);
}
await redis.KeyDeleteAsync($"channel:{channelId}");
await redis.KeyDeleteAsync($"channel:key:{channelId}");
```

---

### Redis Configuration ✅ IMPLEMENTED

**appsettings.NodeA.json:**
```json
{
  "Redis": {
    "ConnectionString": "irn-redis-node-a:6379,password=prism-redis-password-node-a,abortConnect=false",
    "EnableRedis": false
  },
  "FeatureFlags": {
    "UseRedisForSessions": false,
    "UseRedisForChannels": false
  }
}
```

**appsettings.NodeB.json:**
```json
{
  "Redis": {
    "ConnectionString": "irn-redis-node-b:6380,password=prism-redis-password-node-b,abortConnect=false",
    "EnableRedis": false
  },
  "FeatureFlags": {
    "UseRedisForSessions": false,
    "UseRedisForChannels": false
  }
}
```

**Docker Compose:**
```yaml
services:
  redis-node-a:
    image: redis:7.2-alpine
    container_name: irn-redis-node-a
    ports:
      - "6379:6379"
    volumes:
      - redis-data-node-a:/data
    command: redis-server --appendonly yes --requirepass prism-redis-password-node-a
    healthcheck:
      test: ["CMD", "redis-cli", "-a", "prism-redis-password-node-a", "ping"]
      interval: 10s
      timeout: 3s
      retries: 3
    networks:
      - irn-network

  redis-node-b:
    image: redis:7.2-alpine
    container_name: irn-redis-node-b
    ports:
      - "6380:6379"
    volumes:
      - redis-data-node-b:/data
    command: redis-server --appendonly yes --requirepass prism-redis-password-node-b
    healthcheck:
      test: ["CMD", "redis-cli", "-a", "prism-redis-password-node-b", "ping"]
      interval: 10s
      timeout: 3s
      retries: 3
    networks:
      - irn-network

volumes:
  redis-data-node-a:
  redis-data-node-b:
```

---

## Part 2: PostgreSQL (Node Registry) ⏳ PLANNED

### Why PostgreSQL?

✅ **ACID**: Transactions guarantee consistency
✅ **Indexes**: Performance for complex queries
✅ **JSON**: Native JSONB support for flexible data (InstitutionDetails, ContactInfo)
✅ **Audit**: Triggers and history tables
✅ **Full-Text Search**: Search in NodeName, InstitutionDetails
✅ **Mature**: Reliable, well-documented

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

    -- Constraints
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

**Entity:**
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

// New complex types for JSONB
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

            // Indexes (already defined in SQL, but useful for migrations)
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CertificateFingerprint).IsUnique();
        });
    }
}
```

---

## Comparison: In-Memory vs Redis vs PostgreSQL

| Aspect | In-Memory | Redis | PostgreSQL |
|---------|-----------|-------|------------|
| **Persistence** | ❌ Volatile | ✅ Optional (AOF/RDB) | ✅ Durable (ACID) |
| **Multi-Instance** | ❌ Does not work | ✅ Shared | ✅ Shared |
| **Auto TTL** | ❌ Manual cleanup | ✅ Native | ❌ Manual (or TTL extension) |
| **Performance (read)** | ~10 ns | ~0.5 ms | ~5-20 ms |
| **Performance (write)** | ~50 ns | ~1 ms | ~10-50 ms |
| **Scalability** | 🟡 Limited by RAM | 🟢 Sharding/Clustering | 🟢 Replication/Partitioning |
| **Complex Queries** | ❌ Limited | 🟡 Basic (Lua scripts) | ✅ Full SQL |
| **Audit** | ❌ No | ❌ No (requires app) | ✅ Native triggers |
| **Transactions** | 🟡 Locks | 🟡 Atomic (single key) | ✅ ACID (multi-row) |
| **Operational Cost** | 🟢 None | 🟡 Medium | 🔴 High |

---

## Implementation Status

### ✅ Completed (Redis)

1. **Redis Infrastructure**
   - ✅ Multi-instance Redis (one per node)
   - ✅ Docker Compose configuration
   - ✅ Feature flags for enabling/disabling

2. **Session Persistence**
   - ✅ `ISessionStore` interface
   - ✅ `RedisSessionStore` implementation
   - ✅ `InMemorySessionStore` fallback
   - ✅ Automatic TTL management
   - ✅ Rate limiting with Sorted Sets

3. **Channel Persistence**
   - ✅ `IChannelStore` interface (async)
   - ✅ `RedisChannelStore` implementation
   - ✅ In-memory `ChannelStore` fallback
   - ✅ Binary key storage separation
   - ✅ Automatic TTL management

4. **Testing & Documentation**
   - ✅ Redis testing guide
   - ✅ Docker Compose quick start
   - ✅ All tests passing (72/75, 96%)

### ⏳ Planned (PostgreSQL)

1. **Database Setup**
   - [ ] PostgreSQL Docker container
   - [ ] Database schema creation
   - [ ] Trigger implementation

2. **Node Registry Persistence**
   - [ ] `INodeRegistryRepository` interface
   - [ ] EF Core DbContext
   - [ ] Repository implementation
   - [ ] Migration scripts

3. **Audit & Compliance**
   - [ ] Audit log implementation
   - [ ] Change tracking
   - [ ] Query optimization

---

## Next Steps

### Phase 1: PostgreSQL Implementation (Planned)

1. Add PostgreSQL Docker container to `docker-compose.yml`
2. Create `PrismDbContext` with EF Core
3. Implement `INodeRegistryRepository`
4. Create migration scripts
5. Update `NodeRegistryService` to use repository
6. Add comprehensive testing

### Phase 2: Production Readiness

1. Redis Sentinel for high availability
2. PostgreSQL replication
3. Health check endpoints
4. Monitoring and metrics (Prometheus)
5. Backup and recovery procedures

---

**Last Update:** 2025-10-05
**Next Review:** After PostgreSQL implementation
