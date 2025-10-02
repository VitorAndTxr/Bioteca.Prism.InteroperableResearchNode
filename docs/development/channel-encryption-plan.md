# Plano de Implementação: Criptografia de Canal para Fases 2-4

**Status**: 📋 Planejado
**Prioridade**: 🔴 Alta (Requisito de Segurança Crítico)
**Última atualização**: 2025-10-02

## Problema Identificado

Atualmente, o processo de identificação e registro (Fases 2-4) **NÃO ocorre dentro do canal criptografado** estabelecido na Fase 1.

### Estado Atual (Incorreto)
```
1. ✅ Canal estabelecido (/api/channel/open) - gera chave simétrica
2. ❌ Identificação (/api/channel/identify) - valida ChannelId mas NÃO criptografa payload
3. ❌ Registro (/api/node/register) - SEM validação de canal, SEM criptografia
```

### Estado Desejado (Correto)
```
1. ✅ Canal estabelecido (/api/channel/open) - gera chave simétrica
2. ✅ Identificação (/api/channel/identify) - payload criptografado com chave do canal
3. ✅ Registro (/api/node/register) - payload criptografado com chave do canal
4. ✅ Autenticação (Fase 3) - payload criptografado com chave do canal
5. ✅ Sessão (Fase 4) - payload criptografado com chave do canal
```

## Requisitos

### Requisito 1: Todas as comunicações após Fase 1 devem ser criptografadas
- **Fase 2 (Identificação)**: `/api/channel/identify` deve aceitar apenas payloads criptografados
- **Fase 2 (Registro)**: `/api/node/register` deve aceitar apenas payloads criptografados
- **Fase 3 (Autenticação)**: Todos os endpoints devem usar criptografia do canal
- **Fase 4 (Sessão)**: Todos os endpoints devem usar criptografia do canal

### Requisito 2: Validação de Canal
- Todo endpoint (exceto `/api/channel/open`) deve validar:
  - Header `X-Channel-Id` está presente
  - `ChannelId` existe no dicionário de canais ativos
  - Canal não está expirado
  - Payload está criptografado com a chave do canal

### Requisito 3: Formato de Payload Criptografado
```json
{
  "encryptedData": "base64-encoded-encrypted-payload",
  "iv": "base64-encoded-initialization-vector",
  "authTag": "base64-encoded-authentication-tag"
}
```

## Plano de Implementação

### Etapa 1: Criar Serviços de Criptografia de Payload

#### 1.1 Estender `IChannelEncryptionService`
**Arquivo**: `Bioteca.Prism.Service/Interfaces/Node/IChannelEncryptionService.cs`

```csharp
public interface IChannelEncryptionService
{
    // Métodos existentes...
    string GenerateNonce();
    byte[] DeriveKey(byte[] sharedSecret, byte[] salt, byte[] info);

    // NOVOS métodos para criptografia de payload
    EncryptedPayload EncryptPayload(object payload, byte[] symmetricKey);
    T DecryptPayload<T>(EncryptedPayload encryptedPayload, byte[] symmetricKey);
}

public class EncryptedPayload
{
    public string EncryptedData { get; set; } = string.Empty;
    public string Iv { get; set; } = string.Empty;
    public string AuthTag { get; set; } = string.Empty;
}
```

#### 1.2 Implementar métodos em `ChannelEncryptionService`
**Arquivo**: `Bioteca.Prism.Service/Services/Node/ChannelEncryptionService.cs`

```csharp
public EncryptedPayload EncryptPayload(object payload, byte[] symmetricKey)
{
    // 1. Serializar payload para JSON
    var jsonPayload = JsonSerializer.Serialize(payload);
    var plaintextBytes = Encoding.UTF8.GetBytes(jsonPayload);

    // 2. Gerar IV (Initialization Vector)
    var iv = new byte[12]; // 96 bits para AES-GCM
    using var rng = RandomNumberGenerator.Create();
    rng.GetBytes(iv);

    // 3. Criptografar com AES-256-GCM
    using var aes = new AesGcm(symmetricKey);
    var ciphertext = new byte[plaintextBytes.Length];
    var tag = new byte[16]; // 128 bits authentication tag

    aes.Encrypt(iv, plaintextBytes, ciphertext, tag);

    // 4. Retornar payload criptografado
    return new EncryptedPayload
    {
        EncryptedData = Convert.ToBase64String(ciphertext),
        Iv = Convert.ToBase64String(iv),
        AuthTag = Convert.ToBase64String(tag)
    };
}

public T DecryptPayload<T>(EncryptedPayload encryptedPayload, byte[] symmetricKey)
{
    // 1. Decodificar base64
    var ciphertext = Convert.FromBase64String(encryptedPayload.EncryptedData);
    var iv = Convert.FromBase64String(encryptedPayload.Iv);
    var tag = Convert.FromBase64String(encryptedPayload.AuthTag);

    // 2. Descriptografar com AES-256-GCM
    using var aes = new AesGcm(symmetricKey);
    var plaintextBytes = new byte[ciphertext.Length];

    aes.Decrypt(iv, ciphertext, tag, plaintextBytes);

    // 3. Desserializar JSON
    var jsonPayload = Encoding.UTF8.GetString(plaintextBytes);
    return JsonSerializer.Deserialize<T>(jsonPayload)!;
}
```

### Etapa 2: Criar Middleware de Validação de Canal

#### 2.1 Criar `ChannelValidationMiddleware`
**Arquivo**: `Bioteca.Prism.InteroperableResearchNode/Middleware/ChannelValidationMiddleware.cs`

```csharp
public class ChannelValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ChannelValidationMiddleware> _logger;

    public ChannelValidationMiddleware(RequestDelegate next, ILogger<ChannelValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Endpoints que NÃO precisam de canal (apenas /api/channel/open e /api/channel/initiate)
        var path = context.Request.Path.Value?.ToLower() ?? "";

        if (path == "/api/channel/open" || path == "/api/channel/initiate" || path == "/api/channel/health")
        {
            await _next(context);
            return;
        }

        // Validar presença do header X-Channel-Id
        if (!context.Request.Headers.TryGetValue("X-Channel-Id", out var channelId))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                error = new
                {
                    code = "ERR_MISSING_CHANNEL_ID",
                    message = "X-Channel-Id header is required for this endpoint",
                    retryable = false
                }
            });
            return;
        }

        // Validar que o canal existe (buscar no ChannelController ou criar um ChannelStore)
        // TODO: Implementar validação de canal ativo

        await _next(context);
    }
}
```

#### 2.2 Registrar Middleware
**Arquivo**: `Bioteca.Prism.InteroperableResearchNode/Program.cs`

```csharp
// Adicionar ANTES de app.MapControllers()
app.UseMiddleware<ChannelValidationMiddleware>();
```

### Etapa 3: Criar Model Binders para Descriptografia Automática

#### 3.1 Criar `EncryptedPayloadModelBinder`
**Arquivo**: `Bioteca.Prism.InteroperableResearchNode/ModelBinders/EncryptedPayloadModelBinder.cs`

```csharp
public class EncryptedPayloadModelBinder : IModelBinder
{
    private readonly IChannelEncryptionService _encryptionService;
    private readonly ILogger<EncryptedPayloadModelBinder> _logger;

    public EncryptedPayloadModelBinder(IChannelEncryptionService encryptionService, ILogger<EncryptedPayloadModelBinder> logger)
    {
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public async Task BindModelAsync(ModelBindingContext bindingContext)
    {
        // 1. Ler corpo da requisição
        using var reader = new StreamReader(bindingContext.HttpContext.Request.Body);
        var body = await reader.ReadToEndAsync();

        // 2. Desserializar EncryptedPayload
        var encryptedPayload = JsonSerializer.Deserialize<EncryptedPayload>(body);

        // 3. Obter ChannelId do header
        var channelId = bindingContext.HttpContext.Request.Headers["X-Channel-Id"].ToString();

        // 4. Buscar chave simétrica do canal (TODO: implementar ChannelStore)
        var symmetricKey = GetChannelSymmetricKey(channelId);

        // 5. Descriptografar payload
        var decryptedPayload = _encryptionService.DecryptPayload(encryptedPayload, symmetricKey, bindingContext.ModelType);

        bindingContext.Result = ModelBindingResult.Success(decryptedPayload);
    }
}
```

#### 3.2 Criar Atributo `[EncryptedPayload]`
**Arquivo**: `Bioteca.Prism.InteroperableResearchNode/Attributes/EncryptedPayloadAttribute.cs`

```csharp
[AttributeUsage(AttributeTargets.Parameter)]
public class EncryptedPayloadAttribute : Attribute, IBindingSourceMetadata
{
    public BindingSource BindingSource => BindingSource.Body;
}
```

### Etapa 4: Criar Serviço Centralizado para Gerenciamento de Canais

#### 4.1 Criar `IChannelStore`
**Arquivo**: `Bioteca.Prism.Service/Interfaces/Node/IChannelStore.cs`

```csharp
public interface IChannelStore
{
    void AddChannel(string channelId, ChannelContext context);
    ChannelContext? GetChannel(string channelId);
    bool RemoveChannel(string channelId);
    bool IsChannelValid(string channelId);
}

public class ChannelContext
{
    public string ChannelId { get; set; } = string.Empty;
    public byte[] SymmetricKey { get; set; } = Array.Empty<byte>();
    public string SelectedCipher { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
```

#### 4.2 Implementar `ChannelStore`
**Arquivo**: `Bioteca.Prism.Service/Services/Node/ChannelStore.cs`

```csharp
public class ChannelStore : IChannelStore
{
    private static readonly ConcurrentDictionary<string, ChannelContext> _channels = new();
    private readonly ILogger<ChannelStore> _logger;

    public ChannelStore(ILogger<ChannelStore> logger)
    {
        _logger = logger;
    }

    public void AddChannel(string channelId, ChannelContext context)
    {
        _channels.TryAdd(channelId, context);
        _logger.LogInformation("Channel {ChannelId} added to store", channelId);
    }

    public ChannelContext? GetChannel(string channelId)
    {
        if (_channels.TryGetValue(channelId, out var context))
        {
            if (context.ExpiresAt > DateTime.UtcNow)
            {
                return context;
            }

            // Canal expirado - remover
            _channels.TryRemove(channelId, out _);
            _logger.LogWarning("Channel {ChannelId} expired and removed", channelId);
        }

        return null;
    }

    public bool RemoveChannel(string channelId)
    {
        var removed = _channels.TryRemove(channelId, out var context);
        if (removed && context != null)
        {
            Array.Clear(context.SymmetricKey, 0, context.SymmetricKey.Length);
            _logger.LogInformation("Channel {ChannelId} removed from store", channelId);
        }
        return removed;
    }

    public bool IsChannelValid(string channelId)
    {
        return GetChannel(channelId) != null;
    }
}
```

### Etapa 5: Atualizar Controllers para Usar Criptografia

#### 5.1 Atualizar `ChannelController.IdentifyNode`
**Arquivo**: `Bioteca.Prism.InteroperableResearchNode/Controllers/ChannelController.cs`

```csharp
[HttpPost("identify")]
[ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(HandshakeError), StatusCodes.Status400BadRequest)]
public async Task<IActionResult> IdentifyNode([EncryptedPayload] NodeIdentifyRequest request)
{
    try
    {
        // Obter ChannelId do header
        var channelId = Request.Headers["X-Channel-Id"].ToString();

        // Validar canal
        var channelContext = _channelStore.GetChannel(channelId);
        if (channelContext == null)
        {
            return BadRequest(CreateError("ERR_INVALID_CHANNEL", "Channel does not exist or has expired"));
        }

        _logger.LogInformation("Received encrypted node identification for NodeId: {NodeId}", request.NodeId);

        // ... (lógica de identificação existente)

        // Criptografar resposta
        var encryptedResponse = _encryptionService.EncryptPayload(response, channelContext.SymmetricKey);

        return Ok(encryptedResponse);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error identifying node");
        return StatusCode(StatusCodes.Status500InternalServerError, CreateError("ERR_IDENTIFICATION_FAILED", "Failed to identify node"));
    }
}
```

#### 5.2 Atualizar `ChannelController.RegisterNode`
**Arquivo**: `Bioteca.Prism.InteroperableResearchNode/Controllers/ChannelController.cs`

```csharp
[HttpPost("register")]
[Route("/api/node/register")]
[ProducesResponseType(typeof(EncryptedPayload), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(HandshakeError), StatusCodes.Status400BadRequest)]
public async Task<IActionResult> RegisterNode([EncryptedPayload] NodeRegistrationRequest request)
{
    try
    {
        // Obter ChannelId do header
        var channelId = Request.Headers["X-Channel-Id"].ToString();

        // Validar canal
        var channelContext = _channelStore.GetChannel(channelId);
        if (channelContext == null)
        {
            return BadRequest(CreateError("ERR_INVALID_CHANNEL", "Channel does not exist or has expired"));
        }

        _logger.LogInformation("Received encrypted node registration for NodeId: {NodeId}", request.NodeId);

        var response = await _nodeRegistry.RegisterNodeAsync(request);

        // Criptografar resposta
        var encryptedResponse = _encryptionService.EncryptPayload(response, channelContext.SymmetricKey);

        return Ok(encryptedResponse);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error registering node");
        return StatusCode(StatusCodes.Status500InternalServerError, CreateError("ERR_REGISTRATION_FAILED", "Failed to register node"));
    }
}
```

#### 5.3 Atualizar `ChannelController.OpenChannel` para usar `IChannelStore`
**Arquivo**: `Bioteca.Prism.InteroperableResearchNode/Controllers/ChannelController.cs`

```csharp
// Substituir _activeChannels por _channelStore
var channelContext = new ChannelContext
{
    ChannelId = channelId,
    SymmetricKey = symmetricKey,
    SelectedCipher = selectedCipher,
    CreatedAt = DateTime.UtcNow,
    ExpiresAt = DateTime.UtcNow.AddMinutes(30)
};

_channelStore.AddChannel(channelId, channelContext);
```

### Etapa 6: Atualizar Cliente (`NodeChannelClient`)

#### 6.1 Adicionar métodos de criptografia ao cliente
**Arquivo**: `Bioteca.Prism.Service/Services/Node/NodeChannelClient.cs`

```csharp
public async Task<NodeStatusResponse> IdentifyNodeAsync(string channelId, NodeIdentifyRequest request)
{
    var channelContext = GetChannel(channelId);
    if (channelContext == null)
    {
        throw new InvalidOperationException("Channel not found or expired");
    }

    // Criptografar payload
    var encryptedPayload = _encryptionService.EncryptPayload(request, channelContext.SymmetricKey);

    // Enviar requisição
    var httpClient = _httpClientFactory.CreateClient();
    httpClient.DefaultRequestHeaders.Add("X-Channel-Id", channelId);

    var response = await httpClient.PostAsJsonAsync($"{channelContext.RemoteNodeUrl}/api/channel/identify", encryptedPayload);

    if (!response.IsSuccessStatusCode)
    {
        var error = await response.Content.ReadFromJsonAsync<HandshakeError>();
        throw new Exception($"Identification failed: {error?.Error?.Message}");
    }

    // Descriptografar resposta
    var encryptedResponse = await response.Content.ReadFromJsonAsync<EncryptedPayload>();
    return _encryptionService.DecryptPayload<NodeStatusResponse>(encryptedResponse!, channelContext.SymmetricKey);
}

public async Task<NodeRegistrationResponse> RegisterNodeAsync(string channelId, NodeRegistrationRequest request)
{
    var channelContext = GetChannel(channelId);
    if (channelContext == null)
    {
        throw new InvalidOperationException("Channel not found or expired");
    }

    // Criptografar payload
    var encryptedPayload = _encryptionService.EncryptPayload(request, channelContext.SymmetricKey);

    // Enviar requisição
    var httpClient = _httpClientFactory.CreateClient();
    httpClient.DefaultRequestHeaders.Add("X-Channel-Id", channelId);

    var response = await httpClient.PostAsJsonAsync($"{channelContext.RemoteNodeUrl}/api/node/register", encryptedPayload);

    if (!response.IsSuccessStatusCode)
    {
        var error = await response.Content.ReadFromJsonAsync<HandshakeError>();
        throw new Exception($"Registration failed: {error?.Error?.Message}");
    }

    // Descriptografar resposta
    var encryptedResponse = await response.Content.ReadFromJsonAsync<EncryptedPayload>();
    return _encryptionService.DecryptPayload<NodeRegistrationResponse>(encryptedResponse!, channelContext.SymmetricKey);
}
```

### Etapa 7: Atualizar Scripts de Teste

#### 7.1 Atualizar `test-phase2-full.ps1`
- Adicionar header `X-Channel-Id` em todas as requisições após `/api/channel/open`
- Criptografar payloads de identificação e registro
- Descriptografar respostas

#### 7.2 Criar helper functions para criptografia em PowerShell
```powershell
function Encrypt-Payload {
    param(
        [string]$Payload,
        [string]$SymmetricKey
    )
    # Implementar usando .NET AesGcm
}

function Decrypt-Payload {
    param(
        [string]$EncryptedData,
        [string]$Iv,
        [string]$AuthTag,
        [string]$SymmetricKey
    )
    # Implementar usando .NET AesGcm
}
```

### Etapa 8: Registrar Serviços no DI

**Arquivo**: `Bioteca.Prism.InteroperableResearchNode/Program.cs`

```csharp
// Adicionar ChannelStore como Singleton (compartilhado entre requisições)
builder.Services.AddSingleton<IChannelStore, ChannelStore>();

// Registrar ModelBinder
builder.Services.AddControllers(options =>
{
    options.ModelBinderProviders.Insert(0, new EncryptedPayloadModelBinderProvider());
});
```

## Checklist de Implementação

### Fase 1: Serviços Base
- [ ] Estender `IChannelEncryptionService` com métodos `EncryptPayload` e `DecryptPayload`
- [ ] Implementar criptografia AES-256-GCM em `ChannelEncryptionService`
- [ ] Criar `EncryptedPayload` model
- [ ] Criar testes unitários para criptografia/descriptografia

### Fase 2: Gerenciamento de Canais
- [ ] Criar `IChannelStore` interface
- [ ] Implementar `ChannelStore` com `ConcurrentDictionary`
- [ ] Migrar `_activeChannels` de `ChannelController` para `ChannelStore`
- [ ] Registrar `ChannelStore` como Singleton no DI

### Fase 3: Validação de Canal
- [ ] Criar `ChannelValidationMiddleware`
- [ ] Registrar middleware no pipeline
- [ ] Testar que endpoints sem `X-Channel-Id` retornam 401

### Fase 4: Model Binding
- [ ] Criar `EncryptedPayloadModelBinder`
- [ ] Criar `EncryptedPayloadAttribute`
- [ ] Registrar ModelBinder no DI
- [ ] Testar deserialização automática de payloads criptografados

### Fase 5: Atualizar Controllers
- [ ] Atualizar `IdentifyNode` para aceitar payload criptografado
- [ ] Atualizar `RegisterNode` para aceitar payload criptografado
- [ ] Criptografar todas as respostas dos endpoints
- [ ] Adicionar validação de canal em todos os endpoints (exceto `/open` e `/initiate`)

### Fase 6: Atualizar Cliente
- [ ] Adicionar `IdentifyNodeAsync` com criptografia
- [ ] Adicionar `RegisterNodeAsync` com criptografia
- [ ] Testar integração cliente-servidor com payloads criptografados

### Fase 7: Testes
- [ ] Atualizar `test-phase2-full.ps1` com criptografia
- [ ] Criar helpers PowerShell para criptografia
- [ ] Testar cenário completo: open → identify → register
- [ ] Validar que payloads sem criptografia são rejeitados

### Fase 8: Documentação
- [ ] Atualizar Swagger/OpenAPI com novos schemas
- [ ] Documentar formato de payload criptografado
- [ ] Atualizar `manual-testing-guide.md` com exemplos de criptografia

## Testes de Validação

### Teste 1: Payload Criptografado com Sucesso
```bash
# 1. Abrir canal
POST /api/channel/open → retorna X-Channel-Id: abc-123

# 2. Identificar com payload criptografado
POST /api/channel/identify
Headers: X-Channel-Id: abc-123
Body: { "encryptedData": "...", "iv": "...", "authTag": "..." }
→ Deve retornar payload criptografado com status do nó
```

### Teste 2: Requisição sem X-Channel-Id
```bash
POST /api/channel/identify
Body: { "nodeId": "..." }
→ Deve retornar 401 com ERR_MISSING_CHANNEL_ID
```

### Teste 3: Requisição com Payload Não Criptografado
```bash
POST /api/channel/identify
Headers: X-Channel-Id: abc-123
Body: { "nodeId": "...", "nodeName": "..." }  # Payload em texto claro
→ Deve retornar 400 com erro de deserialização
```

### Teste 4: Canal Expirado
```bash
# 1. Abrir canal
POST /api/channel/open → retorna X-Channel-Id: abc-123

# 2. Aguardar 31 minutos (canal expira em 30 min)

# 3. Tentar identificar
POST /api/channel/identify
Headers: X-Channel-Id: abc-123
→ Deve retornar 401 com ERR_INVALID_CHANNEL
```

## Cronograma Estimado

| Etapa | Esforço | Dependências |
|-------|---------|--------------|
| Fase 1: Serviços Base | 4h | - |
| Fase 2: Gerenciamento de Canais | 2h | Fase 1 |
| Fase 3: Validação de Canal | 2h | Fase 2 |
| Fase 4: Model Binding | 3h | Fase 1, Fase 2 |
| Fase 5: Atualizar Controllers | 3h | Fase 1-4 |
| Fase 6: Atualizar Cliente | 2h | Fase 1-5 |
| Fase 7: Testes | 4h | Fase 1-6 |
| Fase 8: Documentação | 2h | Fase 1-7 |
| **Total** | **22h** | - |

## Referências

- [Handshake Protocol](../architecture/handshake-protocol.md) - Especificação completa do protocolo
- [AES-GCM Documentation](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.aesgcm) - .NET AES-GCM API
- [ASP.NET Core Model Binding](https://learn.microsoft.com/en-us/aspnet/core/mvc/models/model-binding) - Custom model binding
- [ASP.NET Core Middleware](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/) - Middleware pipeline

## Notas de Segurança

⚠️ **Importante**: Esta implementação é **crítica para a segurança** do protocolo. Sem criptografia de payload nas Fases 2-4, as seguintes vulnerabilidades existem:

1. **Exposição de Identidade**: NodeId, certificados e credenciais trafegam em texto claro
2. **Man-in-the-Middle**: Atacante pode interceptar e modificar identificação/registro
3. **Replay Attacks**: Mensagens podem ser capturadas e reenviadas
4. **Violação do Princípio de Defesa em Profundidade**: Canal existe mas não é usado

✅ **Após implementação**, o protocolo terá:
- ✅ Criptografia ponta-a-ponta para todas as fases
- ✅ Perfect Forward Secrecy (chaves efêmeras)
- ✅ Autenticação de mensagens (AES-GCM auth tag)
- ✅ Proteção contra replay (nonces + timestamps)
- ✅ Validação de canal em todas as requisições
