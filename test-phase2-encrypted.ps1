# Script de teste para Fase 2 COM CRIPTOGRAFIA DE CANAL
# Este script testa o protocolo completo com payload criptografado

Write-Host "=== Teste Fase 2 - COM CRIPTOGRAFIA DE CANAL ===" -ForegroundColor Cyan
Write-Host ""

# Função para criptografar payload usando AES-GCM (chamando endpoint de teste)
function Encrypt-Payload {
    param(
        [Parameter(Mandatory=$true)]
        [object]$Payload,

        [Parameter(Mandatory=$true)]
        [string]$SymmetricKey,

        [Parameter(Mandatory=$true)]
        [string]$NodeUrl
    )

    $encryptBody = @{
        payload = $Payload
        symmetricKeyBase64 = $SymmetricKey
    } | ConvertTo-Json -Depth 10

    $encrypted = Invoke-RestMethod -Uri "$NodeUrl/api/testing/encrypt-payload" `
        -Method Post `
        -ContentType "application/json" `
        -Body $encryptBody

    return $encrypted
}

# Função para descriptografar payload
function Decrypt-Payload {
    param(
        [Parameter(Mandatory=$true)]
        [object]$EncryptedPayload,

        [Parameter(Mandatory=$true)]
        [string]$SymmetricKey,

        [Parameter(Mandatory=$true)]
        [string]$NodeUrl
    )

    $decryptBody = @{
        encryptedData = $EncryptedPayload.encryptedData
        iv = $EncryptedPayload.iv
        authTag = $EncryptedPayload.authTag
        symmetricKeyBase64 = $SymmetricKey
    } | ConvertTo-Json

    $decrypted = Invoke-RestMethod -Uri "$NodeUrl/api/testing/decrypt-payload" `
        -Method Post `
        -ContentType "application/json" `
        -Body $decryptBody

    return $decrypted
}

# Aguardar nós ficarem prontos
Write-Host "Aguardando nós ficarem prontos..." -ForegroundColor Yellow
Start-Sleep -Seconds 3

# Teste 1: Estabelecer canal criptografado (Fase 1)
Write-Host "`n[Teste 1] Estabelecer Canal Criptografado (Fase 1)" -ForegroundColor Green
Write-Host "No A -> No B (Handshake):"
try {
    $body = @{
        remoteNodeUrl = "http://node-b:8080"
    } | ConvertTo-Json

    $channelResult = Invoke-RestMethod -Uri "http://localhost:5000/api/channel/initiate" `
        -Method Post `
        -ContentType "application/json" `
        -Body $body

    if ($channelResult.success) {
        Write-Host "  [OK] Canal estabelecido!" -ForegroundColor Green
        Write-Host "    Channel ID: $($channelResult.channelId)" -ForegroundColor Cyan
        Write-Host "    Cipher: $($channelResult.selectedCipher)" -ForegroundColor Cyan

        $channelId = $channelResult.channelId
        $symmetricKey = $channelResult.symmetricKey  # Base64 encoded
    } else {
        Write-Host "  [ERRO] Falha ao estabelecer canal" -ForegroundColor Red
        exit
    }
} catch {
    Write-Host "  [ERRO] Requisicao falhou: $_" -ForegroundColor Red
    exit
}

# Teste 2: Gerar identidade para Nó A
Write-Host "`n[Teste 2] Gerar Identidade para No A" -ForegroundColor Green
try {
    $identityBody = @{
        nodeId = "node-a-encrypted-001"
        nodeName = "Node A - Encrypted Test"
        channelId = $channelId
        validityYears = 2
        password = "test123"
    } | ConvertTo-Json

    $identity = Invoke-RestMethod -Uri "http://localhost:5000/api/testing/generate-node-identity" `
        -Method Post `
        -ContentType "application/json" `
        -Body $identityBody

    Write-Host "  [OK] Identidade gerada!" -ForegroundColor Green

    $nodeId = $identity.nodeId
    $certificate = $identity.certificate
    $signature = $identity.identificationRequest.signature
    $signedData = $identity.identificationRequest.signedData

} catch {
    Write-Host "  [ERRO] Falha ao gerar identidade: $_" -ForegroundColor Red
    exit
}

# Teste 3: Registrar Nó A no Nó B (COM CRIPTOGRAFIA)
Write-Host "`n[Teste 3] Registrar No A no No B (Payload Criptografado)" -ForegroundColor Green
try {
    # Payload em texto claro
    $registrationPayload = @{
        nodeId = $nodeId
        nodeName = "Node A - Encrypted Test"
        certificate = $certificate
        contactInfo = "admin@node-a.test"
        institutionDetails = "Test Institution A"
        nodeUrl = "http://node-a:8080"
        requestedCapabilities = @("search", "retrieve")
    }

    # IMPORTANTE: Para este teste funcionar, precisamos de um endpoint de teste
    # que simule a criptografia. Por enquanto, vamos testar sem criptografia
    # para validar o fluxo básico.

    Write-Host "  [AVISO] Criptografia de payload ainda nao implementada no script" -ForegroundColor Yellow
    Write-Host "  [INFO] Use o cliente .NET para teste completo com criptografia" -ForegroundColor Yellow

    # TODO: Implementar chamada com payload criptografado usando helper functions
    # $encryptedPayload = Encrypt-Payload -Payload $registrationPayload -SymmetricKey $symmetricKey -NodeUrl "http://localhost:5001"
    # $headers = @{ "X-Channel-Id" = $channelId }
    # $regResult = Invoke-RestMethod -Uri "http://localhost:5001/api/node/register" `
    #     -Method Post `
    #     -ContentType "application/json" `
    #     -Headers $headers `
    #     -Body ($encryptedPayload | ConvertTo-Json)

} catch {
    Write-Host "  [ERRO] Requisicao falhou: $_" -ForegroundColor Red
}

# Teste 4: Identificar Nó A no Nó B (COM CRIPTOGRAFIA)
Write-Host "`n[Teste 4] Identificar No A no No B (Payload Criptografado)" -ForegroundColor Green
try {
    # Payload em texto claro
    $identifyPayload = @{
        channelId = $channelId
        nodeId = $nodeId
        certificate = $certificate
        signature = $signature
        signedData = $signedData
    }

    Write-Host "  [AVISO] Criptografia de payload ainda nao implementada no script" -ForegroundColor Yellow
    Write-Host "  [INFO] Use o cliente .NET para teste completo com criptografia" -ForegroundColor Yellow

    # TODO: Implementar chamada com payload criptografado
    # $encryptedPayload = Encrypt-Payload -Payload $identifyPayload -SymmetricKey $symmetricKey -NodeUrl "http://localhost:5001"
    # $headers = @{ "X-Channel-Id" = $channelId }
    # $identifyResult = Invoke-RestMethod -Uri "http://localhost:5001/api/channel/identify" `
    #     -Method Post `
    #     -ContentType "application/json" `
    #     -Headers $headers `
    #     -Body ($encryptedPayload | ConvertTo-Json)

} catch {
    Write-Host "  [ERRO] Requisicao falhou: $_" -ForegroundColor Red
}

Write-Host "`n=== Resumo do Teste ===" -ForegroundColor Cyan
Write-Host "1. Canal criptografado estabelecido: [OK]" -ForegroundColor Green
Write-Host "2. Identidade gerada: [OK]" -ForegroundColor Green
Write-Host "3. Registro com criptografia: [PENDENTE - requer helper C#]" -ForegroundColor Yellow
Write-Host "4. Identificacao com criptografia: [PENDENTE - requer helper C#]" -ForegroundColor Yellow
Write-Host ""
Write-Host "NOTA: Para teste completo com criptografia de payload," -ForegroundColor Cyan
Write-Host "use o NodeChannelClient.IdentifyNodeAsync() e RegisterNodeAsync()" -ForegroundColor Cyan
Write-Host "diretamente em codigo C#." -ForegroundColor Cyan
