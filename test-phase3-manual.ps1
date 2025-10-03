# Manual Phase 3 Testing Script
# Complete flow: Channel → Register → Approve → Challenge → Authenticate

Write-Host "=== PHASE 3 MANUAL TESTING SCRIPT ===" -ForegroundColor Cyan
Write-Host ""

# ============================================================================
# PHASE 1: Establish Encrypted Channel
# ============================================================================
Write-Host "PHASE 1: Establishing encrypted channel..." -ForegroundColor Yellow

$phase1Response = Invoke-RestMethod -Uri "http://localhost:5000/api/channel/initiate" `
    -Method Post `
    -ContentType "application/json" `
    -Body '{"remoteNodeUrl": "http://irn-node-b:8080"}'

$channelId = $phase1Response.channelId
Write-Host "✓ Channel established: $channelId" -ForegroundColor Green
Write-Host ""

# ============================================================================
# PHASE 2.1: Generate Certificate
# ============================================================================
Write-Host "PHASE 2.1: Generating certificate for test-node-001..." -ForegroundColor Yellow

$certRequest = @{
    subjectName = "test-node-001"
    validityYears = 2
    password = "test123"
} | ConvertTo-Json

$certResponse = Invoke-RestMethod -Uri "http://localhost:5000/api/testing/generate-certificate" `
    -Method Post `
    -ContentType "application/json" `
    -Body $certRequest

$certificate = $certResponse.certificate
$certificateWithPrivateKey = $certResponse.certificateWithPrivateKey
$password = $certResponse.password

Write-Host "✓ Certificate generated" -ForegroundColor Green
Write-Host "  Thumbprint: $($certResponse.thumbprint)" -ForegroundColor Gray
Write-Host "  Valid: $($certResponse.validFrom) to $($certResponse.validTo)" -ForegroundColor Gray
Write-Host ""

# ============================================================================
# PHASE 2.2: Register Node
# ============================================================================
Write-Host "PHASE 2.2: Registering node test-node-001..." -ForegroundColor Yellow

# Generate signature for registration
$timestamp = Get-Date -Format "o"
$signDataRequest = @{
    data = ""
    channelId = $channelId
    nodeId = "test-node-001"
    certificateWithPrivateKey = $certificateWithPrivateKey
    password = $password
    timestamp = $timestamp
} | ConvertTo-Json

$signResponse = Invoke-RestMethod -Uri "http://localhost:5000/api/testing/sign-data" `
    -Method Post `
    -ContentType "application/json" `
    -Body $signDataRequest

$signature = $signResponse.signature

# Build registration request
$registrationPayload = @{
    channelId = $channelId
    nodeId = "test-node-001"
    nodeName = "Test Node 001"
    nodeUrl = "http://test-node-001:8080"
    certificate = $certificate
    signature = $signature
    timestamp = $timestamp
    requestedCapabilities = @("query:read", "query:aggregate")
} | ConvertTo-Json

# Encrypt registration payload
$encryptRequest = @{
    channelId = $channelId
    payload = $registrationPayload | ConvertFrom-Json
} | ConvertTo-Json -Depth 10

$encryptedPayload = Invoke-RestMethod -Uri "http://localhost:5000/api/testing/encrypt-payload" `
    -Method Post `
    -ContentType "application/json" `
    -Body $encryptRequest

# Send registration request to Node B
$registrationResponse = Invoke-RestMethod -Uri "http://localhost:5001/api/node/register" `
    -Method Post `
    -ContentType "application/json" `
    -Headers @{"X-Channel-Id" = $channelId} `
    -Body ($encryptedPayload.encryptedPayload | ConvertTo-Json)

# Decrypt response
$decryptRequest = @{
    channelId = $channelId
    encryptedPayload = $registrationResponse
} | ConvertTo-Json -Depth 10

$decryptedRegistration = Invoke-RestMethod -Uri "http://localhost:5000/api/testing/decrypt-payload" `
    -Method Post `
    -ContentType "application/json" `
    -Body $decryptRequest

Write-Host "✓ Node registered" -ForegroundColor Green
Write-Host "  Status: $($decryptedRegistration.decryptedPayload.status)" -ForegroundColor Gray
Write-Host "  Message: $($decryptedRegistration.decryptedPayload.message)" -ForegroundColor Gray
Write-Host ""

# ============================================================================
# PHASE 2.3: Approve Node (Manual step - simulated by status change)
# ============================================================================
Write-Host "PHASE 2.3: Approving node test-node-001..." -ForegroundColor Yellow

$approvalResponse = Invoke-RestMethod -Uri "http://localhost:5001/api/node/test-node-001/status" `
    -Method Put `
    -ContentType "application/json" `
    -Body '{"status": "Authorized", "grantedCapabilities": ["query:read", "query:aggregate"]}'

Write-Host "✓ Node approved" -ForegroundColor Green
Write-Host "  Status: $($approvalResponse.status)" -ForegroundColor Gray
Write-Host "  Capabilities: $($approvalResponse.grantedCapabilities -join ', ')" -ForegroundColor Gray
Write-Host ""

# ============================================================================
# PHASE 3.1: Request Challenge
# ============================================================================
Write-Host "PHASE 3.1: Requesting authentication challenge..." -ForegroundColor Yellow

$challengeRequest = @{
    channelId = $channelId
    nodeId = "test-node-001"
} | ConvertTo-Json

$challengeResponse = Invoke-RestMethod -Uri "http://localhost:5000/api/testing/request-challenge" `
    -Method Post `
    -ContentType "application/json" `
    -Body $challengeRequest

$challengeData = $challengeResponse.challengeResponse.challengeData
$challengeExpiresAt = $challengeResponse.challengeResponse.expiresAt

Write-Host "✓ Challenge received" -ForegroundColor Green
Write-Host "  Challenge Data: $($challengeData.Substring(0, 20))..." -ForegroundColor Gray
Write-Host "  Expires At: $challengeExpiresAt" -ForegroundColor Gray
Write-Host "  TTL: $($challengeResponse.challengeResponse.challengeTtlSeconds) seconds" -ForegroundColor Gray
Write-Host ""

# ============================================================================
# PHASE 3.2: Sign Challenge
# ============================================================================
Write-Host "PHASE 3.2: Signing challenge with private key..." -ForegroundColor Yellow

$authTimestamp = Get-Date -Format "o"

$signChallengeRequest = @{
    challengeData = $challengeData
    channelId = $channelId
    nodeId = "test-node-001"
    certificateWithPrivateKey = $certificateWithPrivateKey
    password = $password
    timestamp = $authTimestamp
} | ConvertTo-Json

$signedChallengeResponse = Invoke-RestMethod -Uri "http://localhost:5000/api/testing/sign-challenge" `
    -Method Post `
    -ContentType "application/json" `
    -Body $signChallengeRequest

$challengeSignature = $signedChallengeResponse.signature

Write-Host "✓ Challenge signed" -ForegroundColor Green
Write-Host "  Signed Data: $($signedChallengeResponse.signedData.Substring(0, 40))..." -ForegroundColor Gray
Write-Host "  Signature: $($challengeSignature.Substring(0, 40))..." -ForegroundColor Gray
Write-Host ""

# ============================================================================
# PHASE 3.3: Authenticate with Signed Challenge
# ============================================================================
Write-Host "PHASE 3.3: Authenticating with signed challenge..." -ForegroundColor Yellow

$authenticateRequest = @{
    channelId = $channelId
    nodeId = "test-node-001"
    challengeData = $challengeData
    signature = $challengeSignature
    timestamp = $authTimestamp
} | ConvertTo-Json

$authResponse = Invoke-RestMethod -Uri "http://localhost:5000/api/testing/authenticate" `
    -Method Post `
    -ContentType "application/json" `
    -Body $authenticateRequest

Write-Host "✓ AUTHENTICATION SUCCESSFUL!" -ForegroundColor Green -BackgroundColor DarkGreen
Write-Host ""
Write-Host "SESSION DETAILS:" -ForegroundColor Cyan
Write-Host "  Authenticated: $($authResponse.authenticationResponse.authenticated)" -ForegroundColor White
Write-Host "  Session Token: $($authResponse.authenticationResponse.sessionToken)" -ForegroundColor White
Write-Host "  Expires At: $($authResponse.authenticationResponse.sessionExpiresAt)" -ForegroundColor White
Write-Host "  Capabilities: $($authResponse.authenticationResponse.grantedCapabilities -join ', ')" -ForegroundColor White
Write-Host "  Next Phase: $($authResponse.authenticationResponse.nextPhase)" -ForegroundColor White
Write-Host ""

# ============================================================================
# Summary
# ============================================================================
Write-Host "=== TEST COMPLETED SUCCESSFULLY ===" -ForegroundColor Green
Write-Host ""
Write-Host "SUMMARY:" -ForegroundColor Cyan
Write-Host "  ✓ Phase 1: Encrypted channel established" -ForegroundColor Gray
Write-Host "  ✓ Phase 2: Node registered and approved" -ForegroundColor Gray
Write-Host "  ✓ Phase 3: Challenge-response authentication completed" -ForegroundColor Gray
Write-Host ""
Write-Host "Session token is valid for 1 hour and can be used for authenticated requests." -ForegroundColor Yellow
Write-Host ""

# Save session token to file for later use
$sessionInfo = @{
    channelId = $channelId
    nodeId = "test-node-001"
    sessionToken = $authResponse.authenticationResponse.sessionToken
    expiresAt = $authResponse.authenticationResponse.sessionExpiresAt
    capabilities = $authResponse.authenticationResponse.grantedCapabilities
} | ConvertTo-Json

$sessionInfo | Out-File "session-token.json" -Encoding UTF8
Write-Host "Session info saved to session-token.json" -ForegroundColor Gray
