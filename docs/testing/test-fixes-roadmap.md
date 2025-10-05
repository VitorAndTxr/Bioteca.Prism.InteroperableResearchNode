# Test Fixes Roadmap

**Date**: 2025-10-02
**Current Status**: 34/56 tests passing (61%)
**Goal**: 100% tests passing before implementing Phase 3

---

## 📊 Current Status Summary

### ✅ What's Working (34 tests)

| Category | Status | Details |
|----------|--------|---------|
| **Phase 1 - Encrypted Channel** | 6/6 (100%) | ✅ All passing |
| **Certificates and Signatures** | 14/15 (93%) | ✅ Nearly perfect |
| **Phase 2 - Node Identification** | 5/6 (83%) | ✅ Core functioning |
| **Encrypted Channel Integration** | 2/3 (67%) | ⚠️ Most OK |

### ❌ What Needs Fixing (22 tests)

1. **NodeChannelClient** - 6/7 failures (14% passing)
2. **Security and Edge Cases** - 11/17 failures (36% passing)
3. **Encryption Cases** - 3 failures
4. **Minor Bug** - 1 failure (timezone)

---

## 🎯 Action Plan

### **PRIORITY 1: Fix NodeChannelClient (6-8 hours)**

**Problem**: Tests use `INodeChannelClient` which internally uses `IHttpClientFactory.CreateClient()` to make real HTTP requests, but cannot connect to `TestWebApplicationFactory` in-memory servers.

**Affected tests (6):**
- `InitiateChannel_WithValidRemoteUrl_EstablishesChannel`
- `IdentifyNode_WithInvalidSignature_ReturnsError`
- `IdentifyNode_UnknownNode_ReturnsNotKnown`
- `FullWorkflow_InitiateRegisterIdentify_WorksEndToEnd`
- `RegisterNode_AfterChannelEstablished_SuccessfullyRegisters`
- `IdentifyNode_AfterRegistration_ReturnsPending`

**Recommended Solution: Option A - Mock IHttpClientFactory**

#### Step 1: Create TestHttpClientFactory

```csharp
// File: Bioteca.Prism.InteroperableResearchNode.Test/Helpers/TestHttpClientFactory.cs

namespace Bioteca.Prism.InteroperableResearchNode.Test.Helpers;

public class TestHttpClientFactory : IHttpClientFactory
{
    private readonly Dictionary<string, HttpClient> _clients = new();

    public void RegisterClient(string name, HttpClient client)
    {
        _clients[name] = client;
    }

    public HttpClient CreateClient(string name)
    {
        // If no registered client, create default
        if (_clients.TryGetValue(name, out var client))
        {
            return client;
        }

        // Return default client (can be empty or throw exception)
        return _clients.TryGetValue("default", out var defaultClient)
            ? defaultClient
            : new HttpClient();
    }
}
```

#### Step 2: Update TestWebApplicationFactory

```csharp
// File: TestWebApplicationFactory.cs

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private string _environmentName = "Development";
    private Action<IServiceCollection>? _configureTestServices;

    public TestWebApplicationFactory()
    {
    }

    public static TestWebApplicationFactory Create(string environmentName)
    {
        var factory = new TestWebApplicationFactory();
        factory._environmentName = environmentName;
        return factory;
    }

    public TestWebApplicationFactory WithHttpClient(string name, HttpClient client)
    {
        _configureTestServices = services =>
        {
            // Remove default IHttpClientFactory
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IHttpClientFactory));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add our test factory
            var testFactory = new TestHttpClientFactory();
            testFactory.RegisterClient(name, client);
            testFactory.RegisterClient("", client); // Default client
            services.AddSingleton<IHttpClientFactory>(testFactory);
        };
        return this;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(_environmentName);

        builder.ConfigureServices(services =>
        {
            _configureTestServices?.Invoke(services);
        });
    }
}
```

#### Step 3: Update NodeChannelClientTests

```csharp
[Fact]
public async Task InitiateChannel_WithValidRemoteUrl_EstablishesChannel()
{
    // Arrange - Create remote factory
    using var remoteFactory = TestWebApplicationFactory.Create("RemoteNode");
    var remoteClient = remoteFactory.CreateClient();

    // Configure local factory to use remote factory's HttpClient
    var localFactory = TestWebApplicationFactory.Create("LocalNode")
        .WithHttpClient("", remoteClient);

    var channelClient = localFactory.Services.GetRequiredService<INodeChannelClient>();
    var remoteUrl = remoteClient.BaseAddress!.ToString().TrimEnd('/');

    // Act
    var result = await channelClient.OpenChannelAsync(remoteUrl);

    // Assert
    result.Should().NotBeNull();
    result.Success.Should().BeTrue();
    result.ChannelId.Should().NotBeNullOrEmpty();
}
```

**Estimated Time**: 6-8 hours (includes testing and debugging)

---

### **PRIORITY 2: Investigate Encryption Failures (2-4 hours)**

**Problem**: Some tests fail with "Failed to decrypt data - authentication failed"

**Affected tests (3):**
- `FullWorkflow_EstablishChannel_RegisterNode_Identify_Authorize`
- `IdentifyNode_UnknownNode_ReturnsNotKnown` (Phase2NodeIdentificationTests)
- `IdentifyNode_PendingNode_ReturnsPending`

**Investigation:**

#### Step 1: Add detailed logging

```csharp
// In Phase2NodeIdentificationTests.cs - EstablishChannelAsync method
private async Task<(string channelId, byte[] symmetricKey)> EstablishChannelAsync()
{
    // ... existing code ...

    var symmetricKey = encryptionService.DeriveKey(sharedSecret, salt, info);

    // DEBUG: Log to verify
    _logger.LogInformation("Channel established: {ChannelId}, Key length: {KeyLength}",
        channelId, symmetricKey.Length);

    return (channelId, symmetricKey);
}
```

#### Step 2: Check if channel expires during test

```csharp
// Check if channel still exists before using
var channel = _channelStore.GetChannel(channelId);
if (channel == null || channel.ExpiresAt < DateTime.UtcNow)
{
    // Channel expired! Recreate
}
```

#### Step 3: Review operation order

Verify all tests:
1. Establish channel BEFORE using
2. Use SAME channelId consistently
3. Don't reuse keys between tests

**Estimated Time**: 2-4 hours

---

### **PRIORITY 3: Implement Missing Validations (4-6 hours)**

**Problem**: 11 tests validate unimplemented features (TDD)

#### Step 1: Timestamp Validation (1-2 hours)

**Affected tests:**
- `OpenChannel_WithFutureTimestamp_ReturnsBadRequest`
- `OpenChannel_WithOldTimestamp_ReturnsBadRequest`

**Implementation:**

```csharp
// File: ChannelController.cs - ValidateChannelOpenRequest method

private HandshakeError? ValidateChannelOpenRequest(ChannelOpenRequest request)
{
    // ... existing validations ...

    // Validate timestamp
    var now = DateTime.UtcNow;
    var timestampAge = now - request.Timestamp;

    if (timestampAge.TotalSeconds < -300) // 5 minutes in future
    {
        return CreateError(
            "ERR_INVALID_TIMESTAMP",
            "Request timestamp is too far in the future",
            new Dictionary<string, object>
            {
                ["requestTimestamp"] = request.Timestamp,
                ["serverTime"] = now,
                ["maxSkewSeconds"] = 300
            },
            retryable: true
        );
    }

    if (timestampAge.TotalSeconds > 30) // 30 seconds in past
    {
        return CreateError(
            "ERR_INVALID_TIMESTAMP",
            "Request timestamp is too old",
            new Dictionary<string, object>
            {
                ["requestTimestamp"] = request.Timestamp,
                ["serverTime"] = now,
                ["maxAgeSeconds"] = 30
            },
            retryable: true
        );
    }

    return null;
}
```

#### Step 2: Nonce Validation (30 min)

**Affected tests:**
- `OpenChannel_WithInvalidNonce_ReturnsBadRequest`
- `OpenChannel_WithShortNonce_ReturnsBadRequest`

```csharp
// In ValidateChannelOpenRequest

if (string.IsNullOrWhiteSpace(request.Nonce))
{
    return CreateError("ERR_CHANNEL_FAILED", "Nonce is required", retryable: true);
}

// Validate minimum size (12 bytes = 16 chars base64)
try
{
    var nonceBytes = Convert.FromBase64String(request.Nonce);
    if (nonceBytes.Length < 12)
    {
        return CreateError(
            "ERR_INVALID_NONCE",
            "Nonce must be at least 12 bytes",
            new Dictionary<string, object> { ["minBytes"] = 12, ["actualBytes"] = nonceBytes.Length },
            retryable: true
        );
    }
}
catch (FormatException)
{
    return CreateError("ERR_INVALID_NONCE", "Nonce must be valid base64", retryable: true);
}
```

#### Step 3: Expired Certificate Validation (1 hour)

**Affected test:**
- `RegisterNode_WithExpiredCertificate_ReturnsBadRequest`

```csharp
// File: NodeConnectionController.cs - RegisterNode method

[HttpPost("register")]
public async Task<IActionResult> RegisterNode([FromBody] EncryptedPayload encryptedPayload)
{
    // ... decrypt payload ...

    var request = decryptedRequest as NodeRegistrationRequest;

    // Validate certificate
    try
    {
        var certBytes = Convert.FromBase64String(request.Certificate);
        var cert = new X509Certificate2(certBytes);

        // Check if expired
        if (cert.NotAfter < DateTime.UtcNow)
        {
            return BadRequest(new
            {
                error = "Certificate has expired",
                expiredAt = cert.NotAfter,
                currentTime = DateTime.UtcNow
            });
        }

        if (cert.NotBefore > DateTime.UtcNow)
        {
            return BadRequest(new
            {
                error = "Certificate not yet valid",
                validFrom = cert.NotBefore,
                currentTime = DateTime.UtcNow
            });
        }
    }
    catch (Exception ex)
    {
        return BadRequest(new { error = "Invalid certificate format", details = ex.Message });
    }

    // ... rest of logic ...
}
```

#### Step 4: Required Fields Validation (1 hour)

**Affected tests:**
- `RegisterNode_WithEmptyNodeId_ReturnsBadRequest`
- `RegisterNode_WithEmptyNodeName_ReturnsBadRequest`
- `GenerateCertificate_WithEmptySubjectName_ReturnsBadRequest`

**Option A: Use Data Annotations**

```csharp
// File: Domain/Requests/Node/NodeRegistrationRequest.cs

public class NodeRegistrationRequest
{
    [Required(ErrorMessage = "NodeId is required")]
    [MinLength(1, ErrorMessage = "NodeId cannot be empty")]
    public string NodeId { get; set; } = string.Empty;

    [Required(ErrorMessage = "NodeName is required")]
    [MinLength(1, ErrorMessage = "NodeName cannot be empty")]
    public string NodeName { get; set; } = string.Empty;

    // ... other fields ...
}
```

**Option B: Manual Controller Validation**

```csharp
if (string.IsNullOrWhiteSpace(request.NodeId))
{
    return BadRequest(new { error = "NodeId is required" });
}

if (string.IsNullOrWhiteSpace(request.NodeName))
{
    return BadRequest(new { error = "NodeName is required" });
}
```

#### Step 5: Enum Validation (30 min)

**Affected test:**
- `UpdateNodeStatus_ToInvalidStatus_ReturnsBadRequest`

```csharp
// File: NodeConnectionController.cs

[HttpPut("{nodeId}/status")]
public IActionResult UpdateNodeStatus(string nodeId, [FromBody] UpdateNodeStatusRequest request)
{
    // Validate enum
    if (!Enum.IsDefined(typeof(AuthorizationStatus), request.Status))
    {
        return BadRequest(new
        {
            error = "Invalid status value",
            validValues = Enum.GetNames(typeof(AuthorizationStatus))
        });
    }

    // ... rest of logic ...
}
```

#### Step 6: Other cases (1 hour)

- `RegisterNode_Twice_SecondRegistrationUpdatesInfo` - Implement update logic
- `RegisterNode_WithInvalidCertificateFormat_ReturnsBadRequest` - Already implemented in Step 3

**Total Estimated Time**: 4-6 hours

---

### **PRIORITY 4: Fix Timezone Bug (30 min)**

**Affected test:**
- `CertificateHelper_GenerateCertificate_ProducesValidCertificate`

**Problem**: Date comparison with different timezones

**Solution:**

```csharp
// File: CertificateAndSignatureTests.cs

[Fact]
public void CertificateHelper_GenerateCertificate_ProducesValidCertificate()
{
    // Arrange
    var subjectName = "test-certificate";
    var validityYears = 1;

    // Act
    var certificate = CertificateHelper.GenerateSelfSignedCertificate(subjectName, validityYears);

    // Assert
    certificate.Should().NotBeNull();
    certificate.SubjectName.Name.Should().Contain(subjectName);

    // Use ToUniversalTime() to normalize
    var now = DateTime.UtcNow;
    certificate.NotBefore.ToUniversalTime().Should().BeCloseTo(now, TimeSpan.FromMinutes(1));

    var expectedExpiry = now.AddYears(validityYears);
    certificate.NotAfter.ToUniversalTime().Should().BeCloseTo(expectedExpiry, TimeSpan.FromMinutes(1));
}
```

**Estimated Time**: 30 minutes

---

## 📅 Suggested Schedule

| Day | Task | Hours | Tests Fixed |
|-----|------|-------|-------------|
| **Day 1** | PRIORITY 1 - NodeChannelClient (Part 1) | 4h | 0 (setup) |
| **Day 2** | PRIORITY 1 - NodeChannelClient (Part 2) | 4h | +6 tests |
| **Day 3** | PRIORITY 2 - Investigate Encryption | 3h | +3 tests |
| **Day 3** | PRIORITY 4 - Timezone Bug | 0.5h | +1 test |
| **Day 4** | PRIORITY 3 - Validations (Timestamp + Nonce) | 3h | +4 tests |
| **Day 5** | PRIORITY 3 - Validations (Certificate + Fields) | 3h | +7 tests |
| **TOTAL** | - | **17.5h** | **+22 tests (100%)** |

---

## ✅ Progress Checklist

### NodeChannelClient (6 tests)
- [ ] Create `TestHttpClientFactory`
- [ ] Update `TestWebApplicationFactory` with `WithHttpClient()` method
- [ ] Refactor `InitiateChannel_WithValidRemoteUrl_EstablishesChannel`
- [ ] Refactor `IdentifyNode_WithInvalidSignature_ReturnsError`
- [ ] Refactor `IdentifyNode_UnknownNode_ReturnsNotKnown`
- [ ] Refactor `FullWorkflow_InitiateRegisterIdentify_WorksEndToEnd`
- [ ] Refactor `RegisterNode_AfterChannelEstablished_SuccessfullyRegisters`
- [ ] Refactor `IdentifyNode_AfterRegistration_ReturnsPending`

### Encryption (3 tests)
- [ ] Add debug logging
- [ ] Verify channel expiration
- [ ] Fix `FullWorkflow_EstablishChannel_RegisterNode_Identify_Authorize`
- [ ] Fix `IdentifyNode_UnknownNode_ReturnsNotKnown`
- [ ] Fix `IdentifyNode_PendingNode_ReturnsPending`

### Validations (11 tests)
- [ ] Implement timestamp validation (future)
- [ ] Implement timestamp validation (past)
- [ ] Implement nonce validation (invalid)
- [ ] Implement nonce validation (short)
- [ ] Implement expired certificate validation
- [ ] Implement certificate format validation
- [ ] Implement empty NodeId validation
- [ ] Implement empty NodeName validation
- [ ] Implement invalid status validation
- [ ] Implement empty SubjectName validation
- [ ] Implement duplicate registration logic

### Timezone Bug (1 test)
- [ ] Fix date comparison in `CertificateHelper_GenerateCertificate_ProducesValidCertificate`

---

## 🎯 Final Goal

**Current Status**: 34/56 (61%)
**Target**: 56/56 (100%)
**Tests to fix**: 22
**Estimated time**: 17.5 hours (~3 work days)

---

## 📚 Useful Resources

- **Run tests**: `dotnet test --verbosity normal`
- **Run specific test**: `dotnet test --filter "FullyQualifiedName~TestName"`
- **See detailed output**: `dotnet test --logger "console;verbosity=detailed"`
- **Documentation**: `docs/testing/phase2-encrypted-manual-testing.md`

---

**Last updated**: 2025-10-02
**Author**: Claude Code Assistant
