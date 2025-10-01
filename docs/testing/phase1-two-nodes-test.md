# Teste de Comunicação Fase 1: Duas Instâncias

Este documento descreve como testar a Fase 1 do protocolo de handshake entre duas instâncias do IRN.

## Pré-requisitos

- .NET 8.0 SDK instalado
- Duas portas disponíveis (ex: 5001 e 5002)

## Passo 1: Configurar Duas Instâncias

✅ Os arquivos de configuração já foram criados:
- `appsettings.NodeA.json` - Nó A na porta **5000** (HTTP)
- `appsettings.NodeB.json` - Nó B na porta **5001** (HTTP)

## Passo 2: Iniciar as Instâncias

Abra dois terminais no diretório do projeto:

**Terminal 1 (Nó A) - Porta 5000:**
```powershell
cd Bioteca.Prism.InteroperableResearchNode
$env:ASPNETCORE_ENVIRONMENT="NodeA"
dotnet run --no-launch-profile
```

**Terminal 2 (Nó B) - Porta 5001:**
```powershell
cd Bioteca.Prism.InteroperableResearchNode
$env:ASPNETCORE_ENVIRONMENT="NodeB"
dotnet run --no-launch-profile
```

Aguarde até ver em cada terminal:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000  (ou 5001)
```

## Passo 3: Testar Handshake

### Cenário 1: Nó A inicia handshake com Nó B

Use o seguinte comando HTTP (ou ferramenta como Postman/Insomnia):

```http
POST http://localhost:5000/api/channel/initiate
Content-Type: application/json

{
  "remoteNodeUrl": "http://localhost:5001"
}
```

**Resposta esperada (200 OK):**
```json
{
  "success": true,
  "channelId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "symmetricKey": "base64-encoded-key...",
  "selectedCipher": "AES-256-GCM",
  "remoteNodeUrl": "http://localhost:5001",
  "clientNonce": "...",
  "serverNonce": "..."
}
```

### Cenário 2: Nó B inicia handshake com Nó A

```http
POST http://localhost:5001/api/channel/initiate
Content-Type: application/json

{
  "remoteNodeUrl": "http://localhost:5000"
}
```

## Passo 4: Verificar Canais Estabelecidos

### No Nó A - Ver canal onde atuou como cliente:

```http
GET https://localhost:5001/api/channel/{channelId}
```

**Resposta esperada:**
```json
{
  "channelId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "cipher": "AES-256-GCM",
  "remoteNodeUrl": "https://localhost:5002",
  "createdAt": "2025-10-01T12:00:00Z",
  "expiresAt": "2025-10-01T12:30:00Z",
  "isExpired": false,
  "role": "client"
}
```

### No Nó B - Ver canal onde atuou como servidor:

```http
GET https://localhost:5002/api/channel/{channelId}
```

**Resposta esperada:**
```json
{
  "channelId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "cipher": "AES-256-GCM",
  "createdAt": "2025-10-01T12:00:00Z",
  "expiresAt": "2025-10-01T12:30:00Z",
  "isExpired": false,
  "role": "server"
}
```

## Validações de Segurança

### 1. Perfect Forward Secrecy
Após encerrar ambas as instâncias, reinicie-as. Os canais anteriores não devem mais existir e novas chaves efêmeras serão geradas.

### 2. Validação de Chave Pública
Tente enviar uma chave pública inválida:

```http
POST https://localhost:5002/api/channel/open
Content-Type: application/json

{
  "protocolVersion": "1.0",
  "ephemeralPublicKey": "invalid-key-data",
  "keyExchangeAlgorithm": "ECDH-P384",
  "supportedCiphers": ["AES-256-GCM"],
  "timestamp": "2025-10-01T12:00:00Z",
  "nonce": "cmFuZG9tLW5vbmNl"
}
```

**Resposta esperada (400 Bad Request):**
```json
{
  "error": {
    "code": "ERR_INVALID_EPHEMERAL_KEY",
    "message": "Ephemeral public key is invalid or malformed",
    "details": {
      "reason": "invalid_curve_point"
    },
    "retryable": true
  }
}
```

### 3. Negociação de Cifras
Tente com cifra não suportada:

```http
POST https://localhost:5001/api/channel/initiate
Content-Type: application/json

{
  "remoteNodeUrl": "https://localhost:5002"
}
```

Se modificar `SupportedCiphers` no Nó B para `["DES-CBC"]`, a resposta deve ser:

```json
{
  "success": false,
  "error": {
    "code": "ERR_CHANNEL_FAILED",
    "message": "No compatible cipher found",
    "details": {
      "clientCiphers": ["AES-256-GCM", "ChaCha20-Poly1305"],
      "serverCiphers": ["DES-CBC"]
    },
    "retryable": false
  }
}
```

## Logs Esperados

### Nó A (Cliente):
```
info: Bioteca.Prism.InteroperableResearchNode.Services.Node.NodeChannelClient[0]
      Initiating channel handshake with https://localhost:5002
info: Bioteca.Prism.InteroperableResearchNode.Services.Node.EphemeralKeyService[0]
      Generated ephemeral ECDH key pair with curve P384
info: Bioteca.Prism.InteroperableResearchNode.Services.Node.NodeChannelClient[0]
      Channel 3fa85f64-5717-4562-b3fc-2c963f66afa6 established successfully with https://localhost:5002
```

### Nó B (Servidor):
```
info: Bioteca.Prism.InteroperableResearchNode.Controllers.ChannelController[0]
      Received channel open request from client
info: Bioteca.Prism.InteroperableResearchNode.Services.Node.EphemeralKeyService[0]
      Generated ephemeral ECDH key pair with curve P384
info: Bioteca.Prism.InteroperableResearchNode.Controllers.ChannelController[0]
      Channel 3fa85f64-5717-4562-b3fc-2c963f66afa6 established successfully with cipher AES-256-GCM
```

## Próximos Passos

Após validar a Fase 1, você pode:
1. Implementar Fase 2 (Identificação e Autorização)
2. Usar o canal estabelecido para trocar mensagens criptografadas
3. Implementar testes automatizados

## Troubleshooting

### Erro de certificado HTTPS
Se houver erro de certificado SSL:
```bash
dotnet dev-certs https --trust
```

### Porta já em uso
Altere as portas nos arquivos `appsettings.Node*.json` para portas disponíveis.

### HttpClient não confia no certificado de desenvolvimento
No código do cliente, adicione:
```csharp
builder.Services.AddHttpClient().ConfigurePrimaryHttpMessageHandler(() =>
    new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true
    });
```
⚠️ **Apenas para desenvolvimento/testes!**
