# Endpoints de Teste de Criptografia

## Visão Geral

Esta documentação descreve os novos endpoints de teste que permitem criptografar e descriptografar payloads usando as chaves simétricas de canais ativos. Esses endpoints são fundamentais para:

- **Testar a comunicação criptografada** entre nós
- **Validar a integridade** dos dados criptografados
- **Debug e troubleshooting** de problemas de criptografia
- **Desenvolver e testar** novos endpoints que usam criptografia

## 🔐 Endpoints Disponíveis

### 1. GET /api/testing/channel-info/{channelId}

Obtém informações sobre um canal ativo.

#### Parâmetros
- `channelId` (path): ID do canal a ser consultado

#### Resposta de Sucesso (200)
```json
{
  "channelId": "550e8400-e29b-41d4-a716-446655440000",
  "cipher": "AES-256-GCM",
  "role": "client",
  "createdAt": "2025-10-02T10:00:00Z",
  "expiresAt": "2025-10-02T10:30:00Z",
  "isExpired": false,
  "remoteNodeUrl": "http://node-b:8080",
  "clientNonce": "abc123...",
  "serverNonce": "def456...",
  "symmetricKeyLength": 32
}
```

#### Respostas de Erro
- **404 Not Found**: Canal não encontrado ou expirado

#### Exemplo de Uso
```powershell
Invoke-RestMethod -Uri "http://localhost:5000/api/testing/channel-info/550e8400-e29b-41d4-a716-446655440000" -Method Get
```

---

### 2. POST /api/testing/encrypt-payload

Criptografa um payload JSON usando a chave simétrica de um canal ativo.

#### Corpo da Requisição
```json
{
  "channelId": "550e8400-e29b-41d4-a716-446655440000",
  "payload": {
    // Qualquer objeto JSON válido
    "message": "Hello, World!",
    "data": {
      "temperature": 36.5,
      "heartRate": 72
    }
  }
}
```

#### Resposta de Sucesso (200)
```json
{
  "channelId": "550e8400-e29b-41d4-a716-446655440000",
  "encryptedPayload": {
    "encryptedData": "SGVsbG8gV29ybGQhCg==",
    "iv": "MTIzNDU2Nzg5MDEyMzQ1Ng==",
    "authTag": "YWJjZGVmZ2hpamtsbW5vcA=="
  },
  "channelInfo": {
    "cipher": "AES-256-GCM",
    "role": "client",
    "createdAt": "2025-10-02T10:00:00Z",
    "expiresAt": "2025-10-02T10:30:00Z"
  },
  "usage": "Use this encrypted payload in requests to /api/channel/send-message or similar encrypted endpoints"
}
```

#### Respostas de Erro
- **404 Not Found**: Canal não encontrado ou expirado
- **500 Internal Server Error**: Erro ao criptografar

#### Processo de Criptografia
1. **Serialização**: Payload é serializado para JSON
2. **Conversão**: JSON é convertido para bytes UTF-8
3. **Criptografia**: Bytes são criptografados com AES-256-GCM
4. **Codificação**: Resultado é codificado em Base64

#### Exemplo de Uso
```powershell
$request = @{
    channelId = "550e8400-e29b-41d4-a716-446655440000"
    payload = @{
        message = "Hello, World!"
        timestamp = (Get-Date).ToUniversalTime().ToString("o")
    }
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5000/api/testing/encrypt-payload" `
    -Method Post `
    -ContentType "application/json" `
    -Body $request
```

---

### 3. POST /api/testing/decrypt-payload

Descriptografa um payload criptografado usando a chave simétrica de um canal ativo.

#### Corpo da Requisição
```json
{
  "channelId": "550e8400-e29b-41d4-a716-446655440000",
  "encryptedPayload": {
    "encryptedData": "SGVsbG8gV29ybGQhCg==",
    "iv": "MTIzNDU2Nzg5MDEyMzQ1Ng==",
    "authTag": "YWJjZGVmZ2hpamtsbW5vcA=="
  }
}
```

#### Resposta de Sucesso (200)
```json
{
  "channelId": "550e8400-e29b-41d4-a716-446655440000",
  "decryptedPayload": {
    "message": "Hello, World!",
    "timestamp": "2025-10-02T10:30:00Z"
  },
  "channelInfo": {
    "cipher": "AES-256-GCM",
    "role": "client",
    "createdAt": "2025-10-02T10:00:00Z",
    "expiresAt": "2025-10-02T10:30:00Z"
  }
}
```

#### Respostas de Erro
- **404 Not Found**: Canal não encontrado ou expirado
- **400 Bad Request**: Falha na autenticação (payload adulterado ou chave errada)
- **500 Internal Server Error**: Erro ao descriptografar

#### Processo de Descriptografia
1. **Decodificação**: Dados Base64 são decodificados para bytes
2. **Descriptografia**: Bytes são descriptografados com AES-256-GCM
3. **Validação**: Tag de autenticação é verificada (detecta adulteração)
4. **Conversão**: Bytes UTF-8 são convertidos para string JSON
5. **Desserialização**: String JSON é desserializada para objeto

#### Exemplo de Uso
```powershell
$request = @{
    channelId = "550e8400-e29b-41d4-a716-446655440000"
    encryptedPayload = @{
        encryptedData = "SGVsbG8gV29ybGQhCg=="
        iv = "MTIzNDU2Nzg5MDEyMzQ1Ng=="
        authTag = "YWJjZGVmZ2hpamtsbW5vcA=="
    }
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5000/api/testing/decrypt-payload" `
    -Method Post `
    -ContentType "application/json" `
    -Body $request
```

---

## 🔒 Detalhes Técnicos de Criptografia

### Algoritmo: AES-256-GCM

**AES-256-GCM** (Galois/Counter Mode) foi escolhido por:

- ✅ **Confidencialidade**: Criptografia forte com chave de 256 bits
- ✅ **Integridade**: Tag de autenticação detecta adulteração
- ✅ **Performance**: Modo GCM é altamente eficiente
- ✅ **Padrão da Indústria**: Recomendado pelo NIST

### Componentes do Payload Criptografado

#### 1. EncryptedData
- Dados criptografados do payload original
- Codificado em Base64 para transmissão
- Tamanho: aproximadamente igual ao payload original

#### 2. IV (Initialization Vector / Nonce)
- Valor único usado para cada criptografia
- 12 bytes (96 bits) para AES-GCM
- Gerado aleatoriamente usando `RandomNumberGenerator`
- Codificado em Base64

#### 3. AuthTag (Authentication Tag)
- Tag de autenticação GCM
- 16 bytes (128 bits)
- Verifica integridade e autenticidade
- Qualquer modificação nos dados invalidará a tag
- Codificado em Base64

### Fluxo de Derivação de Chave

```
Shared Secret (ECDH)
        ↓
    HKDF-SHA256
        ↓
  (salt: nonces combinados)
  (info: contexto do canal)
        ↓
Symmetric Key (32 bytes)
        ↓
    AES-256-GCM
```

---

## 📋 Casos de Uso

### 1. Testar Comunicação Criptografada

```powershell
# 1. Abrir canal
$channel = Invoke-RestMethod -Uri "http://localhost:5000/api/channel/initiate" `
    -Method Post -ContentType "application/json" `
    -Body '{"remoteNodeUrl": "http://node-b:8080"}'

# 2. Criptografar payload
$encrypted = Invoke-RestMethod -Uri "http://localhost:5000/api/testing/encrypt-payload" `
    -Method Post -ContentType "application/json" `
    -Body (@{
        channelId = $channel.channelId
        payload = @{ message = "Test" }
    } | ConvertTo-Json)

# 3. Enviar para o nó remoto
# (implementar endpoint que aceita payloads criptografados)

# 4. Verificar descriptografia local
$decrypted = Invoke-RestMethod -Uri "http://localhost:5000/api/testing/decrypt-payload" `
    -Method Post -ContentType "application/json" `
    -Body (@{
        channelId = $channel.channelId
        encryptedPayload = $encrypted.encryptedPayload
    } | ConvertTo-Json)
```

### 2. Validar Integridade dos Dados

```powershell
# Criptografar
$encrypted = # ... (ver acima)

# Adulterar o authTag
$tampered = $encrypted.encryptedPayload
$tampered.authTag = "TAMPERED_TAG"

# Tentar descriptografar (deve falhar)
try {
    Invoke-RestMethod -Uri "http://localhost:5000/api/testing/decrypt-payload" `
        -Method Post -ContentType "application/json" `
        -Body (@{
            channelId = $channel.channelId
            encryptedPayload = $tampered
        } | ConvertTo-Json)
} catch {
    Write-Host "✅ Adulteração detectada corretamente: $($_.Exception.Message)"
}
```

### 3. Debug de Problemas de Criptografia

```powershell
# Obter informações detalhadas do canal
$channelInfo = Invoke-RestMethod -Uri "http://localhost:5000/api/testing/channel-info/$channelId" -Method Get

Write-Host "Canal Info:"
Write-Host "  Cipher: $($channelInfo.cipher)"
Write-Host "  Key Length: $($channelInfo.symmetricKeyLength) bytes"
Write-Host "  Expires At: $($channelInfo.expiresAt)"
Write-Host "  Is Expired: $($channelInfo.isExpired)"
```

### 4. Testar Payloads Complexos

```powershell
$complexPayload = @{
    biosignalType = "ECG"
    patientId = "PATIENT-123"
    timestamp = (Get-Date).ToUniversalTime().ToString("o")
    samplingRate = 500
    values = @(0.5, 0.6, 0.7, 0.8, 1.0, 1.2, 1.0, 0.8, 0.6, 0.5)
    metadata = @{
        leadType = "Lead II"
        deviceId = "ECG-DEVICE-001"
        location = "Lab A"
        annotations = @(
            @{ time = 1.5; type = "P-wave" }
            @{ time = 2.1; type = "QRS-complex" }
        )
    }
}

$encrypted = Invoke-RestMethod -Uri "http://localhost:5000/api/testing/encrypt-payload" `
    -Method Post -ContentType "application/json" `
    -Body (@{
        channelId = $channelId
        payload = $complexPayload
    } | ConvertTo-Json -Depth 10)

# Verificar que estrutura complexa foi preservada
$decrypted = # ... descriptografar
# Verificar: $decrypted.decryptedPayload.metadata.annotations[0].type == "P-wave"
```

---

## 🛡️ Segurança

### Proteções Implementadas

1. **Autenticação de Origem**
   - Tag GCM verifica que dados vêm da fonte correta
   - Impossível falsificar sem a chave

2. **Detecção de Adulteração**
   - Qualquer modificação nos dados invalida a tag
   - Falha imediata na descriptografia

3. **Confidencialidade**
   - AES-256 é considerado seguro até 2030+
   - Chave de 256 bits resiste a ataques de força bruta

4. **Unicidade**
   - IV único para cada criptografia
   - Previne ataques de replay e análise de padrões

5. **Expiração de Canal**
   - Canais expiram após 30 minutos
   - Força renovação periódica de chaves

### Limitações Conhecidas

⚠️ **Armazenamento em Memória**
- Canais são armazenados em memória (não persistem entre reinícios)
- Em produção, usar Redis ou similar para clusters

⚠️ **Endpoint de Teste**
- Esses endpoints devem ser **desabilitados em produção**
- Atualmente disponíveis apenas em ambientes `Development`, `NodeA`, e `NodeB`

⚠️ **Sem Rate Limiting**
- Não há limitação de taxa nestes endpoints
- Implementar throttling em produção

---

## 🧪 Testes Automatizados

### Script PowerShell
```powershell
.\test-encryption.ps1
```

Este script testa:
- ✅ Health check dos nós
- ✅ Abertura de canal criptografado
- ✅ Obtenção de informações do canal
- ✅ Criptografia de payload
- ✅ Descriptografia de payload
- ✅ Verificação de integridade
- ✅ Detecção de adulteração
- ✅ Rejeição de canal inválido

### Testes C# (xUnit)
```csharp
// Ver: Bioteca.Prism.InteroperableResearchNode.Test/
// - EncryptedChannelIntegrationTests.cs
// - SecurityAndEdgeCaseTests.cs
```

---

## 📚 Referências

### Documentação Relacionada
- [Channel Encryption Implementation](channel-encryption-implementation.md)
- [Handshake Protocol](../architecture/handshake-protocol.md)
- [Manual Testing Guide](../testing/manual-testing-guide.md)

### Padrões e Especificações
- [NIST SP 800-38D](https://nvlpubs.nist.gov/nistpubs/Legacy/SP/nistspecialpublication800-38d.pdf) - GCM Mode
- [RFC 5869](https://tools.ietf.org/html/rfc5869) - HKDF
- [FIPS 197](https://nvlpubs.nist.gov/nistpubs/FIPS/NIST.FIPS.197.pdf) - AES

### Exemplos HTTP
- [docs/api-examples/testing-encryption.http](../api-examples/testing-encryption.http)

---

## 🔧 Troubleshooting

### Erro: "Channel not found"
**Causa**: Canal expirado ou ID incorreto

**Solução**:
```powershell
# Verificar se canal ainda existe
Invoke-RestMethod -Uri "http://localhost:5000/api/testing/channel-info/$channelId" -Method Get

# Se expirado, abrir novo canal
$newChannel = Invoke-RestMethod -Uri "http://localhost:5000/api/channel/initiate" `
    -Method Post -ContentType "application/json" `
    -Body '{"remoteNodeUrl": "http://node-b:8080"}'
```

### Erro: "Authentication failed"
**Causa**: Payload foi adulterado ou chave incorreta

**Solução**:
- Verificar que `channelId` está correto
- Não modificar `encryptedData`, `iv` ou `authTag`
- Verificar que canal não expirou entre criptografia e descriptografia

### Erro: "Failed to encrypt payload"
**Causa**: Payload inválido ou erro de serialização

**Solução**:
- Verificar que payload é um objeto JSON válido
- Evitar referências circulares
- Limitar profundidade de objetos aninhados

---

**Última atualização**: 2025-10-02

