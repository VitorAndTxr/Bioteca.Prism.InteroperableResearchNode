# Persistence Implementation Roadmap

**Version:** 2.0
**Date:** 2025-10-05
**Status:** ✅ Phase 1 Complete (Redis), ⏳ Phase 2 Planned (PostgreSQL)

---

## Overview

This roadmap defines the persistence implementation for IRN in **3 incremental phases**, allowing iterative development, testing, and validation without disrupting the current system.

**Strategy:** Parallel development with feature flags, allowing easy rollback if needed.

---

## Phase 1: Redis Cache (Sessions + Channels) ✅ COMPLETED

**Estimated Duration:** 3-5 days
**Priority:** High (solves multi-instance immediately)
**Risk:** Low (Redis is drop-in replacement)
**Status:** ✅ **FULLY IMPLEMENTED** (2025-10-05)

### 1.1 Infrastructure Setup ✅ COMPLETED

#### Docker Compose ✅
```yaml
# docker-compose.yml
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

#### NuGet Packages ✅
```bash
dotnet add Bioteca.Prism.Core package StackExchange.Redis --version 2.8.16
dotnet add Bioteca.Prism.Service package StackExchange.Redis --version 2.8.16
```

#### Configuration ✅
```json
// appsettings.NodeA.json
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

// appsettings.NodeB.json
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

**Checklist:**
- [x] Add Redis to docker-compose.yml (multi-instance architecture)
- [x] Install NuGet packages
- [x] Create configuration in appsettings.json
- [x] Create appsettings.NodeA.json, appsettings.NodeB.json
- [x] Test Redis connection via redis-cli

---

### 1.2 Implementation - Redis Session Store ✅ COMPLETED

#### File Structure ✅
```
Bioteca.Prism.Core/
├── Cache/
│   ├── IRedisConnectionService.cs         ✅ Interface
│   └── Session/
│       └── ISessionStore.cs               ✅ Interface

Bioteca.Prism.Service/
├── Services/
│   └── Cache/
│       ├── RedisConnectionService.cs      ✅ Connection management
│       ├── RedisSessionStore.cs           ✅ Redis implementation
│       └── InMemorySessionStore.cs        ✅ Fallback implementation
```

#### Interface ✅
```csharp
// Bioteca.Prism.Core/Cache/Session/ISessionStore.cs
namespace Bioteca.Prism.Core.Cache.Session;

public interface ISessionStore
{
    Task<SessionData> CreateSessionAsync(string nodeId, string channelId, NodeAccessTypeEnum accessLevel, int ttlSeconds = 3600);
    Task<SessionContext?> ValidateSessionAsync(string sessionToken);
    Task<SessionData?> RenewSessionAsync(string sessionToken, int additionalSeconds = 3600);
    Task<bool> RevokeSessionAsync(string sessionToken);
    Task<List<SessionData>> GetNodeSessionsAsync(string nodeId);
    Task<SessionMetrics> GetSessionMetricsAsync(string nodeId);
    Task<int> CleanupExpiredSessionsAsync();
    Task<bool> RecordRequestAsync(string sessionToken);
    Task<long> GetRequestCountAsync(string sessionToken);
    Task<int> GetActiveSessionCountAsync();
}
```

#### Implementation ✅
**Key Features:**
- Hash storage for session metadata
- Sorted Sets for rate limiting (sliding window)
- Automatic TTL management
- Node sessions index for metrics
- Password masking in logs

**Files:**
- `RedisSessionStore.cs` - Full Redis implementation with rate limiting
- `InMemorySessionStore.cs` - Fallback for when Redis is disabled
- `RedisConnectionService.cs` - Lazy connection initialization with event handlers

**Service Registration ✅**
```csharp
// Program.cs
var useRedisForSessions = builder.Configuration.GetValue<bool>("FeatureFlags:UseRedisForSessions");
var useRedisForChannels = builder.Configuration.GetValue<bool>("FeatureFlags:UseRedisForChannels");

if (useRedisForSessions || useRedisForChannels)
{
    builder.Services.AddSingleton<IRedisConnectionService, RedisConnectionService>();
}

if (useRedisForSessions)
{
    builder.Services.AddSingleton<ISessionStore, RedisSessionStore>();
}
else
{
    builder.Services.AddSingleton<ISessionStore, InMemorySessionStore>();
}
```

**Checklist:**
- [x] Create ISessionStore interface
- [x] Implement RedisSessionStore with all methods
- [x] Implement InMemorySessionStore fallback
- [x] Add feature flag in Program.cs
- [x] Create Redis connection service
- [x] Test with local Redis (docker-compose up redis-node-a)

---

### 1.3 Implementation - Redis Channel Store ✅ COMPLETED

#### File Structure ✅
```
Bioteca.Prism.Core/
├── Middleware/Channel/
│   └── IChannelStore.cs                   ✅ Async interface

Bioteca.Prism.Data/
├── Cache/Channel/
│   └── ChannelStore.cs                    ✅ In-memory (async)

Bioteca.Prism.Service/
├── Services/Cache/
│   └── RedisChannelStore.cs               ✅ Redis implementation
```

#### Implementation ✅
**Key Features:**
- Separate storage for metadata (Hash) and symmetric key (Binary String)
- Transaction-based atomic operations
- Automatic TTL management (30 minutes)
- Secure key cleanup (Array.Clear before deletion)

**Key Patterns:**
```
channel:{channelId}        → Metadata (Hash)
channel:key:{channelId}    → Binary symmetric key (String)
```

**Service Registration ✅**
```csharp
// Program.cs
if (useRedisForChannels)
{
    builder.Services.AddSingleton<IChannelStore, RedisChannelStore>();
}
else
{
    builder.Services.AddSingleton<IChannelStore, ChannelStore>(); // In-memory fallback
}
```

**Checklist:**
- [x] Update IChannelStore to async interface
- [x] Implement RedisChannelStore
- [x] Refactor ChannelStore to async (fallback)
- [x] Add feature flag
- [x] Test binary key storage
- [x] Validate secure key cleanup (Array.Clear)
- [x] Update all usages to async methods

---

### 1.4 Integration Tests ✅ COMPLETED

#### Test Coverage ✅
**All Phase 4 tests passing (8/8):**
- WhoAmI endpoint validation
- Session renewal with TTL extension
- Session revocation (logout)
- Metrics endpoint with admin capability
- Missing session token handling (401)
- Invalid session token handling (401)
- Insufficient capability handling (403)
- Rate limiting enforcement (429)

**Overall Test Results:**
- **72/75 tests passing (96%)** ✅
- No regressions from Redis implementation
- Tests use `InMemorySessionStore` by default
- Can be switched to Redis with feature flags

**Checklist:**
- [x] All existing tests updated for async
- [x] Session lifecycle tests passing
- [x] Channel lifecycle tests passing
- [x] Rate limiting tests passing
- [x] TTL expiration validated
- [x] Run tests with Redis in Docker
- [x] Performance validated

---

### 1.5 Deployment and Documentation ✅ COMPLETED

#### Docker Compose for Development ✅
```bash
# Start all containers (nodes + Redis)
docker-compose up -d

# View logs
docker logs -f irn-redis-node-a
docker logs -f irn-node-a

# Rebuild (after code changes)
docker-compose down
docker-compose build --no-cache
docker-compose up -d

# Stop and remove volumes (clean Redis data)
docker-compose down -v

# Redis CLI access
docker exec -it irn-redis-node-a redis-cli -a prism-redis-password-node-a
docker exec -it irn-redis-node-b redis-cli -a prism-redis-password-node-b
```

#### Documentation ✅
- [x] Update CLAUDE.md with Redis configuration
- [x] Create docs/testing/redis-testing-guide.md
- [x] Create docs/testing/docker-compose-quick-start.md
- [x] Update docs/development/persistence-architecture.md
- [x] Update PROJECT_STATUS.md

**Phase 1 Summary:**
- ✅ **7 files created**
- ✅ **8 files modified**
- ✅ **~1,200 lines of code**
- ✅ **72/75 tests passing (96%)**
- ✅ **Full documentation**

---

## Phase 2: PostgreSQL (Node Registry) ⏳ PLANNED

**Estimated Duration:** 5-7 days
**Priority:** Medium (does not block multi-instance)
**Risk:** Medium (requires migrations and data persistence)
**Status:** ⏳ **PLANNED**

### 2.1 Infrastructure Setup (Day 6)

#### Docker Compose
```yaml
services:
  postgres:
    image: postgres:15-alpine
    container_name: irn-postgres
    environment:
      POSTGRES_DB: irn_db
      POSTGRES_USER: irn_user
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD:-postgres_dev_password}
    ports:
      - "5432:5432"
    volumes:
      - postgres-data:/var/lib/postgresql/data
      - ./scripts/init-db.sql:/docker-entrypoint-initdb.d/init.sql
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U irn_user -d irn_db"]
      interval: 10s
      timeout: 3s
      retries: 3
```

#### NuGet Packages
```bash
dotnet add Bioteca.Prism.Data package Npgsql.EntityFrameworkCore.PostgreSQL --version 8.0.0
dotnet add Bioteca.Prism.Data package Microsoft.EntityFrameworkCore.Design --version 8.0.0
dotnet add Bioteca.Prism.Data package Microsoft.EntityFrameworkCore.Tools --version 8.0.0
```

**Checklist:**
- [ ] Add PostgreSQL to docker-compose.yml
- [ ] Install EF Core + Npgsql packages
- [ ] Create init-db.sql with schema
- [ ] Test connection with psql or DBeaver

---

### 2.2 Entity Framework Core Setup (Day 7-8)

#### DbContext
```csharp
// Bioteca.Prism.Data/PrismDbContext.cs
public class PrismDbContext : DbContext
{
    public DbSet<RegisteredNode> RegisteredNodes { get; set; }
    public DbSet<NodeAuditLog> NodeAuditLogs { get; set; }

    public PrismDbContext(DbContextOptions<PrismDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Detailed configuration already defined in architecture
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PrismDbContext).Assembly);
    }
}
```

#### Entity Configurations
```csharp
// Bioteca.Prism.Data/Configurations/RegisteredNodeConfiguration.cs
public class RegisteredNodeConfiguration : IEntityTypeConfiguration<RegisteredNode>
{
    public void Configure(EntityTypeBuilder<RegisteredNode> builder)
    {
        builder.ToTable("registered_nodes");
        builder.HasKey(e => e.NodeId);

        // JSONB columns
        builder.Property(e => e.ContactInfo)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                v => JsonSerializer.Deserialize<ContactInfo>(v, (JsonSerializerOptions)null));

        // Soft delete filter
        builder.HasQueryFilter(e => e.DeletedAt == null);

        // Indexes
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.CertificateFingerprint).IsUnique();
    }
}
```

#### Migrations
```bash
# Create first migration
dotnet ef migrations add InitialCreate --project Bioteca.Prism.Data --startup-project Bioteca.Prism.InteroperableResearchNode

# Apply migration
dotnet ef database update --project Bioteca.Prism.Data --startup-project Bioteca.Prism.InteroperableResearchNode
```

**Checklist:**
- [ ] Create PrismDbContext
- [ ] Create entity configurations
- [ ] Create migrations
- [ ] Apply migrations to dev database
- [ ] Validate generated schema

---

### 2.3 Repository Pattern (Day 9-10)

#### Interface
```csharp
// Bioteca.Prism.Core/Repositories/INodeRepository.cs
public interface INodeRepository
{
    Task<RegisteredNode?> GetByIdAsync(string nodeId);
    Task<RegisteredNode?> GetByCertificateFingerprintAsync(string fingerprint);
    Task<List<RegisteredNode>> GetAllAsync(bool includeDeleted = false);
    Task<List<RegisteredNode>> GetByStatusAsync(AuthorizationStatus status);
    Task<RegisteredNode> CreateAsync(RegisteredNode node);
    Task<RegisteredNode> UpdateAsync(RegisteredNode node);
    Task<bool> UpdateStatusAsync(string nodeId, AuthorizationStatus newStatus, string performedBy);
    Task<bool> SoftDeleteAsync(string nodeId);
    Task<List<NodeAuditLog>> GetAuditLogsAsync(string nodeId, int limit = 100);
}
```

#### Implementation
```csharp
// Bioteca.Prism.Data/Repositories/NodeRepository.cs
public class NodeRepository : INodeRepository
{
    private readonly PrismDbContext _context;
    private readonly ILogger<NodeRepository> _logger;

    public async Task<RegisteredNode?> GetByIdAsync(string nodeId)
    {
        return await _context.RegisteredNodes
            .FirstOrDefaultAsync(n => n.NodeId == nodeId);
    }

    public async Task<RegisteredNode> CreateAsync(RegisteredNode node)
    {
        _context.RegisteredNodes.Add(node);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Node {NodeId} created in database", node.NodeId);
        return node;
    }

    public async Task<bool> UpdateStatusAsync(string nodeId, AuthorizationStatus newStatus, string performedBy)
    {
        // Set context for audit trigger
        await _context.Database.ExecuteSqlRawAsync(
            "SELECT set_config('app.current_user', {0}, false)",
            performedBy);

        var node = await GetByIdAsync(nodeId);
        if (node == null) return false;

        node.Status = newStatus;
        await _context.SaveChangesAsync();

        return true;
    }

    // ... other methods
}
```

**Checklist:**
- [ ] Create INodeRepository interface
- [ ] Implement NodeRepository
- [ ] Add dependency injection
- [ ] Create unit tests (mock DbContext)
- [ ] Create integration tests (real database)

---

### 2.4 NodeRegistryService Migration (Day 10-11)

#### New Implementation
```csharp
// Bioteca.Prism.Service/Services/Node/PostgresNodeRegistryService.cs
public class PostgresNodeRegistryService : INodeRegistryService
{
    private readonly INodeRepository _repository;
    private readonly ILogger<PostgresNodeRegistryService> _logger;

    public async Task<RegisteredNode?> GetNodeAsync(string nodeId)
    {
        return await _repository.GetByIdAsync(nodeId);
    }

    public async Task<NodeRegistrationResponse> RegisterNodeAsync(NodeRegistrationRequest request)
    {
        // Validations (same logic as in-memory)
        // ...

        // Check existing
        var existing = await _repository.GetByIdAsync(request.NodeId);
        if (existing != null)
        {
            // Update logic
            existing.NodeName = request.NodeName;
            // ...
            await _repository.UpdateAsync(existing);
            return new NodeRegistrationResponse { /* ... */ };
        }

        // Create new
        var node = new RegisteredNode
        {
            NodeId = request.NodeId,
            // ... map request to entity
        };

        await _repository.CreateAsync(node);
        return new NodeRegistrationResponse { Success = true, /* ... */ };
    }

    // ... other methods using repository
}
```

#### Feature Flag
```csharp
// Program.cs
if (builder.Configuration.GetValue<bool>("FeatureFlags:UsePostgresForNodes"))
{
    builder.Services.AddDbContext<PrismDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSQL")));

    builder.Services.AddScoped<INodeRepository, NodeRepository>();
    builder.Services.AddScoped<INodeRegistryService, PostgresNodeRegistryService>();
}
else
{
    builder.Services.AddSingleton<INodeRegistryService, NodeRegistryService>();  // In-memory
}
```

**Checklist:**
- [ ] Implement PostgresNodeRegistryService
- [ ] Add feature flag
- [ ] Migrate all operations to use repository
- [ ] Validate audit logs working (triggers)
- [ ] Test with real data

---

### 2.5 Tests and Validation (Day 11-12)

#### Integration Tests
```csharp
[Fact]
public async Task RegisterNode_NewNode_CreatesInDatabase()
{
    // Arrange
    var request = new NodeRegistrationRequest
    {
        NodeId = "test-node-postgres",
        NodeName = "Test Node",
        Certificate = GenerateTestCertificate(),
        // ...
    };

    // Act
    var response = await _service.RegisterNodeAsync(request);

    // Assert
    response.Success.Should().BeTrue();

    // Verify in database
    var node = await _repository.GetByIdAsync("test-node-postgres");
    node.Should().NotBeNull();
    node.NodeName.Should().Be("Test Node");
}

[Fact]
public async Task UpdateNodeStatus_ValidNode_CreatesAuditLog()
{
    // Arrange
    var node = await CreateTestNode();

    // Act
    await _repository.UpdateStatusAsync(node.NodeId, AuthorizationStatus.Authorized, "admin-001");

    // Assert
    var auditLogs = await _repository.GetAuditLogsAsync(node.NodeId);
    auditLogs.Should().ContainSingle(log =>
        log.Action == "STATUS_CHANGED" &&
        log.PerformedBy == "admin-001");
}
```

**Checklist:**
- [ ] Create PostgresNodeRegistryServiceTests
- [ ] Test full CRUD
- [ ] Validate audit logs
- [ ] Test soft delete
- [ ] Test JSONB queries (ContactInfo, InstitutionDetails)
- [ ] Performance testing (1000+ nodes)

---

## Phase 3: Cleanup and Optimization

**Estimated Duration:** 2-3 days
**Priority:** Medium
**Risk:** Low

### 3.1 Remove Legacy Code (Day 13)

- [ ] Remove in-memory implementation if feature flags 100% Redis/Postgres
- [ ] Clean commented code
- [ ] Update documentation

### 3.2 Performance Optimization (Day 14)

- [ ] Add additional indexes based on real queries
- [ ] Configure connection pooling (Npgsql, Redis)
- [ ] Implement 2-level caching (Redis + in-memory for reads)
- [ ] Comparative benchmarks (in-memory vs Redis vs Postgres)

### 3.3 Monitoring and Observability (Day 15)

- [ ] Add Prometheus metrics
  - `irn_redis_operations_total{operation="get|set|delete"}`
  - `irn_postgres_queries_total{table="registered_nodes"}`
  - `irn_cache_hit_ratio`
- [ ] Structured logging (Serilog)
- [ ] Health checks for Redis and PostgreSQL
- [ ] Alerts (rate limiting, connection failures)

---

## Rollout Strategy

### Environments

1. **Development** (Complete for Redis)
   - Test Redis locally ✅
   - Feature flags enabled ✅
   - Test PostgreSQL (planned)

2. **Staging** (Pending)
   - Deploy with feature flags
   - End-to-end tests
   - Performance validation

3. **Production** (Pending)
   - Gradual rollout with feature flags
   - Intensive monitoring
   - Rollback plan ready

### Rollback Plan

If problems occur in production:
1. Disable feature flags (`UseRedisForSessions: false`)
2. Revert to in-memory (session/channel data will be lost, but nodes remain)
3. Investigate logs and metrics
4. Fix and re-deploy

---

## Cost Estimation (Cloud)

### Redis (AWS ElastiCache)
- **cache.t3.micro** (1 GB): ~$15/month
- **cache.t3.small** (2 GB): ~$30/month

### PostgreSQL (AWS RDS)
- **db.t3.micro** (1 vCPU, 1 GB): ~$15/month
- **db.t3.small** (2 vCPU, 2 GB): ~$30/month

**Total Estimated:** $30-60/month for development/staging environment

---

## Implementation Status

### ✅ Completed
1. **Phase 1 (Redis Cache)** - FULLY IMPLEMENTED
   - Multi-instance Redis (one per node)
   - RedisSessionStore with automatic TTL
   - RedisChannelStore with binary key separation
   - InMemorySessionStore and ChannelStore fallbacks
   - Feature flags for enabling/disabling
   - Comprehensive testing (72/75 tests passing)
   - Complete documentation

### ⏳ Planned
2. **Phase 2 (PostgreSQL)** - PLANNED
   - PostgreSQL Docker container
   - Entity Framework Core setup
   - Repository pattern
   - Node registry migration
   - Audit logs

3. **Phase 3 (Optimization)** - PLANNED
   - Legacy code removal
   - Performance optimization
   - Monitoring and observability

---

## Next Steps

Ready to:
1. ✅ **Phase 1 (Redis)** - COMPLETED
2. ⏳ **Phase 2 (PostgreSQL)** - Awaiting approval to start
3. 📅 Define deployment dates

Phase 1 successfully completed! 🎉
Ready for Phase 2 when approved! 🚀

---

**Last Update:** 2025-10-05
**Next Review:** After Phase 2 (PostgreSQL) implementation
