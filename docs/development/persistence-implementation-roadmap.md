# Persistence Implementation Roadmap

**Versão:** 1.0
**Data:** 2025-10-03
**Status:** 📋 Planning

---

## Visão Geral

Este roadmap define a implementação de persistência para o IRN em **3 fases incrementais**, permitindo desenvolvimento, teste e validação iterativos sem interromper o sistema atual.

**Estratégia:** Desenvolvimento paralelo com feature flags, permitindo rollback fácil se necessário.

---

## Fase 1: Redis Cache (Sessions + Channels)

**Duração Estimada:** 3-5 dias
**Prioridade:** Alta (resolve multi-instance imediatamente)
**Risco:** Baixo (Redis é drop-in replacement)

### 1.1 Setup de Infraestrutura (Dia 1)

#### Docker Compose
```yaml
# docker-compose.yml
services:
  redis:
    image: redis:7.2-alpine
    container_name: irn-redis
    ports:
      - "6379:6379"
    volumes:
      - redis-data:/data
    command: >
      redis-server
      --appendonly yes
      --maxmemory 512mb
      --maxmemory-policy allkeys-lru
      --requirepass ${REDIS_PASSWORD:-redis_dev_password}
    healthcheck:
      test: ["CMD", "redis-cli", "--no-auth-warning", "-a", "${REDIS_PASSWORD:-redis_dev_password}", "ping"]
      interval: 10s
      timeout: 3s
      retries: 3
    networks:
      - irn-network

volumes:
  redis-data:
    driver: local
```

#### NuGet Packages
```bash
dotnet add Bioteca.Prism.Service package StackExchange.Redis --version 2.7.27
dotnet add Bioteca.Prism.Service package Microsoft.Extensions.Caching.StackExchangeRedis --version 8.0.0
```

#### Configuration
```json
// appsettings.json
{
  "Redis": {
    "ConnectionString": "localhost:6379,password=redis_dev_password",
    "InstanceName": "IRN:",
    "Database": 0,
    "ConnectTimeout": 5000,
    "SyncTimeout": 5000,
    "AbortOnConnectFail": false
  },
  "Cache": {
    "SessionTtlSeconds": 3600,
    "ChannelTtlSeconds": 1800,
    "RateLimitWindowSeconds": 60,
    "RateLimitMaxRequests": 60
  },
  "FeatureFlags": {
    "UseRedisForSessions": true,
    "UseRedisForChannels": true
  }
}
```

**Checklist Dia 1:**
- [ ] Adicionar Redis ao docker-compose.yml
- [ ] Instalar NuGet packages
- [ ] Criar configuração em appsettings.json
- [ ] Criar appsettings.Development.json, appsettings.NodeA.json, appsettings.NodeB.json
- [ ] Testar conexão Redis via redis-cli

---

### 1.2 Implementação - Redis Session Store (Dia 2-3)

#### Estrutura de Arquivos
```
Bioteca.Prism.Data/
├── Cache/
│   ├── Session/
│   │   ├── IRedisSessionStore.cs          # Interface
│   │   ├── RedisSessionStore.cs           # Implementação
│   │   └── RedisSessionStoreOptions.cs    # Configuração
│   └── Channel/
│       ├── IRedisChannelStore.cs
│       ├── RedisChannelStore.cs
│       └── ChannelStore.cs  (existing - keep for fallback)
```

#### Interface
```csharp
// Bioteca.Prism.Data/Cache/Session/IRedisSessionStore.cs
namespace Bioteca.Prism.Data.Cache.Session;

public interface IRedisSessionStore
{
    Task<SessionData> CreateSessionAsync(string nodeId, string channelId, NodeAccessTypeEnum accessLevel, int ttlSeconds = 3600);
    Task<SessionContext?> ValidateSessionAsync(string sessionToken);
    Task<SessionData?> RenewSessionAsync(string sessionToken, int additionalSeconds = 3600);
    Task<bool> RevokeSessionAsync(string sessionToken);
    Task<List<SessionData>> GetNodeSessionsAsync(string nodeId);
    Task<SessionMetrics> GetSessionMetricsAsync(string nodeId);
    Task<int> CleanupExpiredSessionsAsync();  // No-op para Redis (TTL automático)
    Task<bool> RecordRequestAsync(string sessionToken);
}
```

#### Implementação
```csharp
// Bioteca.Prism.Data/Cache/Session/RedisSessionStore.cs
using StackExchange.Redis;
using System.Text.Json;

namespace Bioteca.Prism.Data.Cache.Session;

public class RedisSessionStore : IRedisSessionStore
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _db;
    private readonly ILogger<RedisSessionStore> _logger;
    private readonly string _instancePrefix;
    private readonly int _maxRequestsPerMinute;

    public RedisSessionStore(
        IConnectionMultiplexer redis,
        ILogger<RedisSessionStore> logger,
        IConfiguration configuration)
    {
        _redis = redis;
        _db = redis.GetDatabase();
        _logger = logger;
        _instancePrefix = configuration["Redis:InstanceName"] ?? "IRN:";
        _maxRequestsPerMinute = configuration.GetValue<int>("Cache:RateLimitMaxRequests", 60);
    }

    public async Task<SessionData> CreateSessionAsync(
        string nodeId,
        string channelId,
        NodeAccessTypeEnum accessLevel,
        int ttlSeconds = 3600)
    {
        var sessionToken = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;

        var sessionData = new SessionData
        {
            SessionToken = sessionToken,
            NodeId = nodeId,
            ChannelId = channelId,
            CreatedAt = now,
            ExpiresAt = now.AddSeconds(ttlSeconds),
            LastAccessedAt = now,
            AccessLevel = accessLevel,
            RequestCount = 0
        };

        // Store session data as Hash
        var sessionKey = $"{_instancePrefix}session:{sessionToken}";
        var hashEntries = new HashEntry[]
        {
            new("nodeId", nodeId),
            new("channelId", channelId),
            new("createdAt", now.ToString("O")),
            new("expiresAt", sessionData.ExpiresAt.ToString("O")),
            new("lastAccessedAt", now.ToString("O")),
            new("accessLevel", (int)accessLevel),
            new("requestCount", 0)
        };

        await _db.HashSetAsync(sessionKey, hashEntries);
        await _db.KeyExpireAsync(sessionKey, TimeSpan.FromSeconds(ttlSeconds));

        // Add to node sessions index
        var nodeSessionsKey = $"{_instancePrefix}session:node:{nodeId}:sessions";
        await _db.SetAddAsync(nodeSessionsKey, sessionToken);
        await _db.KeyExpireAsync(nodeSessionsKey, TimeSpan.FromSeconds(ttlSeconds + 3600)); // Extra buffer

        _logger.LogInformation(
            "Redis: Session created for node {NodeId}: {SessionToken}, expires at {ExpiresAt}",
            nodeId, sessionToken, sessionData.ExpiresAt);

        return sessionData;
    }

    public async Task<SessionContext?> ValidateSessionAsync(string sessionToken)
    {
        var sessionKey = $"{_instancePrefix}session:{sessionToken}";

        // Check existence
        if (!await _db.KeyExistsAsync(sessionKey))
        {
            _logger.LogWarning("Redis: Session not found: {SessionToken}", sessionToken);
            return null;
        }

        // Get session data
        var hashEntries = await _db.HashGetAllAsync(sessionKey);
        if (hashEntries.Length == 0)
        {
            return null;
        }

        var data = hashEntries.ToDictionary(
            x => x.Name.ToString(),
            x => x.Value.ToString());

        var expiresAt = DateTime.Parse(data["expiresAt"]);
        if (expiresAt < DateTime.UtcNow)
        {
            _logger.LogWarning("Redis: Session expired: {SessionToken}", sessionToken);
            await _db.KeyDeleteAsync(sessionKey);
            return null;
        }

        // Update last accessed time
        await _db.HashSetAsync(sessionKey, "lastAccessedAt", DateTime.UtcNow.ToString("O"));

        var context = new SessionContext
        {
            SessionToken = sessionToken,
            NodeId = data["nodeId"],
            ChannelId = data["channelId"],
            ExpiresAt = expiresAt,
            NodeAccessLevel = (NodeAccessTypeEnum)int.Parse(data["accessLevel"]),
            RequestCount = int.Parse(data["requestCount"])
        };

        _logger.LogDebug(
            "Redis: Session validated: {SessionToken}, node {NodeId}, {RemainingSeconds}s remaining",
            sessionToken, context.NodeId, context.GetRemainingSeconds());

        return context;
    }

    public async Task<bool> RecordRequestAsync(string sessionToken)
    {
        var sessionKey = $"{_instancePrefix}session:{sessionToken}";

        // Check session exists
        if (!await _db.KeyExistsAsync(sessionKey))
        {
            return false;
        }

        // Increment request count
        await _db.HashIncrementAsync(sessionKey, "requestCount");
        await _db.HashSetAsync(sessionKey, "lastAccessedAt", DateTime.UtcNow.ToString("O"));

        // Rate limiting using Sorted Set (sliding window)
        var rateLimitKey = $"{_instancePrefix}session:ratelimit:{sessionToken}";
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var windowStart = now - 60; // Last 60 seconds

        // Remove old entries
        await _db.SortedSetRemoveRangeByScoreAsync(rateLimitKey, 0, windowStart);

        // Count current requests
        var requestCount = await _db.SortedSetLengthAsync(rateLimitKey);

        if (requestCount >= _maxRequestsPerMinute)
        {
            var nodeId = await _db.HashGetAsync(sessionKey, "nodeId");
            _logger.LogWarning(
                "Redis: Rate limit exceeded for session {SessionToken}, node {NodeId}",
                sessionToken, nodeId);
            return false;
        }

        // Add current request
        var requestId = Guid.NewGuid().ToString();
        await _db.SortedSetAddAsync(rateLimitKey, requestId, now);
        await _db.KeyExpireAsync(rateLimitKey, TimeSpan.FromSeconds(120)); // 2 minutes

        return true;
    }

    // ... outros métodos (RenewSessionAsync, RevokeSessionAsync, GetNodeSessionsAsync, etc.)
}
```

#### Service Registration
```csharp
// Program.cs
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var connectionString = configuration["Redis:ConnectionString"];
    return ConnectionMultiplexer.Connect(connectionString);
});

// Feature flag: Choose implementation
if (builder.Configuration.GetValue<bool>("FeatureFlags:UseRedisForSessions"))
{
    builder.Services.AddSingleton<ISessionService, RedisSessionStore>();
    builder.Services.AddSingleton<IRedisSessionStore, RedisSessionStore>();
}
else
{
    builder.Services.AddSingleton<ISessionService, SessionService>();  // In-memory fallback
}
```

**Checklist Dia 2-3:**
- [ ] Criar interface IRedisSessionStore
- [ ] Implementar RedisSessionStore com todos métodos
- [ ] Adicionar feature flag no Program.cs
- [ ] Criar testes unitários para RedisSessionStore
- [ ] Testar com Redis local (docker-compose up redis)

---

### 1.3 Implementação - Redis Channel Store (Dia 3-4)

#### Implementação
```csharp
// Bioteca.Prism.Data/Cache/Channel/RedisChannelStore.cs
public class RedisChannelStore : IChannelStore
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _db;
    private readonly ILogger<RedisChannelStore> _logger;
    private readonly string _instancePrefix;

    public async void AddChannel(string channelId, ChannelContext context)
    {
        var channelKey = $"{_instancePrefix}channel:{channelId}";
        var keyKey = $"{channelKey}:key";

        // Store metadata
        var hashEntries = new HashEntry[]
        {
            new("role", context.Role.ToString()),
            new("establishedAt", context.EstablishedAt.ToString("O")),
            new("expiresAt", context.ExpiresAt.ToString("O")),
            new("remoteNodeId", context.RemoteNodeId ?? "")
        };

        await _db.HashSetAsync(channelKey, hashEntries);

        // Store symmetric key (binary)
        await _db.StringSetAsync(keyKey, context.SymmetricKey);

        // Set TTL
        var ttl = context.ExpiresAt - DateTime.UtcNow;
        await _db.KeyExpireAsync(channelKey, ttl);
        await _db.KeyExpireAsync(keyKey, ttl);

        _logger.LogInformation(
            "Redis: Channel {ChannelId} added (role: {Role}, expires: {ExpiresAt})",
            channelId, context.Role, context.ExpiresAt);
    }

    public async ChannelContext? GetChannel(string channelId)
    {
        var channelKey = $"{_instancePrefix}channel:{channelId}";
        var keyKey = $"{channelKey}:key";

        // Check existence
        if (!await _db.KeyExistsAsync(channelKey))
        {
            return null;
        }

        // Get metadata
        var hashEntries = await _db.HashGetAllAsync(channelKey);
        var data = hashEntries.ToDictionary(x => x.Name.ToString(), x => x.Value.ToString());

        // Get symmetric key
        var symmetricKey = (byte[])await _db.StringGetAsync(keyKey);

        // Validate TTL
        var expiresAt = DateTime.Parse(data["expiresAt"]);
        if (expiresAt < DateTime.UtcNow)
        {
            _logger.LogWarning("Redis: Channel {ChannelId} expired, removing", channelId);
            await _db.KeyDeleteAsync(new RedisKey[] { channelKey, keyKey });
            return null;
        }

        var context = new ChannelContext
        {
            ChannelId = channelId,
            Role = Enum.Parse<ChannelRole>(data["role"]),
            EstablishedAt = DateTime.Parse(data["establishedAt"]),
            ExpiresAt = expiresAt,
            SymmetricKey = symmetricKey,
            RemoteNodeId = data.ContainsKey("remoteNodeId") ? data["remoteNodeId"] : null
        };

        _logger.LogDebug("Redis: Retrieved channel {ChannelId}", channelId);
        return context;
    }

    public async bool RemoveChannel(string channelId)
    {
        var channelKey = $"{_instancePrefix}channel:{channelId}";
        var keyKey = $"{channelKey}:key";

        // Get symmetric key to clear it securely
        var symmetricKey = (byte[])await _db.StringGetAsync(keyKey);
        if (symmetricKey != null)
        {
            Array.Clear(symmetricKey, 0, symmetricKey.Length);
        }

        // Delete keys
        var deleted = await _db.KeyDeleteAsync(new RedisKey[] { channelKey, keyKey });

        if (deleted > 0)
        {
            _logger.LogInformation("Redis: Channel {ChannelId} removed", channelId);
        }

        return deleted > 0;
    }
}
```

**Checklist Dia 3-4:**
- [ ] Implementar RedisChannelStore
- [ ] Adicionar feature flag
- [ ] Testar armazenamento de chaves binárias
- [ ] Validar limpeza segura de chaves (Array.Clear)
- [ ] Integração com testes existentes

---

### 1.4 Testes de Integração (Dia 4-5)

#### Testes Redis
```csharp
// Bioteca.Prism.InteroperableResearchNode.Test/RedisIntegrationTests.cs
public class RedisSessionStoreTests : IAsyncLifetime
{
    private IConnectionMultiplexer _redis;
    private RedisSessionStore _store;

    public async Task InitializeAsync()
    {
        _redis = await ConnectionMultiplexer.ConnectAsync("localhost:6379");
        _store = new RedisSessionStore(_redis, logger, configuration);

        // Clear test data
        var db = _redis.GetDatabase();
        await db.ExecuteAsync("FLUSHDB");
    }

    [Fact]
    public async Task CreateSession_ValidData_StoresInRedis()
    {
        // Arrange
        var nodeId = "test-node-001";
        var channelId = "test-channel-123";

        // Act
        var session = await _store.CreateSessionAsync(nodeId, channelId, NodeAccessTypeEnum.ReadWrite);

        // Assert
        session.Should().NotBeNull();
        session.NodeId.Should().Be(nodeId);
        session.ChannelId.Should().Be(channelId);

        // Verify in Redis
        var db = _redis.GetDatabase();
        var exists = await db.KeyExistsAsync($"IRN:session:{session.SessionToken}");
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateSession_ExpiredSession_ReturnsNull()
    {
        // Arrange
        var session = await _store.CreateSessionAsync("node-001", "channel-123", NodeAccessTypeEnum.ReadOnly, ttlSeconds: 1);
        await Task.Delay(2000); // Wait for expiration

        // Act
        var validated = await _store.ValidateSessionAsync(session.SessionToken);

        // Assert
        validated.Should().BeNull();
    }

    [Fact]
    public async Task RecordRequest_ExceedsRateLimit_ReturnsFalse()
    {
        // Arrange
        var session = await _store.CreateSessionAsync("node-001", "channel-123", NodeAccessTypeEnum.ReadWrite);

        // Act: Send 61 requests (limit is 60)
        for (int i = 0; i < 60; i++)
        {
            var result = await _store.RecordRequestAsync(session.SessionToken);
            result.Should().BeTrue();
        }

        var exceededResult = await _store.RecordRequestAsync(session.SessionToken);

        // Assert
        exceededResult.Should().BeFalse();
    }
}
```

**Checklist Dia 4-5:**
- [ ] Criar RedisSessionStoreTests com 10+ testes
- [ ] Criar RedisChannelStoreTests com 8+ testes
- [ ] Testar rate limiting com sliding window
- [ ] Testar expiração automática (TTL)
- [ ] Rodar testes com Redis no Docker
- [ ] Validar performance (benchmarks)

---

### 1.5 Deployment e Documentação (Dia 5)

#### Docker Compose para Desenvolvimento
```yaml
# docker-compose.dev.yml
version: '3.8'

services:
  redis:
    image: redis:7.2-alpine
    ports:
      - "6379:6379"
    volumes:
      - redis-data-dev:/data
    command: redis-server --appendonly yes --loglevel notice

  node-a:
    build: .
    environment:
      - ASPNETCORE_ENVIRONMENT=NodeA
      - Redis__ConnectionString=redis:6379
    depends_on:
      - redis

  node-b:
    build: .
    environment:
      - ASPNETCORE_ENVIRONMENT=NodeB
      - Redis__ConnectionString=redis:6379
    depends_on:
      - redis
```

#### Documentação
- [ ] Atualizar CLAUDE.md com Redis configuration
- [ ] Criar docs/deployment/redis-setup.md
- [ ] Documentar troubleshooting Redis
- [ ] Atualizar PROJECT_STATUS.md

---

## Fase 2: PostgreSQL (Node Registry)

**Duração Estimada:** 5-7 dias
**Prioridade:** Média (não bloqueia multi-instance)
**Risco:** Médio (requer migrations e data persistence)

### 2.1 Setup de Infraestrutura (Dia 6)

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

**Checklist Dia 6:**
- [ ] Adicionar PostgreSQL ao docker-compose.yml
- [ ] Instalar pacotes EF Core + Npgsql
- [ ] Criar init-db.sql com schema
- [ ] Testar conexão com psql ou DBeaver

---

### 2.2 Entity Framework Core Setup (Dia 7-8)

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
        // Configuração detalhada já definida na arquitetura
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
# Criar primeira migration
dotnet ef migrations add InitialCreate --project Bioteca.Prism.Data --startup-project Bioteca.Prism.InteroperableResearchNode

# Aplicar migration
dotnet ef database update --project Bioteca.Prism.Data --startup-project Bioteca.Prism.InteroperableResearchNode
```

**Checklist Dia 7-8:**
- [ ] Criar PrismDbContext
- [ ] Criar entity configurations
- [ ] Criar migrations
- [ ] Aplicar migrations no banco dev
- [ ] Validar schema gerado

---

### 2.3 Repository Pattern (Dia 9-10)

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

#### Implementação
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

    // ... outros métodos
}
```

**Checklist Dia 9-10:**
- [ ] Criar INodeRepository interface
- [ ] Implementar NodeRepository
- [ ] Adicionar injeção de dependência
- [ ] Criar testes unitários (mock DbContext)
- [ ] Criar testes de integração (banco real)

---

### 2.4 Migração do NodeRegistryService (Dia 10-11)

#### Nova Implementação
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
        // Validações (mesma lógica do in-memory)
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

    // ... outros métodos usando repository
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

**Checklist Dia 10-11:**
- [ ] Implementar PostgresNodeRegistryService
- [ ] Adicionar feature flag
- [ ] Migrar todas operações para usar repository
- [ ] Validar audit logs funcionando (triggers)
- [ ] Testar com dados reais

---

### 2.5 Testes e Validação (Dia 11-12)

#### Testes de Integração
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

**Checklist Dia 11-12:**
- [ ] Criar PostgresNodeRegistryServiceTests
- [ ] Testar CRUD completo
- [ ] Validar audit logs
- [ ] Testar soft delete
- [ ] Testar queries JSONB (ContactInfo, InstitutionDetails)
- [ ] Performance testing (1000+ nodes)

---

## Fase 3: Cleanup e Otimização

**Duração Estimada:** 2-3 dias
**Prioridade:** Média
**Risco:** Baixo

### 3.1 Remover Código Legacy (Dia 13)

- [ ] Remover implementação in-memory se feature flags 100% Redis/Postgres
- [ ] Limpar código comentado
- [ ] Atualizar documentação

### 3.2 Performance Optimization (Dia 14)

- [ ] Adicionar índices adicionais baseado em queries reais
- [ ] Configurar connection pooling (Npgsql, Redis)
- [ ] Implementar caching em 2 níveis (Redis + in-memory para reads)
- [ ] Benchmarks comparativos (in-memory vs Redis vs Postgres)

### 3.3 Monitoring e Observabilidade (Dia 15)

- [ ] Adicionar métricas Prometheus
  - `irn_redis_operations_total{operation="get|set|delete"}`
  - `irn_postgres_queries_total{table="registered_nodes"}`
  - `irn_cache_hit_ratio`
- [ ] Logs estruturados (Serilog)
- [ ] Health checks para Redis e PostgreSQL
- [ ] Alertas (rate limiting, connection failures)

---

## Rollout Strategy

### Ambientes

1. **Development** (Dia 1-12)
   - Testar Redis + PostgreSQL localmente
   - Feature flags habilitados

2. **Staging** (Dia 13-14)
   - Deploy com feature flags
   - Testes end-to-end
   - Validação de performance

3. **Production** (Dia 15+)
   - Rollout gradual com feature flags
   - Monitoramento intensivo
   - Rollback plan pronto

### Rollback Plan

Se houver problemas em produção:
1. Desabilitar feature flags (`UseRedisForSessions: false`)
2. Reverter para in-memory (dados de sessões/canais serão perdidos, mas nós permanecem)
3. Investigar logs e métricas
4. Corrigir e re-deploy

---

## Estimativa de Custos (Cloud)

### Redis (AWS ElastiCache)
- **cache.t3.micro** (1 GB): ~$15/mês
- **cache.t3.small** (2 GB): ~$30/mês

### PostgreSQL (AWS RDS)
- **db.t3.micro** (1 vCPU, 1 GB): ~$15/mês
- **db.t3.small** (2 vCPU, 2 GB): ~$30/mês

**Total Estimado:** $30-60/mês para ambiente de desenvolvimento/staging

---

## Próximos Passos

Aguardando aprovação para:
1. ✅ Iniciar Fase 1 (Redis)
2. ⏸️ Aguardar conclusão Fase 1 para iniciar Fase 2 (PostgreSQL)
3. 📅 Definir datas de deployment

Pronto para começar implementação! 🚀
