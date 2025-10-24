# Phase 2: Node Identification

Phase 2 handles node identification using X.509 certificates and RSA-2048 digital signatures. Nodes present their certificates to identify themselves and determine their authorization status.

## Overview

**Purpose**: Identify connecting nodes and verify authorization status
**Security**: X.509 certificates + RSA-2048 signatures + SHA-256 fingerprints
**Prerequisites**: Completed Phase 1 (encrypted channel)
**Result**: Node registration status (Unknown/Pending/Authorized/Revoked)

## Node States

| Status | Description | Can Proceed to Phase 3 |
|--------|-------------|------------------------|
| `Unknown` | Node not registered | No - Must register first |
| `Pending` | Registration awaiting approval | No - Awaiting admin approval |
| `Authorized` | Node authorized for communication | Yes |
| `Revoked` | Authorization revoked | No - Access denied |

## Endpoints

### POST /api/node/identify

Identifies a node using its X.509 certificate after an encrypted channel is established.

#### Request

**Headers**:
```http
X-Channel-Id: {channelId}
Content-Type: application/json
```

**Body** (encrypted via channel):
```json
{
  "nodeId": "node-a",
  "certificate": "base64-encoded-x509-certificate",
  "signature": "base64-encoded-rsa-signature",
  "timestamp": "2025-10-23T10:00:00Z"
}
```

**Field Descriptions**:
- `nodeId`: Unique identifier for the node (protocol-level)
- `certificate`: X.509 certificate in Base64 format
- `signature`: RSA-2048 signature of `nodeId|timestamp`
- `timestamp`: Current UTC timestamp

#### Response

**Status**: 200 OK

**Body** (encrypted via channel):

**Known and Authorized Node**:
```json
{
  "isKnown": true,
  "status": "Authorized",
  "nodeId": "node-a",
  "registrationId": "f7e6d5c4-b3a2-1098-7654-321098765432",
  "nodeName": "Research Node Alpha",
  "timestamp": "2025-10-23T10:00:01Z",
  "message": "Node is authorized. Proceed to Phase 3 (Mutual Authentication).",
  "nextPhase": "phase3_authenticate"
}
```

**Unknown Node**:
```json
{
  "isKnown": false,
  "status": "Unknown",
  "nodeId": "node-a",
  "timestamp": "2025-10-23T10:00:01Z",
  "registrationUrl": "http://localhost:5000/api/node/register",
  "message": "Node is not registered. Please register using the provided URL.",
  "nextPhase": null
}
```

**Pending Node**:
```json
{
  "isKnown": true,
  "status": "Pending",
  "nodeId": "node-a",
  "registrationId": "f7e6d5c4-b3a2-1098-7654-321098765432",
  "nodeName": "Research Node Alpha",
  "timestamp": "2025-10-23T10:00:01Z",
  "message": "Node registration is pending approval.",
  "nextPhase": null
}
```

**Revoked Node**:
```json
{
  "isKnown": true,
  "status": "Revoked",
  "nodeId": "node-a",
  "registrationId": "f7e6d5c4-b3a2-1098-7654-321098765432",
  "nodeName": "Research Node Alpha",
  "timestamp": "2025-10-23T10:00:01Z",
  "message": "Node authorization has been revoked.",
  "nextPhase": null
}
```

#### Error Responses

**400 Bad Request** - Missing channel
```json
{
  "error": {
    "code": "ERR_NO_CHANNEL",
    "message": "X-Channel-Id header is required",
    "retryable": false
  }
}
```

**400 Bad Request** - Invalid signature
```json
{
  "error": {
    "code": "ERR_INVALID_SIGNATURE",
    "message": "Certificate signature verification failed",
    "retryable": false
  }
}
```

#### curl Example

```bash
# Assuming you have CHANNEL_ID from Phase 1
CHANNEL_ID="a1b2c3d4-e5f6-4789-a1b2-c3d4e5f67890"

# Read certificate
CERTIFICATE=$(cat node-a.crt | base64 -w 0)

# Create signature
TIMESTAMP=$(date -u +%Y-%m-%dT%H:%M:%SZ)
SIGNATURE=$(echo -n "node-a|$TIMESTAMP" | openssl dgst -sha256 -sign node-a.key | base64 -w 0)

# Create request payload
REQUEST='{
  "nodeId": "node-a",
  "certificate": "'$CERTIFICATE'",
  "signature": "'$SIGNATURE'",
  "timestamp": "'$TIMESTAMP'"
}'

# Encrypt payload (simplified - actual encryption via AES-256-GCM)
# In practice, use proper encryption with channel key

curl -X POST http://localhost:5000/api/node/identify \
  -H "X-Channel-Id: $CHANNEL_ID" \
  -H "Content-Type: application/json" \
  -d "$REQUEST"
```

#### C# Client Example

```csharp
public async Task<NodeStatusResponse> IdentifyNodeAsync(
    string channelId,
    byte[] symmetricKey,
    X509Certificate2 certificate,
    RSA privateKey)
{
    var nodeId = "node-a";
    var timestamp = DateTime.UtcNow;

    // Create signature
    var dataToSign = $"{nodeId}|{timestamp:O}";
    var signature = privateKey.SignData(
        Encoding.UTF8.GetBytes(dataToSign),
        HashAlgorithmName.SHA256,
        RSASignaturePadding.Pkcs1
    );

    var request = new NodeIdentifyRequest
    {
        NodeId = nodeId,
        Certificate = Convert.ToBase64String(certificate.RawData),
        Signature = Convert.ToBase64String(signature),
        Timestamp = timestamp
    };

    // Encrypt request
    var encryptedPayload = EncryptPayload(request, symmetricKey);

    var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/node/identify");
    httpRequest.Headers.Add("X-Channel-Id", channelId);
    httpRequest.Content = JsonContent.Create(encryptedPayload);

    var response = await httpClient.SendAsync(httpRequest);

    // Decrypt response
    var encryptedResponse = await response.Content.ReadFromJsonAsync<EncryptedPayload>();
    var decryptedResponse = DecryptPayload<NodeStatusResponse>(encryptedResponse, symmetricKey);

    return decryptedResponse;
}
```

---

### POST /api/node/register

Registers a new node in the system.

#### Request

**Headers**:
```http
X-Channel-Id: {channelId}
Content-Type: application/json
```

**Body** (encrypted via channel):
```json
{
  "nodeId": "node-a",
  "nodeName": "Research Node Alpha",
  "certificate": "base64-encoded-x509-certificate",
  "organizationName": "University Research Lab",
  "contactEmail": "admin@research-lab.edu",
  "nodeAccessLevel": "ReadWrite",
  "nodeUrl": "http://node-a.research-lab.edu:5000"
}
```

**Field Descriptions**:
- `nodeId`: Unique identifier for the node
- `nodeName`: Human-readable name
- `certificate`: X.509 certificate (Base64)
- `organizationName`: Organization operating the node
- `contactEmail`: Administrative contact
- `nodeAccessLevel`: Access level ("ReadOnly", "ReadWrite", "Admin")
- `nodeUrl`: Public URL for this node

#### Response

**Status**: 200 OK

**Body** (encrypted via channel):

**Success**:
```json
{
  "success": true,
  "registrationId": "f7e6d5c4-b3a2-1098-7654-321098765432",
  "status": "Pending",
  "message": "Node registered successfully. Awaiting administrator approval.",
  "certificateFingerprint": "SHA256:abc123def456...",
  "timestamp": "2025-10-23T10:00:00Z"
}
```

**Already Registered**:
```json
{
  "success": false,
  "message": "Node with this certificate is already registered",
  "registrationId": "f7e6d5c4-b3a2-1098-7654-321098765432",
  "status": "Authorized"
}
```

#### curl Example

```bash
# Create registration payload
REQUEST='{
  "nodeId": "node-a",
  "nodeName": "Research Node Alpha",
  "certificate": "'$(cat node-a.crt | base64 -w 0)'",
  "organizationName": "University Research Lab",
  "contactEmail": "admin@research-lab.edu",
  "nodeAccessLevel": "ReadWrite",
  "nodeUrl": "http://localhost:5000"
}'

# Encrypt and send (simplified)
curl -X POST http://localhost:5000/api/node/register \
  -H "X-Channel-Id: $CHANNEL_ID" \
  -H "Content-Type: application/json" \
  -d "$REQUEST"
```

---

### GET /api/node/nodes

Lists all registered nodes (administrative endpoint).

#### Request

**Headers**: None required (admin authentication recommended in production)

#### Response

**Status**: 200 OK

**Body**:
```json
[
  {
    "id": "f7e6d5c4-b3a2-1098-7654-321098765432",
    "nodeId": "node-a",
    "nodeName": "Research Node Alpha",
    "organizationName": "University Research Lab",
    "status": "Authorized",
    "nodeAccessLevel": "ReadWrite",
    "certificateFingerprint": "SHA256:abc123def456...",
    "registeredAt": "2025-10-20T10:00:00Z",
    "lastAuthenticatedAt": "2025-10-23T10:00:00Z"
  },
  {
    "id": "a8b7c6d5-e4f3-2109-8765-432109876543",
    "nodeId": "node-b",
    "nodeName": "Research Node Beta",
    "organizationName": "Medical Research Institute",
    "status": "Pending",
    "nodeAccessLevel": "ReadOnly",
    "certificateFingerprint": "SHA256:def789ghi012...",
    "registeredAt": "2025-10-22T14:30:00Z",
    "lastAuthenticatedAt": null
  }
]
```

#### curl Example

```bash
curl -X GET http://localhost:5000/api/node/nodes
```

---

### PUT /api/node/{id}/status

Updates a node's authorization status (administrative endpoint).

#### Request

**Headers**:
```http
Content-Type: application/json
```

**Parameters**:
- `id` (path): Node registration ID (GUID)

**Body**:
```json
{
  "status": "Authorized"
}
```

**Valid Status Values**:
- `0` or `"Unknown"`
- `1` or `"Pending"`
- `2` or `"Authorized"`
- `3` or `"Revoked"`

#### Response

**Status**: 200 OK

**Body**:
```json
{
  "message": "Node status updated successfully",
  "id": "f7e6d5c4-b3a2-1098-7654-321098765432",
  "status": "Authorized"
}
```

**Status**: 404 Not Found
```json
{
  "message": "Node not found"
}
```

**Status**: 400 Bad Request
```json
{
  "error": {
    "code": "ERR_INVALID_STATUS",
    "message": "Invalid status value: 5",
    "retryable": false
  }
}
```

#### curl Example

```bash
# Authorize a pending node
curl -X PUT http://localhost:5000/api/node/f7e6d5c4-b3a2-1098-7654-321098765432/status \
  -H "Content-Type: application/json" \
  -d '{"status": "Authorized"}'

# Revoke a node
curl -X PUT http://localhost:5000/api/node/f7e6d5c4-b3a2-1098-7654-321098765432/status \
  -H "Content-Type: application/json" \
  -d '{"status": "Revoked"}'
```

---

## Certificate Requirements

X.509 certificates must meet these requirements:

1. **Key Algorithm**: RSA-2048 or higher
2. **Signature Algorithm**: SHA256WithRSA or higher
3. **Subject DN**: Must include CN (Common Name)
4. **Validity**: Not expired
5. **Format**: DER or PEM encoding

### Certificate Fingerprint Calculation

```csharp
// C# Example
using (var sha256 = SHA256.Create())
{
    var certBytes = certificate.RawData;
    var hash = sha256.ComputeHash(certBytes);
    var fingerprint = Convert.ToBase64String(hash);
}
```

```bash
# OpenSSL Example
openssl x509 -in node.crt -outform DER | sha256sum | xxd -r -p | base64
```

## Dual-Identifier Architecture

The system uses three types of identifiers:

1. **NodeId** (string): Protocol-level identifier (e.g., "node-a")
   - Used in handshake messages
   - Human-readable
   - Unique per deployment

2. **RegistrationId** (GUID): Database primary key
   - Used for administrative operations
   - Generated during registration
   - Immutable

3. **Certificate Fingerprint** (SHA-256): Natural key
   - Derived from certificate
   - Used for authentication
   - Changes if certificate renewed

## Security Considerations

1. **Certificate Validation**: Verify certificate chain in production
2. **Signature Verification**: All requests must be signed
3. **Fingerprint Storage**: Store as indexed column for fast lookup
4. **Status Transitions**: Audit all status changes
5. **Revocation**: Immediately deny access to revoked nodes

## Testing

### Complete Identification Flow

```bash
#!/bin/bash
# test-phase2.sh

NODE_A="http://localhost:5000"
CHANNEL_ID="$1"  # Pass channel ID from Phase 1

echo "Testing Phase 2: Node Identification"
echo "====================================="

# 1. Identify as unknown node
echo -e "\n1. Testing unknown node identification..."
# (Create and encrypt identification request)
# Response should show isKnown: false

# 2. Register node
echo -e "\n2. Registering node..."
# (Create and encrypt registration request)
# Response should show success: true, status: Pending

# 3. List all nodes
echo -e "\n3. Listing all nodes..."
curl -s $NODE_A/api/node/nodes | jq '.'

# 4. Update node status to Authorized
echo -e "\n4. Authorizing node..."
NODE_ID=$(curl -s $NODE_A/api/node/nodes | jq -r '.[0].id')
curl -X PUT $NODE_A/api/node/$NODE_ID/status \
  -H "Content-Type: application/json" \
  -d '{"status": "Authorized"}'

# 5. Re-identify as authorized node
echo -e "\n5. Testing authorized node identification..."
# Response should show isKnown: true, status: Authorized

echo -e "\nPhase 2 test complete!"
```

## Common Issues

### Issue: "Node is not registered"
**Solution**: Complete registration process and wait for admin approval

### Issue: "Certificate signature verification failed"
**Solution**: Ensure private key matches certificate and signature algorithm is correct

### Issue: "Node status is Pending"
**Solution**: Administrator must approve the node registration

## Next Steps

After successful identification with `Authorized` status:
1. Note the `registrationId` for reference
2. Proceed to [Phase 3: Mutual Authentication](phase3-authentication.md)
3. Complete challenge-response authentication

---

**Related Documentation**:
- [Phase 1: Channel Establishment](phase1-channel.md)
- [Phase 3: Mutual Authentication](phase3-authentication.md)
- [Node Identifier Architecture](../architecture/NODE_IDENTIFIER_ARCHITECTURE.md)