# Fase 4: Session Management and Access Control

**Status:** 📋 Planejado
**Última atualização:** 2025-10-03 - 06:00
**Pré-requisito:** Fase 3 completa (session token gerado)

---

## Visão Geral

A Fase 4 implementa o **gerenciamento de sessões autenticadas** e **controle de acesso baseado em capabilities** para recursos protegidos do IRN. Session tokens gerados na Fase 3 são validados e utilizados para autorizar operações específicas.

## Objetivos

1. **Validar Session Tokens**: Verificar autenticidade e validade de tokens recebidos
2. **Carregar Contexto de Sessão**: Obter capabilities e metadados do nó autenticado
3. **Autorizar Operações**: Verificar se nó possui capability necessária para operação
4. **Gerenciar Ciclo de Vida**: Renovação e revogação de sessões
5. **Auditoria e Métricas**: Tracking de uso e operações autenticadas
6. **Rate Limiting**: Proteção contra abuso

---

## Arquitetura

### Componentes

```
┌─────────────────────────────────────────────────────────────┐
│                     Protected Endpoint                       │
│  [PrismEncryptedChannelConnection<T>]                       │
│  [PrismAuthenticatedSession(RequiredCapability="query:read")]│
│                          ↓                                    │
│  1. Decrypt payload (via channel key)                        │
│  2. Validate session token (Bearer token)                    │
│  3. Check capability authorization                           │
│  4. Load SessionContext into HttpContext.Items               │
│  5. Execute business logic                                   │
│  6. Track request metrics                                    │
│  7. Encrypt response (via channel key)                       │
└─────────────────────────────────────────────────────────────┘
                           │
                           ↓
                  ┌────────────────┐
                  │ SessionService │
                  ├────────────────┤
                  │ - Validate     │
                  │ - Renew        │
                  │ - Revoke       │
                  │ - Metrics      │
                  │ - Cleanup      │
                  └────────────────┘
                           │
                           ↓
          ┌─────────────────────────────────┐
          │  Session Storage (In-Memory)    │
          │  ConcurrentDictionary<string,   │
          │     SessionData>                │
          └─────────────────────────────────┘
```

### Session Data Model

```csharp
// Domain/Entities/Session/SessionData.cs
public class SessionData
{
    public string SessionToken { get; set; } = string.Empty;
    public string NodeId { get; set; } = string.Empty;
    public string ChannelId { get; set; } = string.Empty;
    public List<string> GrantedCapabilities { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public int RequestCount { get; set; }

    public bool IsExpired() => DateTime.UtcNow > ExpiresAt;

    public bool HasCapability(string capability)
        => GrantedCapabilities.Contains(capability, StringComparer.OrdinalIgnoreCase);
}

// Core/Middleware/Session/SessionContext.cs
public class SessionContext
{
    public string SessionToken { get; set; } = string.Empty;
    public string NodeId { get; set; } = string.Empty;
    public string ChannelId { get; set; } = string.Empty;
    public List<string> GrantedCapabilities { get; set; } = new();
    public DateTime ExpiresAt { get; set; }

    public bool HasCapability(string capability)
        => GrantedCapabilities.Contains(capability, StringComparer.OrdinalIgnoreCase);
}
```

---

## Service Interface

```csharp
// Core/Middleware/Session/ISessionService.cs
public interface ISessionService
{
    /// <summary>
    /// Validate session token and return session context
    /// </summary>
    /// <returns>SessionContext if valid, null if invalid or expired</returns>
    Task<SessionContext?> ValidateSessionAsync(string sessionToken);

    /// <summary>
    /// Renew session by extending expiration time
    /// </summary>
    /// <returns>New expiration time, or null if session doesn't exist</returns>
    Task<DateTime?> RenewSessionAsync(string sessionToken);

    /// <summary>
    /// Revoke/invalidate a session (logout)
    /// </summary>
    Task<bool> RevokeSessionAsync(string sessionToken);

    /// <summary>
    /// Get session usage metrics for a node
    /// </summary>
    Task<SessionMetrics> GetSessionMetricsAsync(string nodeId);

    /// <summary>
    /// Cleanup expired sessions (background job)
    /// </summary>
    Task<int> CleanupExpiredSessionsAsync();

    /// <summary>
    /// Track request activity for rate limiting
    /// </summary>
    Task IncrementRequestCountAsync(string sessionToken);
}

public class SessionMetrics
{
    public int ActiveSessions { get; set; }
    public int TotalRequests { get; set; }
    public DateTime? LastActivity { get; set; }
}
```

---

## Attribute Implementation

```csharp
// Core/Middleware/Session/PrismAuthenticatedSessionAttribute.cs
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class PrismAuthenticatedSessionAttribute : Attribute, IAsyncActionFilter
{
    public string? RequiredCapability { get; set; }

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var sessionService = context.HttpContext.RequestServices
            .GetRequiredService<ISessionService>();

        // 1. Extract Bearer token from Authorization header
        if (!context.HttpContext.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            context.Result = new UnauthorizedObjectResult(new
            {
                error = "ERR_NO_AUTH_HEADER",
                message = "Authorization header is required"
            });
            return;
        }

        var token = authHeader.ToString();
        if (!token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            context.Result = new UnauthorizedObjectResult(new
            {
                error = "ERR_INVALID_AUTH_FORMAT",
                message = "Authorization header must be in format: Bearer {token}"
            });
            return;
        }

        var sessionToken = token.Substring("Bearer ".Length).Trim();

        // 2. Validate session token
        var sessionContext = await sessionService.ValidateSessionAsync(sessionToken);

        if (sessionContext == null)
        {
            context.Result = new UnauthorizedObjectResult(new
            {
                error = "ERR_INVALID_SESSION",
                message = "Session token is invalid or expired"
            });
            return;
        }

        // 3. Check required capability (if specified)
        if (!string.IsNullOrEmpty(RequiredCapability))
        {
            if (!sessionContext.HasCapability(RequiredCapability))
            {
                context.Result = new ObjectResult(new
                {
                    error = "ERR_INSUFFICIENT_CAPABILITIES",
                    message = $"This operation requires capability: {RequiredCapability}",
                    grantedCapabilities = sessionContext.GrantedCapabilities
                })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
                return;
            }
        }

        // 4. Store session context in HttpContext.Items
        context.HttpContext.Items["SessionContext"] = sessionContext;

        // 5. Track request
        await sessionService.IncrementRequestCountAsync(sessionToken);

        // 6. Continue to controller action
        await next();
    }
}
```

---

## Endpoints

### Session Management

#### `GET /api/session/whoami`

**Descrição**: Retorna informações da sessão atual (endpoint de teste).

**Headers:**
```
Authorization: Bearer {sessionToken}
X-Channel-Id: {channelId}
```

**Response:**
```json
{
  "nodeId": "test-node-001",
  "channelId": "abc-123",
  "capabilities": ["query:read", "query:aggregate"],
  "expiresAt": "2025-10-03T07:00:00Z",
  "requestCount": 42,
  "lastActivity": "2025-10-03T06:30:00Z"
}
```

**Controller:**
```csharp
[HttpGet("whoami")]
[PrismAuthenticatedSession]
public IActionResult WhoAmI()
{
    var sessionContext = HttpContext.Items["SessionContext"] as SessionContext;

    return Ok(new
    {
        nodeId = sessionContext!.NodeId,
        channelId = sessionContext.ChannelId,
        capabilities = sessionContext.GrantedCapabilities,
        expiresAt = sessionContext.ExpiresAt
    });
}
```

---

#### `POST /api/session/renew`

**Descrição**: Renova sessão estendendo o TTL (antes de expirar).

**Headers:**
```
Authorization: Bearer {sessionToken}
```

**Response:**
```json
{
  "success": true,
  "newExpiresAt": "2025-10-03T08:00:00Z",
  "message": "Session renewed for 1 hour"
}
```

**Controller:**
```csharp
[HttpPost("renew")]
[PrismAuthenticatedSession]
public async Task<IActionResult> RenewSession()
{
    var sessionContext = HttpContext.Items["SessionContext"] as SessionContext;
    var newExpiresAt = await _sessionService.RenewSessionAsync(sessionContext!.SessionToken);

    if (newExpiresAt == null)
        return NotFound(new { error = "Session not found" });

    return Ok(new
    {
        success = true,
        newExpiresAt = newExpiresAt.Value,
        message = "Session renewed for 1 hour"
    });
}
```

---

#### `POST /api/session/revoke`

**Descrição**: Revoga sessão (logout).

**Headers:**
```
Authorization: Bearer {sessionToken}
```

**Response:**
```json
{
  "success": true,
  "message": "Session revoked successfully"
}
```

**Controller:**
```csharp
[HttpPost("revoke")]
[PrismAuthenticatedSession]
public async Task<IActionResult> RevokeSession()
{
    var sessionContext = HttpContext.Items["SessionContext"] as SessionContext;
    var revoked = await _sessionService.RevokeSessionAsync(sessionContext!.SessionToken);

    return Ok(new
    {
        success = revoked,
        message = revoked ? "Session revoked successfully" : "Session not found"
    });
}
```

---

### Protected Resources (Examples)

#### `POST /api/query/execute`

**Descrição**: Executa query federada (requer capability `query:read`).

**Headers:**
```
Authorization: Bearer {sessionToken}
X-Channel-Id: {channelId}
Content-Type: application/json
```

**Request Body (encrypted):**
```json
{
  "encryptedData": "base64-ciphertext",
  "iv": "base64-iv",
  "authTag": "base64-tag"
}
```

**Decrypted Payload:**
```json
{
  "queryType": "patient_count",
  "filters": {
    "ageRange": [18, 65],
    "condition": "diabetes"
  }
}
```

**Response (encrypted):**
```json
{
  "encryptedData": "base64-ciphertext",
  "iv": "base64-iv",
  "authTag": "base64-tag"
}
```

**Decrypted Response:**
```json
{
  "queryId": "query-123",
  "nodeId": "test-node-001",
  "result": {
    "patientCount": 1234
  },
  "timestamp": "2025-10-03T06:00:00Z"
}
```

**Controller:**
```csharp
[HttpPost("execute")]
[PrismEncryptedChannelConnection<FederatedQueryRequest>]
[PrismAuthenticatedSession(RequiredCapability = "query:read")]
public async Task<IActionResult> ExecuteQuery()
{
    var request = HttpContext.Items["DecryptedRequest"] as FederatedQueryRequest;
    var sessionContext = HttpContext.Items["SessionContext"] as SessionContext;
    var channelContext = HttpContext.Items["ChannelContext"] as ChannelContext;

    // Execute query
    var result = await _queryService.ExecuteAsync(request!, sessionContext!.NodeId);

    // Encrypt response
    var encryptedResponse = _encryptionService.EncryptPayload(result, channelContext!.SymmetricKey);

    return Ok(encryptedResponse);
}
```

---

## Capabilities

| Capability | Descrição | Endpoints |
|------------|-----------|-----------|
| `query:read` | Executar queries federadas de leitura | `/api/query/execute` |
| `query:aggregate` | Executar queries de agregação cross-node | `/api/query/aggregate` |
| `data:write` | Submeter dados de pesquisa | `/api/data/submit` |
| `data:delete` | Deletar dados próprios | `/api/data/{id}` (DELETE) |
| `admin:node` | Administração do nó | `/api/admin/*` |

---

## Rate Limiting

**Strategy**: Token bucket algorithm per session

**Limites por Capability:**
- `query:read`: 100 requisições/minuto
- `query:aggregate`: 10 requisições/minuto
- `data:write`: 50 requisições/minuto
- `data:delete`: 20 requisições/minuto
- `admin:node`: 100 requisições/minuto

**Implementation:**
```csharp
public class RateLimitingMiddleware
{
    private readonly ConcurrentDictionary<string, TokenBucket> _buckets = new();

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var sessionContext = context.Items["SessionContext"] as SessionContext;
        if (sessionContext == null)
        {
            await next(context);
            return;
        }

        var key = $"{sessionContext.SessionToken}:{context.Request.Path}";
        var bucket = _buckets.GetOrAdd(key, _ => new TokenBucket(100, TimeSpan.FromMinutes(1)));

        if (!bucket.TryConsume())
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "ERR_RATE_LIMIT_EXCEEDED",
                message = "Rate limit exceeded. Please try again later.",
                retryAfter = bucket.GetRetryAfterSeconds()
            });
            return;
        }

        await next(context);
    }
}
```

---

## Métricas (Prometheus)

```csharp
// Métricas expostas em /metrics
irn_session_active_total{node_id="test-node-001"} 5
irn_session_requests_total{node_id="test-node-001",capability="query:read"} 1234
irn_session_requests_total{node_id="test-node-001",capability="data:write"} 567
irn_session_expired_total 42
irn_session_revoked_total 15
irn_rate_limit_exceeded_total{endpoint="/api/query/execute"} 3
```

---

## Audit Logging

**Eventos Auditados:**
- Session renewal
- Session revocation
- Protected resource access
- Authorization failures (missing capabilities)
- Rate limit violations

**Log Format:**
```json
{
  "timestamp": "2025-10-03T06:00:00Z",
  "level": "INFO",
  "event": "SESSION_RESOURCE_ACCESS",
  "nodeId": "test-node-001",
  "sessionToken": "abc...xyz",
  "resource": "/api/query/execute",
  "capability": "query:read",
  "result": "success"
}
```

---

## Testing Strategy

### Unit Tests

```csharp
[TestClass]
public class SessionServiceTests
{
    [TestMethod]
    public async Task ValidateSession_ValidToken_ReturnsContext()
    {
        // Arrange
        var service = new SessionService();
        var token = await CreateTestSession();

        // Act
        var context = await service.ValidateSessionAsync(token);

        // Assert
        Assert.IsNotNull(context);
        Assert.AreEqual("test-node-001", context.NodeId);
    }

    [TestMethod]
    public async Task ValidateSession_ExpiredToken_ReturnsNull()
    {
        // ...
    }
}
```

### Integration Tests

```csharp
[TestClass]
public class Phase4SessionManagementTests
{
    [TestMethod]
    public async Task WhoAmI_WithValidToken_ReturnsSessionInfo()
    {
        // 1. Complete Phase 1-3 (get session token)
        // 2. Call GET /api/session/whoami with Bearer token
        // 3. Verify response contains correct session info
    }

    [TestMethod]
    public async Task ProtectedEndpoint_WithoutToken_Returns401()
    {
        // ...
    }

    [TestMethod]
    public async Task ProtectedEndpoint_WithInsufficientCapability_Returns403()
    {
        // ...
    }
}
```

---

## Implementation Roadmap

### Step 1: Session Service
- [ ] Create `ISessionService` interface
- [ ] Implement `SessionService` with in-memory storage
- [ ] Add session renewal logic
- [ ] Add session revocation logic
- [ ] Add cleanup background job

### Step 2: Middleware
- [ ] Create `PrismAuthenticatedSessionAttribute`
- [ ] Implement Bearer token extraction
- [ ] Implement session validation
- [ ] Implement capability checking
- [ ] Add metrics tracking

### Step 3: Endpoints
- [ ] Implement `/api/session/whoami`
- [ ] Implement `/api/session/renew`
- [ ] Implement `/api/session/revoke`
- [ ] Add SessionController

### Step 4: Protected Resources (Example)
- [ ] Create QueryController with `/api/query/execute`
- [ ] Implement federated query logic (placeholder)
- [ ] Add capability-based authorization

### Step 5: Testing
- [ ] Unit tests for SessionService
- [ ] Integration tests for protected endpoints
- [ ] End-to-end test for complete flow (Phases 1-4)

### Step 6: Rate Limiting & Metrics
- [ ] Implement rate limiting middleware
- [ ] Add Prometheus metrics
- [ ] Add audit logging

### Step 7: Documentation
- [ ] Update manual testing guide
- [ ] Create API documentation
- [ ] Update PROJECT_STATUS.md

---

## Security Considerations

1. **Token Storage**: Sessions stored in-memory (production should use Redis/database)
2. **Token Format**: Simple GUID (production should use JWT with signing)
3. **HTTPS Required**: All session tokens must be transmitted over HTTPS (enforce in production)
4. **Token Rotation**: Consider implementing token rotation for long-lived sessions
5. **Revocation List**: Maintain list of revoked tokens (important for JWT)
6. **Audit All Access**: Log all authenticated operations for compliance

---

## Future Enhancements (Post-Phase 4)

1. **JWT Tokens**: Replace GUID tokens with signed JWTs
2. **Redis Storage**: Move session storage to Redis for scalability
3. **Token Refresh**: Implement refresh token mechanism
4. **Multi-Factor Auth**: Add MFA for sensitive operations
5. **IP Whitelisting**: Allow nodes to restrict access by IP
6. **Distributed Tracing**: OpenTelemetry integration for request tracing
