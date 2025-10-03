# Endpoints de Teste para Fase 3 - Autenticação Mútua

**Versão**: 0.5.0
**Data**: 2025-10-03

Este documento demonstra como usar os endpoints de teste da TestingController para executar manualmente o fluxo completo da Fase 3.

## 📋 Pré-requisitos

1. Nó registrado e **autorizado** (status=Authorized)
2. Canal estabelecido (Fase 1)
3. Certificado com chave privada (para assinatura)

## 🔄 Fluxo Completo

### Passo 1: Estabelecer Canal (Fase 1)

```bash
curl -X POST http://localhost:5000/api/channel/initiate \
  -H "Content-Type: application/json" \
  -d '{"remoteNodeUrl": "http://node-b:8080"}'
```

**Resposta:**
```json
{
  "success": true,
  "channelId": "abc-123-def-456",
  "symmetricKey": "base64-key...",
  "selectedCipher": "AES-256-GCM"
}
```

**Salvar:** `channelId` para usar nos próximos passos

---

### Passo 2: Gerar Certificado (se necessário)

```bash
curl -X POST http://localhost:5000/api/testing/generate-certificate \
  -H "Content-Type: application/json" \
  -d '{
    "subjectName": "test-node-001",
    "validityYears": 2,
    "password": "test123"
  }'
```

**Resposta:**
```json
{
  "subjectName": "test-node-001",
  "certificate": "MIIC5TCCA...",
  "certificateWithPrivateKey": "MIIJCg...",
  "password": "test123",
  "thumbprint": "79B7FC808E5BAFC...",
  "usage": {
    "certificate": "Use this for registration (public key)",
    "certificateWithPrivateKey": "Use this to sign data (includes private key)",
    "password": "Password to load the PFX certificate"
  }
}
```

**Salvar:**
- `certificate` (para registro)
- `certificateWithPrivateKey` (para assinatura)
- `password`

---

### Passo 3: Registrar Nó (Fase 2)

⚠️ **IMPORTANTE**: O payload deve ser criptografado. Use o endpoint `/api/testing/encrypt-payload`.

**3.1. Criar payload de registro:**
```json
{
  "nodeId": "test-node-001",
  "nodeName": "Test Node",
  "certificate": "MIIC5TCCA...",
  "contactInfo": "admin@test.com",
  "institutionDetails": "Test Institution",
  "nodeUrl": "http://test:8080",
  "requestedCapabilities": ["search", "retrieve"]
}
```

**3.2. Criptografar payload:**
```bash
curl -X POST http://localhost:5000/api/testing/encrypt-payload \
  -H "Content-Type: application/json" \
  -d '{
    "channelId": "abc-123-def-456",
    "payload": {
      "nodeId": "test-node-001",
      "nodeName": "Test Node",
      "certificate": "MIIC5TCCA...",
      "contactInfo": "admin@test.com",
      "institutionDetails": "Test Institution",
      "nodeUrl": "http://test:8080",
      "requestedCapabilities": ["search", "retrieve"]
    }
  }'
```

**3.3. Registrar com payload criptografado:**
```bash
curl -X POST http://localhost:5001/api/node/register \
  -H "Content-Type: application/json" \
  -H "X-Channel-Id: abc-123-def-456" \
  -d '{
    "encryptedData": "...",
    "iv": "...",
    "authTag": "..."
  }'
```

---

### Passo 4: Aprovar Nó (Admin)

```bash
curl -X PUT http://localhost:5001/api/node/test-node-001/status \
  -H "Content-Type: application/json" \
  -d '{"status": 1}'
```

**Resposta:**
```json
{
  "message": "Node status updated successfully",
  "nodeId": "test-node-001",
  "status": 1
}
```

---

### Passo 5: ✨ Solicitar Challenge (Fase 3)

**Novo endpoint de teste:**

```bash
curl -X POST http://localhost:5000/api/testing/request-challenge \
  -H "Content-Type: application/json" \
  -d '{
    "channelId": "abc-123-def-456",
    "nodeId": "test-node-001"
  }'
```

**Resposta:**
```json
{
  "success": true,
  "channelId": "abc-123-def-456",
  "nodeId": "test-node-001",
  "challengeResponse": {
    "challengeData": "YXNkZmFzZGZhc2RmYXNkZg==",
    "challengeTimestamp": "2025-10-03T00:00:01Z",
    "challengeTtlSeconds": 300,
    "expiresAt": "2025-10-03T00:05:01Z"
  },
  "nextStep": {
    "action": "Sign the challengeData with your node's private key",
    "format": "{ChallengeData}{ChannelId}{NodeId}{Timestamp:O}",
    "endpoint": "POST /api/testing/authenticate"
  }
}
```

**Salvar:** `challengeData` para próximo passo

---

### Passo 6: Assinar Challenge

**6.1. Construir dados para assinar:**

Formato: `{ChallengeData}{ChannelId}{NodeId}{Timestamp:O}`

Exemplo:
```
YXNkZmFzZGZhc2RmYXNkZgabc-123-def-456test-node-0012025-10-03T00:00:02.1234567Z
```

**6.2. Assinar com certificado:**

```bash
curl -X POST http://localhost:5000/api/testing/sign-data \
  -H "Content-Type: application/json" \
  -d '{
    "data": "YXNkZmFzZGZhc2RmYXNkZgabc-123-def-456test-node-0012025-10-03T00:00:02.1234567Z",
    "certificateWithPrivateKey": "MIIJCg...",
    "password": "test123"
  }'
```

**Resposta:**
```json
{
  "data": "YXNkZmFzZGZhc2RmYXNkZgabc-123-def-456test-node-0012025-10-03T00:00:02.1234567Z",
  "signature": "as8xW2gPKcRnPDma...",
  "algorithm": "RSA-SHA256"
}
```

**Salvar:** `signature`

---

### Passo 7: ✨ Autenticar com Challenge Assinado

**Novo endpoint de teste:**

```bash
curl -X POST http://localhost:5000/api/testing/authenticate \
  -H "Content-Type: application/json" \
  -d '{
    "channelId": "abc-123-def-456",
    "nodeId": "test-node-001",
    "challengeData": "YXNkZmFzZGZhc2RmYXNkZg==",
    "signature": "as8xW2gPKcRnPDma...",
    "timestamp": "2025-10-03T00:00:02.1234567Z"
  }'
```

**Resposta de Sucesso:**
```json
{
  "success": true,
  "channelId": "abc-123-def-456",
  "nodeId": "test-node-001",
  "authenticationResponse": {
    "authenticated": true,
    "sessionToken": "a1b2c3d4e5f67890abcdef1234567890",
    "sessionExpiresAt": "2025-10-03T01:00:02Z",
    "grantedCapabilities": ["search", "retrieve"],
    "message": "Authentication successful",
    "nextPhase": "phase4_session",
    "timestamp": "2025-10-03T00:00:02Z"
  },
  "usage": {
    "sessionToken": "Use this token in subsequent authenticated requests",
    "ttl": "Session expires in 1 hour",
    "capabilities": ["search", "retrieve"]
  }
}
```

**🎉 Sucesso!** Session token recebido!

---

## ❌ Tratamento de Erros

### Erro: Nó Não Autorizado

```json
{
  "success": false,
  "error": "Failed to request challenge",
  "message": "Challenge request failed: Node status is Pending. Only authorized nodes can authenticate.",
  "hint": "Make sure the node is registered and authorized (status=Authorized)"
}
```

**Solução:** Aprovar o nó via `PUT /api/node/{nodeId}/status` com `status: 1`

---

### Erro: Challenge Expirado

```json
{
  "success": false,
  "error": "Failed to authenticate",
  "message": "Authentication failed: Challenge response verification failed",
  "possibleCauses": [
    "Challenge has expired (TTL: 5 minutes)",
    "Challenge data does not match the one generated",
    "Invalid signature (wrong private key or wrong data format)",
    "Challenge was already used (one-time use only)"
  ]
}
```

**Solução:** Solicitar novo challenge via `POST /api/testing/request-challenge`

---

### Erro: Assinatura Inválida

**Causas comuns:**

1. **Formato do timestamp incorreto:** Deve ser `{DateTime:O}` (ISO 8601 com offset)
   - ✅ Correto: `2025-10-03T00:00:02.1234567Z`
   - ❌ Errado: `2025-10-03 00:00:02`

2. **Ordem dos campos incorreta:**
   - ✅ Correto: `{ChallengeData}{ChannelId}{NodeId}{Timestamp:O}`
   - ❌ Errado: `{NodeId}{ChallengeData}{ChannelId}{Timestamp}`

3. **Certificado errado:** Certifique-se de usar `certificateWithPrivateKey` (PFX com chave privada)

4. **Timestamp diferente:** O timestamp usado na assinatura deve ser **exatamente** o mesmo enviado no request

---

## 🔧 Endpoints Auxiliares

### Verificar Informações do Canal

```bash
curl http://localhost:5000/api/testing/channel-info/abc-123-def-456
```

### Descriptografar Payload (Debug)

```bash
curl -X POST http://localhost:5000/api/testing/decrypt-payload \
  -H "Content-Type: application/json" \
  -d '{
    "channelId": "abc-123-def-456",
    "encryptedPayload": {
      "encryptedData": "...",
      "iv": "...",
      "authTag": "..."
    }
  }'
```

---

## 📝 Script PowerShell Completo

```powershell
# Fase 1: Estabelecer Canal
$channel = Invoke-RestMethod -Uri "http://localhost:5000/api/channel/initiate" `
  -Method Post -ContentType "application/json" `
  -Body '{"remoteNodeUrl": "http://node-b:8080"}'

$channelId = $channel.channelId
Write-Host "Channel ID: $channelId"

# Fase 2: Gerar Certificado
$cert = Invoke-RestMethod -Uri "http://localhost:5000/api/testing/generate-certificate" `
  -Method Post -ContentType "application/json" `
  -Body '{"subjectName": "test-node-001", "validityYears": 2, "password": "test123"}'

# Fase 2: Registrar Nó (simplificado - requer criptografia)
# ... (ver documentação completa)

# Fase 2: Aprovar Nó
Invoke-RestMethod -Uri "http://localhost:5001/api/node/test-node-001/status" `
  -Method Put -ContentType "application/json" `
  -Body '{"status": 1}'

# Fase 3: Solicitar Challenge
$challenge = Invoke-RestMethod -Uri "http://localhost:5000/api/testing/request-challenge" `
  -Method Post -ContentType "application/json" `
  -Body (@{
    channelId = $channelId
    nodeId = "test-node-001"
  } | ConvertTo-Json)

$challengeData = $challenge.challengeResponse.challengeData
Write-Host "Challenge Data: $challengeData"

# Fase 3: Assinar Challenge
$timestamp = Get-Date -Format "o"
$dataToSign = "$challengeData$channelId" + "test-node-001$timestamp"

$signResult = Invoke-RestMethod -Uri "http://localhost:5000/api/testing/sign-data" `
  -Method Post -ContentType "application/json" `
  -Body (@{
    data = $dataToSign
    certificateWithPrivateKey = $cert.certificateWithPrivateKey
    password = "test123"
  } | ConvertTo-Json)

$signature = $signResult.signature

# Fase 3: Autenticar
$authResult = Invoke-RestMethod -Uri "http://localhost:5000/api/testing/authenticate" `
  -Method Post -ContentType "application/json" `
  -Body (@{
    channelId = $channelId
    nodeId = "test-node-001"
    challengeData = $challengeData
    signature = $signature
    timestamp = $timestamp
  } | ConvertTo-Json)

Write-Host "✅ Authenticated!"
Write-Host "Session Token: $($authResult.authenticationResponse.sessionToken)"
Write-Host "Expires At: $($authResult.authenticationResponse.sessionExpiresAt)"
Write-Host "Capabilities: $($authResult.authenticationResponse.grantedCapabilities -join ', ')"
```

---

## 🎯 Resumo dos Endpoints da Fase 3

| Endpoint | Método | Descrição |
|----------|--------|-----------|
| `/api/testing/request-challenge` | POST | Solicita challenge para autenticação |
| `/api/testing/authenticate` | POST | Autentica com challenge assinado |
| `/api/testing/sign-data` | POST | Assina dados com certificado |
| `/api/testing/encrypt-payload` | POST | Criptografa payload para canal |
| `/api/testing/decrypt-payload` | POST | Descriptografa payload (debug) |

---

## ✅ Checklist de Teste

- [ ] Canal estabelecido (Fase 1)
- [ ] Nó registrado (Fase 2)
- [ ] Nó autorizado (status=Authorized)
- [ ] Challenge solicitado via `/api/testing/request-challenge`
- [ ] Challenge recebido com `challengeData` e TTL de 5 minutos
- [ ] Dados construídos corretamente: `{ChallengeData}{ChannelId}{NodeId}{Timestamp:O}`
- [ ] Assinatura gerada com certificado correto
- [ ] Autenticação realizada via `/api/testing/authenticate`
- [ ] Session token recebido com TTL de 1 hora
- [ ] Capabilities incluídas na resposta

---

## 📚 Documentação Relacionada

- [Manual Testing Guide](./manual-testing-guide.md) - Guia completo de testes manuais
- [Handshake Protocol](../architecture/handshake-protocol.md) - Especificação do protocolo
- [Phase 3 Implementation Plan](../development/phase3-authentication-plan.md) - Plano de implementação
