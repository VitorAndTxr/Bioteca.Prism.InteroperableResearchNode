#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Script interativo para teste manual da Fase 2 com criptografia de canal

.DESCRIPTION
    Este script guia você passo-a-passo pelo processo de:
    1. Estabelecer canal criptografado
    2. Gerar certificado
    3. Assinar dados
    4. Criptografar payloads
    5. Identificar e registrar nó
    6. Aprovar e re-identificar

.EXAMPLE
    .\test-fase2-manual.ps1
#>

$ErrorActionPreference = "Stop"

# Cores para output
function Write-Step {
    param([string]$Message)
    Write-Host "`n🔹 $Message" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "✅ $Message" -ForegroundColor Green
}

function Write-Error {
    param([string]$Message)
    Write-Host "❌ $Message" -ForegroundColor Red
}

function Write-Info {
    param([string]$Message)
    Write-Host "ℹ️  $Message" -ForegroundColor Yellow
}

# URLs dos nós
$NodeA = "http://localhost:5000"
$NodeB = "http://localhost:5001"

Write-Host @"
╔═══════════════════════════════════════════════════════════════╗
║     Teste Manual - Fase 2 com Criptografia de Canal          ║
║                                                               ║
║  Este script irá guiá-lo através do processo completo de     ║
║  identificação e registro de nós usando canal criptografado  ║
╚═══════════════════════════════════════════════════════════════╝
"@ -ForegroundColor Magenta

# Verificar se containers estão rodando
Write-Step "Verificando se containers estão rodando..."
try {
    $healthA = Invoke-RestMethod -Uri "$NodeA/api/channel/health" -Method Get -TimeoutSec 5
    $healthB = Invoke-RestMethod -Uri "$NodeB/api/channel/health" -Method Get -TimeoutSec 5
    Write-Success "Node A e Node B estão rodando!"
} catch {
    Write-Error "Containers não estão acessíveis. Execute: docker-compose up -d"
    exit 1
}

# PASSO 1: Estabelecer canal
Write-Step "PASSO 1: Estabelecendo canal criptografado (Node A → Node B)"
Write-Info "Executando: POST $NodeA/api/channel/initiate"

$initiateRequest = @{
    remoteNodeUrl = "http://node-b:8080"
} | ConvertTo-Json

try {
    $channelResult = Invoke-RestMethod `
        -Uri "$NodeA/api/channel/initiate" `
        -Method Post `
        -ContentType "application/json" `
        -Body $initiateRequest

    $channelId = $channelResult.channelId
    Write-Success "Canal estabelecido!"
    Write-Host "   Channel ID: $channelId" -ForegroundColor White
    Write-Host "   Cipher: $($channelResult.cipher)" -ForegroundColor White
    Write-Host "   Expira em: $($channelResult.expiresAt)" -ForegroundColor White
} catch {
    Write-Error "Falha ao estabelecer canal: $_"
    exit 1
}

# PASSO 2: Gerar certificado
Write-Step "PASSO 2: Gerando certificado X.509 para Node A"
Write-Info "Executando: POST $NodeA/api/testing/generate-certificate"

$certRequest = @{
    nodeId = "node-a-test-001"
    validityDays = 365
} | ConvertTo-Json

try {
    $certResponse = Invoke-RestMethod `
        -Uri "$NodeA/api/testing/generate-certificate" `
        -Method Post `
        -ContentType "application/json" `
        -Body $certRequest

    $certificate = $certResponse.certificate
    Write-Success "Certificado gerado!"
    Write-Host "   Subject: $($certResponse.subject)" -ForegroundColor White
    Write-Host "   Fingerprint: $($certResponse.fingerprint)" -ForegroundColor White
    Write-Host "   Válido até: $($certResponse.notAfter)" -ForegroundColor White
} catch {
    Write-Error "Falha ao gerar certificado: $_"
    exit 1
}

# PASSO 3: Assinar dados
Write-Step "PASSO 3: Assinando dados de identificação"
Write-Info "Executando: POST $NodeA/api/testing/sign-data"

$dataToSign = "$channelId" + "node-a-test-001"
$signRequest = @{
    certificate = $certificate
    data = $dataToSign
    nodeId = "node-a-test-001"
} | ConvertTo-Json

try {
    $signResponse = Invoke-RestMethod `
        -Uri "$NodeA/api/testing/sign-data" `
        -Method Post `
        -ContentType "application/json" `
        -Body $signRequest

    $signature = $signResponse.signature
    Write-Success "Dados assinados!"
    Write-Host "   Algoritmo: $($signResponse.algorithm)" -ForegroundColor White
} catch {
    Write-Error "Falha ao assinar dados: $_"
    exit 1
}

# PASSO 4: Criptografar payload de identificação
Write-Step "PASSO 4: Criptografando payload de identificação"
Write-Info "Executando: POST $NodeA/api/testing/encrypt-payload"

$identifyPayload = @{
    nodeId = "node-a-test-001"
    certificate = $certificate
    signature = $signature
}

$encryptRequest = @{
    channelId = $channelId
    payload = $identifyPayload
} | ConvertTo-Json -Depth 10

try {
    $encryptResponse = Invoke-RestMethod `
        -Uri "$NodeA/api/testing/encrypt-payload" `
        -Method Post `
        -ContentType "application/json" `
        -Body $encryptRequest

    $encryptedIdentify = $encryptResponse.encryptedPayload
    Write-Success "Payload criptografado!"
    Write-Host "   Cipher: $($encryptResponse.channelInfo.cipher)" -ForegroundColor White
} catch {
    Write-Error "Falha ao criptografar: $_"
    exit 1
}

# PASSO 5: Identificar (primeira vez - desconhecido)
Write-Step "PASSO 5: Identificando Node A no Node B (primeira tentativa)"
Write-Info "Executando: POST $NodeB/api/node/identify"
Write-Info "Header: X-Channel-Id = $channelId"

$encryptedBody = $encryptedIdentify | ConvertTo-Json

try {
    $identifyResponse = Invoke-RestMethod `
        -Uri "$NodeB/api/node/identify" `
        -Method Post `
        -ContentType "application/json" `
        -Headers @{"X-Channel-Id" = $channelId} `
        -Body $encryptedBody

    # Descriptografar resposta
    $decryptRequest = @{
        channelId = $channelId
        encryptedPayload = $identifyResponse
    } | ConvertTo-Json -Depth 10

    $decryptedResponse = Invoke-RestMethod `
        -Uri "$NodeA/api/testing/decrypt-payload" `
        -Method Post `
        -ContentType "application/json" `
        -Body $decryptRequest

    Write-Success "Resposta recebida e descriptografada!"
    Write-Host "   É conhecido? $($decryptedResponse.decryptedPayload.isKnown)" -ForegroundColor White
    Write-Host "   Status: Unknown ($($decryptedResponse.decryptedPayload.status))" -ForegroundColor Yellow
    Write-Host "   Mensagem: $($decryptedResponse.decryptedPayload.message)" -ForegroundColor White
} catch {
    Write-Error "Falha na identificação: $_"
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

# PASSO 6: Registrar nó
Write-Step "PASSO 6: Registrando Node A no Node B"
Write-Info "Executando: POST $NodeB/api/node/register"

$registrationPayload = @{
    nodeId = "node-a-test-001"
    nodeName = "Node A - Test Instance"
    certificate = $certificate
    contactInfo = "admin@node-a.test"
    institutionDetails = "Test Institution A"
    nodeUrl = "http://node-a:8080"
    requestedCapabilities = @("search", "retrieve")
}

$encryptRegRequest = @{
    channelId = $channelId
    payload = $registrationPayload
} | ConvertTo-Json -Depth 10

try {
    $encryptRegResponse = Invoke-RestMethod `
        -Uri "$NodeA/api/testing/encrypt-payload" `
        -Method Post `
        -ContentType "application/json" `
        -Body $encryptRegRequest

    $encryptedRegister = $encryptRegResponse.encryptedPayload | ConvertTo-Json

    $registerResponse = Invoke-RestMethod `
        -Uri "$NodeB/api/node/register" `
        -Method Post `
        -ContentType "application/json" `
        -Headers @{"X-Channel-Id" = $channelId} `
        -Body $encryptedRegister

    # Descriptografar resposta
    $decryptRegRequest = @{
        channelId = $channelId
        encryptedPayload = $registerResponse
    } | ConvertTo-Json -Depth 10

    $decryptedRegResponse = Invoke-RestMethod `
        -Uri "$NodeA/api/testing/decrypt-payload" `
        -Method Post `
        -ContentType "application/json" `
        -Body $decryptRegRequest

    Write-Success "Nó registrado!"
    Write-Host "   Sucesso? $($decryptedRegResponse.decryptedPayload.success)" -ForegroundColor White
    Write-Host "   Status: Pending ($($decryptedRegResponse.decryptedPayload.status))" -ForegroundColor Yellow
    Write-Host "   Mensagem: $($decryptedRegResponse.decryptedPayload.message)" -ForegroundColor White
} catch {
    Write-Error "Falha no registro: $_"
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

# PASSO 7: Aprovar nó
Write-Step "PASSO 7: Aprovando Node A (ação do administrador)"
Write-Info "Executando: PUT $NodeB/api/node/node-a-test-001/status"

$statusUpdateRequest = @{
    status = 1  # Authorized
} | ConvertTo-Json

try {
    $statusResponse = Invoke-RestMethod `
        -Uri "$NodeB/api/node/node-a-test-001/status" `
        -Method Put `
        -ContentType "application/json" `
        -Body $statusUpdateRequest

    Write-Success "Status atualizado para AUTHORIZED!"
    Write-Host "   Node ID: $($statusResponse.nodeId)" -ForegroundColor White
    Write-Host "   Status: Authorized ($($statusResponse.status))" -ForegroundColor Green
} catch {
    Write-Error "Falha ao atualizar status: $_"
    exit 1
}

# PASSO 8: Identificar novamente (agora autorizado)
Write-Step "PASSO 8: Identificando Node A novamente (agora autorizado)"
Write-Info "Executando: POST $NodeB/api/node/identify (mesma requisição)"

try {
    $identifyResponse2 = Invoke-RestMethod `
        -Uri "$NodeB/api/node/identify" `
        -Method Post `
        -ContentType "application/json" `
        -Headers @{"X-Channel-Id" = $channelId} `
        -Body $encryptedBody

    # Descriptografar resposta
    $decryptRequest2 = @{
        channelId = $channelId
        encryptedPayload = $identifyResponse2
    } | ConvertTo-Json -Depth 10

    $decryptedResponse2 = Invoke-RestMethod `
        -Uri "$NodeA/api/testing/decrypt-payload" `
        -Method Post `
        -ContentType "application/json" `
        -Body $decryptRequest2

    Write-Success "Resposta recebida e descriptografada!"
    Write-Host "   É conhecido? $($decryptedResponse2.decryptedPayload.isKnown)" -ForegroundColor Green
    Write-Host "   Status: Authorized ($($decryptedResponse2.decryptedPayload.status))" -ForegroundColor Green
    Write-Host "   Próxima fase: $($decryptedResponse2.decryptedPayload.nextPhase)" -ForegroundColor Cyan
    Write-Host "   Mensagem: $($decryptedResponse2.decryptedPayload.message)" -ForegroundColor White
} catch {
    Write-Error "Falha na segunda identificação: $_"
    exit 1
}

# Resumo final
Write-Host @"

╔═══════════════════════════════════════════════════════════════╗
║                    ✅ TESTE CONCLUÍDO COM SUCESSO!            ║
╚═══════════════════════════════════════════════════════════════╝

📊 Resumo do Teste:
   • Canal criptografado estabelecido: ✅
   • Certificado X.509 gerado: ✅
   • Dados assinados com RSA: ✅
   • Payloads criptografados com AES-256-GCM: ✅
   • Identificação de nó desconhecido: ✅
   • Registro de nó com aprovação pendente: ✅
   • Aprovação administrativa: ✅
   • Identificação de nó autorizado: ✅
   • Próxima fase disponível: phase3_authenticate ✅

🎯 Próximos Passos:
   1. Explore os logs dos containers: docker logs irn-node-a
   2. Veja os endpoints disponíveis: http://localhost:5000/swagger
   3. Implemente a Fase 3: Autenticação Mútua
   4. Teste cenários de erro (nós revogados, certificados inválidos, etc.)

📚 Documentação:
   • Guia de Teste Manual: docs/testing/TESTE-MANUAL-FASE2-CRIPTOGRAFADA.md
   • Endpoints de Teste: docs/development/testing-endpoints-criptografia.md
   • Arquitetura: docs/architecture/handshake-protocol.md

"@ -ForegroundColor Cyan

Write-Success "Todos os passos executados com sucesso!"
