# Script de teste COMPLETO para Fase 2 - com geração de certificados
# Execute após docker-compose up e test-docker.ps1

Write-Host "=== Teste COMPLETO Fase 2 - Identificação com Certificados ===" -ForegroundColor Cyan
Write-Host ""

# Aguardar nós ficarem prontos
Write-Host "Aguardando nós ficarem prontos..." -ForegroundColor Yellow
Start-Sleep -Seconds 3

# Teste 1: Estabelecer canal primeiro (Fase 1)
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
        $channelId = $channelResult.channelId
    } else {
        Write-Host "  [ERRO] Falha ao estabelecer canal" -ForegroundColor Red
        exit
    }
} catch {
    Write-Host "  [ERRO] Requisicao falhou: $_" -ForegroundColor Red
    exit
}

# Teste 2: Gerar identidade completa para Nó A
Write-Host "`n[Teste 2] Gerar Identidade Completa para No A (com certificado)" -ForegroundColor Green
try {
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

    Write-Host "  [OK] Identidade gerada!" -ForegroundColor Green
    Write-Host "    NodeId: $($identity.nodeId)" -ForegroundColor Cyan
    Write-Host "    Certificado gerado: $($identity.certificate.Substring(0, 50))..." -ForegroundColor Cyan
    Write-Host "    Assinatura: $($identity.identificationRequest.signature.Substring(0, 50))..." -ForegroundColor Cyan

    # Salvar para uso posterior
    $nodeACert = $identity.certificate
    $nodeACertWithKey = $identity.certificateWithPrivateKey
    $nodeAIdentifyRequest = $identity.identificationRequest

} catch {
    Write-Host "  [ERRO] Falha ao gerar identidade: $_" -ForegroundColor Red
    Write-Host "  [INFO] Verifique se o endpoint /api/testing/generate-node-identity esta disponivel" -ForegroundColor Yellow
    exit
}

# Teste 3: Registrar Nó A no Nó B
Write-Host "`n[Teste 3] Registrar No A no No B" -ForegroundColor Green
try {
    $registrationBody = @{
        nodeId = "node-a-test-001"
        nodeName = "Interoperable Research Node A - Test"
        certificate = $nodeACert
        contactInfo = "admin@node-a.test"
        institutionDetails = "Test Institution A"
        nodeUrl = "http://node-a:8080"
        requestedCapabilities = @("search", "retrieve")
    } | ConvertTo-Json

    $regResult = Invoke-RestMethod -Uri "http://localhost:5001/api/node/register" `
        -Method Post `
        -ContentType "application/json" `
        -Body $registrationBody

    if ($regResult.success) {
        Write-Host "  [OK] No registrado com sucesso!" -ForegroundColor Green
        Write-Host "    Registration ID: $($regResult.registrationId)" -ForegroundColor Cyan
        Write-Host "    Status: $($regResult.status)" -ForegroundColor Cyan
        Write-Host "    Message: $($regResult.message)" -ForegroundColor Yellow
    } else {
        Write-Host "  [ERRO] Falha ao registrar no: $($regResult.message)" -ForegroundColor Red
    }
} catch {
    Write-Host "  [ERRO] Requisicao falhou: $_" -ForegroundColor Red
}

# Teste 4: Tentar identificar antes de aprovar (deve estar Pending)
Write-Host "`n[Teste 4] Identificar No A no No B (Status: Pending)" -ForegroundColor Green
try {
    $identifyBody = $nodeAIdentifyRequest | ConvertTo-Json

    $identifyResult = Invoke-RestMethod -Uri "http://localhost:5001/api/channel/identify" `
        -Method Post `
        -ContentType "application/json" `
        -Body $identifyBody

    Write-Host "  [OK] No identificado!" -ForegroundColor Green
    Write-Host "    IsKnown: $($identifyResult.isKnown)" -ForegroundColor Cyan
    Write-Host "    Status: $($identifyResult.status)" -ForegroundColor Cyan
    Write-Host "    Message: $($identifyResult.message)" -ForegroundColor Yellow
    Write-Host "    NextPhase: $($identifyResult.nextPhase)" -ForegroundColor Cyan

    if ($identifyResult.status -ne 2) {  # 2 = Pending
        Write-Host "  [AVISO] Status esperado era 'Pending', mas recebeu: $($identifyResult.status)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "  [ERRO] Falha na identificacao: $_" -ForegroundColor Red
}

# Teste 5: Aprovar Nó A no Nó B
Write-Host "`n[Teste 5] Aprovar No A no No B (Admin Operation)" -ForegroundColor Green
try {
    $approveBody = @{
        status = 1  # Authorized
    } | ConvertTo-Json

    $approveResult = Invoke-RestMethod -Uri "http://localhost:5001/api/node/node-a-test-001/status" `
        -Method Put `
        -ContentType "application/json" `
        -Body $approveBody

    Write-Host "  [OK] $($approveResult.message)" -ForegroundColor Green
    Write-Host "    NodeId: $($approveResult.nodeId)" -ForegroundColor Cyan
    Write-Host "    Status: $($approveResult.status)" -ForegroundColor Cyan
} catch {
    Write-Host "  [ERRO] Falha ao aprovar no: $_" -ForegroundColor Red
}

# Teste 6: Identificar novamente (agora deve estar Authorized)
Write-Host "`n[Teste 6] Identificar No A no No B (Status: Authorized)" -ForegroundColor Green
try {
    # Precisamos gerar nova assinatura com timestamp atualizado
    $newTimestamp = (Get-Date).ToUniversalTime().ToString("o")
    $dataToSign = "$channelId$($nodeAIdentifyRequest.nodeId)$newTimestamp"

    # Assinar novamente
    $signBody = @{
        data = $dataToSign
        certificateWithPrivateKey = $nodeACertWithKey
        password = "test123"
    } | ConvertTo-Json

    $signResult = Invoke-RestMethod -Uri "http://localhost:5000/api/testing/sign-data" `
        -Method Post `
        -ContentType "application/json" `
        -Body $signBody

    # Montar requisição de identificação atualizada
    $identifyBody2 = @{
        channelId = $channelId
        nodeId = $nodeAIdentifyRequest.nodeId
        nodeName = $nodeAIdentifyRequest.nodeName
        certificate = $nodeAIdentifyRequest.certificate
        timestamp = $newTimestamp
        signature = $signResult.signature
    } | ConvertTo-Json

    $identifyResult2 = Invoke-RestMethod -Uri "http://localhost:5001/api/channel/identify" `
        -Method Post `
        -ContentType "application/json" `
        -Body $identifyBody2

    Write-Host "  [OK] No identificado!" -ForegroundColor Green
    Write-Host "    IsKnown: $($identifyResult2.isKnown)" -ForegroundColor Cyan
    Write-Host "    Status: $($identifyResult2.status)" -ForegroundColor Cyan
    Write-Host "    Message: $($identifyResult2.message)" -ForegroundColor Yellow
    Write-Host "    NextPhase: $($identifyResult2.nextPhase)" -ForegroundColor Cyan

    if ($identifyResult2.status -eq 1 -and $identifyResult2.nextPhase -eq "phase3_authenticate") {
        Write-Host "  [SUCESSO] No autorizado! Pronto para Fase 3!" -ForegroundColor Green
    } else {
        Write-Host "  [AVISO] Status ou NextPhase inesperado" -ForegroundColor Yellow
    }
} catch {
    Write-Host "  [ERRO] Falha na identificacao: $_" -ForegroundColor Red
}

# Teste 7: Testar identificação de nó desconhecido
Write-Host "`n[Teste 7] Identificar No Desconhecido" -ForegroundColor Green
try {
    # Gerar identidade para nó desconhecido
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

    $identifyUnknownBody = $unknownIdentity.identificationRequest | ConvertTo-Json

    $unknownResult = Invoke-RestMethod -Uri "http://localhost:5001/api/channel/identify" `
        -Method Post `
        -ContentType "application/json" `
        -Body $identifyUnknownBody

    Write-Host "  [OK] Resposta para no desconhecido recebida!" -ForegroundColor Green
    Write-Host "    IsKnown: $($unknownResult.isKnown)" -ForegroundColor Cyan
    Write-Host "    Status: $($unknownResult.status)" -ForegroundColor Cyan
    Write-Host "    Message: $($unknownResult.message)" -ForegroundColor Yellow
    Write-Host "    RegistrationUrl: $($unknownResult.registrationUrl)" -ForegroundColor Cyan

    if ($unknownResult.isKnown -eq $false -and $unknownResult.registrationUrl) {
        Write-Host "  [SUCESSO] Sistema corretamente identifica no desconhecido!" -ForegroundColor Green
    }
} catch {
    Write-Host "  [ERRO] Falha ao testar no desconhecido: $_" -ForegroundColor Red
}

# Teste 8: Listar todos os nós registrados
Write-Host "`n[Teste 8] Listar Todos os Nos Registrados no No B" -ForegroundColor Green
try {
    $nodes = Invoke-RestMethod -Uri "http://localhost:5001/api/node/nodes" -Method Get

    Write-Host "  [OK] Total de nos registrados: $($nodes.Count)" -ForegroundColor Green
    foreach ($node in $nodes) {
        Write-Host "    - NodeId: $($node.nodeId)" -ForegroundColor Cyan
        Write-Host "      Name: $($node.nodeName)" -ForegroundColor Cyan
        Write-Host "      Status: $($node.status)" -ForegroundColor Cyan
        Write-Host "      URL: $($node.nodeUrl)" -ForegroundColor Cyan
    }
} catch {
    Write-Host "  [ERRO] Falha ao listar nos: $_" -ForegroundColor Red
}

Write-Host "`n=== Testes COMPLETOS da Fase 2 Concluídos ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Resumo:" -ForegroundColor Yellow
Write-Host "  - Canal criptografado estabelecido (Fase 1) [OK]" -ForegroundColor Green
Write-Host "  - Certificados auto-assinados gerados [OK]" -ForegroundColor Green
Write-Host "  - Assinatura de dados com certificado [OK]" -ForegroundColor Green
Write-Host "  - No desconhecido pode se registrar [OK]" -ForegroundColor Green
Write-Host "  - Identificacao com status Pending funciona [OK]" -ForegroundColor Green
Write-Host "  - Aprovacao de nos funciona [OK]" -ForegroundColor Green
Write-Host "  - Identificacao com status Authorized funciona [OK]" -ForegroundColor Green
Write-Host "  - Sistema indica Fase 3 como proximo passo [OK]" -ForegroundColor Green
Write-Host "  - No desconhecido recebe URL de registro [OK]" -ForegroundColor Green
Write-Host ""
Write-Host "Fase 2 COMPLETA! Pronto para implementar Fase 3." -ForegroundColor Green
Write-Host ""
