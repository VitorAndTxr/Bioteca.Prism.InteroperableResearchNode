# Próximos Passos - Correção dos Testes

**Data**: 2025-10-02
**Status Atual**: 34/56 testes passando (61%)
**Objetivo**: 100% dos testes passando antes de implementar Phase 3

---

## 📊 Resumo do Estado Atual

### ✅ O que está funcionando (34 testes)

| Categoria | Status | Detalhes |
|-----------|--------|----------|
| **Phase 1 - Canal Criptografado** | 6/6 (100%) | ✅ Todos passando |
| **Certificados e Assinaturas** | 14/15 (93%) | ✅ Quase perfeito |
| **Phase 2 - Identificação de Nós** | 5/6 (83%) | ✅ Core funcionando |
| **Integração Canal Criptografado** | 2/3 (67%) | ⚠️ Maioria OK |

### ❌ O que precisa ser corrigido (22 testes)

1. **NodeChannelClient** - 6/7 falhas (14% passando)
2. **Segurança e Edge Cases** - 11/17 falhas (36% passando)
3. **Casos de Criptografia** - 3 falhas
4. **Bug menor** - 1 falha (timezone)

---

## 🎯 Plano de Ação

### **PRIORIDADE 1: Corrigir NodeChannelClient (6-8 horas)**

**Problema**: Os testes usam `INodeChannelClient` que internamente usa `IHttpClientFactory.CreateClient()` para fazer requisições HTTP reais, mas não consegue conectar aos servidores in-memory do `TestWebApplicationFactory`.

**Testes afetados (6):**
- `InitiateChannel_WithValidRemoteUrl_EstablishesChannel`
- `IdentifyNode_WithInvalidSignature_ReturnsError`
- `IdentifyNode_UnknownNode_ReturnsNotKnown`
- `FullWorkflow_InitiateRegisterIdentify_WorksEndToEnd`
- `RegisterNode_AfterChannelEstablished_SuccessfullyRegisters`
- `IdentifyNode_AfterRegistration_ReturnsPending`

**Solução Recomendada: Opção A - Mock IHttpClientFactory**

#### Passo 1: Criar TestHttpClientFactory

```csharp
// File: Bioteca.Prism.InteroperableResearchNode.Test/Helpers/TestHttpClientFactory.cs

namespace Bioteca.Prism.InteroperableResearchNode.Test.Helpers;

public class TestHttpClientFactory : IHttpClientFactory
{
    private readonly Dictionary<string, HttpClient> _clients = new();

    public void RegisterClient(string name, HttpClient client)
    {
        _clients[name] = client;
    }

    public HttpClient CreateClient(string name)
    {
        // Se não tem um cliente registrado, cria um padrão
        if (_clients.TryGetValue(name, out var client))
        {
            return client;
        }

        // Retorna um cliente padrão (pode ser vazio ou lançar exceção)
        return _clients.TryGetValue("default", out var defaultClient)
            ? defaultClient
            : new HttpClient();
    }
}
```

#### Passo 2: Atualizar TestWebApplicationFactory

```csharp
// File: TestWebApplicationFactory.cs

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private string _environmentName = "Development";
    private Action<IServiceCollection>? _configureTestServices;

    public TestWebApplicationFactory()
    {
    }

    public static TestWebApplicationFactory Create(string environmentName)
    {
        var factory = new TestWebApplicationFactory();
        factory._environmentName = environmentName;
        return factory;
    }

    public TestWebApplicationFactory WithHttpClient(string name, HttpClient client)
    {
        _configureTestServices = services =>
        {
            // Remove o IHttpClientFactory padrão
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IHttpClientFactory));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Adiciona nosso factory de teste
            var testFactory = new TestHttpClientFactory();
            testFactory.RegisterClient(name, client);
            testFactory.RegisterClient("", client); // Cliente padrão
            services.AddSingleton<IHttpClientFactory>(testFactory);
        };
        return this;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(_environmentName);

        builder.ConfigureServices(services =>
        {
            _configureTestServices?.Invoke(services);
        });
    }
}
```

#### Passo 3: Atualizar NodeChannelClientTests

```csharp
[Fact]
public async Task InitiateChannel_WithValidRemoteUrl_EstablishesChannel()
{
    // Arrange - Criar factory remota
    using var remoteFactory = TestWebApplicationFactory.Create("RemoteNode");
    var remoteClient = remoteFactory.CreateClient();

    // Configurar factory local para usar o HttpClient da factory remota
    var localFactory = TestWebApplicationFactory.Create("LocalNode")
        .WithHttpClient("", remoteClient);

    var channelClient = localFactory.Services.GetRequiredService<INodeChannelClient>();
    var remoteUrl = remoteClient.BaseAddress!.ToString().TrimEnd('/');

    // Act
    var result = await channelClient.OpenChannelAsync(remoteUrl);

    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeTrue();
    result.ChannelId.Should().NotBeNullOrEmpty();
}
```

**Tempo Estimado**: 6-8 horas (inclui testes e debugging)

---

### **PRIORIDADE 2: Investigar Falhas de Criptografia (2-4 horas)**

**Problema**: Alguns testes falham com "Failed to decrypt data - authentication failed"

**Testes afetados (3):**
- `FullWorkflow_EstablishChannel_RegisterNode_Identify_Authorize`
- `IdentifyNode_UnknownNode_ReturnsNotKnown` (Phase2NodeIdentificationTests)
- `IdentifyNode_PendingNode_ReturnsPending`

**Investigação:**

#### Passo 1: Adicionar logging detalhado

```csharp
// Em Phase2NodeIdentificationTests.cs - método EstablishChannelAsync
private async Task<(string channelId, byte[] symmetricKey)> EstablishChannelAsync()
{
    // ... código existente ...

    var symmetricKey = encryptionService.DeriveKey(sharedSecret, salt, info);

    // DEBUG: Log para verificar
    _logger.LogInformation("Channel established: {ChannelId}, Key length: {KeyLength}",
        channelId, symmetricKey.Length);

    return (channelId, symmetricKey);
}
```

#### Passo 2: Verificar se canal expira durante teste

```csharp
// Verificar se o canal ainda existe antes de usar
var channel = _channelStore.GetChannel(channelId);
if (channel == null || channel.ExpiresAt < DateTime.UtcNow)
{
    // Canal expirou! Recriar
}
```

#### Passo 3: Revisar ordem de operações

Verificar se todos os testes:
1. Estabelecem canal ANTES de usar
2. Usam o MESMO channelId consistentemente
3. Não reutilizam keys entre testes

**Tempo Estimado**: 2-4 horas

---

### **PRIORIDADE 3: Implementar Validações Faltantes (4-6 horas)**

**Problema**: 11 testes validam features não implementadas (TDD)

#### Passo 1: Validação de Timestamp (1-2 horas)

**Testes afetados:**
- `OpenChannel_WithFutureTimestamp_ReturnsBadRequest`
- `OpenChannel_WithOldTimestamp_ReturnsBadRequest`

**Implementação:**

```csharp
// File: ChannelController.cs - método ValidateChannelOpenRequest

private HandshakeError? ValidateChannelOpenRequest(ChannelOpenRequest request)
{
    // ... validações existentes ...

    // Validar timestamp
    var now = DateTime.UtcNow;
    var timestampAge = now - request.Timestamp;

    if (timestampAge.TotalSeconds < -300) // 5 minutos no futuro
    {
        return CreateError(
            "ERR_INVALID_TIMESTAMP",
            "Request timestamp is too far in the future",
            new Dictionary<string, object>
            {
                ["requestTimestamp"] = request.Timestamp,
                ["serverTime"] = now,
                ["maxSkewSeconds"] = 300
            },
            retryable: true
        );
    }

    if (timestampAge.TotalSeconds > 30) // 30 segundos no passado
    {
        return CreateError(
            "ERR_INVALID_TIMESTAMP",
            "Request timestamp is too old",
            new Dictionary<string, object>
            {
                ["requestTimestamp"] = request.Timestamp,
                ["serverTime"] = now,
                ["maxAgeSeconds"] = 30
            },
            retryable: true
        );
    }

    return null;
}
```

#### Passo 2: Validação de Nonce (30 min)

**Testes afetados:**
- `OpenChannel_WithInvalidNonce_ReturnsBadRequest`
- `OpenChannel_WithShortNonce_ReturnsBadRequest`

```csharp
// Em ValidateChannelOpenRequest

if (string.IsNullOrWhiteSpace(request.Nonce))
{
    return CreateError("ERR_CHANNEL_FAILED", "Nonce is required", retryable: true);
}

// Validar tamanho mínimo (12 bytes = 16 chars base64)
try
{
    var nonceBytes = Convert.FromBase64String(request.Nonce);
    if (nonceBytes.Length < 12)
    {
        return CreateError(
            "ERR_INVALID_NONCE",
            "Nonce must be at least 12 bytes",
            new Dictionary<string, object> { ["minBytes"] = 12, ["actualBytes"] = nonceBytes.Length },
            retryable: true
        );
    }
}
catch (FormatException)
{
    return CreateError("ERR_INVALID_NONCE", "Nonce must be valid base64", retryable: true);
}
```

#### Passo 3: Validação de Certificado Expirado (1 hora)

**Teste afetado:**
- `RegisterNode_WithExpiredCertificate_ReturnsBadRequest`

```csharp
// File: NodeConnectionController.cs - método RegisterNode

[HttpPost("register")]
public async Task<IActionResult> RegisterNode([FromBody] EncryptedPayload encryptedPayload)
{
    // ... descriptografar payload ...

    var request = decryptedRequest as NodeRegistrationRequest;

    // Validar certificado
    try
    {
        var certBytes = Convert.FromBase64String(request.Certificate);
        var cert = new X509Certificate2(certBytes);

        // Verificar se expirou
        if (cert.NotAfter < DateTime.UtcNow)
        {
            return BadRequest(new
            {
                error = "Certificate has expired",
                expiredAt = cert.NotAfter,
                currentTime = DateTime.UtcNow
            });
        }

        if (cert.NotBefore > DateTime.UtcNow)
        {
            return BadRequest(new
            {
                error = "Certificate not yet valid",
                validFrom = cert.NotBefore,
                currentTime = DateTime.UtcNow
            });
        }
    }
    catch (Exception ex)
    {
        return BadRequest(new { error = "Invalid certificate format", details = ex.Message });
    }

    // ... resto da lógica ...
}
```

#### Passo 4: Validação de Campos Obrigatórios (1 hora)

**Testes afetados:**
- `RegisterNode_WithEmptyNodeId_ReturnsBadRequest`
- `RegisterNode_WithEmptyNodeName_ReturnsBadRequest`
- `GenerateCertificate_WithEmptySubjectName_ReturnsBadRequest`

**Opção A: Usar Data Annotations**

```csharp
// File: Domain/Requests/Node/NodeRegistrationRequest.cs

public class NodeRegistrationRequest
{
    [Required(ErrorMessage = "NodeId is required")]
    [MinLength(1, ErrorMessage = "NodeId cannot be empty")]
    public string NodeId { get; set; } = string.Empty;

    [Required(ErrorMessage = "NodeName is required")]
    [MinLength(1, ErrorMessage = "NodeName cannot be empty")]
    public string NodeName { get; set; } = string.Empty;

    // ... outros campos ...
}
```

**Opção B: Validação Manual no Controller**

```csharp
if (string.IsNullOrWhiteSpace(request.NodeId))
{
    return BadRequest(new { error = "NodeId is required" });
}

if (string.IsNullOrWhiteSpace(request.NodeName))
{
    return BadRequest(new { error = "NodeName is required" });
}
```

#### Passo 5: Validação de Enum (30 min)

**Teste afetado:**
- `UpdateNodeStatus_ToInvalidStatus_ReturnsBadRequest`

```csharp
// File: NodeConnectionController.cs

[HttpPut("{nodeId}/status")]
public IActionResult UpdateNodeStatus(string nodeId, [FromBody] UpdateNodeStatusRequest request)
{
    // Validar enum
    if (!Enum.IsDefined(typeof(AuthorizationStatus), request.Status))
    {
        return BadRequest(new
        {
            error = "Invalid status value",
            validValues = Enum.GetNames(typeof(AuthorizationStatus))
        });
    }

    // ... resto da lógica ...
}
```

#### Passo 6: Outros casos (1 hora)

- `RegisterNode_Twice_SecondRegistrationUpdatesInfo` - Implementar lógica de update
- `RegisterNode_WithInvalidCertificateFormat_ReturnsBadRequest` - Já implementado no Passo 3

**Tempo Total Estimado**: 4-6 horas

---

### **PRIORIDADE 4: Corrigir Bug de Timezone (30 min)**

**Teste afetado:**
- `CertificateHelper_GenerateCertificate_ProducesValidCertificate`

**Problema**: Comparação de datas com timezones diferentes

**Solução:**

```csharp
// File: CertificateAndSignatureTests.cs

[Fact]
public void CertificateHelper_GenerateCertificate_ProducesValidCertificate()
{
    // Arrange
    var subjectName = "test-certificate";
    var validityYears = 1;

    // Act
    var certificate = CertificateHelper.GenerateSelfSignedCertificate(subjectName, validityYears);

    // Assert
    certificate.Should().NotBeNull();
    certificate.SubjectName.Name.Should().Contain(subjectName);

    // Usar ToUniversalTime() para normalizar
    var now = DateTime.UtcNow;
    certificate.NotBefore.ToUniversalTime().Should().BeCloseTo(now, TimeSpan.FromMinutes(1));

    var expectedExpiry = now.AddYears(validityYears);
    certificate.NotAfter.ToUniversalTime().Should().BeCloseTo(expectedExpiry, TimeSpan.FromMinutes(1));
}
```

**Tempo Estimado**: 30 minutos

---

## 📅 Cronograma Sugerido

| Dia | Tarefa | Horas | Testes Corrigidos |
|-----|--------|-------|-------------------|
| **Dia 1** | PRIORIDADE 1 - NodeChannelClient (Parte 1) | 4h | 0 (setup) |
| **Dia 2** | PRIORIDADE 1 - NodeChannelClient (Parte 2) | 4h | +6 testes |
| **Dia 3** | PRIORIDADE 2 - Investigar Criptografia | 3h | +3 testes |
| **Dia 3** | PRIORIDADE 4 - Bug Timezone | 0.5h | +1 teste |
| **Dia 4** | PRIORIDADE 3 - Validações (Timestamp + Nonce) | 3h | +4 testes |
| **Dia 5** | PRIORIDADE 3 - Validações (Certificado + Campos) | 3h | +7 testes |
| **TOTAL** | - | **17.5h** | **+22 testes (100%)** |

---

## ✅ Checklist de Progresso

### NodeChannelClient (6 testes)
- [ ] Criar `TestHttpClientFactory`
- [ ] Atualizar `TestWebApplicationFactory` com método `WithHttpClient()`
- [ ] Refatorar `InitiateChannel_WithValidRemoteUrl_EstablishesChannel`
- [ ] Refatorar `IdentifyNode_WithInvalidSignature_ReturnsError`
- [ ] Refatorar `IdentifyNode_UnknownNode_ReturnsNotKnown`
- [ ] Refatorar `FullWorkflow_InitiateRegisterIdentify_WorksEndToEnd`
- [ ] Refatorar `RegisterNode_AfterChannelEstablished_SuccessfullyRegisters`
- [ ] Refatorar `IdentifyNode_AfterRegistration_ReturnsPending`

### Criptografia (3 testes)
- [ ] Adicionar logging de debug
- [ ] Verificar expiração de canal
- [ ] Corrigir `FullWorkflow_EstablishChannel_RegisterNode_Identify_Authorize`
- [ ] Corrigir `IdentifyNode_UnknownNode_ReturnsNotKnown`
- [ ] Corrigir `IdentifyNode_PendingNode_ReturnsPending`

### Validações (11 testes)
- [ ] Implementar validação de timestamp (futuro)
- [ ] Implementar validação de timestamp (passado)
- [ ] Implementar validação de nonce (inválido)
- [ ] Implementar validação de nonce (curto)
- [ ] Implementar validação de certificado expirado
- [ ] Implementar validação de formato de certificado
- [ ] Implementar validação de NodeId vazio
- [ ] Implementar validação de NodeName vazio
- [ ] Implementar validação de status inválido
- [ ] Implementar validação de SubjectName vazio
- [ ] Implementar lógica de registro duplicado

### Bug Timezone (1 teste)
- [ ] Corrigir comparação de datas em `CertificateHelper_GenerateCertificate_ProducesValidCertificate`

---

## 🎯 Meta Final

**Status Atual**: 34/56 (61%)
**Meta**: 56/56 (100%)
**Testes a corrigir**: 22
**Tempo estimado**: 17.5 horas (~3 dias de trabalho)

---

## 📚 Recursos Úteis

- **Rodar testes**: `dotnet test --verbosity normal`
- **Rodar teste específico**: `dotnet test --filter "FullyQualifiedName~NomeDoTeste"`
- **Ver output detalhado**: `dotnet test --logger "console;verbosity=detailed"`
- **Documentação**: `docs/testing/TESTE-MANUAL-FASE2-CRIPTOGRAFADA.md`

---

**Última atualização**: 2025-10-02
**Autor**: Claude Code Assistant
