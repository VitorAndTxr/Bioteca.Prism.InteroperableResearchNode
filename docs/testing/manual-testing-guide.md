# Guia de Testes Manuais e Discovery - Fases 1, 2 e 3

**Versão**: 0.5.0 (com autenticação mútua)
**Última atualização**: 2025-10-03

Este guia fornece um roteiro passo a passo para testar e entender manualmente o funcionamento das Fases 1, 2 e 3 do protocolo de handshake, ideal para debugging e aprendizado.

## ⚠️ IMPORTANTE: Criptografia de Canal

**A partir da versão 0.3.0**, todas as comunicações após o estabelecimento do canal (Fase 1) **DEVEM ser criptografadas** usando a chave simétrica derivada do canal.

- ✅ **Fase 1** (`/api/channel/open`, `/api/channel/initiate`) - Sem criptografia (estabelece o canal)
- 🔒 **Fase 2** (`/api/channel/identify`, `/api/node/register`) - **Payload criptografado obrigatório**
- 🔒 **Fase 3** (`/api/node/challenge`, `/api/node/authenticate`) - **Payload criptografado obrigatório**
- 🔒 **Fase 4** - **Payload criptografado obrigatório** (planejada)

**Formato do payload criptografado**:
```json
{
  "encryptedData": "base64-encoded-ciphertext",
  "iv": "base64-encoded-initialization-vector",
  "authTag": "base64-encoded-authentication-tag"
}
```

**Header obrigatório para Fase 2+**: `X-Channel-Id: {channelId}`

## 📋 Pré-requisitos

1. **Visual Studio 2022** ou **Visual Studio Code** com extensão C# Dev Kit
2. **Docker Desktop** rodando
3. **Postman**, **Insomnia**, ou **curl** para testes de API
4. **Redis CLI** (para inspeção de persistência) - incluído no container Docker
5. **Conhecimento básico de**:
   - Criptografia assimétrica (ECDH, RSA)
   - Certificados X.509
   - REST APIs
   - Redis (opcional, para testes de persistência)

## 🎯 Objetivo

Entender o fluxo completo de:
1. **Fase 1**: Estabelecimento de canal criptografado com chaves efêmeras
2. **Fase 2**: Identificação e autorização de nós com certificados
3. **Fase 3**: Autenticação mútua via challenge-response com prova de posse de chave privada

---

## 🚀 Parte 1: Preparação do Ambiente

### Passo 1.1: Subir os Containers Docker

```powershell
# Navegar até a pasta do projeto
cd D:\Repos\Faculdade\PRISM\InteroperableResearchNode

# Subir os containers
docker-compose up -d

# Verificar se estão rodando
docker ps
```

**Resultado esperado:**
```
NAMES        STATUS       PORTS
irn-node-a   Up X min     0.0.0.0:5000->8080/tcp
irn-node-b   Up X min     0.0.0.0:5001->8080/tcp
```

### Passo 1.2: Verificar Health Check

**Teste Node A:**
```powershell
curl http://localhost:5000/api/channel/health
```

**Teste Node B:**
```powershell
curl http://localhost:5001/api/channel/health
```

**Resultado esperado:**
```json
{
  "status": "healthy",
  "timestamp": "2025-10-01T..."
}
```

### Passo 1.3: Verificar Conectividade Redis (Opcional - Persistência)

**Verificar Redis Node A:**
```powershell
docker exec -it irn-redis-node-a redis-cli -a prism-redis-password-node-a PING
```

**Resultado esperado:** `PONG`

**Verificar Redis Node B:**
```powershell
docker exec -it irn-redis-node-b redis-cli -a prism-redis-password-node-b PING
```

**Resultado esperado:** `PONG`

**💡 Nota:** Se Redis não estiver habilitado (FeatureFlags:UseRedisForSessions=false), os containers Redis não estarão rodando e o sistema usará armazenamento in-memory.

### Passo 1.4: Acessar Swagger UI

Abra no navegador:
- **Node A**: http://localhost:5000/swagger
- **Node B**: http://localhost:5001/swagger

Explore os endpoints disponíveis em cada nó.

---

## 🔐 Parte 2: FASE 1 - Canal Criptografado

### Objetivo da Fase 1
Estabelecer um canal criptografado entre dois nós usando **chaves efêmeras ECDH** (Perfect Forward Secrecy).

### Passo 2.1: Entender a Arquitetura

**Componentes envolvidos:**
- `EphemeralKeyService.cs` - Gera e gerencia chaves ECDH efêmeras
- `ChannelEncryptionService.cs` - Deriva chaves simétricas usando HKDF
- `ChannelController.cs` - Endpoints `/open` (servidor) e `/initiate` (cliente)
- `NodeChannelClient.cs` - Cliente HTTP para iniciar handshake

### Passo 2.2: Debugging - Iniciar Handshake

**Configure breakpoints em:**

1. **`ChannelController.cs:168`** - Método `InitiateHandshake` (Node A - cliente)
   ```csharp
   public async Task<IActionResult> InitiateHandshake([FromBody] InitiateHandshakeRequest request)
   ```

2. **`NodeChannelClient.cs:40`** - Método `OpenChannelAsync` (Node A - lógica cliente)
   ```csharp
   public async Task<ChannelEstablishmentResult> OpenChannelAsync(string remoteNodeUrl)
   ```

3. **`ChannelController.cs:49`** - Método `OpenChannel` (Node B - servidor)
   ```csharp
   public IActionResult OpenChannel([FromBody] ChannelOpenRequest request)
   ```

4. **`EphemeralKeyService.cs:18`** - Geração de chaves ECDH
   ```csharp
   public ECDiffieHellman GenerateEphemeralKeyPair(string curve = "P384")
   ```

**Executar teste:**

```powershell
# Via PowerShell
Invoke-RestMethod -Uri "http://localhost:5000/api/channel/initiate" `
  -Method Post `
  -ContentType "application/json" `
  -Body '{"remoteNodeUrl": "http://node-b:8080"}'

# Via curl
curl -X POST http://localhost:5000/api/channel/initiate `
  -H "Content-Type: application/json" `
  -d '{"remoteNodeUrl": "http://node-b:8080"}'
```

### Passo 2.3: Inspecionar Variáveis Durante Debug

**No Node A (Cliente) - `NodeChannelClient.cs:OpenChannelAsync`:**

Pause na linha após geração de chaves e inspecione:

```csharp
var clientEcdh = _ephemeralKeyService.GenerateEphemeralKeyPair(curve);
// 🔍 INSPECIONAR: clientEcdh
// - Verificar que é um ECDiffieHellman
// - Verificar curve: P384
```

```csharp
var clientPublicKey = _ephemeralKeyService.ExportPublicKey(clientEcdh);
// 🔍 INSPECIONAR: clientPublicKey
// - String Base64 com ~120 caracteres
// - Esta chave será enviada ao servidor
```

**No Node B (Servidor) - `ChannelController.cs:OpenChannel`:**

```csharp
var serverEcdh = _ephemeralKeyService.GenerateEphemeralKeyPair(curve);
var serverPublicKey = _ephemeralKeyService.ExportPublicKey(serverEcdh);
// 🔍 INSPECIONAR: serverPublicKey
// - Diferente da chave do cliente
// - Esta chave será enviada de volta
```

```csharp
var sharedSecret = _ephemeralKeyService.DeriveSharedSecret(serverEcdh, clientEcdh);
// 🔍 INSPECIONAR: sharedSecret
// - byte[] com 48 bytes (ECDH P-384)
// - Este é o segredo compartilhado!
// - Ambos os lados derivam o MESMO valor
```

```csharp
var symmetricKey = _encryptionService.DeriveKey(sharedSecret, salt, info);
// 🔍 INSPECIONAR: symmetricKey
// - byte[] com 32 bytes (AES-256)
// - Derivado via HKDF-SHA256
// - Usado para criptografia do canal
```

### Passo 2.4: Validar Resultado

**Resposta esperada:**
```json
{
  "success": true,
  "channelId": "db7b9540-a1da-44c5-87c9-e78c933e4745",
  "symmetricKey": "[base64 string]",
  "selectedCipher": "AES-256-GCM",
  "remoteNodeUrl": "http://node-b:8080",
  "clientNonce": "[base64]",
  "serverNonce": "[base64]"
}
```

**Pontos de validação:**
- ✅ `success: true`
- ✅ `channelId` é um GUID válido
- ✅ `selectedCipher` é uma das cifras suportadas
- ✅ Chaves efêmeras foram descartadas após derivação

### Passo 2.5: Verificar Canal Estabelecido

**No Node A (cliente):**
```powershell
curl http://localhost:5000/api/channel/{channelId}
```

**Resultado esperado:**
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

**No Node B (servidor):**
```powershell
curl http://localhost:5001/api/channel/{channelId}
```

**Resultado esperado:**
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

**Observação importante:**
- O **mesmo `channelId`** existe em **ambos os nós**
- Roles diferentes: `client` vs `server`

### Passo 2.6: Logs do Docker

**Verificar logs do Node A:**
```powershell
docker logs irn-node-a --tail 20
```

**Procurar por:**
```
info: Bioteca.Prism.Service.Services.Node.EphemeralKeyService[0]
      Generated ephemeral ECDH key pair with curve P384
info: Bioteca.Prism.Service.Services.Node.NodeChannelClient[0]
      Channel {channelId} established successfully with http://node-b:8080
```

**Verificar logs do Node B:**
```powershell
docker logs irn-node-b --tail 20
```

**Procurar por:**
```
info: Bioteca.Prism.Service.Services.Node.EphemeralKeyService[0]
      Derived shared secret (48 bytes)
info: Bioteca.Prism.InteroperableResearchNode.Controllers.ChannelController[0]
      Channel {channelId} established successfully with cipher AES-256-GCM
```

### Passo 2.7: 🗄️ Verificar Persistência Redis do Canal (Opcional)

**Se Redis estiver habilitado** (FeatureFlags:UseRedisForChannels=true):

**No Node A - Listar channels armazenados:**
```powershell
docker exec -it irn-redis-node-a redis-cli -a prism-redis-password-node-a KEYS "channel:*"
```

**Resultado esperado:**
```
1) "channel:{channelId}"
2) "channel:key:{channelId}"
```

**Inspecionar metadata do channel:**
```powershell
docker exec -it irn-redis-node-a redis-cli -a prism-redis-password-node-a GET "channel:{channelId}"
```

**Resultado esperado:** JSON com ChannelId, SelectedCipher, RemoteNodeUrl, CreatedAt, ExpiresAt, Role

**Verificar TTL (Time To Live) do channel:**
```powershell
docker exec -it irn-redis-node-a redis-cli -a prism-redis-password-node-a TTL "channel:{channelId}"
```

**Resultado esperado:** ~1800 segundos (30 minutos)

**No Node B - Verificar channel do lado servidor:**
```powershell
docker exec -it irn-redis-node-b redis-cli -a prism-redis-password-node-b KEYS "channel:*"
```

**💡 Observação:**
- O **mesmo channelId** existe em **ambas as instâncias Redis** (uma por nó)
- Cada nó tem sua própria instância Redis isolada (redis-node-a, redis-node-b)
- A chave simétrica binária está armazenada em `channel:key:{channelId}` (32 bytes AES-256)
- Após 30 minutos, o canal expira automaticamente (TTL gerenciado pelo Redis)

**Teste de persistência** (opcional):
```powershell
# 1. Reiniciar APENAS o Node A (Redis continua rodando)
docker restart irn-node-a

# 2. Aguardar Node A ficar online
docker logs -f irn-node-a

# 3. Verificar se o canal ainda existe no Redis
docker exec -it irn-redis-node-a redis-cli -a prism-redis-password-node-a EXISTS "channel:{channelId}"

# Resultado: 1 (existe) - Canal sobreviveu ao restart do Node A!
```

---

## 🆔 Parte 3: FASE 2 - Identificação de Nós

### Objetivo da Fase 2
Identificar e autorizar nós usando certificados X.509 e assinaturas digitais.

### Passo 3.1: Entender a Arquitetura

**Componentes envolvidos:**
- `NodeRegistryService.cs` - Gerencia registro de nós (in-memory)
- `CertificateHelper.cs` - Utilitários para certificados
- `ChannelController.cs` - Endpoint `/identify` (Fase 2)
- `TestingController.cs` - Endpoints de geração de certificados

### Passo 3.2: Estabelecer Canal (Pré-requisito)

⚠️ **IMPORTANTE**: Antes de registrar ou identificar, você **DEVE** estabelecer um canal criptografado.

```powershell
# Estabelecer canal entre Node A e Node B
$channelResult = Invoke-RestMethod -Uri "http://localhost:5000/api/channel/initiate" `
  -Method Post `
  -ContentType "application/json" `
  -Body '{"remoteNodeUrl": "http://node-b:8080"}'

$channelId = $channelResult.channelId
$symmetricKey = $channelResult.symmetricKey

Write-Host "Channel ID: $channelId"
Write-Host "Symmetric Key (base64): $symmetricKey"
```

**Por que isso é necessário?**
- O `channelId` será usado no header `X-Channel-Id`
- A `symmetricKey` será usada para criptografar/descriptografar payloads
- Sem canal válido, endpoints de Fase 2+ retornam erro `ERR_MISSING_CHANNEL_ID`

### Passo 3.3: Gerar Certificado Auto-Assinado

**Endpoint de teste:**
```powershell
curl -X POST http://localhost:5000/api/testing/generate-certificate `
  -H "Content-Type: application/json" `
  -d '{"subjectName": "node-a-test-001", "validityYears": 2, "password": "test123"}'
```

**Resultado esperado:**
```json
{
  "subjectName": "node-a-test-001",
  "certificate": "MIIC5TCCA...",  // Chave pública (Base64)
  "certificateWithPrivateKey": "MIIJC...",  // PFX com chave privada
  "password": "test123",
  "validFrom": "2025-10-01T...",
  "validTo": "2027-10-01T...",
  "thumbprint": "79B7FC808E5BAFC...",
  "serialNumber": "4C770147DA3CAEFA",
  "usage": {
    "certificate": "Use this for registration and identification (public key)",
    "certificateWithPrivateKey": "Use this to sign data (includes private key)",
    "password": "Password to load the PFX certificate"
  }
}
```

**💡 IMPORTANTE:**
- `certificate` - Apenas chave pública (enviar no registro)
- `certificateWithPrivateKey` - PFX com chave privada (para assinar dados)
- Salve ambos em um arquivo JSON para usar nos próximos passos

### Passo 3.3: Debugging - Geração de Certificado

**Configure breakpoint em:**

**`CertificateHelper.cs:18`** - Método `GenerateSelfSignedCertificate`
```csharp
public static X509Certificate2 GenerateSelfSignedCertificate(string subjectName, int validityYears = 2)
```

**Inspecione:**
```csharp
using var rsa = RSA.Create(2048);
// 🔍 INSPECIONAR: rsa
// - Algoritmo RSA com 2048 bits

var certificate = request.CreateSelfSigned(notBefore, notAfter);
// 🔍 INSPECIONAR: certificate
// - Subject: CN=node-a-test-001
// - HasPrivateKey: true
// - SignatureAlgorithm: sha256RSA
```

### Passo 3.4: Gerar Identidade Completa (com Assinatura)

Este endpoint gera certificado + assinatura pronta para identificação.

**Primeiro, estabeleça um canal (Fase 1):**
```powershell
$channelResult = Invoke-RestMethod -Uri "http://localhost:5000/api/channel/initiate" `
  -Method Post -ContentType "application/json" `
  -Body '{"remoteNodeUrl": "http://node-b:8080"}'

$channelId = $channelResult.channelId
Write-Host "Channel ID: $channelId"
```

**Agora gere a identidade:**
```powershell
$identityBody = @{
    nodeId = "node-a-test-001"
    nodeName = "Interoperable Research Node A - Test"
    channelId = $channelId
    validityYears = 2
    password = "test123"
} | ConvertTo-Json

$identity = Invoke-RestMethod -Uri "http://localhost:5000/api/testing/generate-node-identity" `
  -Method Post `
  -ContentType "application/json" `
  -Body $identityBody

# Salvar para uso posterior
$identity | ConvertTo-Json -Depth 10 | Out-File "node-a-identity.json"
```

**Resultado esperado:**
```json
{
  "nodeId": "node-a-test-001",
  "nodeName": "Interoperable Research Node A - Test",
  "certificate": "MIIC5TCCA...",
  "certificateWithPrivateKey": "MIIJC...",
  "password": "test123",
  "identificationRequest": {
    "channelId": "db7b9540-...",
    "nodeId": "node-a-test-001",
    "nodeName": "Interoperable Research Node A - Test",
    "certificate": "MIIC5TCCA...",
    "timestamp": "2025-10-01T23:50:21.123Z",
    "signature": "as8xW2gPKcRnPDma..."  // ⬅️ Assinatura RSA-SHA256
  },
  "usage": "Use 'identificationRequest' object to call /api/channel/identify"
}
```

**💡 O que está acontecendo:**
1. Certificado X.509 é gerado
2. Dados são assinados: `channelId + nodeId + timestamp`
3. Assinatura usa a chave privada do certificado (RSA-SHA256)
4. Objeto `identificationRequest` está pronto para uso

### Passo 3.5: Debugging - Assinatura de Dados

**Configure breakpoint em:**

**`CertificateHelper.cs:57`** - Método `SignData`
```csharp
public static string SignData(string data, X509Certificate2 certificate)
```

**Inspecione:**
```csharp
using var rsa = certificate.GetRSAPrivateKey();
// 🔍 INSPECIONAR: rsa
// - Verifica que certificado tem chave privada

var dataBytes = System.Text.Encoding.UTF8.GetBytes(data);
// 🔍 INSPECIONAR: data
// - Exemplo: "db7b9540-a1da-44c5-87c9-e78c933e4745node-a-test-0012025-10-01T23:50:21.123Z"

var signature = rsa.SignData(dataBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
// 🔍 INSPECIONAR: signature
// - byte[] com assinatura RSA
// - Será convertida para Base64
```

### Passo 3.6: Registrar Nó Desconhecido (COM CRIPTOGRAFIA)

⚠️ **BREAKING CHANGE (v0.3.0)**: Endpoint agora requer payload criptografado!

**Opção 1: Usando NodeChannelClient (C#) - RECOMENDADO**

```csharp
// Em código C# (teste de integração)
var registrationRequest = new NodeRegistrationRequest
{
    NodeId = "node-a-test-001",
    NodeName = "Interoperable Research Node A - Test",
    Certificate = identity.Certificate,
    ContactInfo = "admin@node-a.test",
    InstitutionDetails = "Test Institution A",
    NodeUrl = "http://node-a:8080",
    RequestedCapabilities = new[] { "search", "retrieve" }
};

var regResult = await _nodeChannelClient.RegisterNodeAsync(channelId, registrationRequest);
```

**Opção 2: PowerShell (limitado - requer helpers C#)**

⚠️ PowerShell não pode facilmente criptografar payloads AES-GCM. Você precisaria:

1. Criar helper C# que aceita JSON e retorna EncryptedPayload
2. Chamar via `Invoke-RestMethod` com headers apropriados

**Exemplo conceitual** (não funcional sem helpers):
```powershell
# Payload em texto claro
$registrationPayload = @{
    nodeId = "node-a-test-001"
    nodeName = "Interoperable Research Node A - Test"
    certificate = $identity.certificate
    contactInfo = "admin@node-a.test"
    institutionDetails = "Test Institution A"
    nodeUrl = "http://node-a:8080"
    requestedCapabilities = @("search", "retrieve")
}

# TODO: Criptografar payload (requer helper)
# $encryptedPayload = Encrypt-PayloadHelper -Payload $registrationPayload -Key $symmetricKey

# Enviar com header X-Channel-Id
# $regResult = Invoke-RestMethod -Uri "http://localhost:5001/api/node/register" `
#   -Method Post `
#   -ContentType "application/json" `
#   -Headers @{"X-Channel-Id" = $channelId} `
#   -Body ($encryptedPayload | ConvertTo-Json)
```

**Para testes PowerShell completos**, use o script atualizado:
```powershell
.\test-phase2-encrypted.ps1
```

**Resultado esperado:**
```json
{
  "success": true,
  "registrationId": "f6cdb452-17a1-4d8f-9241-0974f80c56ef",
  "status": 0,  // Pending
  "message": "Registration received. Pending administrator approval.",
  "estimatedApprovalTime": "1.00:00:00"
}
```

### Passo 3.7: Debugging - Registro de Nó (COM DESCRIPTOGRAFIA)

**Configure breakpoints em:**

**`ChannelController.cs:357`** - Método `RegisterNode` (novo - com criptografia)
```csharp
public async Task<IActionResult> RegisterNode([FromBody] EncryptedPayload encryptedRequest)
```

**Inspecione:**
```csharp
// Validar header X-Channel-Id
if (!Request.Headers.TryGetValue("X-Channel-Id", out var channelIdHeader))
// 🔍 VERIFICAR: Header deve estar presente

var channelContext = _channelStore.GetChannel(channelId);
// 🔍 INSPECIONAR: channelContext
// - ChannelId deve existir
// - SymmetricKey não pode estar vazio

// Descriptografar request
request = _encryptionService.DecryptPayload<NodeRegistrationRequest>(encryptedRequest, channelContext.SymmetricKey);
// 🔍 INSPECIONAR: request (após descriptografia)
// - NodeId, NodeName, Certificate devem estar presentes
```

**`NodeRegistryService.cs:82`** - Método `RegisterNodeAsync`
```csharp
public Task<NodeRegistrationResponse> RegisterNodeAsync(NodeRegistrationRequest request)
```

**Inspecione:**
```csharp
// Verificar se nó já existe
if (_nodes.ContainsKey(request.NodeId))
// 🔍 INSPECIONAR: _nodes
// - Dictionary<string, RegisteredNode>
// - Deve estar vazio na primeira execução

var fingerprint = CalculateCertificateFingerprint(certBytes);
// 🔍 INSPECIONAR: fingerprint
// - Hash SHA-256 do certificado em Base64
// - Usado para identificação única

var registeredNode = new RegisteredNode { ... };
// 🔍 INSPECIONAR: registeredNode
// - NodeId: "node-a-test-001"
// - Status: Pending (2)
// - Certificate: armazenado
// - CertificateFingerprint: calculado
```

### Passo 3.8: Identificar Nó (Status Pending) - COM CRIPTOGRAFIA

⚠️ **BREAKING CHANGE (v0.3.0)**: Endpoint agora requer payload criptografado!

**Usando NodeChannelClient (C#) - RECOMENDADO:**

```csharp
var identifyRequest = new NodeIdentifyRequest
{
    NodeId = "node-a-test-001",
    Certificate = identity.Certificate,
    Signature = identity.IdentificationRequest.Signature,
    SignedData = identity.IdentificationRequest.SignedData
};

var statusResponse = await _nodeChannelClient.IdentifyNodeAsync(channelId, identifyRequest);
```

**PowerShell (conceitual - requer helpers)**:
```powershell
# TODO: Implementar com helper de criptografia
# Ver test-phase2-encrypted.ps1 para detalhes
```

**Resultado esperado:**
```json
{
  "isKnown": true,
  "status": 2,  // Pending
  "nodeId": "node-a-test-001",
  "nodeName": "Interoperable Research Node A - Test",
  "timestamp": "2025-10-01T...",
  "message": "Node registration is pending approval.",
  "nextPhase": null,  // Bloqueado até aprovação
  "registrationUrl": null
}
```

**💡 Observação:**
- Nó é **conhecido** (`isKnown: true`)
- Mas ainda está **pendente** (`status: 2`)
- **Não pode** prosseguir para Fase 3 (`nextPhase: null`)

### Passo 3.9: Debugging - Identificação de Nó (COM DESCRIPTOGRAFIA)

**Configure breakpoints em:**

**`ChannelController.cs:225`** - Método `IdentifyNode` (novo - com criptografia)
```csharp
public async Task<IActionResult> IdentifyNode([FromBody] EncryptedPayload encryptedRequest)
```

**Inspecione:**
```csharp
// Validar header X-Channel-Id
if (!Request.Headers.TryGetValue("X-Channel-Id", out var channelIdHeader))
// 🔍 VERIFICAR: Header deve estar presente

var channelContext = _channelStore.GetChannel(channelId);
// 🔍 INSPECIONAR: channelContext
// - ChannelId, SymmetricKey devem existir

// Descriptografar request
request = _encryptionService.DecryptPayload<NodeIdentifyRequest>(encryptedRequest, channelContext.SymmetricKey);
// 🔍 INSPECIONAR: request (após descriptografia)
// - NodeId, Certificate, Signature devem estar presentes
```

**No método original (após descriptografia):**

**Inspecione:**
```csharp
// Verificar assinatura
var signatureValid = await _nodeRegistry.VerifyNodeSignatureAsync(request);
// 🔍 INSPECIONAR: signatureValid
// - Deve ser true se assinatura estiver correta

var registeredNode = await _nodeRegistry.GetNodeAsync(request.NodeId);
// 🔍 INSPECIONAR: registeredNode
// - NodeId: "node-a-test-001"
// - Status: Pending (2)
// - Certificate: armazenado

switch (registeredNode.Status)
{
    case AuthorizationStatus.Pending:
        // 🔍 BREAKPOINT AQUI
        // - Este é o caso atual
        // - Resposta bloqueará progresso
}
```

**No `NodeRegistryService.cs:44` - Método `VerifyNodeSignatureAsync`:**

```csharp
using var cert = new X509Certificate2(certBytes);
// 🔍 INSPECIONAR: cert
// - Subject: CN=node-a-test-001
// - Carregado do Base64

using var rsa = cert.GetRSAPublicKey();
// 🔍 INSPECIONAR: rsa
// - Chave pública extraída do certificado

var signedData = $"{request.ChannelId}{request.NodeId}{request.Timestamp:O}";
// 🔍 INSPECIONAR: signedData
// - Deve ser EXATAMENTE igual ao que foi assinado
// - Formato do timestamp é crítico!

var isValid = rsa.VerifyData(dataBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
// 🔍 INSPECIONAR: isValid
// - true se assinatura for válida
// - false se dados não baterem ou assinatura incorreta
```

### Passo 3.10: Aprovar Nó (Admin)

**Aprovar o nó registrado:**
```powershell
$approveBody = @{
    status = 1  # Authorized
} | ConvertTo-Json

$approveResult = Invoke-RestMethod -Uri "http://localhost:5001/api/node/node-a-test-001/status" `
  -Method Put `
  -ContentType "application/json" `
  -Body $approveBody

$approveResult
```

**Resultado esperado:**
```json
{
  "message": "Node status updated successfully",
  "nodeId": "node-a-test-001",
  "status": 1  // Authorized
}
```

### Passo 3.11: Debugging - Atualização de Status

**Configure breakpoint em:**

**`NodeRegistryService.cs:156`** - Método `UpdateNodeStatusAsync`
```csharp
public Task<bool> UpdateNodeStatusAsync(string nodeId, AuthorizationStatus status)
```

**Inspecione:**
```csharp
if (!_nodes.TryGetValue(nodeId, out var node))
// 🔍 INSPECIONAR: node
// - RegisteredNode encontrado
// - Status atual: Pending (2)

node.Status = status;
node.UpdatedAt = DateTime.UtcNow;
// 🔍 INSPECIONAR após execução:
// - Status: Authorized (1)
// - UpdatedAt: atualizado
```

### Passo 3.12: Identificar Nó (Status Authorized) - COM CRIPTOGRAFIA

**Usando NodeChannelClient (C#) - RECOMENDADO:**

```csharp
// Gerar nova assinatura com timestamp atualizado
var newTimestamp = DateTime.UtcNow;
var dataToSign = $"{channelId}{identity.NodeId}{newTimestamp:O}";

// Assinar dados
var signature = CertificateHelper.SignData(dataToSign, certificateWithPrivateKey);

// Criar request
var identifyRequest = new NodeIdentifyRequest
{
    NodeId = identity.NodeId,
    Certificate = identity.Certificate,
    Signature = signature,
    SignedData = dataToSign
};

// Identificar (payload será criptografado automaticamente)
var statusResponse = await _nodeChannelClient.IdentifyNodeAsync(channelId, identifyRequest);

Console.WriteLine($"IsKnown: {statusResponse.IsKnown}");
Console.WriteLine($"Status: {statusResponse.Status}"); // Should be "Authorized"
Console.WriteLine($"NextPhase: {statusResponse.NextPhase}"); // Should be "phase3_authenticate"
```

**PowerShell (conceitual)**:
```powershell
# Requer helpers C# para criptografia
# Ver test-phase2-encrypted.ps1
```

**Resultado esperado:**
```json
{
  "isKnown": true,
  "status": 1,  // Authorized ✅
  "nodeId": "node-a-test-001",
  "nodeName": "Interoperable Research Node A - Test",
  "timestamp": "2025-10-01T...",
  "message": "Node is authorized. Proceed to Phase 3 (Mutual Authentication).",
  "nextPhase": "phase3_authenticate",  // ✅ Pronto para Fase 3!
  "registrationUrl": null
}
```

**💡 Observação:**
- Nó é **autorizado** (`status: 1`)
- **Pode prosseguir** para Fase 3 (`nextPhase: "phase3_authenticate"`)

### Passo 3.13: Listar Nós Registrados (Admin)

**Listar todos os nós:**
```powershell
$nodes = Invoke-RestMethod -Uri "http://localhost:5001/api/node/nodes" -Method Get
$nodes | ConvertTo-Json -Depth 3
```

**Resultado esperado:**
```json
[
  {
    "nodeId": "node-a-test-001",
    "nodeName": "Interoperable Research Node A - Test",
    "certificate": "MIIC5TCCA...",
    "certificateFingerprint": "abc123...",
    "nodeUrl": "http://node-a:8080",
    "status": 1,  // Authorized
    "capabilities": ["search", "retrieve"],
    "contactInfo": "admin@node-a.test",
    "institutionDetails": "Test Institution A",
    "registeredAt": "2025-10-01T...",
    "updatedAt": "2025-10-01T...",
    "lastAuthenticatedAt": null,
    "metadata": {}
  }
]
```

---

## 🔐 Parte 4: FASE 3 - Autenticação Mútua Challenge/Response

### Objetivo da Fase 3
Autenticar mutuamente os nós usando **challenge-response** com prova criptográfica de posse de chave privada, gerando session tokens para comunicação autenticada.

### Pré-requisito
⚠️ **IMPORTANTE**: O nó deve estar **AUTORIZADO** (status=Authorized) antes de solicitar autenticação.

### Passo 4.1: Entender a Arquitetura

**Componentes envolvidos:**
- `ChallengeService.cs` - Geração e verificação de challenges
- `IChallengeService.cs` - Interface do serviço
- `NodeConnectionController.cs` - Endpoints `/challenge` e `/authenticate`
- `ChallengeRequest.cs`, `ChallengeResponseRequest.cs` - DTOs
- `ChallengeResponse.cs`, `AuthenticationResponse.cs` - Respostas

**Fluxo:**
```
1. Nó solicita challenge (requer status Authorized)
2. Servidor gera challenge aleatório (32 bytes, TTL 5 min)
3. Nó assina challenge com chave privada
4. Servidor verifica assinatura
5. Servidor gera session token (TTL 1 hora)
```

### Passo 4.2: Garantir Nó Autorizado

**Verificar status do nó:**
```powershell
$nodes = Invoke-RestMethod -Uri "http://localhost:5001/api/node/nodes" -Method Get
$myNode = $nodes | Where-Object { $_.nodeId -eq "node-a-test-001" }
Write-Host "Status: $($myNode.status)"  # Deve ser 1 (Authorized)
```

**Se não estiver autorizado, aprovar:**
```powershell
$approveBody = @{ status = 1 } | ConvertTo-Json
Invoke-RestMethod -Uri "http://localhost:5001/api/node/node-a-test-001/status" `
  -Method Put `
  -ContentType "application/json" `
  -Body $approveBody
```

### Passo 4.3: Solicitar Challenge (COM CRIPTOGRAFIA)

**Usando NodeChannelClient (C#) - RECOMENDADO:**

```csharp
// Pré-requisito: canal estabelecido e nó autorizado
var channelResult = await _nodeChannelClient.OpenChannelAsync("http://node-b:8080");
var channelId = channelResult.ChannelId;

// Solicitar challenge
var challengeResponse = await _nodeChannelClient.RequestChallengeAsync(channelId, "node-a-test-001");

Console.WriteLine($"Challenge Data: {challengeResponse.ChallengeData}");
Console.WriteLine($"Expires At: {challengeResponse.ExpiresAt}");
Console.WriteLine($"TTL: {challengeResponse.ChallengeTtlSeconds} seconds");
```

**Payload da requisição (criptografado automaticamente):**
```json
{
  "channelId": "abc-123",
  "nodeId": "node-a-test-001",
  "timestamp": "2025-10-03T00:00:00Z"
}
```

**Resposta esperada (descriptografada):**
```json
{
  "challengeData": "YXNkZmFzZGZhc2RmYXNkZmFzZGZhc2RmYXNkZg==",
  "challengeTimestamp": "2025-10-03T00:00:01Z",
  "challengeTtlSeconds": 300,
  "expiresAt": "2025-10-03T00:05:01Z"
}
```

### Passo 4.4: Debugging - Geração de Challenge

**Configure breakpoint em:**

**`ChallengeService.cs:26`** - Método `GenerateChallengeAsync`
```csharp
public Task<ChallengeResponse> GenerateChallengeAsync(string channelId, string nodeId)
```

**Inspecione:**
```csharp
var challengeBytes = RandomNumberGenerator.GetBytes(32);
// 🔍 INSPECIONAR: challengeBytes
// - 32 bytes aleatórios
// - Gerados com RandomNumberGenerator (criptograficamente seguro)

var challengeData = Convert.ToBase64String(challengeBytes);
// 🔍 INSPECIONAR: challengeData
// - String Base64 do challenge
// - ~44 caracteres

var key = $"{channelId}:{nodeId}";
_activeChallenges[key] = new ChallengeData { ... };
// 🔍 INSPECIONAR: _activeChallenges
// - ConcurrentDictionary com challenges ativos
// - Chave: "{channelId}:{nodeId}"
// - Valor: ChallengeData com TTL de 5 minutos
```

**No `NodeConnectionController.cs:225`** - Método `RequestChallenge`:
```csharp
// Verificar se nó está autorizado
if (registeredNode.Status != AuthorizationStatus.Authorized)
// 🔍 BREAKPOINT AQUI se erro ERR_NODE_NOT_AUTHORIZED
// - Nó deve ter status Authorized (1)
```

### Passo 4.5: Assinar Challenge

**Carregar certificado com chave privada:**
```csharp
// Usar o certificateWithPrivateKey salvo anteriormente
var certWithPrivateKey = new X509Certificate2(
    Convert.FromBase64String(identity.CertificateWithPrivateKey),
    identity.Password
);

// Construir dados para assinar
var timestamp = DateTime.UtcNow;
var dataToSign = $"{challengeResponse.ChallengeData}{channelId}{nodeId}{timestamp:O}";

// Assinar com chave privada
var signature = CertificateHelper.SignData(dataToSign, certWithPrivateKey);

Console.WriteLine($"Data to Sign: {dataToSign}");
Console.WriteLine($"Signature: {signature}");
```

**💡 Formato da assinatura:**
- `{ChallengeData}{ChannelId}{NodeId}{Timestamp:O}`
- Exemplo: `YXNkZmFz...abc-123node-a-test-0012025-10-03T00:00:02.1234567Z`

### Passo 4.6: Debugging - Assinatura de Challenge

**Configure breakpoint em:**

**`CertificateHelper.cs:57`** - Método `SignData`
```csharp
public static string SignData(string data, X509Certificate2 certificate)
```

**Inspecione:**
```csharp
using var rsa = certificate.GetRSAPrivateKey();
// 🔍 INSPECIONAR: rsa
// - Verificar que chave privada está disponível
// - Se null, certificado não tem chave privada

var signature = rsa.SignData(dataBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
// 🔍 INSPECIONAR: signature
// - byte[] com assinatura RSA (256 bytes para RSA-2048)
// - Será convertida para Base64
```

### Passo 4.7: Submeter Resposta ao Challenge (COM CRIPTOGRAFIA)

**Usando NodeChannelClient (C#) - RECOMENDADO:**

```csharp
// Criar request com challenge assinado
var challengeResponseRequest = new ChallengeResponseRequest
{
    ChannelId = channelId,
    NodeId = nodeId,
    ChallengeData = challengeResponse.ChallengeData,
    Signature = signature,
    Timestamp = timestamp
};

// Autenticar (payload criptografado automaticamente)
var authResponse = await _nodeChannelClient.AuthenticateAsync(channelId, challengeResponseRequest);

Console.WriteLine($"Authenticated: {authResponse.Authenticated}");
Console.WriteLine($"Session Token: {authResponse.SessionToken}");
Console.WriteLine($"Expires At: {authResponse.SessionExpiresAt}");
Console.WriteLine($"Capabilities: {string.Join(", ", authResponse.GrantedCapabilities)}");
Console.WriteLine($"Next Phase: {authResponse.NextPhase}");
```

**Payload da requisição (criptografado automaticamente):**
```json
{
  "channelId": "abc-123",
  "nodeId": "node-a-test-001",
  "challengeData": "YXNkZmFz...",
  "signature": "as8xW2gP...",
  "timestamp": "2025-10-03T00:00:02Z"
}
```

**Resposta esperada (descriptografada):**
```json
{
  "authenticated": true,
  "nodeId": "node-a-test-001",
  "sessionToken": "a1b2c3d4e5f6",
  "sessionExpiresAt": "2025-10-03T01:00:02Z",
  "grantedCapabilities": ["search", "retrieve"],
  "message": "Authentication successful",
  "nextPhase": "phase4_session",
  "timestamp": "2025-10-03T00:00:02Z"
}
```

### Passo 4.8: Debugging - Verificação de Challenge

**Configure breakpoint em:**

**`ChallengeService.cs:58`** - Método `VerifyChallengeResponseAsync`
```csharp
public Task<bool> VerifyChallengeResponseAsync(ChallengeResponseRequest request, string certificate)
```

**Inspecione:**
```csharp
var key = $"{request.ChannelId}:{request.NodeId}";
if (!_activeChallenges.TryGetValue(key, out var challengeData))
// 🔍 INSPECIONAR: challengeData
// - Deve existir (challenge foi gerado antes)
// - Se null, challenge não foi solicitado ou expirou

if (challengeData.ExpiresAt < DateTime.UtcNow)
// 🔍 VERIFICAR: Expiração
// - Challenge tem TTL de 5 minutos
// - Se expirado, retorna false

if (request.ChallengeData != challengeData.ChallengeValue)
// 🔍 VERIFICAR: Dados do challenge
// - Deve corresponder exatamente ao challenge gerado

var signedData = $"{request.ChallengeData}{request.ChannelId}{request.NodeId}{request.Timestamp:O}";
// 🔍 INSPECIONAR: signedData
// - Deve ser EXATAMENTE igual ao que foi assinado pelo cliente

var isValid = rsa.VerifyData(dataBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
// 🔍 INSPECIONAR: isValid
// - true se assinatura for válida
// - false se assinatura incorreta ou dados não correspondem
```

**No `NodeConnectionController.cs:279`** - Método `Authenticate`:
```csharp
var isValid = await _challengeService.VerifyChallengeResponseAsync(request, registeredNode.Certificate);

if (!isValid)
{
    // 🔍 BREAKPOINT AQUI se falhar
    // Causas possíveis:
    // - Challenge expirado (> 5 min)
    // - Challenge não encontrado
    // - Dados do challenge não correspondem
    // - Assinatura inválida
    await _challengeService.InvalidateChallengeAsync(channelId!, request.NodeId);
    return BadRequest(...);
}

// Gerar session token
var authResponse = await _challengeService.GenerateAuthenticationResultAsync(...);
// 🔍 INSPECIONAR: authResponse
// - SessionToken gerado (GUID)
// - SessionExpiresAt: 1 hora no futuro
// - GrantedCapabilities: capabilities do nó registrado
```

### Passo 4.9: Validar Session Token

**Session token gerado:**
```csharp
// Session token é um GUID (versão simplificada)
// Produção: usar JWT com claims
Console.WriteLine($"Session Token: {authResponse.SessionToken}");
// Exemplo: "a1b2c3d4-e5f6-7890-abcd-ef1234567890"

Console.WriteLine($"Expires At: {authResponse.SessionExpiresAt}");
// Exemplo: "2025-10-03T01:00:02Z" (1 hora no futuro)
```

**💡 Armazenamento:**
- Session tokens são armazenados em `ConcurrentDictionary<string, string>`
- Chave: sessionToken
- Valor: nodeId
- TTL: 1 hora (3600 segundos)

### Passo 4.10: Debugging - Geração de Session Token

**Configure breakpoint em:**

**`ChallengeService.cs:121`** - Método `GenerateAuthenticationResultAsync`
```csharp
public Task<AuthenticationResponse> GenerateAuthenticationResultAsync(string nodeId, List<string> grantedCapabilities)
```

**Inspecione:**
```csharp
var sessionToken = Guid.NewGuid().ToString("N");
// 🔍 INSPECIONAR: sessionToken
// - GUID sem hífens (32 caracteres)
// - Exemplo: "a1b2c3d4e5f67890abcdef1234567890"

var expiresAt = DateTime.UtcNow.AddSeconds(SessionTtlSeconds);
// 🔍 INSPECIONAR: expiresAt
// - 1 hora no futuro (SessionTtlSeconds = 3600)

_sessionTokens[sessionToken] = nodeId;
// 🔍 INSPECIONAR: _sessionTokens
// - ConcurrentDictionary armazenando tokens ativos
// - Token → NodeId mapping
```

### Passo 4.11: 🗄️ Verificar Persistência Redis da Session (Opcional)

**Se Redis estiver habilitado** (FeatureFlags:UseRedisForSessions=true):

**Listar sessions armazenadas:**
```powershell
docker exec -it irn-redis-node-b redis-cli -a prism-redis-password-node-b KEYS "session:*"
```

**Resultado esperado:**
```
1) "session:{sessionToken}"
2) "session:node:{nodeId}:sessions"
3) "session:ratelimit:{sessionToken}"
```

**Inspecionar dados da session:**
```powershell
docker exec -it irn-redis-node-b redis-cli -a prism-redis-password-node-b GET "session:{sessionToken}"
```

**Resultado esperado:** JSON com SessionToken, NodeId, ChannelId, CreatedAt, ExpiresAt, AccessLevel, RequestCount

**Verificar TTL da session:**
```powershell
docker exec -it irn-redis-node-b redis-cli -a prism-redis-password-node-b TTL "session:{sessionToken}"
```

**Resultado esperado:** ~3600 segundos (1 hora)

**Verificar rate limiting (Sorted Set):**
```powershell
docker exec -it irn-redis-node-b redis-cli -a prism-redis-password-node-b ZRANGE "session:ratelimit:{sessionToken}" 0 -1 WITHSCORES
```

**Resultado esperado:** Lista de timestamps (scores) representando requests recentes (últimos 60 segundos)

**Verificar sessions por nó:**
```powershell
docker exec -it irn-redis-node-b redis-cli -a prism-redis-password-node-b SMEMBERS "session:node:{nodeId}:sessions"
```

**Resultado esperado:** Set com IDs de sessions ativas para este nó

**💡 Observações:**
- **Session metadata**: Armazenado em hash JSON com TTL de 1 hora
- **Rate limiting**: Sorted Set com sliding window de 60 requests/minuto
- **Node sessions index**: Set para rastrear todas as sessions de um nó
- TTL automático gerenciado pelo Redis (expira após 1 hora de inatividade)

**Teste de persistência de session** (opcional):
```powershell
# 1. Criar session (via autenticação Phase 3)
# 2. Verificar session no Redis
docker exec -it irn-redis-node-b redis-cli -a prism-redis-password-node-b EXISTS "session:{sessionToken}"

# 3. Reiniciar APENAS o Node B (Redis continua rodando)
docker restart irn-node-b

# 4. Aguardar Node B ficar online
docker logs -f irn-node-b

# 5. Verificar se a session ainda existe
docker exec -it irn-redis-node-b redis-cli -a prism-redis-password-node-b EXISTS "session:{sessionToken}"

# Resultado: 1 (existe) - Session sobreviveu ao restart!
# 6. Tentar usar a session (deve funcionar normalmente)
```

**Monitorar criação de session em tempo real:**
```powershell
# Terminal 1: Monitor Redis em tempo real
docker exec -it irn-redis-node-b redis-cli -a prism-redis-password-node-b MONITOR

# Terminal 2: Autenticar e observar comandos SET/HSET/ZADD no Monitor
```

### Passo 4.12: Testar Fluxo Completo com Testes Automatizados

**Executar teste de integração:**
```powershell
dotnet test --filter "FullyQualifiedName~Phase3MutualAuthenticationTests.Authenticate_WithValidChallengeResponse_ReturnsSessionToken"
```

**O que o teste faz:**
1. Estabelece canal (Fase 1)
2. Registra nó (Fase 2)
3. Autoriza nó (admin)
4. Solicita challenge (Fase 3)
5. Assina challenge com chave privada
6. Submete resposta assinada
7. Valida session token recebido

---

## 🧪 Parte 5: Cenários de Teste Adicionais

### Cenário 5.1: Challenge com Nó Não Autorizado (Fase 3)

**Tentar solicitar challenge sem estar autorizado:**
```csharp
// Registrar nó mas NÃO autorizar
var registrationRequest = new NodeRegistrationRequest
{
    NodeId = "unauthorized-node",
    NodeName = "Unauthorized Test Node",
    Certificate = certificate,
    // ... outros campos
};

await _nodeChannelClient.RegisterNodeAsync(channelId, registrationRequest);
// Status: Pending

// Tentar solicitar challenge
try
{
    var challenge = await _nodeChannelClient.RequestChallengeAsync(channelId, "unauthorized-node");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    // Expected: "Challenge request failed: Node status is Pending. Only authorized nodes can authenticate."
}
```

**Resultado esperado:**
```json
{
  "error": {
    "code": "ERR_NODE_NOT_AUTHORIZED",
    "message": "Node status is Pending. Only authorized nodes can authenticate.",
    "retryable": false
  }
}
```

### Cenário 5.2: Assinatura Inválida no Challenge (Fase 3)

**Submeter challenge com assinatura incorreta:**
```csharp
// Solicitar challenge válido
var challengeResponse = await _nodeChannelClient.RequestChallengeAsync(channelId, nodeId);

// Criar resposta com assinatura INVÁLIDA
var invalidRequest = new ChallengeResponseRequest
{
    ChannelId = channelId,
    NodeId = nodeId,
    ChallengeData = challengeResponse.ChallengeData,
    Signature = "INVALID_SIGNATURE_BASE64",
    Timestamp = DateTime.UtcNow
};

try
{
    var result = await _nodeChannelClient.AuthenticateAsync(channelId, invalidRequest);
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    // Expected: "Authentication failed: Challenge response verification failed"
}
```

**Resultado esperado:**
```json
{
  "error": {
    "code": "ERR_INVALID_CHALLENGE_RESPONSE",
    "message": "Challenge response verification failed",
    "retryable": false
  }
}
```

### Cenário 5.3: Challenge Expirado (Fase 3)

**Aguardar expiração do challenge (> 5 minutos):**
```csharp
// Solicitar challenge
var challengeResponse = await _nodeChannelClient.RequestChallengeAsync(channelId, nodeId);

// Aguardar 6 minutos (TTL = 5 min)
await Task.Delay(TimeSpan.FromMinutes(6));

// Tentar usar challenge expirado
var certWithKey = new X509Certificate2(...);
var dataToSign = $"{challengeResponse.ChallengeData}{channelId}{nodeId}{DateTime.UtcNow:O}";
var signature = CertificateHelper.SignData(dataToSign, certWithKey);

var request = new ChallengeResponseRequest
{
    ChannelId = channelId,
    NodeId = nodeId,
    ChallengeData = challengeResponse.ChallengeData,
    Signature = signature,
    Timestamp = DateTime.UtcNow
};

try
{
    var result = await _nodeChannelClient.AuthenticateAsync(channelId, request);
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    // Expected: Challenge expired or not found
}
```

**Debugging:**
- Breakpoint em `ChallengeService.cs:68` (verificação de expiração)
- `challengeData.ExpiresAt < DateTime.UtcNow` será `true`

### Cenário 5.4: Challenge Data Incorreto (Fase 3)

**Submeter challenge data diferente do gerado:**
```csharp
// Solicitar challenge válido
var challengeResponse = await _nodeChannelClient.RequestChallengeAsync(channelId, nodeId);

// Usar OUTRO challenge data (não o gerado)
var wrongChallengeData = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

var certWithKey = new X509Certificate2(...);
var dataToSign = $"{wrongChallengeData}{channelId}{nodeId}{DateTime.UtcNow:O}";
var signature = CertificateHelper.SignData(dataToSign, certWithKey);

var request = new ChallengeResponseRequest
{
    ChannelId = channelId,
    NodeId = nodeId,
    ChallengeData = wrongChallengeData,  // ❌ Errado!
    Signature = signature,
    Timestamp = DateTime.UtcNow
};

try
{
    var result = await _nodeChannelClient.AuthenticateAsync(channelId, request);
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    // Expected: Challenge data mismatch
}
```

**Debugging:**
- Breakpoint em `ChallengeService.cs:74` (verificação de challenge data)
- `request.ChallengeData != challengeData.ChallengeValue` será `true`

### Cenário 5.5: Reutilização de Challenge (One-Time Use)

**Tentar usar mesmo challenge duas vezes:**
```csharp
// Primeira autenticação (sucesso)
var challengeResponse = await _nodeChannelClient.RequestChallengeAsync(channelId, nodeId);
var certWithKey = new X509Certificate2(...);
var dataToSign = $"{challengeResponse.ChallengeData}{channelId}{nodeId}{DateTime.UtcNow:O}";
var signature = CertificateHelper.SignData(dataToSign, certWithKey);

var request = new ChallengeResponseRequest
{
    ChannelId = channelId,
    NodeId = nodeId,
    ChallengeData = challengeResponse.ChallengeData,
    Signature = signature,
    Timestamp = DateTime.UtcNow
};

var firstAuth = await _nodeChannelClient.AuthenticateAsync(channelId, request);
Console.WriteLine($"First auth: {firstAuth.Authenticated}"); // true

// Tentar reutilizar MESMO challenge (deve falhar)
try
{
    var secondAuth = await _nodeChannelClient.AuthenticateAsync(channelId, request);
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    // Expected: Challenge not found (foi invalidado após primeiro uso)
}
```

**💡 Observação:**
- Challenge é invalidado após autenticação bem-sucedida
- Proteção contra replay attacks
- Cada autenticação requer novo challenge

### Cenário 5.6: Nó Desconhecido Tenta se Identificar

**Gerar identidade para nó não registrado:**
```powershell
$unknownIdentityBody = @{
    nodeId = "unknown-node-999"
    nodeName = "Unknown Test Node"
    channelId = $channelId
    validityYears = 1
    password = "test123"
} | ConvertTo-Json

$unknownIdentity = Invoke-RestMethod -Uri "http://localhost:5000/api/testing/generate-node-identity" `
  -Method Post `
  -ContentType "application/json" `
  -Body $unknownIdentityBody

# Tentar identificar
$identifyUnknownBody = $unknownIdentity.identificationRequest | ConvertTo-Json

$unknownResult = Invoke-RestMethod -Uri "http://localhost:5001/api/channel/identify" `
  -Method Post `
  -ContentType "application/json" `
  -Body $identifyUnknownBody

$unknownResult
```

**Resultado esperado:**
```json
{
  "isKnown": false,  // ❌ Nó desconhecido
  "status": 0,  // Unknown
  "nodeId": "unknown-node-999",
  "timestamp": "2025-10-01T...",
  "message": "Node is not registered. Please register using the provided URL.",
  "nextPhase": null,
  "registrationUrl": "http://localhost:5001/api/node/register"  // ✅ URL para registro
}
```

**Debugging:**
- Breakpoint em `ChannelController.cs:267` (verificação de nó conhecido)
- `registeredNode` será `null`
- Sistema retorna URL de registro

### Cenário 4.2: Assinatura Inválida (COM CRIPTOGRAFIA)

**Tentar identificar com assinatura incorreta:**
```csharp
var invalidRequest = new NodeIdentifyRequest
{
    NodeId = "node-a-test-001",
    Certificate = identity.Certificate,
    Signature = "INVALID_SIGNATURE_BASE64",
    SignedData = "some-data"
};

try
{
    var result = await _nodeChannelClient.IdentifyNodeAsync(channelId, invalidRequest);
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    // Expected: "Identification failed: Node signature verification failed"
}
```

**Resultado esperado:**
```json
{
  "error": {
    "code": "ERR_INVALID_SIGNATURE",
    "message": "Node signature verification failed",
    "retryable": false
  }
}
```

**Debugging:**
- Breakpoint em `NodeRegistryService.cs:56` (verificação de assinatura)
- `isValid` será `false`

### Cenário 4.3: Header X-Channel-Id Ausente

**Novo erro (v0.3.0)**: Requisições sem header `X-Channel-Id` são rejeitadas.

```csharp
// Tentar enviar sem header (via HTTP direto, não usando NodeChannelClient)
var httpClient = new HttpClient();
var encryptedPayload = new { encryptedData = "...", iv = "...", authTag = "..." };

var response = await httpClient.PostAsJsonAsync(
    "http://localhost:5001/api/channel/identify",
    encryptedPayload
);
// NO HEADER X-Channel-Id!

// Response: 400 Bad Request
```

**Resultado esperado:**
```json
{
  "error": {
    "code": "ERR_MISSING_CHANNEL_ID",
    "message": "X-Channel-Id header is required",
    "retryable": false
  }
}
```

**Debugging:**
- Breakpoint em `ChannelController.cs:230` (validação de header)

### Cenário 4.4: Canal Inválido ou Expirado

**Tentar identificar com channelId inexistente:**
```csharp
var invalidChannelId = "00000000-0000-0000-0000-000000000000";

try
{
    var result = await _nodeChannelClient.IdentifyNodeAsync(invalidChannelId, identifyRequest);
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    // Expected: "Channel not found or expired"
}
```

**Resultado esperado:**
```json
{
  "error": {
    "code": "ERR_INVALID_CHANNEL",
    "message": "Channel does not exist or has expired",
    "retryable": true
  }
}
```

**Debugging:**
- Breakpoint em `ChannelController.cs:242` (validação de canal)
- `_channelStore.GetChannel(channelId)` retorna `null`

---

## 📊 Parte 5: Análise de Fluxo Completo

### Fluxo Fase 1 + Fase 2

```
┌─────────────┐                                    ┌─────────────┐
│   Node A    │                                    │   Node B    │
│  (Cliente)  │                                    │ (Servidor)  │
└──────┬──────┘                                    └──────┬──────┘
       │                                                  │
       │  1. POST /api/channel/initiate                  │
       │     {remoteNodeUrl: "http://node-b:8080"}       │
       ├────────────────────────────────────────────────>│
       │                                                  │
       │  Gera chave ECDH efêmera (cliente)              │
       │  ┌──────────────────────┐                       │
       │  │ clientEcdh (P-384)   │                       │
       │  │ clientPublicKey      │                       │
       │  └──────────────────────┘                       │
       │                                                  │
       │  2. POST /api/channel/open                      │
       │     {ephemeralPublicKey, supportedCiphers}      │
       ├────────────────────────────────────────────────>│
       │                                                  │
       │                       Gera chave ECDH efêmera (servidor)
       │                       ┌──────────────────────┐  │
       │                       │ serverEcdh (P-384)   │  │
       │                       │ serverPublicKey      │  │
       │                       └──────────────────────┘  │
       │                                                  │
       │                       Deriva sharedSecret        │
       │                       (ECDH entre client e server keys)
       │                       ┌──────────────────────┐  │
       │                       │ sharedSecret (48B)   │  │
       │                       └──────────────────────┘  │
       │                                                  │
       │                       Deriva symmetricKey        │
       │                       (HKDF-SHA256)              │
       │                       ┌──────────────────────┐  │
       │                       │ symmetricKey (32B)   │  │
       │                       └──────────────────────┘  │
       │                                                  │
       │  3. ChannelReadyResponse                        │
       │     {serverPublicKey, selectedCipher, nonce}    │
       │<────────────────────────────────────────────────┤
       │                                                  │
       │  Deriva sharedSecret (cliente)                  │
       │  Deriva symmetricKey (cliente)                  │
       │                                                  │
       │  ✅ Canal Criptografado Estabelecido            │
       │  ChannelId: db7b9540-a1da-44c5-87c9-e78c933e4745│
       │                                                  │
       │  ════════════════════════════════════════════   │
       │           FASE 2: IDENTIFICAÇÃO                 │
       │  ════════════════════════════════════════════   │
       │                                                  │
       │  4. POST /api/testing/generate-node-identity    │
       │     {nodeId, channelId}                         │
       ├─────────┐                                       │
       │         │ Gera certificado X.509                │
       │<────────┘ Assina: channelId+nodeId+timestamp    │
       │                                                  │
       │  5. POST /api/node/register                     │
       │     Headers: X-Channel-Id: abc-123              │
       │     Body: EncryptedPayload {                    │
       │       encryptedData: "...",  ← CRIPTOGRAFADO!   │
       │       iv: "...",                                │
       │       authTag: "..."                            │
       │     }                                            │
       ├────────────────────────────────────────────────>│
       │                                                  │
       │                       Valida X-Channel-Id        │
       │                       Busca symmetricKey         │
       │                       Descriptografa payload     │
       │                                                  │
       │                       Armazena RegisteredNode    │
       │                       Status: Pending            │
       │                       Criptografa resposta       │
       │                                                  │
       │  6. EncryptedPayload (RegistrationResponse)     │
       │     {encryptedData, iv, authTag}                │
       │<────────────────────────────────────────────────┤
       │                                                  │
       │  Descriptografa resposta                        │
       │  {success: true, status: Pending}               │
       │                                                  │
       │  7. POST /api/channel/identify                  │
       │     Headers: X-Channel-Id: abc-123              │
       │     Body: EncryptedPayload {                    │
       │       encryptedData: "...",  ← CRIPTOGRAFADO!   │
       │       iv: "...",                                │
       │       authTag: "..."                            │
       │     }                                            │
       ├────────────────────────────────────────────────>│
       │                                                  │
       │                       Valida X-Channel-Id        │
       │                       Descriptografa payload     │
       │                       Verifica assinatura        │
       │                       Busca nó registrado        │
       │                       Status: Pending            │
       │                       Criptografa resposta       │
       │                                                  │
       │  8. EncryptedPayload (NodeStatusResponse)       │
       │     {encryptedData, iv, authTag}                │
       │<────────────────────────────────────────────────┤
       │                                                  │
       │  Descriptografa resposta                        │
       │  {isKnown: true, status: Pending, nextPhase: null}
       │                                                  │
       │                                                  │
       │         [ADMIN aprova o nó]                     │
       │                                                  │
       │  9. PUT /api/node/{nodeId}/status               │
       │     {status: Authorized}                        │
       ├────────────────────────────────────────────────>│
       │                                                  │
       │                       Atualiza Status: Authorized│
       │                                                  │
       │  10. POST /api/channel/identify (nova tentativa)│
       │      {channelId, certificate, signature}        │
       ├────────────────────────────────────────────────>│
       │                                                  │
       │                       Verifica assinatura        │
       │                       Busca nó registrado        │
       │                       Status: Authorized ✅      │
       │                                                  │
       │  11. NodeStatusResponse                         │
       │      {isKnown: true, status: Authorized,        │
       │       nextPhase: "phase3_authenticate"} ✅      │
       │<────────────────────────────────────────────────┤
       │                                                  │
       │  ✅ Pronto para Fase 3!                         │
       │                                                  │
       │  ════════════════════════════════════════════   │
       │      FASE 3: AUTENTICAÇÃO MÚTUA                │
       │  ════════════════════════════════════════════   │
       │                                                  │
       │  11. POST /api/node/challenge                   │
       │      Headers: X-Channel-Id: abc-123             │
       │      Body: EncryptedPayload {                   │
       │        encryptedData: "...",  ← CRIPTOGRAFADO!  │
       │        iv: "...",                               │
       │        authTag: "..."                           │
       │      }                                           │
       ├────────────────────────────────────────────────>│
       │                                                  │
       │                       Valida X-Channel-Id        │
       │                       Descriptografa payload     │
       │                       Verifica status: Authorized│
       │                                                  │
       │                       Gera challenge (32 bytes)  │
       │                       ┌──────────────────────┐  │
       │                       │ RandomNumberGenerator│  │
       │                       │ 32 bytes aleatórios  │  │
       │                       └──────────────────────┘  │
       │                       TTL: 5 minutos             │
       │                       Armazena: {channelId:nodeId}│
       │                       Criptografa resposta       │
       │                                                  │
       │  12. EncryptedPayload (ChallengeResponse)       │
       │      {encryptedData, iv, authTag}               │
       │<────────────────────────────────────────────────┤
       │                                                  │
       │  Descriptografa resposta                        │
       │  {challengeData: "abc...", ttl: 300}            │
       │                                                  │
       │  Assina challenge com chave privada             │
       │  ┌──────────────────────────────────┐           │
       │  │ dataToSign = challengeData +     │           │
       │  │   channelId + nodeId + timestamp │           │
       │  │ signature = RSA.Sign(dataToSign) │           │
       │  └──────────────────────────────────┘           │
       │                                                  │
       │  13. POST /api/node/authenticate                │
       │      Headers: X-Channel-Id: abc-123             │
       │      Body: EncryptedPayload {                   │
       │        encryptedData: "...",  ← CRIPTOGRAFADO!  │
       │        iv: "...",                               │
       │        authTag: "..."                           │
       │      }                                           │
       │      Contém: {challengeData, signature}         │
       ├────────────────────────────────────────────────>│
       │                                                  │
       │                       Valida X-Channel-Id        │
       │                       Descriptografa payload     │
       │                       Busca challenge armazenado │
       │                       Verifica expiração (< 5min)│
       │                       Verifica challengeData     │
       │                       Verifica assinatura RSA    │
       │                       ┌──────────────────────┐  │
       │                       │ RSA.Verify(signature)│  │
       │                       │ com cert público     │  │
       │                       └──────────────────────┘  │
       │                       ✅ Assinatura válida       │
       │                                                  │
       │                       Gera session token         │
       │                       ┌──────────────────────┐  │
       │                       │ GUID (32 chars)      │  │
       │                       │ TTL: 1 hora          │  │
       │                       └──────────────────────┘  │
       │                       Invalida challenge (one-time)│
       │                       Atualiza lastAuthenticatedAt│
       │                       Criptografa resposta       │
       │                                                  │
       │  14. EncryptedPayload (AuthenticationResponse)  │
       │      {encryptedData, iv, authTag}               │
       │<────────────────────────────────────────────────┤
       │                                                  │
       │  Descriptografa resposta                        │
       │  {authenticated: true,                          │
       │   sessionToken: "abc...",                       │
       │   expiresAt: "2025-10-03T01:00:00Z",           │
       │   grantedCapabilities: ["search", "retrieve"],  │
       │   nextPhase: "phase4_session"}                  │
       │                                                  │
       │  ✅ Autenticado com Session Token               │
       │                                                  │
```

---

## 🎓 Conceitos Aprendidos

### Criptografia

1. **ECDH (Elliptic Curve Diffie-Hellman)**
   - Troca de chaves sem compartilhar segredos
   - Curva P-384 (48 bytes de shared secret)
   - Perfect Forward Secrecy (chaves efêmeras descartadas)

2. **HKDF (HMAC-based Key Derivation Function)**
   - Deriva chave simétrica do shared secret
   - Usa salt (nonces combinados) e info
   - Resultado: AES-256 key (32 bytes)

3. **AES-256-GCM (Advanced Encryption Standard - Galois/Counter Mode)**
   - Criptografia simétrica autenticada
   - 256-bit key size
   - Authenticated Encryption with Associated Data (AEAD)

4. **Certificados X.509**
   - Identificação de nós
   - Chave pública + metadados
   - Auto-assinados para teste

5. **Assinaturas Digitais RSA**
   - Prova de posse da chave privada
   - SHA-256 para hash
   - PKCS#1 padding

6. **Challenge-Response Protocol** (Fase 3)
   - Prova de identidade sem transmitir chave privada
   - Challenge aleatório de 32 bytes
   - One-time use (proteção contra replay)
   - TTL curto (5 minutos)

### Arquitetura

1. **Separation of Concerns**
   - Domain: Modelos
   - Service: Lógica de negócio
   - API: Controllers

2. **Dual Role Pattern**
   - Nós podem ser cliente E servidor
   - Mesmo código, comportamentos diferentes

3. **In-Memory Storage**
   - Adequado para POC/testes
   - Produção requer persistência

---

## ✅ Checklist de Validação

### Fase 1 - Canal Criptografado
- ✅ Canal estabelecido com sucesso
- ✅ Chaves efêmeras geradas (P-384)
- ✅ Shared secret derivado (48 bytes)
- ✅ Symmetric key derivado (32 bytes)
- ✅ Mesmo channelId em ambos os nós
- ✅ Roles corretos (client/server)
- ✅ ChannelStore armazena canal
- ✅ Logs mostram informações esperadas

### Fase 2 - Identificação e Autorização (COM CRIPTOGRAFIA - v0.3.0)
- ✅ Certificado auto-assinado gerado
- ✅ Assinatura RSA-SHA256 criada
- ✅ **Header X-Channel-Id obrigatório**
- ✅ **Payload criptografado com AES-256-GCM**
- ✅ **Descriptografia bem-sucedida no servidor**
- ✅ **Resposta criptografada retornada**
- ✅ Nó desconhecido pode se registrar (com criptografia)
- ✅ Status inicial é Pending
- ✅ Identificação com Pending bloqueia progresso
- [ ] Admin pode aprovar nós
- ✅ Identificação com Authorized permite Fase 3
- ✅ Nó desconhecido recebe URL de registro
- ✅ Assinatura inválida é rejeitada
- ✅ Canal inválido é rejeitado (ERR_INVALID_CHANNEL)
- ✅ **Header ausente é rejeitado (ERR_MISSING_CHANNEL_ID)**
- ✅ **Payload não criptografado é rejeitado (ERR_DECRYPTION_FAILED)**
- ✅ Listagem de nós funciona

### Fase 3 - Autenticação Mútua Challenge/Response (v0.5.0)
- [ ] **Challenge gerado com 32 bytes aleatórios**
- [ ] **Challenge TTL de 5 minutos**
- [ ] **Apenas nós autorizados podem solicitar challenge**
- [ ] **Challenge armazenado com chave {ChannelId}:{NodeId}**
- [ ] **Challenge assinado corretamente: {ChallengeData}{ChannelId}{NodeId}{Timestamp:O}**
- [ ] **Assinatura RSA-2048 verificada com certificado público**
- [ ] **Challenge data deve corresponder exatamente**
- [ ] **Challenge expirado é rejeitado**
- [ ] **Assinatura inválida é rejeitada**
- [ ] **Session token gerado (GUID, 32 caracteres)**
- [ ] **Session TTL de 1 hora**
- [ ] **Capabilities do nó incluídas na resposta**
- [ ] **Challenge invalidado após uso (one-time)**
- [ ] **Reutilização de challenge é bloqueada**
- [ ] **NextPhase retorna "phase4_session"**
- [ ] **Nó não autorizado recebe ERR_NODE_NOT_AUTHORIZED**
- [ ] **LastAuthenticatedAt atualizado no registro do nó**

---

## 🐛 Dicas de Debugging

### Problemas Comuns

**1. Assinatura sempre inválida**
- Verificar formato do timestamp: deve ser ISO 8601 com `ToString("O")`
- Verificar ordem dos campos: `channelId + nodeId + timestamp`
- Verificar que certificado tem chave privada

**2. Canal não encontrado**
- Verificar que channelId é o mesmo
- Verificar expiração (30 minutos)
- Verificar que canal foi criado no nó correto

**3. Certificado inválido**
- Verificar encoding Base64
- Verificar que certificado está completo
- Verificar formato PFX vs CER

### Ferramentas Úteis

**PowerShell:**
```powershell
# Formatar JSON
$response | ConvertTo-Json -Depth 10

# Salvar resposta
$response | ConvertTo-Json -Depth 10 | Out-File "response.json"

# Decodificar Base64
[System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($base64String))
```

**Docker:**
```powershell
# Ver logs em tempo real
docker logs -f irn-node-a

# Inspecionar container
docker exec -it irn-node-a /bin/bash
```

---

## 🔄 Migração para v0.3.0

### Breaking Changes

Se você estava usando a **versão 0.2.0** (sem criptografia de canal):

**ANTES (v0.2.0)**:
```powershell
# Payloads em texto claro
$identifyBody = @{
    channelId = $channelId
    nodeId = "node-a-001"
    certificate = $cert
    signature = $sig
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5001/api/channel/identify" `
  -Method Post `
  -Body $identifyBody
```

**AGORA (v0.3.0+)**:
```csharp
// Usar NodeChannelClient (C#) - payloads criptografados automaticamente
var identifyRequest = new NodeIdentifyRequest
{
    NodeId = "node-a-001",
    Certificate = cert,
    Signature = sig
};

var result = await _nodeChannelClient.IdentifyNodeAsync(channelId, identifyRequest);
```

### Scripts de Teste Atualizados

**Para testes automatizados**, use:
```powershell
# Fase 1 apenas
.\test-docker.ps1

# Fase 2 com criptografia (limitado - requer helpers C#)
.\test-phase2-encrypted.ps1

# Fase 2 COMPLETO (via código C# - RECOMENDADO)
# Criar teste de integração usando NodeChannelClient
```

## 📚 Próximos Passos

Após completar este guia:

1. **Experimentar variações**
   - Diferentes algoritmos de curva
   - Múltiplos canais simultâneos
   - Registro de múltiplos nós
   - Testar expiração de canais (30 min)
   - Testar expiração de challenges (5 min)
   - Testar expiração de session tokens (1 hora)

2. **✅ Fase 3 Completa - Implementar Fase 4**
   - Uso de session tokens em requisições
   - Middleware de validação de token
   - Renovação de tokens
   - Revogação de tokens
   - Capacidades granulares por token

3. **Produtização**
   - Persistência de dados (PostgreSQL/SQL Server)
   - Certificados reais (Let's Encrypt, CA corporativa)
   - Session tokens JWT com claims
   - Rate limiting por nó
   - Auditoria de eventos de autenticação
   - Middleware para validação global de canal e token
   - Distributed cache para challenges e tokens (Redis)

## 📖 Documentação Adicional

- [Implementação de Criptografia de Canal](../development/channel-encryption-implementation.md) - Detalhes da implementação v0.3.0
- [Plano de Criptografia de Canal](../development/channel-encryption-plan.md) - Planejamento completo
- [Protocolo de Handshake](../architecture/handshake-protocol.md) - Especificação atualizada
- [Redis Testing Guide](./redis-testing-guide.md) - **🆕 Guia completo de testes de persistência Redis**
- [Docker Compose Quick Start](./docker-compose-quick-start.md) - Guia rápido Docker Compose com Redis
