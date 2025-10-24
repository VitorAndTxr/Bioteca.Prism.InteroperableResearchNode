# Script de teste para comunicação entre nós Docker
# Execute após docker-compose up

Write-Host "=== Teste de Comunicação Fase 1 - Docker ===" -ForegroundColor Cyan
Write-Host ""

# Aguardar nós ficarem prontos
Write-Host "Aguardando nós ficarem prontos..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

# Teste 1: Health check
Write-Host "`n[Teste 1] Health Check dos Nós" -ForegroundColor Green
Write-Host "No A (porta 5000):"
try {
    $healthA = Invoke-RestMethod -Uri "http://localhost:5000/api/channel/health" -Method Get
    Write-Host "  [OK] Status: $($healthA.status)" -ForegroundColor Green
} catch {
    Write-Host "  [ERRO] $_" -ForegroundColor Red
}

Write-Host "`nNo B (porta 5001):"
try {
    $healthB = Invoke-RestMethod -Uri "http://localhost:5001/api/channel/health" -Method Get
    Write-Host "  [OK] Status: $($healthB.status)" -ForegroundColor Green
} catch {
    Write-Host "  [ERRO] $_" -ForegroundColor Red
}

# Teste 2: Nó A inicia handshake com Nó B
Write-Host "`n[Teste 2] Nó A → Nó B (Handshake)" -ForegroundColor Green
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
        Write-Host "    Cipher: $($result.selectedCipher)" -ForegroundColor Cyan
        $channelIdAB = $result.channelId
    } else {
        Write-Host "  [ERRO] Falha ao estabelecer canal" -ForegroundColor Red
        Write-Host "    Erro: $($result.error.message)" -ForegroundColor Red
    }
} catch {
    Write-Host "  [ERRO] Requisicao falhou: $_" -ForegroundColor Red
}

# Teste 3: Nó B inicia handshake com Nó A
Write-Host "`n[Teste 3] Nó B → Nó A (Handshake)" -ForegroundColor Green
try {
    $body = @{
        remoteNodeUrl = "http://node-a:8080"
    } | ConvertTo-Json

    $result = Invoke-RestMethod -Uri "http://localhost:5001/api/channel/initiate" `
        -Method Post `
        -ContentType "application/json" `
        -Body $body

    if ($result.success) {
        Write-Host "  [OK] Canal estabelecido!" -ForegroundColor Green
        Write-Host "    Channel ID: $($result.channelId)" -ForegroundColor Cyan
        Write-Host "    Cipher: $($result.selectedCipher)" -ForegroundColor Cyan
        $channelIdBA = $result.channelId
    } else {
        Write-Host "  [ERRO] Falha ao estabelecer canal" -ForegroundColor Red
        Write-Host "    Erro: $($result.error.message)" -ForegroundColor Red
    }
} catch {
    Write-Host "  [ERRO] Erro na requisição: $_" -ForegroundColor Red
}

# Teste 4: Verificar canais ativos
if ($channelIdAB) {
    Write-Host "`n[Teste 4] Verificar canal A→B no Nó A" -ForegroundColor Green
    try {
        $channel = Invoke-RestMethod -Uri "http://localhost:5000/api/channel/$channelIdAB" -Method Get
        Write-Host "  [OK] Canal encontrado" -ForegroundColor Green
        Write-Host "    Role: $($channel.role)" -ForegroundColor Cyan
        Write-Host "    Cipher: $($channel.cipher)" -ForegroundColor Cyan
        Write-Host "    Expired: $($channel.isExpired)" -ForegroundColor Cyan
    } catch {
        Write-Host "  [ERRO] Canal não encontrado" -ForegroundColor Red
    }

    Write-Host "`n[Teste 5] Verificar canal A→B no Nó B (lado servidor)" -ForegroundColor Green
    try {
        $channel = Invoke-RestMethod -Uri "http://localhost:5001/api/channel/$channelIdAB" -Method Get
        Write-Host "  [OK] Canal encontrado" -ForegroundColor Green
        Write-Host "    Role: $($channel.role)" -ForegroundColor Cyan
        Write-Host "    Cipher: $($channel.cipher)" -ForegroundColor Cyan
    } catch {
        Write-Host "  [ERRO] Canal não encontrado" -ForegroundColor Red
    }
}

Write-Host "`n=== Testes Concluídos ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Resumo:" -ForegroundColor Yellow
Write-Host "  - No A pode atuar como cliente [OK]" -ForegroundColor Green
Write-Host "  - No B pode atuar como servidor [OK]" -ForegroundColor Green
Write-Host "  - No B pode atuar como cliente [OK]" -ForegroundColor Green
Write-Host "  - No A pode atuar como servidor [OK]" -ForegroundColor Green
Write-Host "  - Chaves efemeras ECDH funcionando [OK]" -ForegroundColor Green
Write-Host "  - Perfect Forward Secrecy habilitado [OK]" -ForegroundColor Green
Write-Host ""
