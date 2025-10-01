# Gerenciamento de Sessões entre Nós IRN

**Status**: 📋 Planejado
**Última atualização**: 2025-10-01

## Visão Geral

Gerencia o ciclo de vida de sessões estabelecidas entre nós IRN após handshake bem-sucedido, incluindo criação, manutenção, renovação e encerramento.

## Ciclo de Vida da Sessão

```
[Handshake] --> [Active] --> [Idle] --> [Renewed/Expired] --> [Closed]
                   │           │
                   └───────────┘
                   (heartbeat)
```

### Estados da Sessão

| Estado | Descrição | Timeout |
|--------|-----------|---------|
| `CREATING` | Sessão sendo estabelecida | 30s |
| `ACTIVE` | Sessão ativa com comunicação recente | - |
| `IDLE` | Sem atividade recente, mas válida | 5 min |
| `EXPIRING` | Próxima da expiração, renovação recomendada | 2 min |
| `EXPIRED` | Sessão expirada, requer novo handshake | - |
| `CLOSING` | Encerramento em andamento | 10s |
| `CLOSED` | Sessão encerrada | - |

## Modelo de Dados

### SessionInfo

```csharp
public class SessionInfo
{
    public Guid SessionId { get; set; }
    public Guid LocalNodeId { get; set; }
    public Guid RemoteNodeId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime LastActivityAt { get; set; }
    public SessionState State { get; set; }

    // Chave simétrica para criptografia da sessão
    public byte[] SessionKey { get; set; }

    // Token de refresh para renovação
    public string RefreshToken { get; set; }

    // Capacidades negociadas
    public SessionCapabilities Capabilities { get; set; }

    // Métricas da sessão
    public SessionMetrics Metrics { get; set; }
}

public enum SessionState
{
    Creating,
    Active,
    Idle,
    Expiring,
    Expired,
    Closing,
    Closed
}

public class SessionCapabilities
{
    public int MaxRequestSize { get; set; }
    public string[] SupportedQueries { get; set; }
    public RateLimitConfig RateLimit { get; set; }
    public bool SupportsStreaming { get; set; }
}

public class SessionMetrics
{
    public long RequestCount { get; set; }
    public long BytesTransferred { get; set; }
    public long ErrorCount { get; set; }
    public TimeSpan AverageLatency { get; set; }
}
```

## Operações de Sessão

### 1. Criação de Sessão

Ocorre após handshake bem-sucedido.

**Request:** `POST /api/node/v1/session/create`

```json
{
  "handshakeToken": "jwt-from-handshake",
  "requestedDuration": 3600,
  "capabilities": {
    "supportsStreaming": true,
    "preferredFormat": "json"
  }
}
```

**Response:**

```json
{
  "sessionId": "550e8400-e29b-41d4-a716-446655440000",
  "sessionKey": "encrypted-aes-key",
  "expiresAt": "2025-10-01T13:00:00Z",
  "refreshToken": "base64-refresh-token",
  "heartbeatInterval": 300,
  "capabilities": {
    "maxRequestSize": 10485760,
    "supportedQueries": ["metadata", "biosignal"],
    "rateLimit": {
      "requestsPerMinute": 60,
      "burstSize": 10
    }
  }
}
```

### 2. Heartbeat (Keep-Alive)

Mantém a sessão ativa e detecta desconexões.

**Request:** `POST /api/node/v1/session/{sessionId}/heartbeat`

```json
{
  "timestamp": "2025-10-01T12:05:00Z",
  "metrics": {
    "pendingRequests": 2,
    "queueSize": 0
  }
}
```

**Response:**

```json
{
  "acknowledged": true,
  "serverTime": "2025-10-01T12:05:01Z",
  "sessionState": "active",
  "remainingTime": 2940
}
```

### 3. Renovação de Sessão

Estende a validade da sessão sem novo handshake completo.

**Request:** `POST /api/node/v1/session/{sessionId}/renew`

```json
{
  "refreshToken": "base64-refresh-token",
  "requestedDuration": 3600
}
```

**Response:**

```json
{
  "newExpiresAt": "2025-10-01T14:00:00Z",
  "newRefreshToken": "new-base64-refresh-token",
  "sessionKey": "optionally-rotated-key"
}
```

### 4. Encerramento de Sessão

Encerra gracefully uma sessão.

**Request:** `DELETE /api/node/v1/session/{sessionId}`

```json
{
  "reason": "normal_closure",
  "message": "Session completed successfully"
}
```

**Response:**

```json
{
  "acknowledged": true,
  "finalMetrics": {
    "totalRequests": 142,
    "totalBytesTransferred": 5242880,
    "sessionDuration": 3542,
    "averageLatency": 125
  }
}
```

## Armazenamento de Sessões

### Opção 1: In-Memory (MVP)

```csharp
public class InMemorySessionStore : ISessionStore
{
    private readonly ConcurrentDictionary<Guid, SessionInfo> _sessions;
    private readonly ILogger<InMemorySessionStore> _logger;

    public Task<SessionInfo> GetSessionAsync(Guid sessionId)
    {
        _sessions.TryGetValue(sessionId, out var session);
        return Task.FromResult(session);
    }

    public Task StoreSessionAsync(SessionInfo session)
    {
        _sessions[session.SessionId] = session;
        return Task.CompletedTask;
    }

    public Task DeleteSessionAsync(Guid sessionId)
    {
        _sessions.TryRemove(sessionId, out _);
        return Task.CompletedTask;
    }
}
```

### Opção 2: Redis (Produção)

- Persistência
- Compartilhamento entre instâncias
- TTL automático

```csharp
public class RedisSessionStore : ISessionStore
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisSessionStore> _logger;

    public async Task<SessionInfo> GetSessionAsync(Guid sessionId)
    {
        var db = _redis.GetDatabase();
        var json = await db.StringGetAsync($"session:{sessionId}");
        return json.HasValue
            ? JsonSerializer.Deserialize<SessionInfo>(json)
            : null;
    }

    // ... implementação completa
}
```

## Background Services

### SessionCleanupService

Remove sessões expiradas periodicamente.

```csharp
public class SessionCleanupService : BackgroundService
{
    private readonly ISessionStore _sessionStore;
    private readonly ILogger<SessionCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await CleanupExpiredSessionsAsync();
            await Task.Delay(_cleanupInterval, stoppingToken);
        }
    }

    private async Task CleanupExpiredSessionsAsync()
    {
        var expiredSessions = await _sessionStore.GetExpiredSessionsAsync();

        foreach (var session in expiredSessions)
        {
            _logger.LogInformation(
                "Cleaning up expired session {SessionId} from node {NodeId}",
                session.SessionId, session.RemoteNodeId);

            await _sessionStore.DeleteSessionAsync(session.SessionId);
        }
    }
}
```

### SessionMonitoringService

Monitora saúde das sessões ativas.

```csharp
public class SessionMonitoringService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await MonitorActiveSessionsAsync();
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task MonitorActiveSessionsAsync()
    {
        var sessions = await _sessionStore.GetActiveSessionsAsync();

        foreach (var session in sessions)
        {
            // Verificar se heartbeat está atrasado
            if (DateTime.UtcNow - session.LastActivityAt > TimeSpan.FromMinutes(6))
            {
                _logger.LogWarning(
                    "Session {SessionId} has not received heartbeat for over 6 minutes",
                    session.SessionId);

                session.State = SessionState.Idle;
                await _sessionStore.UpdateSessionAsync(session);
            }

            // Verificar se está próxima da expiração
            if (session.ExpiresAt - DateTime.UtcNow < TimeSpan.FromMinutes(2))
            {
                session.State = SessionState.Expiring;
                await _sessionStore.UpdateSessionAsync(session);

                // Opcionalmente, notificar o nó remoto
                await NotifySessionExpiringAsync(session);
            }
        }
    }
}
```

## Middleware de Sessão

```csharp
public class NodeSessionMiddleware
{
    private readonly RequestDelegate _next;

    public async Task InvokeAsync(HttpContext context, ISessionStore sessionStore)
    {
        // Endpoints que requerem sessão ativa
        if (context.Request.Path.StartsWithSegments("/api/node/v1/query"))
        {
            if (!context.Request.Headers.TryGetValue("X-Session-Id", out var sessionIdValue))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new { error = "Missing session ID" });
                return;
            }

            if (!Guid.TryParse(sessionIdValue, out var sessionId))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid session ID format" });
                return;
            }

            var session = await sessionStore.GetSessionAsync(sessionId);

            if (session == null || session.State != SessionState.Active)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid or expired session" });
                return;
            }

            // Atualizar última atividade
            session.LastActivityAt = DateTime.UtcNow;
            session.Metrics.RequestCount++;
            await sessionStore.UpdateSessionAsync(session);

            // Adicionar sessão ao contexto para uso nos controllers
            context.Items["Session"] = session;
        }

        await _next(context);
    }
}
```

## Rate Limiting por Sessão

```csharp
public class SessionRateLimiter
{
    private readonly ConcurrentDictionary<Guid, TokenBucket> _buckets = new();

    public bool TryConsume(SessionInfo session)
    {
        var bucket = _buckets.GetOrAdd(session.SessionId, _ =>
            new TokenBucket(
                session.Capabilities.RateLimit.RequestsPerMinute,
                session.Capabilities.RateLimit.BurstSize));

        return bucket.TryConsume();
    }
}

public class TokenBucket
{
    private readonly int _capacity;
    private readonly int _refillRate;
    private int _tokens;
    private DateTime _lastRefill;

    public TokenBucket(int refillRate, int capacity)
    {
        _refillRate = refillRate;
        _capacity = capacity;
        _tokens = capacity;
        _lastRefill = DateTime.UtcNow;
    }

    public bool TryConsume()
    {
        Refill();

        if (_tokens > 0)
        {
            _tokens--;
            return true;
        }

        return false;
    }

    private void Refill()
    {
        var now = DateTime.UtcNow;
        var elapsed = now - _lastRefill;
        var tokensToAdd = (int)(elapsed.TotalMinutes * _refillRate);

        if (tokensToAdd > 0)
        {
            _tokens = Math.Min(_capacity, _tokens + tokensToAdd);
            _lastRefill = now;
        }
    }
}
```

## Implementação

### Estado Atual
📋 **Planejado** - Aguardando implementação do handshake

### Próximos Passos

1. **Criar Interfaces e Models**
   - [ ] `Models/Node/SessionInfo.cs`
   - [ ] `Services/Node/ISessionStore.cs`
   - [ ] `Services/Node/ISessionManagementService.cs`

2. **Implementar Stores**
   - [ ] `Services/Node/InMemorySessionStore.cs`
   - [ ] (Futuro) `Services/Node/RedisSessionStore.cs`

3. **Criar Controller**
   - [ ] `Controllers/Node/NodeSessionController.cs`

4. **Background Services**
   - [ ] `Services/Background/SessionCleanupService.cs`
   - [ ] `Services/Background/SessionMonitoringService.cs`

5. **Middleware**
   - [ ] `Middleware/NodeSessionMiddleware.cs`

6. **Rate Limiting**
   - [ ] `Services/Node/SessionRateLimiter.cs`

### Configuração

Adicionar em `appsettings.json`:

```json
{
  "SessionManagement": {
    "DefaultDuration": 3600,
    "MaxDuration": 86400,
    "HeartbeatInterval": 300,
    "CleanupInterval": 300,
    "Store": "InMemory",
    "Redis": {
      "ConnectionString": "localhost:6379",
      "Database": 0
    }
  }
}
```

## Testes

### Cenários de Teste

1. **Criação e uso normal**
2. **Expiração por timeout**
3. **Renovação bem-sucedida**
4. **Heartbeat mantém sessão viva**
5. **Rate limiting funciona corretamente**
6. **Cleanup remove sessões expiradas**
7. **Múltiplas sessões simultâneas**

## Contexto para IA

### Prompt Sugerido

```
Implementar o gerenciamento de sessões conforme docs/architecture/session-management.md.
Começar pela interface ISessionStore e implementação InMemorySessionStore, depois criar
o SessionManagementService e o NodeSessionController.
```

### Dependências
- Depende de: `handshake-protocol.md` (sessão criada após handshake)
- Depende de: `node-communication.md` (arquitetura geral)

## Referências

- OAuth 2.0 Token Management
- Session Management Best Practices
- Token Bucket Algorithm
