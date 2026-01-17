<#
.SYNOPSIS
    Register a new research node for approval in PostgreSQL database.
.PARAMETER NodeName
    Name of the research node
.PARAMETER NodeUrl
    URL of the remote node
.PARAMETER InstitutionName
    Name of the research institution
.PARAMETER ContactInfo
    Contact email (optional)
.PARAMETER AccessLevel
    Requested access level: 0=ReadOnly, 1=ReadWrite, 2=Admin (default: 1)
#>
param(
    [Parameter(Mandatory=$true)][string]$NodeName,
    [Parameter(Mandatory=$true)][string]$NodeUrl,
    [string]$InstitutionName = "",
    [string]$ContactInfo = "",
    [int]$AccessLevel = 1
)

# Generate self-signed certificate
Write-Host "Generating certificate..." -ForegroundColor Yellow
$cert = New-SelfSignedCertificate -Subject "CN=$NodeName" -CertStoreLocation "Cert:\CurrentUser\My" -KeySpec Signature -KeyLength 2048 -NotAfter (Get-Date).AddYears(5)
$certBase64 = [Convert]::ToBase64String($cert.RawData)

# Calculate fingerprint
$sha256 = [System.Security.Cryptography.SHA256]::Create()
$fingerprintBytes = $sha256.ComputeHash($cert.RawData)
$fingerprint = [Convert]::ToBase64String($fingerprintBytes)

# Remove cert from store
Remove-Item "Cert:\CurrentUser\My\$($cert.Thumbprint)" -ErrorAction SilentlyContinue

$nodeId = [Guid]::NewGuid().ToString()
$now = (Get-Date).ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss.ffffff")
$institutionJson = if ($InstitutionName) { "{`"name`":`"$InstitutionName`"}" } else { "{}" }

$accessLevelStr = switch ($AccessLevel) {
    0 { "ReadOnly" }
    2 { "Admin" }
    default { "ReadWrite" }
}

$sql = @"
INSERT INTO research_nodes (
    id, node_name, node_url, certificate, certificate_fingerprint,
    contact_info, institution_details, status, node_access_level,
    registered_at, updated_at, metadata
) VALUES (
    '$nodeId',
    '$($NodeName -replace "'", "''")',
    '$($NodeUrl -replace "'", "''")',
    '$certBase64',
    '$fingerprint',
    '$($ContactInfo -replace "'", "''")',
    '$($institutionJson -replace "'", "''")',
    'Pending',
    '$accessLevelStr',
    '$now',
    '$now',
    '{}'
);
SELECT id, node_name, status FROM research_nodes WHERE id = '$nodeId';
"@

Write-Host "Registering node in database..." -ForegroundColor Yellow

try {
    # Try with docker first
    $result = docker exec -i irn-postgres-node-a psql -U prism_user_a -d prism_node_a_registry -c $sql 2>&1

    if ($LASTEXITCODE -eq 0) {
        Write-Host "Node registered successfully! Pending approval." -ForegroundColor Green
        Write-Host "ID: $nodeId" -ForegroundColor Cyan
        Write-Host "Name: $NodeName" -ForegroundColor Cyan
        Write-Host "Status: Pending (awaiting approval via application)" -ForegroundColor Yellow
        $result
    } else {
        throw "Docker command failed: $result"
    }
} catch {
    Write-Host "Docker not available, trying direct psql..." -ForegroundColor Yellow

    # Try direct psql
    $env:PGPASSWORD = "prism-secure-password-node-a-2025"
    $result = psql -h localhost -p 5432 -U prism_user_a -d prism_node_a_registry -c $sql 2>&1

    if ($LASTEXITCODE -eq 0) {
        Write-Host "Node registered successfully!" -ForegroundColor Green
        Write-Host "ID: $nodeId" -ForegroundColor Cyan
        $result
    } else {
        Write-Host "Error: $result" -ForegroundColor Red
        exit 1
    }
}
