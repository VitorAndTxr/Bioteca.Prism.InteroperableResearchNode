# Phase 2: Node Identification Flow

**Phase**: 2 of 4
**Purpose**: Identify the remote node using X.509 certificates and establish authorization status
**Security**: X.509 certificates + RSA-2048 signatures + Certificate fingerprint validation

---

## Overview

Phase 2 verifies the identity of the connecting node using X.509 certificates. The certificate fingerprint (SHA-256 hash) serves as the natural key for database lookups, and nodes go through an approval workflow before being granted access.

**Prerequisites**: Phase 1 encrypted channel must be established.

---

## Dual-Identifier System

PRISM uses **three types of identifiers** for nodes:

### 1. NodeId (string) - Protocol-Level Identifier
- Human-readable (e.g., "hospital-research-node-001", "node-a")
- Used in all request/response DTOs for external communication
- Sent in Phase 2 identification and Phase 3 authentication requests
- **NOT stored in database** (protocol-level only)

### 2. RegistrationId / Id (Guid) - Database Primary Key
- Internal unique identifier (e.g., `f6cdb452-17a1-4d8f-9241-0974f80c56ef`)
- Primary key in `research_nodes` table
- Used for all database operations and administrative endpoints
- Returned in `NodeStatusResponse.RegistrationId` after identification

### 3. Certificate Fingerprint (SHA-256) - Natural Key
- SHA-256 hash of the DER-encoded certificate bytes
- 64-character hex string (e.g., `a1b2c3d4...`)
- Used for node lookups during identification and registration
- Enforces uniqueness constraint (unique index in database)
- **True unique identifier** for authentication

**Usage Pattern**:
```
Phase 2 Identification:
  Client sends: NodeId (string) + Certificate
  Server returns: NodeId (string) + RegistrationId (Guid)

Administrative Operations:
  Use: RegistrationId (Guid) for PUT /api/node/{id:guid}/status

Phase 3 Authentication:
  Client sends: NodeId (string)
```

---

## Step-by-Step Flow

### 1. Generate Self-Signed Certificate (Development)

**Client Action** (Node A):
```csharp
// For development only - use real CA certificates in production
var certificate = CertificateHelper.GenerateSelfSignedCertificate("node-a");

// Certificate details:
// - Subject: CN=node-a, O=Research Institution, C=BR
// - Public Key: RSA-2048
// - Signature Algorithm: SHA-256 with RSA
// - Validity: 1 year from creation
```

**Testing Endpoint** (development only):
```http
POST /api/testing/generate-certificate HTTP/1.1
Content-Type: application/json

{
  "nodeId": "node-a",
  "subjectName": "CN=node-a, O=Hospital Research, C=BR"
}

# Response:
{
  "certificate": "MIIDXTCCAkWgAwIBAgIJAKZ...",  // Base64-encoded PEM
  "subjectName": "CN=node-a, O=Hospital Research, C=BR",
  "thumbprint": "a1b2c3d4e5f6...",
  "notBefore": "2025-10-21T10:00:00Z",
  "notAfter": "2026-10-21T10:00:00Z"
}
```

---

### 2. Prepare Identification Request

**Client Action** (Node A):
```csharp
// 1. Load certificate
var cert = new X509Certificate2(certBytes);

// 2. Prepare request data
var requestData = new NodeIdentifyRequest
{
    NodeId = "node-a",  // Protocol-level identifier (string)
    NodeName = "Hospital Research Node A",
    Certificate = Convert.ToBase64String(cert.RawData),
    SubjectName = cert.Subject,
    Timestamp = DateTime.UtcNow.ToString("O"),  // ISO 8601 round-trip format
    Nonce = Convert.ToBase64String(GenerateNonce(16))  // Min 12 bytes
};

// 3. Sign request data with private key
var dataToSign = JsonSerializer.Serialize(requestData);
var signature = SignData(dataToSign, cert.GetRSAPrivateKey());

requestData.Signature = Convert.ToBase64String(signature);
```

**Testing Endpoint** (sign data):
```http
POST /api/testing/sign-data HTTP/1.1
Content-Type: application/json

{
  "certificate": "MIIDXTCCAkWgAwIBAgIJAKZ...",
  "data": "{\"nodeId\":\"node-a\",\"timestamp\":\"2025-10-21T10:30:00Z\"}"
}

# Response:
{
  "signature": "base64-encoded-rsa-signature",
  "algorithm": "SHA256withRSA"
}
```

---

### 3. Encrypt and Send Identification Request

**Client Action** (Node A):
```csharp
// 1. Encrypt request with channel symmetric key (from Phase 1)
var encryptedPayload = _encryptionService.EncryptPayload(requestData, channelSymmetricKey);

// 2. Send to server
var response = await httpClient.PostAsync(
    "https://node-b:5001/api/channel/identify",
    new {
        encryptedData = encryptedPayload.EncryptedData,
        iv = encryptedPayload.Iv,
        authTag = encryptedPayload.AuthTag
    }
);
```

**HTTP Request**:
```http
POST /api/channel/identify HTTP/1.1
Host: node-b:5001
X-Channel-Id: f47ac10b-58cc-4372-a567-0e02b2c3d479
Content-Type: application/json

{
  "encryptedData": "base64-ciphertext",
  "iv": "base64-iv",
  "authTag": "base64-tag"
}
```

---

### 4. Server Processes Identification

**Server Action** (Node B):
```csharp
// ChannelController.Identify()

// 1. [Automatic] PrismEncryptedChannelConnectionAttribute decrypts payload
//    - Validates X-Channel-Id header
//    - Retrieves ChannelContext
//    - Decrypts with AES-256-GCM
//    - Stores in HttpContext.Items["DecryptedRequest"]

var request = HttpContext.Items["DecryptedRequest"] as NodeIdentifyRequest;
var channelContext = HttpContext.Items["ChannelContext"] as ChannelContext;

// 2. Validate timestamp (±5 minutes tolerance)
var timestamp = DateTime.Parse(request.Timestamp);
if (Math.Abs((DateTime.UtcNow - timestamp).TotalMinutes) > 5)
    return BadRequest("Timestamp out of acceptable range");

// 3. Validate nonce (base64, min 12 bytes)
var nonceBytes = Convert.FromBase64String(request.Nonce);
if (nonceBytes.Length < 12)
    return BadRequest("Nonce too short");

// 4. Parse X.509 certificate
var certBytes = Convert.FromBase64String(request.Certificate);
var cert = new X509Certificate2(certBytes);

// 5. Validate certificate
if (cert.NotBefore > DateTime.UtcNow || cert.NotAfter < DateTime.UtcNow)
    return BadRequest("Certificate expired or not yet valid");

// 6. Calculate certificate fingerprint (SHA-256)
using var sha256 = SHA256.Create();
var fingerprint = BitConverter.ToString(sha256.ComputeHash(cert.RawData))
    .Replace("-", "").ToLowerInvariant();

// 7. Verify RSA signature
var dataToVerify = JsonSerializer.Serialize(new {
    request.NodeId,
    request.NodeName,
    request.Certificate,
    request.SubjectName,
    request.Timestamp,
    request.Nonce
    // Note: Signature field excluded from verification data
});
var signatureBytes = Convert.FromBase64String(request.Signature);
var isValid = VerifySignature(dataToVerify, signatureBytes, cert.GetRSAPublicKey());
if (!isValid)
    return Unauthorized("Invalid signature");

// 8. Look up node by certificate fingerprint
var node = await _nodeRepository.GetByCertificateFingerprintAsync(fingerprint);

if (node == null)
{
    // NEW NODE - Create node record with status: Unknown
    node = new ResearchNode
    {
        Id = Guid.NewGuid(),  // RegistrationId
        NodeName = request.NodeName,
        Certificate = request.Certificate,
        CertificateFingerprint = fingerprint,
        Status = AuthorizationStatus.Unknown,
        NodeAccessLevel = NodeAccessTypeEnum.ReadOnly,  // Default
        RegisteredAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
    await _nodeRepository.AddAsync(node);

    // Store IdentifiedNodeId in ChannelContext for subsequent phases
    channelContext.IdentifiedNodeId = node.Id;
    await _channelStore.UpdateChannelAsync(channelContext);

    // Return response: not authorized yet
    var response = new NodeStatusResponse
    {
        IsKnown = false,
        RegistrationId = node.Id,  // Return Guid
        NodeId = request.NodeId,   // Echo back string NodeId
        Status = AuthorizationStatus.Unknown,
        Message = "Node identified but not registered. Please submit registration request.",
        RegistrationUrl = "/api/node/register",
        NextPhase = null
    };
    var encrypted = _encryptionService.EncryptPayload(response, channelContext.SymmetricKey);
    return Ok(encrypted);
}
else if (node.Status == AuthorizationStatus.Pending)
{
    // KNOWN NODE - Pending approval
    channelContext.IdentifiedNodeId = node.Id;
    await _channelStore.UpdateChannelAsync(channelContext);

    var response = new NodeStatusResponse
    {
        IsKnown = true,
        RegistrationId = node.Id,
        NodeId = request.NodeId,
        Status = AuthorizationStatus.Pending,
        Message = "Registration request pending approval.",
        NextPhase = null
    };
    var encrypted = _encryptionService.EncryptPayload(response, channelContext.SymmetricKey);
    return Ok(encrypted);
}
else if (node.Status == AuthorizationStatus.Authorized)
{
    // AUTHORIZED NODE - Can proceed to Phase 3
    channelContext.IdentifiedNodeId = node.Id;
    await _channelStore.UpdateChannelAsync(channelContext);

    var response = new NodeStatusResponse
    {
        IsKnown = true,
        RegistrationId = node.Id,
        NodeId = request.NodeId,
        Status = AuthorizationStatus.Authorized,
        Message = "Node authorized. Proceed to authentication.",
        NextPhase = "phase3_authenticate"
    };
    var encrypted = _encryptionService.EncryptPayload(response, channelContext.SymmetricKey);
    return Ok(encrypted);
}
else  // Revoked
{
    var response = new NodeStatusResponse
    {
        IsKnown = true,
        RegistrationId = node.Id,
        NodeId = request.NodeId,
        Status = AuthorizationStatus.Revoked,
        Message = "Node access has been revoked.",
        NextPhase = null
    };
    var encrypted = _encryptionService.EncryptPayload(response, channelContext.SymmetricKey);
    return Unauthorized(encrypted);
}
```

**HTTP Response** (encrypted):
```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "encryptedData": "base64-ciphertext",
  "iv": "base64-iv",
  "authTag": "base64-tag"
}

# Decrypted payload:
{
  "isKnown": true,
  "registrationId": "f6cdb452-17a1-4d8f-9241-0974f80c56ef",  // Guid
  "nodeId": "node-a",  // String
  "status": 2,  // AuthorizationStatus.Authorized
  "message": "Node authorized. Proceed to authentication.",
  "nextPhase": "phase3_authenticate"
}
```

---

## Node Registration Workflow

### Registration Request (if Unknown)

**Client Action**:
```http
POST /api/node/register HTTP/1.1
X-Channel-Id: f47ac10b-58cc-4372-a567-0e02b2c3d479
Content-Type: application/json

{
  "encryptedData": "...",
  "iv": "...",
  "authTag": "..."
}

# Decrypted payload:
{
  "nodeId": "node-a",
  "nodeName": "Hospital Research Node A",
  "nodeUrl": "https://node-a.hospital.com:5000",
  "certificate": "MIIDXTCCAkWgAwIBAgIJAKZ...",
  "contactInfo": "admin@hospital.com",
  "institutionDetails": {
    "name": "University Hospital",
    "country": "Brazil",
    "city": "São Paulo"
  },
  "requestedAccessLevel": 2  // NodeAccessTypeEnum.ReadWrite
}
```

**Server Action**:
```csharp
// NodeConnectionController.Register()

// 1. Calculate certificate fingerprint
var fingerprint = CalculateFingerprint(request.Certificate);

// 2. Look up existing node
var node = await _nodeRepository.GetByCertificateFingerprintAsync(fingerprint);

if (node == null)
{
    // Create new node with Pending status
    node = new ResearchNode
    {
        Id = Guid.NewGuid(),
        NodeName = request.NodeName,
        Certificate = request.Certificate,
        CertificateFingerprint = fingerprint,
        NodeUrl = request.NodeUrl,
        Status = AuthorizationStatus.Pending,
        NodeAccessLevel = request.RequestedAccessLevel,
        ContactInfo = request.ContactInfo,
        InstitutionDetails = JsonSerializer.Serialize(request.InstitutionDetails),
        RegisteredAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };
    await _nodeRepository.AddAsync(node);
}
else if (node.Status == AuthorizationStatus.Unknown)
{
    // Update existing Unknown node to Pending
    node.NodeName = request.NodeName;
    node.NodeUrl = request.NodeUrl;
    node.ContactInfo = request.ContactInfo;
    node.InstitutionDetails = JsonSerializer.Serialize(request.InstitutionDetails);
    node.Status = AuthorizationStatus.Pending;
    node.NodeAccessLevel = request.RequestedAccessLevel;
    node.UpdatedAt = DateTime.UtcNow;
    await _nodeRepository.UpdateAsync(node);
}

var response = new {
    registrationId = node.Id,
    status = "Pending",
    message = "Registration request submitted. Awaiting administrator approval."
};
var encrypted = _encryptionService.EncryptPayload(response, channelContext.SymmetricKey);
return Ok(encrypted);
```

---

### Administrative Approval (Manual)

**Administrator Action**:
```http
PUT /api/node/f6cdb452-17a1-4d8f-9241-0974f80c56ef/status HTTP/1.1
Authorization: Bearer {admin-token}
Content-Type: application/json

{
  "status": 2,  // AuthorizationStatus.Authorized
  "accessLevel": 2  // NodeAccessTypeEnum.ReadWrite
}

# Response:
{
  "id": "f6cdb452-17a1-4d8f-9241-0974f80c56ef",
  "nodeName": "Hospital Research Node A",
  "status": "Authorized",
  "accessLevel": "ReadWrite",
  "updatedAt": "2025-10-21T10:45:00Z"
}
```

**Database Schema**:
```sql
-- research_nodes table
CREATE TABLE research_nodes (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),  -- RegistrationId
    node_name text NOT NULL,
    certificate text NOT NULL,
    certificate_fingerprint text NOT NULL UNIQUE,  -- Natural key
    node_url text,
    status integer NOT NULL,  -- AuthorizationStatus enum (0=Unknown, 1=Pending, 2=Authorized, 3=Revoked)
    node_access_level integer NOT NULL,  -- NodeAccessTypeEnum (0=ReadOnly, 1=ReadWrite, 2=Admin)
    contact_info text,
    institution_details jsonb,
    registered_at timestamptz NOT NULL,
    updated_at timestamptz NOT NULL,
    last_authenticated_at timestamptz
);

-- Unique index on certificate fingerprint
CREATE UNIQUE INDEX idx_nodes_cert_fingerprint ON research_nodes(certificate_fingerprint);
```

---

## Re-Registration Logic

**Scenario**: Node re-identifies with same certificate but different NodeId.

**Server Behavior**:
```csharp
// If certificate fingerprint matches existing node:
var existingNode = await _nodeRepository.GetByCertificateFingerprintAsync(fingerprint);
if (existingNode != null)
{
    // UPDATE existing node record
    existingNode.NodeName = request.NodeName;  // Update to new name
    existingNode.UpdatedAt = DateTime.UtcNow;
    await _nodeRepository.UpdateAsync(existingNode);

    // Return existing RegistrationId (Guid)
    return existingNode.Id;
}
```

**Rationale**: Certificate fingerprint is the source of truth, not NodeId (which can change).

---

## Validation Rules

### Certificate Validation
✅ Base64 decodable
✅ Valid X.509 structure
✅ Not expired (`NotBefore ≤ Now ≤ NotAfter`)
✅ Contains SubjectName
✅ RSA public key present

### Timestamp Validation
✅ ISO 8601 format (`DateTime.Parse` succeeds)
✅ Within ±5 minutes of server time
✅ Purpose: Prevent replay attacks

### Nonce Validation
✅ Base64 format
✅ Minimum 12 bytes (16 characters base64)
✅ Purpose: Ensure request uniqueness

### Required Fields
✅ NodeId (string, non-empty)
✅ NodeName (string, non-empty)
✅ SubjectName (string, non-empty)
✅ Certificate (base64, valid X.509)

---

## Testing

### Testing Endpoint (Combined Phase 1 + Phase 2)

```http
POST /api/testing/complete-phase1-phase2 HTTP/1.1
Content-Type: application/json

{
  "nodeId": "node-a",
  "nodeName": "Hospital Research Node A",
  "certificate": "MIIDXTCCAkWgAwIBAgIJAKZ..."
}

# Response:
{
  "phase1": {
    "channelId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
    "symmetricKey": "base64-key"
  },
  "phase2": {
    "isKnown": true,
    "registrationId": "f6cdb452-17a1-4d8f-9241-0974f80c56ef",
    "nodeId": "node-a",
    "status": "Authorized",
    "nextPhase": "phase3_authenticate"
  }
}
```

---

### Automated Tests

**Phase 2 Tests** (6/6 passing):
```csharp
[Fact] public async Task Identify_NewNode_ShouldReturnUnknownStatus()
[Fact] public async Task Identify_PendingNode_ShouldReturnPendingStatus()
[Fact] public async Task Identify_AuthorizedNode_ShouldAllowPhase3()
[Fact] public async Task Identify_RevokedNode_ShouldRejectAccess()
[Fact] public async Task Register_UnknownNode_ShouldCreatePendingNode()
[Fact] public async Task ReIdentify_SameCertificate_ShouldUpdateNode()
```

---

## Common Issues

### Issue: "Invalid signature"
**Cause**: Signing data format mismatch or incorrect private key

**Solution**:
1. Ensure signature excludes the `Signature` field itself
2. Use exact same JSON serialization on both sides
3. Verify RSA-2048 algorithm with SHA-256

---

### Issue: "Certificate fingerprint already exists"
**Cause**: Attempting to create new node with existing certificate

**Solution**: This triggers re-registration logic (update existing node)

---

### Issue: "Timestamp out of range"
**Cause**: Client/server clocks not synchronized

**Solution**:
1. Synchronize clocks with NTP
2. Ensure both use UTC time
3. Check tolerance window (±5 minutes)

---

## Next Phase

Once Phase 2 is complete (node identified and authorized), proceed to:

**Phase 3: Mutual Authentication** (`docs/workflows/PHASE3_AUTHENTICATION_FLOW.md`)

---

## Documentation References

- **Security Details**: `docs/SECURITY_OVERVIEW.md`
- **Node Identifiers**: `docs/architecture/NODE_IDENTIFIER_ARCHITECTURE.md`
- **Handshake Protocol**: `docs/architecture/handshake-protocol.md`
- **Manual Testing**: `docs/testing/manual-testing-guide.md`
