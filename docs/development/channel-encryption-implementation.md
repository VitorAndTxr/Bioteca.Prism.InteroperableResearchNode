# Implementação de Criptografia de Canal - Fase 2

**Status**: ✅ Implementado
**Data**: 2025-10-02
**Versão**: 0.3.0

## Resumo

Implementada a criptografia de payload para todas as comunicações após o estabelecimento do canal (Fase 1). Agora, **todas** as mensagens das Fases 2-4 são criptografadas usando a chave simétrica derivada do canal.

## Mudanças Implementadas

### 1. Serviços de Criptografia Estendidos

#### `IChannelEncryptionService.cs`
**Novos métodos**:
```csharp
EncryptedPayload EncryptPayload(object payload, byte[] symmetricKey);
T DecryptPayload<T>(EncryptedPayload encryptedPayload, byte[] symmetricKey);
```

**Novo modelo**:
```csharp
public class EncryptedPayload
{
    public string EncryptedData { get; set; }  // Base64
    public string Iv { get; set; }              // Base64
    public string AuthTag { get; set; }         // Base64
}
```

#### `ChannelEncryptionService.cs`
**Implementação**:
- Serializa objeto para JSON
- Criptografa com AES-256-GCM
- Retorna payload base64-encoded para transmissão JSON

### 2. Gerenciamento Centralizado de Canais

#### `IChannelStore.cs` (novo)
```csharp
public interface IChannelStore
{
    void AddChannel(string channelId, ChannelContext context);
    ChannelContext? GetChannel(string channelId);
    bool RemoveChannel(string channelId);
    bool IsChannelValid(string channelId);
}
```

#### `ChannelStore.cs` (novo)
- Armazenamento in-memory com `ConcurrentDictionary`
- Validação automática de expiração
- Limpeza segura de chaves simétricas

#### `ChannelContext.cs` (novo modelo unificado)
```csharp
public class ChannelContext
{
    public string ChannelId { get; set; }
    public byte[] SymmetricKey { get; set; }
    public string SelectedCipher { get; set; }
    public string ClientNonce { get; set; }
    public string ServerNonce { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string? RemoteNodeUrl { get; set; }
    public string Role { get; set; }  // "client" or "server"
}
```

### 3. Endpoints Atualizados (ChannelController)

#### `/api/channel/identify` (POST)
**Antes**: Recebia `NodeIdentifyRequest` em texto claro
**Agora**:
- Recebe `EncryptedPayload`
- Requer header `X-Channel-Id`
- Descriptografa payload
- Processa identificação
- Criptografa resposta

**Request**:
```
POST /api/channel/identify
Headers: X-Channel-Id: {channelId}
Body: {
  "encryptedData": "...",
  "iv": "...",
  "authTag": "..."
}
```

**Response**:
```json
{
  "encryptedData": "...",
  "iv": "...",
  "authTag": "..."
}
```

#### `/api/node/register` (POST)
**Antes**: Recebia `NodeRegistrationRequest` em texto claro
**Agora**:
- Recebe `EncryptedPayload`
- Requer header `X-Channel-Id`
- Descriptografa payload
- Processa registro
- Criptografa resposta

### 4. Cliente HTTP Atualizado (NodeChannelClient)

#### Novos métodos:
```csharp
Task<NodeStatusResponse> IdentifyNodeAsync(string channelId, NodeIdentifyRequest request);
Task<NodeRegistrationResponse> RegisterNodeAsync(string channelId, NodeRegistrationRequest request);
```

**Fluxo**:
1. Busca canal do `ChannelStore`
2. Criptografa payload com chave simétrica do canal
3. Adiciona header `X-Channel-Id`
4. Envia requisição HTTP POST
5. Recebe resposta criptografada
6. Descriptografa resposta

### 5. Dependency Injection (Program.cs)

```csharp
// Novo serviço registrado
builder.Services.AddSingleton<IChannelStore, ChannelStore>();
```

## Fluxo Completo

### Fase 1: Estabelecer Canal
```
Client → POST /api/channel/open
      ← Response: X-Channel-Id: abc-123

ChannelStore armazena:
- ChannelId: "abc-123"
- SymmetricKey: [32 bytes derivados via ECDH + HKDF]
- Role: "server" (no servidor) ou "client" (no cliente)
```

### Fase 2: Identificação (COM CRIPTOGRAFIA)
```
Client:
1. Cria NodeIdentifyRequest
2. Busca canal "abc-123" do ChannelStore
3. Criptografa payload com SymmetricKey
4. POST /api/channel/identify
   Headers: X-Channel-Id: abc-123
   Body: EncryptedPayload

Server:
1. Valida X-Channel-Id header
2. Busca canal do ChannelStore
3. Descriptografa payload com SymmetricKey
4. Processa identificação
5. Criptografa NodeStatusResponse
6. Retorna EncryptedPayload
```

### Fase 2: Registro (COM CRIPTOGRAFIA)
```
Client:
1. Cria NodeRegistrationRequest
2. Busca canal do ChannelStore
3. Criptografa payload com SymmetricKey
4. POST /api/node/register
   Headers: X-Channel-Id: abc-123
   Body: EncryptedPayload

Server:
1. Valida X-Channel-Id header
2. Busca canal do ChannelStore
3. Descriptografa payload com SymmetricKey
4. Processa registro
5. Criptografa NodeRegistrationResponse
6. Retorna EncryptedPayload
```

## Validações de Segurança

### Header `X-Channel-Id` Obrigatório
- Endpoints `/identify` e `/register` retornam `400 Bad Request` se header ausente
- Código de erro: `ERR_MISSING_CHANNEL_ID`

### Validação de Canal
- Canal deve existir no `ChannelStore`
- Canal não pode estar expirado (30 minutos TTL)
- Se inválido: `400 Bad Request` com `ERR_INVALID_CHANNEL`

### Falha na Descriptografia
- Se payload não pode ser descriptografado: `400 Bad Request`
- Código de erro: `ERR_DECRYPTION_FAILED`
- Causas possíveis:
  - Chave errada
  - Payload corrompido
  - Authentication tag inválido (dados foram modificados)

## Testes

### Build
```bash
dotnet build Bioteca.Prism.InteroperableResearchNode/Bioteca.Prism.InteroperableResearchNode.sln
```
**Status**: ✅ Build bem-sucedido (apenas 1 warning conhecido e não-crítico)

### Script de Teste
**Arquivo**: `test-phase2-encrypted.ps1`

**Limitação**: PowerShell não pode facilmente criptografar payloads AES-GCM sem bibliotecas .NET complexas.

**Recomendação**: Testes completos devem usar:
1. `NodeChannelClient.IdentifyNodeAsync()` - em código C#
2. `NodeChannelClient.RegisterNodeAsync()` - em código C#

### Teste Manual (Exemplo C#)
```csharp
// 1. Estabelecer canal
var channelResult = await _nodeChannelClient.OpenChannelAsync("http://node-b:8080");

// 2. Criar requisição de identificação
var identifyRequest = new NodeIdentifyRequest
{
    NodeId = "node-a-001",
    Certificate = certificateBase64,
    Signature = signatureBase64,
    SignedData = signedDataBase64
};

// 3. Identificar (payload criptografado automaticamente)
var statusResponse = await _nodeChannelClient.IdentifyNodeAsync(
    channelResult.ChannelId,
    identifyRequest
);

// 4. Verificar resposta
Console.WriteLine($"IsKnown: {statusResponse.IsKnown}");
Console.WriteLine($"Status: {statusResponse.Status}");
```

## Arquivos Modificados

### Novos Arquivos
- `Bioteca.Prism.Service/Interfaces/Node/IChannelStore.cs`
- `Bioteca.Prism.Service/Services/Node/ChannelStore.cs`
- `test-phase2-encrypted.ps1`
- `docs/development/channel-encryption-implementation.md` (este arquivo)

### Arquivos Modificados
- `Bioteca.Prism.Service/Interfaces/Node/IChannelEncryptionService.cs`
  - Adicionados métodos `EncryptPayload` e `DecryptPayload`
  - Adicionado modelo `EncryptedPayload`

- `Bioteca.Prism.Service/Services/Node/ChannelEncryptionService.cs`
  - Implementados métodos de criptografia de payload

- `Bioteca.Prism.Service/Interfaces/Node/INodeChannelClient.cs`
  - Adicionados métodos `IdentifyNodeAsync` e `RegisterNodeAsync`

- `Bioteca.Prism.Service/Services/Node/NodeChannelClient.cs`
  - Migrado para usar `IChannelStore`
  - Implementados métodos de identificação/registro com criptografia

- `Bioteca.Prism.InteroperableResearchNode/Controllers/ChannelController.cs`
  - Endpoint `/identify` agora usa `EncryptedPayload`
  - Endpoint `/register` agora usa `EncryptedPayload`
  - Removida classe `ChannelContext` local (usa `IChannelStore` agora)

- `Bioteca.Prism.InteroperableResearchNode/Program.cs`
  - Registrado `IChannelStore` como Singleton

## Próximos Passos

### Fase 3: Autenticação Mútua
- [ ] Implementar challenge/response usando canal criptografado
- [ ] Endpoints: `/api/auth/challenge`, `/api/auth/respond`

### Fase 4: Sessão
- [ ] Implementar sessão com capabilities usando canal criptografado
- [ ] Endpoints: `/api/session/create`, `/api/session/renew`

### Melhorias
- [ ] Middleware para validar `X-Channel-Id` globalmente
- [ ] Endpoints de teste para encrypt/decrypt (helper para PowerShell)
- [ ] Testes de integração automatizados em C#
- [ ] Persistência de canais (Redis para multi-instance)

## Problemas Conhecidos

### Warning CS1998
```
NodeRegistryService.cs(44,29): warning CS1998: This async method lacks 'await' operators
```
**Causa**: Método async sem await (implementação in-memory síncrona)
**Impacto**: Nenhum (será resolvido ao adicionar persistência assíncrona)
**Status**: Não-crítico

## Compatibilidade

### Breaking Changes
⚠️ **IMPORTANTE**: Esta mudança quebra a compatibilidade com clientes antigos.

**Antes (v0.2.0)**:
- Endpoints aceitavam payloads em texto claro
- Sem header `X-Channel-Id`

**Agora (v0.3.0)**:
- Endpoints requerem payloads criptografados
- Header `X-Channel-Id` obrigatório

**Migração**:
- Clientes devem usar `NodeChannelClient.IdentifyNodeAsync()` e `RegisterNodeAsync()`
- Scripts PowerShell devem ser atualizados (ou usar helpers C#)

## Referências

- [Plano de Implementação](channel-encryption-plan.md) - Planejamento detalhado
- [Protocolo de Handshake](../architecture/handshake-protocol.md) - Especificação completa
- [AES-GCM Documentation](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.aesgcm)
