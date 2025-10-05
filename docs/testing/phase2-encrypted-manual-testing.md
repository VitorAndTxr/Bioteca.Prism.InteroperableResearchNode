# 🔒 Manual Testing Guide - Phase 2 with Channel Encryption

**Version**: 1.0.0
**Date**: 2025-10-02
**Objective**: Validate node identification and registration using encrypted channels

---

## 📋 Prerequisites

- Docker Desktop running
- Node A and Node B containers active
- Swagger UI available at:
  - Node A: http://localhost:5000/swagger
  - Node B: http://localhost:5001/swagger

---

## ✅ Initial Verification

### 1. Check containers

```powershell
docker ps
```

**Expected**:
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

**Expected**: `{"status":"healthy","timestamp":"..."}`

---

## 🚀 Complete Test Flow (Via Swagger)

### **STEP 1: Establish Encrypted Channel**

**Node A → Node B**

1. Open **Node A** Swagger: http://localhost:5000/swagger
2. Locate endpoint: `POST /api/channel/initiate`
3. Click **Try it out**
4. Paste the following JSON:

```json
{
  "remoteNodeUrl": "http://node-b:8080"
}
```

5. Click **Execute**

**Expected result**:

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

**⚠️ IMPORTANT**: Copy the `channelId` - you'll use it in all subsequent steps!

---

### **STEP 2: Generate Certificate for Node A**

**On Node A (localhost:5000)**

1. Locate endpoint: `POST /api/testing/generate-certificate`
2. Click **Try it out**
3. Paste the following JSON:

```json
{
  "subjectName": "node-a-test-001",
  "validityYears": 2,
  "password": "string"
}
```

4. Click **Execute**

**Expected result**:

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

**⚠️ IMPORTANT**: Copy the entire `certificate`!

---

### **STEP 3: Sign Identification Data**

**On Node A (localhost:5000)**

1. Locate endpoint: `POST /api/testing/sign-data`
2. Click **Try it out**
3. Paste the following JSON (replace `PASTE_CHANNEL_ID_HERE` and `PASTE_CERTIFICATE_HERE`):

```json
{
  "certificate": "PASTE_CERTIFICATE_HERE",
  "data": "PASTE_CHANNEL_ID_HEREnode-a-test-001",
  "nodeId": "node-a-test-001"
}
```

**Example with real values**:
```json
{
  "certificate": "MIIDXTCCAkWgAwIBAgIQZJ...",
  "data": "a1b2c3d4-e5f6-7890-abcd-ef1234567890node-a-test-001",
  "nodeId": "node-a-test-001"
}
```

4. Click **Execute**

**Expected result**:

```json
{
  "signature": "SGVsbG8gV29ybGQhCg==...",  // Base64 signature
  "algorithm": "SHA256withRSA",
  "timestamp": "2025-10-02T10:31:00Z"
}
```

**⚠️ IMPORTANT**: Copy the `signature`!

---

### **STEP 4: Create Encrypted Identification Payload**

**On Node A (localhost:5000)**

1. Locate endpoint: `POST /api/testing/encrypt-payload`
2. Click **Try it out**
3. Paste the following JSON (replace with copied values):

```json
{
  "channelId": "PASTE_CHANNEL_ID_HERE",
  "payload": {
    "nodeId": "node-a-test-001",
    "certificate": "PASTE_CERTIFICATE_HERE",
    "signature": "PASTE_SIGNATURE_HERE"
  }
}
```

**Example with real values**:
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

4. Click **Execute**

**Expected result**:

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

**⚠️ IMPORTANT**: Copy the entire `encryptedPayload` object (all 3 properties: `encryptedData`, `iv`, `authTag`)!

---

### **STEP 5: Identify Node A on Node B (First Attempt - Unknown)**

**Now go to Node B**: http://localhost:5001/swagger

1. Locate endpoint: `POST /api/node/identify`
2. Click **Try it out**
3. **ADD HEADER**: Click "Add string item" in **Parameters > X-Channel-Id**
   - Paste the `channelId` you copied in STEP 1
4. Paste the `encryptedPayload` you copied in STEP 4:

```json
{
  "encryptedData": "PASTE_HERE",
  "iv": "PASTE_HERE",
  "authTag": "PASTE_HERE"
}
```

5. Click **Execute**

**Expected result** (Node A not yet registered):

```json
{
  "encryptedData": "...",
  "iv": "...",
  "authTag": "..."
}
```

**But the decrypted content would be**:
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

### **STEP 6: Decrypt Response (Optional - To See Result)**

**Go back to Node A**: http://localhost:5000/swagger

1. Locate endpoint: `POST /api/testing/decrypt-payload`
2. Click **Try it out**
3. Paste:

```json
{
  "channelId": "PASTE_CHANNEL_ID_HERE",
  "encryptedPayload": {
    "encryptedData": "PASTE_RESPONSE_FROM_STEP_5",
    "iv": "PASTE_HERE",
    "authTag": "PASTE_HERE"
  }
}
```

4. Click **Execute**

**Result**: You'll see the decrypted response!

---

### **STEP 7: Register Node A on Node B**

**Create encrypted registration payload**

**On Node A (localhost:5000)**:

1. Locate endpoint: `POST /api/testing/encrypt-payload`
2. Paste:

```json
{
  "channelId": "PASTE_CHANNEL_ID_HERE",
  "payload": {
    "nodeId": "node-a-test-001",
    "nodeName": "Node A - Test Instance",
    "certificate": "PASTE_CERTIFICATE_HERE",
    "contactInfo": "admin@node-a.test",
    "institutionDetails": "Test Institution A",
    "nodeUrl": "http://node-a:8080",
    "requestedCapabilities": ["search", "retrieve"]
  }
}
```

3. Copy the returned `encryptedPayload`

**Register on Node B (localhost:5001)**:

1. Locate endpoint: `POST /api/node/register`
2. **ADD HEADER X-Channel-Id**: Paste the `channelId`
3. Paste the `encryptedPayload` you just created
4. Click **Execute**

**Expected result** (decrypted):

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

### **STEP 8: Approve Node A (Administrator Action)**

**On Node B (localhost:5001)**:

1. Locate endpoint: `PUT /api/node/{nodeId}/status`
2. Paste `node-a-test-001` in `nodeId`
3. Paste in body:

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

4. Click **Execute**

**Expected result**:

```json
{
  "message": "Node status updated successfully",
  "nodeId": "node-a-test-001",
  "status": 1
}
```

---

### **STEP 9: Identify Node A Again (Now Authorized)**

**Repeat STEP 5** (same encrypted request)

**Expected result** (decrypted with STEP 6):

```json
{
  "isKnown": true,
  "status": 1,  // Authorized ✅
  "nodeId": "node-a-test-001",
  "nodeName": "Node A - Test Instance",
  "timestamp": "2025-10-02T10:35:00Z",
  "message": "Node is authorized. Proceed to Phase 3 (Mutual Authentication).",
  "nextPhase": "phase3_authenticate",  // ✅ Can proceed!
  "registrationUrl": null
}
```

---

## ✅ Validation Checklist

After executing all steps, you should have validated:

- [x] Encrypted channel established between Node A and Node B
- [x] X.509 certificate generated for Node A
- [x] Data signed with certificate private key
- [x] Payload encrypted with AES-256-GCM
- [x] Unknown node identification returns `isKnown: false`
- [x] Node registration with encrypted payload
- [x] Node status updated to `Authorized`
- [x] Authorized node identification returns `nextPhase: "phase3_authenticate"`
- [x] All payloads transmitted encrypted
- [x] `X-Channel-Id` header required in all Phase 2+ requests

---

## 🐛 Troubleshooting

### Error: "Channel does not exist or has expired"

**Cause**: Channel expired (30 minutes) or invalid `channelId`

**Solution**: Repeat STEP 1 to create a new channel

---

### Error: "Failed to decrypt request payload"

**Cause**:
- Wrong `channelId` in header
- Payload encrypted with different channel
- Invalid JSON format

**Solution**:
1. Verify that the `channelId` in the header matches the one used for encryption
2. Recreate the encrypted payload (STEP 4 or 7)
3. Make sure to copy the entire `encryptedPayload` object (3 properties)

---

### Error: "Node signature verification failed"

**Cause**: Invalid signature or incorrect data

**Solution**:
1. Ensure that `data` in STEP 3 follows the format: `{channelId}{nodeId}`
2. Use the same certificate in STEPs 2, 3, 4, and 7
3. Generate a new signature (STEP 3)

---

### Response 400 instead of 200

**Cause**: Payload not in expected format

**Solution**:
1. Check container logs:
   ```powershell
   docker logs irn-node-b --tail 50
   ```
2. Look for JSON deserialization errors
3. Ensure encrypted payload has 3 properties: `encryptedData`, `iv`, `authTag`

---

## 📚 References

- [Test Endpoints Documentation](../development/testing-endpoints-criptografia.md)
- [Encryption Implementation](../development/channel-encryption-implementation.md)
- [Handshake Architecture](../architecture/handshake-protocol.md)

---

## 🎯 Next Steps

After validating Phase 2, you can:

1. **Test automation**: Use the `test-phase2-full.ps1` script
2. **Implement Phase 3**: Mutual authentication with challenge/response
3. **Explore error scenarios**: Test revoked nodes, expired certificates, etc.
