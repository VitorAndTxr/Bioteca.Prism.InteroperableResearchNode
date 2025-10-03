# 🔒 Guia de Teste Manual - Fase 2 com Criptografia de Canal

**Versão**: 1.0.0
**Data**: 2025-10-02
**Objetivo**: Validar a identificação e registro de nós usando canal criptografado

---

## 📋 Pré-requisitos

- Docker Desktop rodando
- Containers Node A e Node B ativos
- Swagger UI disponível em:
  - Node A: http://localhost:5000/swagger
  - Node B: http://localhost:5001/swagger

---

## ✅ Verificação Inicial

### 1. Verificar containers

```powershell
docker ps
```

**Esperado**:
```
CONTAINER ID   IMAGE              PORTS                    NAMES
...            node-a             0.0.0.0:5000->8080/tcp   irn-node-a
...            node-b             0.0.0.0:5001->8080/tcp   irn-node-b
```

### 2. Health check

```powershell
curl http://localhost:5000/api/channel/health
curl http://localhost:5001/api/channel/health
```

**Esperado**: `{"status":"healthy","timestamp":"..."}`

---

## 🚀 Fluxo Completo de Teste (Via Swagger)

### **PASSO 1: Estabelecer Canal Criptografado**

**Node A → Node B**

1. Abra Swagger do **Node A**: http://localhost:5000/swagger
2. Localize endpoint: `POST /api/channel/initiate`
3. Clique em **Try it out**
4. Cole o seguinte JSON:

```json
{
  "remoteNodeUrl": "http://node-b:8080"
}
```

5. Clique em **Execute**

**Resultado esperado**:

```json
{
  "success": true,
  "channelId": "46e171cb-8ea7-48f3-abe3-8eeef9e41384",
  "symmetricKey": "dVkL4CBHF/ItJk2CPURILlabxoS6WgBPEgTck5UM/Jo=",
  "selectedCipher": "AES-256-GCM",
  "remoteNodeUrl": "http://node-b:8080",
  "error": null,
  "clientNonce": "295uRJj5D5OAfrL3V4Ehnw==",
  "serverNonce": "7pBK8I81yQDRToCH3jIrEw=="
}
```

**⚠️ IMPORTANTE**: Copie o `channelId` - você vai usar em todos os próximos passos!

---

### **PASSO 2: Gerar Certificado para Node A**

**No Node A (localhost:5000)**

1. Localize endpoint: `POST /api/testing/generate-certificate`
2. Clique em **Try it out**
3. Cole o seguinte JSON:

```json
{
  "subjectName": "node-a-test-001",
  "validityYears": 2,
  "password": "string"
}
```

4. Clique em **Execute**

**Resultado esperado**:

```json
{
  "subjectName": "node-a-test-001",
  "certificate": "MIIC5TCCAc2gAwI..",
  "certificateWithPrivateKey": "MIIJIwIBAzCCCN8GCSqGSIb3..",
  "password": "string",
  "validFrom": "2025-10-02T07:53:34+00:00",
  "validTo": "2027-10-02T07:53:34+00:00",
  "thumbprint": "4C42A6F127910CC9C26A4601DEDC2C167D90FBD9",
  "serialNumber": "68570D80B1682584",
  "usage": {
    "certificate": "Use this for registration and identification (public key)",
    "certificateWithPrivateKey": "Use this to sign data (includes private key)",
    "password": "Password to load the PFX certificate"
  }
}
```

**⚠️ IMPORTANTE**: Copie o `certificate` inteiro!

---

### **PASSO 3: Assinar Dados de Identificação**

**No Node A (localhost:5000)**

1. Localize endpoint: `POST /api/testing/sign-data`
2. Clique em **Try it out**
3. Cole o seguinte JSON (substitua `COLE_O_CHANNEL_ID_AQUI` e `COLE_O_CERTIFICATE_AQUI`):

```json
{
  "certificate": "COLE_O_CERTIFICATE_AQUI",
  "data": "COLE_O_CHANNEL_ID_AQUInode-a-test-001",
  "nodeId": "node-a-test-001"
}
```

**Exemplo com valores reais**:
```json
{
  "certificate": "MIIDXTCCAkWgAwIBAgIQZJ...",
  "data": "a1b2c3d4-e5f6-7890-abcd-ef1234567890node-a-test-001",
  "nodeId": "node-a-test-001"
}
```

4. Clique em **Execute**

**Resultado esperado**:

```json
{
  "signature": "SGVsbG8gV29ybGQhCg==...",  // Base64 da assinatura
  "algorithm": "SHA256withRSA",
  "timestamp": "2025-10-02T10:31:00Z"
}
```

**⚠️ IMPORTANTE**: Copie a `signature`!

---

### **PASSO 4: Criar Payload de Identificação Criptografado**

**No Node A (localhost:5000)**

1. Localize endpoint: `POST /api/testing/encrypt-payload`
2. Clique em **Try it out**
3. Cole o seguinte JSON (substitua os valores copiados):

```json
{
  "channelId": "COLE_O_CHANNEL_ID_AQUI",
  "payload": {
    "nodeId": "node-a-test-001",
    "certificate": "COLE_O_CERTIFICATE_AQUI",
    "signature": "COLE_A_SIGNATURE_AQUI"
  }
}
```

**Exemplo com valores reais**:
```json
{
  "channelId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "payload": {
    "nodeId": "node-a-test-001",
    "certificate": "MIIDXTCCAkWgAwIBAgIQZJ...",
    "signature": "SGVsbG8gV29ybGQhCg==..."
  }
}
```

4. Clique em **Execute**

**Resultado esperado**:

```json
{
  "channelId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "encryptedPayload": {
    "encryptedData": "YWJjZGVmZ2hpamtsbW5vcA==...",
    "iv": "MTIzNDU2Nzg5MDEyMzQ1Ng==",
    "authTag": "cXdlcnR5dWlvcGFzZGZnaGo="
  },
  "channelInfo": {
    "cipher": "AES-256-GCM",
    "role": "client",
    "createdAt": "2025-10-02T10:30:00Z",
    "expiresAt": "2025-10-02T11:00:00Z"
  }
}
```

**⚠️ IMPORTANTE**: Copie o objeto `encryptedPayload` inteiro (as 3 propriedades: `encryptedData`, `iv`, `authTag`)!

---

### **PASSO 5: Identificar Node A no Node B (Primeira Tentativa - Desconhecido)**

**Agora vá para Node B**: http://localhost:5001/swagger

1. Localize endpoint: `POST /api/node/identify`
2. Clique em **Try it out**
3. **ADICIONE O HEADER**: Clique em "Add string item" em **Parameters > X-Channel-Id**
   - Cole o `channelId` que você copiou no PASSO 1
4. Cole o `encryptedPayload` que você copiou no PASSO 4:

```json
{
  "encryptedData": "COLE_AQUI",
  "iv": "COLE_AQUI",
  "authTag": "COLE_AQUI"
}
```

5. Clique em **Execute**

**Resultado esperado** (Node A ainda não registrado):

```json
{
  "encryptedData": "...",
  "iv": "...",
  "authTag": "..."
}
```

**Mas o conteúdo descriptografado seria**:
```json
{
  "isKnown": false,
  "status": 0,  // Unknown
  "nodeId": "node-a-test-001",
  "timestamp": "2025-10-02T10:32:00Z",
  "registrationUrl": "http://node-b:8080/api/node/register",
  "message": "Node is not registered. Please register using the provided URL.",
  "nextPhase": null
}
```

---

### **PASSO 6: Descriptografar Resposta (Opcional - Para Ver o Resultado)**

**Volte para Node A**: http://localhost:5000/swagger

1. Localize endpoint: `POST /api/testing/decrypt-payload`
2. Clique em **Try it out**
3. Cole:

```json
{
  "channelId": "COLE_O_CHANNEL_ID_AQUI",
  "encryptedPayload": {
    "encryptedData": "COLE_A_RESPOSTA_DO_PASSO_5",
    "iv": "COLE_AQUI",
    "authTag": "COLE_AQUI"
  }
}
```

4. Clique em **Execute**

**Resultado**: Você verá a resposta descriptografada!

---

### **PASSO 7: Registrar Node A no Node B**

**Criar payload de registro criptografado**

**No Node A (localhost:5000)**:

1. Localize endpoint: `POST /api/testing/encrypt-payload`
2. Cole:

```json
{
  "channelId": "COLE_O_CHANNEL_ID_AQUI",
  "payload": {
    "nodeId": "node-a-test-001",
    "nodeName": "Node A - Test Instance",
    "certificate": "COLE_O_CERTIFICATE_AQUI",
    "contactInfo": "admin@node-a.test",
    "institutionDetails": "Test Institution A",
    "nodeUrl": "http://node-a:8080",
    "requestedCapabilities": ["search", "retrieve"]
  }
}
```

3. Copie o `encryptedPayload` retornado

**Registrar no Node B (localhost:5001)**:

1. Localize endpoint: `POST /api/node/register`
2. **ADICIONE O HEADER X-Channel-Id**: Cole o `channelId`
3. Cole o `encryptedPayload` que você acabou de criar
4. Clique em **Execute**

**Resultado esperado** (descriptografado):

```json
{
  "success": true,
  "registrationId": "f6cdb452-17a1-4d8f-9241-0974f80c56ef",
  "status": 2,  // Pending
  "message": "Registration received. Pending administrator approval.",
  "estimatedApprovalTime": "1.00:00:00"
}
```

---

### **PASSO 8: Aprovar Node A (Ação do Administrador)**

**No Node B (localhost:5001)**:

1. Localize endpoint: `PUT /api/node/{nodeId}/status`
2. Cole `node-a-test-001` em `nodeId`
3. Cole no body:

```json
{
  "status": 1
}
```

**Status codes**:
- `0` = Unknown
- `1` = Authorized ✅
- `2` = Pending
- `3` = Revoked

4. Clique em **Execute**

**Resultado esperado**:

```json
{
  "message": "Node status updated successfully",
  "nodeId": "node-a-test-001",
  "status": 1
}
```

---

### **PASSO 9: Identificar Node A Novamente (Agora Autorizado)**

**Repita o PASSO 5** (mesma requisição criptografada)

**Resultado esperado** (descriptografado com PASSO 6):

```json
{
  "isKnown": true,
  "status": 1,  // Authorized ✅
  "nodeId": "node-a-test-001",
  "nodeName": "Node A - Test Instance",
  "timestamp": "2025-10-02T10:35:00Z",
  "message": "Node is authorized. Proceed to Phase 3 (Mutual Authentication).",
  "nextPhase": "phase3_authenticate",  // ✅ Pode prosseguir!
  "registrationUrl": null
}
```

---

## ✅ Checklist de Validação

Após executar todos os passos, você deve ter validado:

- [x] Canal criptografado estabelecido entre Node A e Node B
- [x] Certificado X.509 gerado para Node A
- [x] Dados assinados com chave privada do certificado
- [x] Payload criptografado com AES-256-GCM
- [x] Identificação de nó desconhecido retorna `isKnown: false`
- [x] Registro de nó com payload criptografado
- [x] Status de nó atualizado para `Authorized`
- [x] Identificação de nó autorizado retorna `nextPhase: "phase3_authenticate"`
- [x] Todos os payloads transmitidos criptografados
- [x] Header `X-Channel-Id` obrigatório em todas as requisições Fase 2+

---

## 🐛 Troubleshooting

### Erro: "Channel does not exist or has expired"

**Causa**: Canal expirou (30 minutos) ou `channelId` inválido

**Solução**: Repita PASSO 1 para criar novo canal

---

### Erro: "Failed to decrypt request payload"

**Causa**:
- `channelId` errado no header
- Payload criptografado com canal diferente
- Formato JSON inválido

**Solução**:
1. Verifique se o `channelId` no header é o mesmo usado na criptografia
2. Recrie o payload criptografado (PASSO 4 ou 7)
3. Certifique-se de copiar todo o objeto `encryptedPayload` (3 propriedades)

---

### Erro: "Node signature verification failed"

**Causa**: Assinatura inválida ou dados incorretos

**Solução**:
1. Certifique-se de que o `data` no PASSO 3 segue o formato: `{channelId}{nodeId}`
2. Use o mesmo certificado nos PASSOs 2, 3, 4 e 7
3. Gere nova assinatura (PASSO 3)

---

### Response 400 em vez de 200

**Causa**: Payload não está no formato esperado

**Solução**:
1. Verifique os logs do container:
   ```powershell
   docker logs irn-node-b --tail 50
   ```
2. Procure por erros de deserialização JSON
3. Certifique-se de que o payload criptografado tem as 3 propriedades: `encryptedData`, `iv`, `authTag`

---

## 📚 Referências

- [Documentação de Endpoints de Teste](../development/testing-endpoints-criptografia.md)
- [Implementação de Criptografia](../development/channel-encryption-implementation.md)
- [Arquitetura do Handshake](../architecture/handshake-protocol.md)

---

## 🎯 Próximos Passos

Após validar a Fase 2, você pode:

1. **Testar automação**: Use o script `test-phase2-full.ps1`
2. **Implementar Fase 3**: Autenticação mútua com challenge/response
3. **Explorar cenários de erro**: Testar nós revogados, certificados expirados, etc.
