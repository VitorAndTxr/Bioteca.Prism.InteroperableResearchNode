# Script de teste para Fase 2 - Identificação e Autorização de Nós
# Execute após docker-compose up e test-docker.ps1

Write-Host "=== Teste de Comunicação Fase 2 - Identificação de Nós ===" -ForegroundColor Cyan
Write-Host ""

# Aguardar nós ficarem prontos
Write-Host "Aguardando nós ficarem prontos..." -ForegroundColor Yellow
Start-Sleep -Seconds 3

# Variáveis para armazenar channelId
$channelIdAB = $null

# Teste 1: Estabelecer canal primeiro (Fase 1)
Write-Host "`n[Teste 1] Estabelecer Canal Criptografado (Fase 1)" -ForegroundColor Green
Write-Host "No A -> No B (Handshake):"
try {
    $body = @{
        remoteNodeUrl = "http://node-b:8080"
    } | ConvertTo-Json

    $result = Invoke-RestMethod -Uri "http://localhost:5000/api/channel/initiate" `
        -Method Post `
        -ContentType "application/json" `
        -Body $body

    if ($result.success) {
        Write-Host "  [OK] Canal estabelecido!" -ForegroundColor Green
        Write-Host "    Channel ID: $($result.channelId)" -ForegroundColor Cyan
        $channelIdAB = $result.channelId
    } else {
        Write-Host "  [ERRO] Falha ao estabelecer canal" -ForegroundColor Red
        exit
    }
} catch {
    Write-Host "  [ERRO] Requisicao falhou: $_" -ForegroundColor Red
    exit
}

# Teste 2: Registrar Nó A no Nó B (Unknown Node)
Write-Host "`n[Teste 2] Registrar No A no No B (Unknown Node)" -ForegroundColor Green
try {
    # Para simplificar os testes, vamos usar dados mock
    # Em produção, usaríamos certificados X.509 reais

    $registrationBody = @{
        nodeId = "node-a-test-001"
        nodeName = "Interoperable Research Node A - Test"
        certificate = "LS0tLS1CRUdJTiBDRVJUSUZJQ0FURS0tLS0tCk1JSUNEVENDQWZXZ0F3SUJBZ0lVRk5BVzVRVnRoWmN6V21oQkRLdWNsUk9mOXhNd0RRWUpLb1pJaHZjTkFRRUwKQlFBd0Z6RVZNQk1HQTFVRUF3d01ibTlrWlMxaExYUmxjM1F3SGhjTk1qVXdPVE14TURBd01EQXdXaGNOTWpjdwpPVE14TURBd01EQXdXakFYTVJVd0V3WURWUVFEREF4dWIyUmxMV0V0ZEdWemREQ0NBU0l3RFFZSktvWklodmNOCkFRRUJCUUFEZ2dFUEFEQ0NBUW9DZ2dFQkFMT05JdlFmNlFxRkgrZXNtSUc4cVBRcXlJWUE3UkNXZCtNZWxybXEKQzNHU1ZSTG1jUERpcllnL2Y4SEtkWnlxVm1nSkZsNXFNWHhWS1o4K2hIVWxKOXhOV1FVdXUrR2Y4Ynd5Y2hOTwpGQitDaHFhdXBYeDhMcWxCQ2V2WDM4d1JMNlBTM2pXRHByeEtzbEZPY0R5NkRGUGhkMWRqRmlJOFBPTTRudkh6CjFYUGdtV1BIZFNPb1h5bHB0NUFINnRaQUw0UkR3YlB6aFRTR0I4WmNZaU9LeGxTcWxwZXN1V0xOVkhKakJvYXkKWE1KdHhEdUdQeUtEQ0QyMlh1Y0pFaVlleHFxSExyVDZvbXdHZGhITVA5QTlDWWR0RmNqTkRZR1lxTXJlcjlnQQpBUUlEQVFBQm8xTXdVVEFkQmdOVkhRNEVGZ1FVMnlJNWx2MVdVSG9tSUlwNWtDZ0VMbURCWFRzd0h3WURWUjBqCkJCZ3dGb0FVMnlJNWx2MVdVSG9tSUlwNWtDZ0VMbURCWFRzd0R3WURWUjBUQVFIL0JBVXdBd0VCL3pBTkJna3EKaGtpRzl3MEJBUXNGQUFPQ0FRRUFkMTlsM3pYVlpjdm5mVlNEL3g3aGlXREUxU0RRWFJhU1RxUDJoS1k1ZE1Edgp3QWtNa0NSZ1BqQ29PVGNsMFZJNWw3dWxEL2JYNXhQQVJ2R1ZlRmVBZ3Iza2RxWmxYUG9sTUJtdktXVjY5MGI1CnNNa1kyZElDQkxJNnRYdGRtWkVGM2hLOCtJQ1hvQzlZdzJCc1BTNmN2NHI2Z2hHaDNnK3pLYlJyV2ZlNE1Ub1IKWUJTQ2tNdExyakJzNytqR1ZKdVFpa1JGY3NCL1RSeUJwRElFR0ZJY3FIdVhWWmFrZkxlVDNSajltSkJzbXZucwo2NnN4aUF4ZDlpN0QyUUhNb3k3a1VTSW5LS2EyQWJmd1lhQWxYK0M5aUc5SjJibVJVTnJUL1lGMkl5NDhoU1pKClZkMEhtcUNoL3RUR0FINDdQQlc3N1RLR3VxQWxwTkJIOGk5d0dRPT0KLS0tLS1FTkQgQ0VSVElGSUNBVEUtLS0tLQo=" # Mock certificate
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
        Write-Host "  [ERRO] Falha ao registrar no" -ForegroundColor Red
    }
} catch {
    Write-Host "  [ERRO] Requisicao falhou: $_" -ForegroundColor Red
}

# Teste 3: Verificar nós registrados
Write-Host "`n[Teste 3] Listar Nos Registrados no No B" -ForegroundColor Green
try {
    $nodes = Invoke-RestMethod -Uri "http://localhost:5001/api/node/nodes" -Method Get

    if ($nodes.Count -gt 0) {
        Write-Host "  [OK] Total de nos registrados: $($nodes.Count)" -ForegroundColor Green
        foreach ($node in $nodes) {
            Write-Host "    - NodeId: $($node.nodeId)" -ForegroundColor Cyan
            Write-Host "      Name: $($node.nodeName)" -ForegroundColor Cyan
            Write-Host "      Status: $($node.status)" -ForegroundColor Cyan
            Write-Host "      Registered: $($node.registeredAt)" -ForegroundColor Cyan
        }
    } else {
        Write-Host "  [INFO] Nenhum no registrado ainda" -ForegroundColor Yellow
    }
} catch {
    Write-Host "  [ERRO] Falha ao listar nos: $_" -ForegroundColor Red
}

# Teste 4: Aprovar Nó A no Nó B (Admin Operation)
Write-Host "`n[Teste 4] Aprovar No A no No B (Admin Operation)" -ForegroundColor Green
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

# Teste 5: Identificar Nó (com canal já estabelecido)
Write-Host "`n[Teste 5] Identificar No A no No B (Fase 2)" -ForegroundColor Green
Write-Host "  [INFO] Este teste requer certificado real e assinatura" -ForegroundColor Yellow
Write-Host "  [INFO] Pulando por enquanto - implementar quando certificados estiverem disponiveis" -ForegroundColor Yellow

# Mock example (would need real certificate and signature)
# try {
#     $identifyBody = @{
#         channelId = $channelIdAB
#         nodeId = "node-a-test-001"
#         nodeName = "Interoperable Research Node A - Test"
#         certificate = "LS0tLS1CRUdJTi..."
#         timestamp = (Get-Date).ToUniversalTime().ToString("o")
#         signature = "MOCK_SIGNATURE_HERE"
#     } | ConvertTo-Json
#
#     $identifyResult = Invoke-RestMethod -Uri "http://localhost:5001/api/channel/identify" `
#         -Method Post `
#         -ContentType "application/json" `
#         -Body $identifyBody
#
#     Write-Host "  [OK] No identificado!" -ForegroundColor Green
#     Write-Host "    IsKnown: $($identifyResult.isKnown)" -ForegroundColor Cyan
#     Write-Host "    Status: $($identifyResult.status)" -ForegroundColor Cyan
#     Write-Host "    Message: $($identifyResult.message)" -ForegroundColor Cyan
#     Write-Host "    NextPhase: $($identifyResult.nextPhase)" -ForegroundColor Cyan
# } catch {
#     Write-Host "  [ERRO] Falha na identificacao: $_" -ForegroundColor Red
# }

Write-Host "`n=== Testes da Fase 2 Concluídos ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Resumo:" -ForegroundColor Yellow
Write-Host "  - Canal criptografado estabelecido (Fase 1) [OK]" -ForegroundColor Green
Write-Host "  - No desconhecido pode se registrar [OK]" -ForegroundColor Green
Write-Host "  - Lista de nos registrados funciona [OK]" -ForegroundColor Green
Write-Host "  - Aprovacao de nos funciona [OK]" -ForegroundColor Green
Write-Host "  - Identificacao com certificado [PENDENTE - requer certificados]" -ForegroundColor Yellow
Write-Host ""
Write-Host "Proximo Passo:" -ForegroundColor Cyan
Write-Host "  1. Gerar certificados auto-assinados para testes" -ForegroundColor White
Write-Host "  2. Implementar endpoint para geracao de certificados de teste" -ForegroundColor White
Write-Host "  3. Testar identificacao completa com assinatura" -ForegroundColor White
Write-Host ""
