# Guia de Testes Manuais e Discovery - Fases 1 e 2

**Versão**: 0.3.0 (com criptografia de canal)
**Última atualização**: 2025-10-02

Este guia fornece um roteiro passo a passo para testar e entender manualmente o funcionamento das Fases 1 e 2 do protocolo de handshake, ideal para debugging e aprendizado.

## ⚠️ IMPORTANTE: Criptografia de Canal

**A partir da versão 0.3.0**, todas as comunicações após o estabelecimento do canal (Fase 1) **DEVEM ser criptografadas** usando a chave simétrica derivada do canal.

- ✅ **Fase 1** (`/api/channel/open`, `/api/channel/initiate`) - Sem criptografia (estabelece o canal)
- 🔒 **Fase 2** (`/api/channel/identify`, `/api/node/register`) - **Payload criptografado obrigatório**
- 🔒 **Fases 3-4** - **Payload criptografado obrigatório**

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
4. **Conhecimento básico de**:
   - Criptografia assimétrica (ECDH, RSA)
   - Certificados X.509
   - REST APIs

## 🎯 Objetivo

Entender o fluxo completo de:
1. **Fase 1**: Estabelecimento de canal criptografado com chaves efêmeras
2. **Fase 2**: Identificação e autorização de nós com certificados

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

### Passo 1.3: Acessar Swagger UI

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

## 🧪 Parte 4: Cenários de Teste Adicionais

### Cenário 4.1: Nó Desconhecido Tenta se Identificar

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

3. **Certificados X.509**
   - Identificação de nós
   - Chave pública + metadados
   - Auto-assinados para teste

4. **Assinaturas Digitais RSA**
   - Prova de posse da chave privada
   - SHA-256 para hash
   - PKCS#1 padding

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

### Fase 1
- [ ] Canal estabelecido com sucesso
- [ ] Chaves efêmeras geradas (P-384)
- [ ] Shared secret derivado (48 bytes)
- [ ] Symmetric key derivado (32 bytes)
- [ ] Mesmo channelId em ambos os nós
- [ ] Roles corretos (client/server)
- [ ] ChannelStore armazena canal
- [ ] Logs mostram informações esperadas

### Fase 2 (COM CRIPTOGRAFIA - v0.3.0)
- [ ] Certificado auto-assinado gerado
- [ ] Assinatura RSA-SHA256 criada
- [ ] **Header X-Channel-Id obrigatório**
- [ ] **Payload criptografado com AES-256-GCM**
- [ ] **Descriptografia bem-sucedida no servidor**
- [ ] **Resposta criptografada retornada**
- [ ] Nó desconhecido pode se registrar (com criptografia)
- [ ] Status inicial é Pending
- [ ] Identificação com Pending bloqueia progresso
- [ ] Admin pode aprovar nós
- [ ] Identificação com Authorized permite Fase 3
- [ ] Nó desconhecido recebe URL de registro
- [ ] Assinatura inválida é rejeitada
- [ ] Canal inválido é rejeitado (ERR_INVALID_CHANNEL)
- [ ] **Header ausente é rejeitado (ERR_MISSING_CHANNEL_ID)**
- [ ] **Payload não criptografado é rejeitado (ERR_DECRYPTION_FAILED)**
- [ ] Listagem de nós funciona

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

2. **Implementar Fase 3**
   - Autenticação mútua COM criptografia
   - Desafio/resposta
   - Proteção contra replay attacks

3. **Produtização**
   - Persistência de dados
   - Certificados reais (Let's Encrypt, etc.)
   - Rate limiting
   - Auditoria de eventos
   - Middleware para validação global de canal

## 📖 Documentação Adicional

- [Implementação de Criptografia de Canal](../development/channel-encryption-implementation.md) - Detalhes da implementação v0.3.0
- [Plano de Criptografia de Canal](../development/channel-encryption-plan.md) - Planejamento completo
- [Protocolo de Handshake](../architecture/handshake-protocol.md) - Especificação atualizada
