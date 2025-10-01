# Plano de Testes - Fase 1: Canal Criptografado

Este documento lista todos os testes necessários para validar a Fase 1 do protocolo de handshake.

## Pré-requisitos

- Docker Compose rodando: `docker-compose up`
- Nó A: http://localhost:5000
- Nó B: http://localhost:5001

## 1. Testes Funcionais Básicos

### 1.1 Health Check
**Objetivo:** Verificar que ambos os nós estão rodando

```powershell
# Nó A
curl http://localhost:5000/api/channel/health

# Nó B
curl http://localhost:5001/api/channel/health
```

**Resultado Esperado:**
```json
{
  "status": "healthy",
  "timestamp": "2025-10-01T..."
}
```

### 1.2 Swagger Disponível
**Objetivo:** Verificar documentação da API

- Nó A: http://localhost:5000/swagger
- Nó B: http://localhost:5001/swagger

**Resultado Esperado:** Interface Swagger carrega com todos os endpoints documentados

## 2. Testes de Handshake (Comunicação Entre Nós)

### 2.1 Nó A → Nó B (A como Cliente, B como Servidor)
**Objetivo:** Node A inicia handshake com Node B

```powershell
curl -X POST http://localhost:5000/api/channel/initiate `
  -H "Content-Type: application/json" `
  -d '{"remoteNodeUrl": "http://node-b:8080"}'
```

**Resultado Esperado:**
```json
{
  "success": true,
  "channelId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "symmetricKey": "...",
  "selectedCipher": "AES-256-GCM",
  "remoteNodeUrl": "http://node-b:8080",
  "clientNonce": "...",
  "serverNonce": "..."
}
```

**Breakpoints para Debug:**
- `ChannelController.cs:168` - Método `InitiateHandshake` (Node A - cliente)
- `NodeChannelClient.cs:40` - Método `OpenChannelAsync` (Node A - lógica cliente)
- `ChannelController.cs:49` - Método `OpenChannel` (Node B - servidor)
- `EphemeralKeyService.cs:18` - Geração de chaves ECDH

### 2.2 Nó B → Nó A (B como Cliente, A como Servidor)
**Objetivo:** Node B inicia handshake com Node A

```powershell
curl -X POST http://localhost:5001/api/channel/initiate `
  -H "Content-Type: application/json" `
  -d '{"remoteNodeUrl": "http://node-a:8080"}'
```

**Resultado Esperado:** Mesmo formato do teste 2.1, mas com Node B como cliente

### 2.3 Verificar Canal no Cliente
**Objetivo:** Consultar canal estabelecido no nó que iniciou (cliente)

```powershell
# Usar o channelId retornado no teste 2.1
curl http://localhost:5000/api/channel/{channelId}
```

**Resultado Esperado:**
```json
{
  "channelId": "...",
  "cipher": "AES-256-GCM",
  "remoteNodeUrl": "http://node-b:8080",
  "createdAt": "2025-10-01T...",
  "expiresAt": "2025-10-01T...",
  "isExpired": false,
  "role": "client"
}
```

### 2.4 Verificar Canal no Servidor
**Objetivo:** Consultar mesmo canal no nó que recebeu (servidor)

```powershell
# Usar o MESMO channelId do teste 2.1
curl http://localhost:5001/api/channel/{channelId}
```

**Resultado Esperado:**
```json
{
  "channelId": "...",
  "cipher": "AES-256-GCM",
  "createdAt": "2025-10-01T...",
  "expiresAt": "2025-10-01T...",
  "isExpired": false,
  "role": "server"
}
```

**✅ Validação:** O mesmo `channelId` deve existir em AMBOS os nós com roles diferentes

## 3. Testes de Segurança

### 3.1 Perfect Forward Secrecy (PFS)
**Objetivo:** Verificar que chaves efêmeras são descartadas

```powershell
# 1. Estabelecer canal
curl -X POST http://localhost:5000/api/channel/initiate `
  -H "Content-Type: application/json" `
  -d '{"remoteNodeUrl": "http://node-b:8080"}'
# Anotar channelId

# 2. Reiniciar containers
docker-compose restart

# 3. Tentar acessar canal antigo
curl http://localhost:5000/api/channel/{channelId-antigo}
```

**Resultado Esperado:** 404 Not Found (canal não existe mais)

**✅ Validação:** Chaves efêmeras foram descartadas, canal anterior não é recuperável

### 3.2 Chaves Efêmeras Diferentes a Cada Handshake
**Objetivo:** Verificar que cada handshake gera novas chaves

```powershell
# Handshake 1
curl -X POST http://localhost:5000/api/channel/initiate `
  -H "Content-Type: application/json" `
  -d '{"remoteNodeUrl": "http://node-b:8080"}'
# Anotar channelId1 e symmetricKey1

# Handshake 2
curl -X POST http://localhost:5000/api/channel/initiate `
  -H "Content-Type: application/json" `
  -d '{"remoteNodeUrl": "http://node-b:8080"}'
# Anotar channelId2 e symmetricKey2
```

**✅ Validação:**
- `channelId1 != channelId2`
- `symmetricKey1 != symmetricKey2`
- Logs mostram "Generated ephemeral ECDH key pair" duas vezes

### 3.3 Validação de Chave Pública Inválida
**Objetivo:** Verificar rejeição de chave efêmera malformada

```powershell
curl -X POST http://localhost:5001/api/channel/open `
  -H "Content-Type: application/json" `
  -d '{
    "protocolVersion": "1.0",
    "ephemeralPublicKey": "invalid-base64-key",
    "keyExchangeAlgorithm": "ECDH-P384",
    "supportedCiphers": ["AES-256-GCM"],
    "timestamp": "2025-10-01T12:00:00Z",
    "nonce": "cmFuZG9tLW5vbmNl"
  }'
```

**Resultado Esperado:**
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

### 3.4 Negociação de Cifras Incompatíveis
**Objetivo:** Verificar erro quando não há cifra comum

Modifique `appsettings.NodeB.json` temporariamente:
```json
"SupportedCiphers": ["DES-CBC"]
```

Reinicie Node B:
```powershell
docker-compose restart node-b
```

Teste:
```powershell
curl -X POST http://localhost:5000/api/channel/initiate `
  -H "Content-Type: application/json" `
  -d '{"remoteNodeUrl": "http://node-b:8080"}'
```

**Resultado Esperado:**
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

**⚠️ Importante:** Reverter appsettings.NodeB.json após o teste!

### 3.5 Replay Attack Prevention
**Objetivo:** Verificar que nonces duplicados são rejeitados

```powershell
# 1. Capturar uma requisição válida (usar Fiddler/Wireshark ou logs)
# 2. Reenviar a MESMA requisição
# 3. Deve ser rejeitada
```

**Resultado Esperado:** Segunda requisição com mesmo nonce é rejeitada

## 4. Testes de Logs e Monitoramento

### 4.1 Verificar Logs de Handshake Bem-Sucedido

**Node A (Cliente):**
```powershell
docker logs irn-node-a --tail 50
```

**Logs Esperados:**
```
info: Bioteca.Prism.Service.Services.Node.NodeChannelClient[0]
      Initiating channel handshake with http://node-b:8080
info: Bioteca.Prism.Service.Services.Node.EphemeralKeyService[0]
      Generated ephemeral ECDH key pair with curve P384
info: Bioteca.Prism.Service.Services.Node.EphemeralKeyService[0]
      Exported public key (121 bytes)
info: Bioteca.Prism.Service.Services.Node.NodeChannelClient[0]
      Channel {channelId} established successfully with http://node-b:8080
```

**Node B (Servidor):**
```powershell
docker logs irn-node-b --tail 50
```

**Logs Esperados:**
```
info: Bioteca.Prism.InteroperableResearchNode.Controllers.ChannelController[0]
      Received channel open request from client
info: Bioteca.Prism.Service.Services.Node.EphemeralKeyService[0]
      Generated ephemeral ECDH key pair with curve P384
info: Bioteca.Prism.Service.Services.Node.EphemeralKeyService[0]
      Derived shared secret (48 bytes)
info: Bioteca.Prism.InteroperableResearchNode.Controllers.ChannelController[0]
      Channel {channelId} established successfully with cipher AES-256-GCM
```

## 5. Teste Automatizado

Execute o script de teste:

```powershell
.\test-docker.ps1
```

**Resultado Esperado:**
```
=== Teste de Comunicação Fase 1 - Docker ===

[Teste 1] Health Check dos Nós
  ✓ Nó A saudável
  ✓ Nó B saudável

[Teste 2] Nó A → Nó B (Handshake)
  ✓ Canal estabelecido: {channelId}

[Teste 3] Nó B → Nó A (Handshake)
  ✓ Canal estabelecido: {channelId}

[Teste 4] Verificar canal A→B no Nó A
  ✓ Canal encontrado (role: client)

[Teste 5] Verificar canal A→B no Nó B
  ✓ Canal encontrado (role: server)

=== Testes Concluídos ===
```

## 6. Checklist de Validação Final

Marque cada item após validação:

- [ ] Health check funciona em ambos os nós
- [ ] Swagger disponível em ambos os nós
- [ ] Node A consegue iniciar handshake com Node B
- [ ] Node B consegue iniciar handshake com Node A
- [ ] Canal aparece em ambos os nós (cliente e servidor)
- [ ] Chaves efêmeras são diferentes a cada handshake
- [ ] Perfect Forward Secrecy funciona (canais descartados após restart)
- [ ] Validação de chave pública inválida funciona
- [ ] Negociação de cifras funciona corretamente
- [ ] Cifras incompatíveis são rejeitadas
- [ ] Logs mostram informações corretas
- [ ] Variáveis inspecionadas no debug mostram valores corretos:
  - [ ] `sharedSecret` tem 48 bytes (ECDH P-384)
  - [ ] `symmetricKey` tem 32 bytes (AES-256)
  - [ ] `channelId` é um GUID válido

## 7. Critérios de Sucesso

A Fase 1 está completa quando:

✅ Ambos os nós podem atuar como **cliente** e **servidor**
✅ Canal criptografado é estabelecido com **chaves efêmeras ECDH**
✅ **Perfect Forward Secrecy** está funcionando
✅ **Validações de segurança** estão implementadas
✅ **Logs** fornecem informações úteis para debug
✅ **Todos os testes** deste documento passam

## Próximos Passos

Após validar a Fase 1:
1. Implementar **Fase 2**: Identificação e Autorização de Nós
2. Implementar **Fase 3**: Autenticação Mútua com Desafio/Resposta
3. Implementar **Fase 4**: Estabelecimento de Sessão com Capabilities
