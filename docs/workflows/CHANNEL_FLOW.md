# Phase 1: Channel Flow (Encrypted Channel Establishment)

**Phase**: 1 of 4
**Purpose**: Establish a secure, encrypted communication channel using ephemeral key exchange
**Security**: ECDH P-384 + AES-256-GCM + Perfect Forward Secrecy

---

## Overview

Phase 1 creates a temporary encrypted channel between two nodes before any sensitive information is exchanged. This channel uses ephemeral keys, ensuring Perfect Forward Secrecy - even if long-term certificates are compromised, past communications remain secure.

---

## Step-by-Step Flow

### 1. Client Initiates Channel Opening

**Client Action** (Node A):
```csharp
// Generate ephemeral ECDH key pair (P-384 curve)
var ecdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP384);
var clientPublicKey = ecdh.PublicKey.ExportSubjectPublicKeyInfo();

// Send to server
var request = new { publicKey = Convert.ToBase64String(clientPublicKey) };
var response = await httpClient.PostAsync("https://node-b:5001/api/channel/open", request);
```

**HTTP Request**:
```http
POST /api/channel/open HTTP/1.1
Host: node-b:5001
Content-Type: application/json

{
  "publicKey": "MHYwEAYHKoZIzj0CAQYFK4EEACIDYgAE..."
}
```

---

### 2. Server Processes Request

**Server Action** (Node B):
```csharp
// ChannelController.OpenChannel()

// 1. Decode client's public key
var clientPublicKeyBytes = Convert.FromBase64String(request.PublicKey);

// 2. Generate server ephemeral ECDH key pair
var serverEcdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP384);

// 3. Import client's public key
var clientPublicKey = ECDiffieHellman.Create();
clientPublicKey.ImportSubjectPublicKeyInfo(clientPublicKeyBytes, out _);

// 4. Derive shared secret using ECDH
var sharedSecret = serverEcdh.DeriveKeyFromHash(
    clientPublicKey.PublicKey,
    HashAlgorithmName.SHA256
);

// 5. Derive symmetric key using HKDF-SHA256
var symmetricKey = HKDF.DeriveKey(
    HashAlgorithmName.SHA256,
    sharedSecret,
    keyLength: 32,  // 256 bits for AES-256
    salt: null,
    info: Encoding.UTF8.GetBytes("PRISM-Channel-Key-v1")
);

// 6. Generate channel ID
var channelId = Guid.NewGuid().ToString();

// 7. Store channel context
var channelContext = new ChannelContext
{
    ChannelId = channelId,
    SymmetricKey = symmetricKey,
    CreatedAt = DateTime.UtcNow,
    ExpiresAt = DateTime.UtcNow.AddMinutes(30)
};
await _channelStore.StoreChannelAsync(channelContext);

// 8. Return server's public key + channel ID
var serverPublicKey = serverEcdh.PublicKey.ExportSubjectPublicKeyInfo();
Response.Headers.Add("X-Channel-Id", channelId);
return Ok(new { publicKey = Convert.ToBase64String(serverPublicKey) });
```

**HTTP Response**:
```http
HTTP/1.1 200 OK
X-Channel-Id: f47ac10b-58cc-4372-a567-0e02b2c3d479
Content-Type: application/json

{
  "publicKey": "MHYwEAYHKoZIzj0CAQYFK4EEACIDYgAE..."
}
```

---

### 3. Client Derives Symmetric Key

**Client Action** (Node A):
```csharp
// 1. Extract channel ID from response header
var channelId = response.Headers.GetValues("X-Channel-Id").FirstOrDefault();

// 2. Decode server's public key
var serverPublicKeyBytes = Convert.FromBase64String(responseBody.PublicKey);

// 3. Import server's public key
var serverPublicKey = ECDiffieHellman.Create();
serverPublicKey.ImportSubjectPublicKeyInfo(serverPublicKeyBytes, out _);

// 4. Derive shared secret (same as server)
var sharedSecret = clientEcdh.DeriveKeyFromHash(
    serverPublicKey.PublicKey,
    HashAlgorithmName.SHA256
);

// 5. Derive symmetric key (same as server)
var symmetricKey = HKDF.DeriveKey(
    HashAlgorithmName.SHA256,
    sharedSecret,
    keyLength: 32,
    salt: null,
    info: Encoding.UTF8.GetBytes("PRISM-Channel-Key-v1")
);

// 6. Store channel context locally
var channelContext = new ChannelContext
{
    ChannelId = channelId,
    SymmetricKey = symmetricKey,
    CreatedAt = DateTime.UtcNow,
    ExpiresAt = DateTime.UtcNow.AddMinutes(30)
};
```

---

### 4. Verify Channel with Test Encryption

**Client Test** (Optional, recommended):
```csharp
// Encrypt a test message
var testMessage = new { test = "Hello, encrypted world!" };
var encrypted = _encryptionService.EncryptPayload(testMessage, symmetricKey);

// Send encrypted test
var testResponse = await httpClient.PostAsync(
    "https://node-b:5001/api/testing/decrypt-test",
    new { channelId, encryptedData = encrypted.EncryptedData, iv = encrypted.Iv, authTag = encrypted.AuthTag }
);

// Server decrypts and returns plaintext
// If successful, channel is verified
```

---

## Channel Storage

### In-Memory Storage (Default)

**Implementation**:
```csharp
// Bioteca.Prism.Data/Cache/Channel/ChannelStore.cs
private readonly ConcurrentDictionary<string, ChannelContext> _channels = new();

public async Task StoreChannelAsync(ChannelContext context)
{
    _channels[context.ChannelId] = context;

    // Schedule automatic cleanup after TTL
    _ = Task.Run(async () =>
    {
        await Task.Delay(TimeSpan.FromMinutes(30));
        _channels.TryRemove(context.ChannelId, out _);
    });
}

public async Task<ChannelContext?> GetChannelAsync(string channelId)
{
    if (_channels.TryGetValue(channelId, out var context))
    {
        if (context.ExpiresAt > DateTime.UtcNow)
            return context;

        // Expired, remove
        _channels.TryRemove(channelId, out _);
    }
    return null;
}
```

**Limitations**:
- Lost on node restart
- Not suitable for load-balanced deployments
- Manual expiration check required

---

### Redis Storage (Production)

**Implementation**:
```csharp
// Bioteca.Prism.Service/Services/Cache/RedisChannelStore.cs
public async Task StoreChannelAsync(ChannelContext context)
{
    var db = _redis.GetDatabase();

    // Store metadata as JSON
    var metadataKey = $"channel:{context.ChannelId}";
    var metadata = new {
        context.ChannelId,
        context.CreatedAt,
        context.ExpiresAt,
        context.IdentifiedNodeId
    };
    await db.StringSetAsync(metadataKey, JsonSerializer.Serialize(metadata), TimeSpan.FromMinutes(30));

    // Store symmetric key separately (binary data)
    var keyKey = $"channel:key:{context.ChannelId}";
    await db.StringSetAsync(keyKey, context.SymmetricKey, TimeSpan.FromMinutes(30));
}

public async Task<ChannelContext?> GetChannelAsync(string channelId)
{
    var db = _redis.GetDatabase();

    // Retrieve metadata
    var metadataKey = $"channel:{channelId}";
    var metadataJson = await db.StringGetAsync(metadataKey);
    if (!metadataJson.HasValue) return null;

    // Retrieve symmetric key
    var keyKey = $"channel:key:{channelId}";
    var symmetricKey = await db.StringGetAsync(keyKey);
    if (!symmetricKey.HasValue) return null;

    // Deserialize and reconstruct
    var metadata = JsonSerializer.Deserialize<ChannelMetadata>((string)metadataJson);
    return new ChannelContext
    {
        ChannelId = metadata.ChannelId,
        SymmetricKey = (byte[])symmetricKey,
        CreatedAt = metadata.CreatedAt,
        ExpiresAt = metadata.ExpiresAt,
        IdentifiedNodeId = metadata.IdentifiedNodeId
    };
}
```

**Benefits**:
- Automatic TTL management (Redis expiration)
- Persistence across node restarts
- Suitable for load-balanced deployments
- Distributed state sharing

**Configuration**:
```json
// appsettings.NodeA.json
{
  "Redis": {
    "ConnectionString": "localhost:6379,password=prism-redis-password-node-a,abortConnect=false"
  },
  "FeatureFlags": {
    "UseRedisForChannels": true
  }
}
```

---

## Encrypted Payload Format

All subsequent requests (Phases 2-4) use this format:

```json
{
  "encryptedData": "base64-encoded-ciphertext",
  "iv": "base64-encoded-iv (12 bytes)",
  "authTag": "base64-encoded-authentication-tag (16 bytes)"
}
```

**Encryption Example**:
```csharp
public EncryptedPayload EncryptPayload(object data, byte[] symmetricKey)
{
    // 1. Serialize to JSON
    var json = JsonSerializer.Serialize(data);
    var plaintext = Encoding.UTF8.GetBytes(json);

    // 2. Generate random IV (96 bits for GCM)
    var iv = new byte[12];
    using (var rng = RandomNumberGenerator.Create())
        rng.GetBytes(iv);

    // 3. Prepare AES-GCM cipher
    using var aes = new AesGcm(symmetricKey);
    var ciphertext = new byte[plaintext.Length];
    var tag = new byte[16];  // 128-bit authentication tag

    // 4. Encrypt
    aes.Encrypt(iv, plaintext, ciphertext, tag);

    // 5. Base64 encode
    return new EncryptedPayload
    {
        EncryptedData = Convert.ToBase64String(ciphertext),
        Iv = Convert.ToBase64String(iv),
        AuthTag = Convert.ToBase64String(tag)
    };
}
```

**Decryption Example**:
```csharp
public T DecryptPayload<T>(EncryptedPayload payload, byte[] symmetricKey)
{
    // 1. Base64 decode
    var ciphertext = Convert.FromBase64String(payload.EncryptedData);
    var iv = Convert.FromBase64String(payload.Iv);
    var tag = Convert.FromBase64String(payload.AuthTag);

    // 2. Prepare AES-GCM cipher
    using var aes = new AesGcm(symmetricKey);
    var plaintext = new byte[ciphertext.Length];

    // 3. Decrypt and verify authentication tag
    aes.Decrypt(iv, ciphertext, tag, plaintext);

    // 4. Deserialize JSON
    var json = Encoding.UTF8.GetString(plaintext);
    return JsonSerializer.Deserialize<T>(json);
}
```

---

## Security Properties

### Perfect Forward Secrecy (PFS)

**Definition**: Compromise of long-term keys does NOT compromise past session keys.

**How PRISM Achieves PFS**:
1. **Ephemeral Keys**: New ECDH key pair generated for each channel
2. **No Key Reuse**: Private keys discarded after channel establishment
3. **Unique Derivation**: Symmetric keys derived uniquely per channel using HKDF
4. **Separation**: Channel keys independent from certificate keys

**Result**: Even if an attacker steals a node's certificate private key, they cannot decrypt past channel communications.

---

### Key Derivation (HKDF-SHA256)

**Why HKDF?**
- Extracts cryptographic strength from ECDH shared secret
- Derives uniformly distributed key material
- Allows domain separation via `info` parameter
- Recommended by NIST SP 800-56C

**Parameters**:
- **IKM** (Input Key Material): ECDH shared secret
- **Salt**: None (not required for ephemeral keys)
- **Info**: `"PRISM-Channel-Key-v1"` (domain separation)
- **L**: 32 bytes (256 bits for AES-256)

---

### AES-256-GCM (Galois/Counter Mode)

**Why GCM?**
- **Authenticated Encryption**: Provides both confidentiality and integrity
- **Performance**: Hardware-accelerated on modern CPUs (AES-NI)
- **Parallelizable**: Faster than CBC mode
- **NIST Approved**: Recommended for new applications

**Properties**:
- **Confidentiality**: Ciphertext reveals no information about plaintext
- **Integrity**: Authentication tag detects any tampering
- **No Padding**: Plaintext length = ciphertext length
- **Nonce Requirement**: IV must be unique per encryption (guaranteed by random generation)

---

## Testing

### Automated Tests

**Phase 1 Tests** (6/6 passing):
```csharp
// Bioteca.Prism.InteroperableResearchNode.Test/Phase1ChannelEstablishmentTests.cs

[Fact]
public async Task OpenChannel_ShouldReturnChannelId()
[Fact]
public async Task OpenChannel_ShouldDeriveSharedSecret()
[Fact]
public async Task EncryptDecrypt_ShouldRoundTrip()
[Fact]
public async Task Channel_ShouldExpireAfter30Minutes()
[Fact]
public async Task MultipleChannels_ShouldHaveUniqueKeys()
[Fact]
public async Task ExpiredChannel_ShouldNotDecrypt()
```

---

### Manual Testing

**Using Swagger** (http://localhost:5000/swagger):

```bash
# 1. Open channel
POST /api/channel/open
Body: { "publicKey": "..." }

# Response:
# Headers: X-Channel-Id: "uuid"
# Body: { "publicKey": "..." }

# 2. Test encryption/decryption
POST /api/testing/decrypt-test
Headers: X-Channel-Id: "uuid"
Body: { "encryptedData": "...", "iv": "...", "authTag": "..." }

# Response: Decrypted plaintext
```

---

### End-to-End Test Script

```bash
#!/bin/bash
# test-phase1.sh

echo "=== Phase 1: Encrypted Channel Establishment ==="

# Start Node A and Node B (Docker)
docker-compose -f docker-compose.application.yml up -d
sleep 5

# Test channel opening
CHANNEL_RESPONSE=$(curl -s -X POST http://localhost:5000/api/channel/open \
  -H "Content-Type: application/json" \
  -d '{"publicKey":"MHYwEAYHKoZIzj0CAQYFK4EEACIDYgAE..."}' \
  -i)

CHANNEL_ID=$(echo "$CHANNEL_RESPONSE" | grep -i "X-Channel-Id" | cut -d' ' -f2 | tr -d '\r')

echo "Channel ID: $CHANNEL_ID"

# Test encryption
curl -X POST http://localhost:5000/api/testing/decrypt-test \
  -H "X-Channel-Id: $CHANNEL_ID" \
  -H "Content-Type: application/json" \
  -d '{"encryptedData":"...","iv":"...","authTag":"..."}'

echo "=== Phase 1 Complete ==="
```

---

## Common Issues and Solutions

### Issue: "Channel not found"

**Cause**: Channel expired (30-minute TTL) or node restarted (in-memory storage)

**Solution**:
1. Re-establish channel (call `/api/channel/open` again)
2. Enable Redis storage for persistence across restarts

---

### Issue: "Decryption failed"

**Cause**: Mismatched symmetric keys or tampered payload

**Solution**:
1. Verify both nodes derived the same shared secret (test with known keys)
2. Check IV and auth tag are correctly transmitted
3. Ensure base64 encoding/decoding is correct
4. Verify AES-GCM implementation uses same parameters

---

### Issue: "Public key import failed"

**Cause**: Incorrect key format or unsupported curve

**Solution**:
1. Ensure both nodes use NIST P-384 curve
2. Export keys in SubjectPublicKeyInfo format (SPKI)
3. Verify base64 encoding is correct

---

## Next Phase

Once Phase 1 is complete (encrypted channel established), proceed to:

**Phase 2: Node Identification** (`docs/workflows/PHASE2_IDENTIFICATION_FLOW.md`)

All Phase 2 requests will be encrypted using the channel symmetric key established in Phase 1.

---

## Documentation References

- **Security Details**: `docs/SECURITY_OVERVIEW.md`
- **Handshake Protocol**: `docs/architecture/handshake-protocol.md`
- **Redis Testing**: `docs/testing/redis-testing-guide.md`
- **Manual Testing**: `docs/testing/manual-testing-guide.md`
