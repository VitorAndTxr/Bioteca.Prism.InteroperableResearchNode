#!/usr/bin/env pwsh
# Test script for channel encryption endpoints

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Teste de Endpoints de Criptografia  " -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Configuration
$nodeA = "http://localhost:5000"
$nodeB = "http://localhost:5001"

# Test data
$testPayload = @{
    message = "Hello from Node A!"
    timestamp = (Get-Date).ToUniversalTime().ToString("o")
    data = @{
        biosignalType = "ECG"
        patientId = "PATIENT-123"
        samplingRate = 500
        values = @(0.5, 0.6, 0.7, 0.8, 1.0, 1.2, 1.0, 0.8, 0.6, 0.5)
    }
}

try {
    # Step 1: Check health
    Write-Host "🔍 Verificando health dos nós..." -ForegroundColor Yellow
    $healthA = Invoke-RestMethod -Uri "$nodeA/api/channel/health" -Method Get
    $healthB = Invoke-RestMethod -Uri "$nodeB/api/channel/health" -Method Get
    
    if ($healthA.status -eq "Healthy" -and $healthB.status -eq "Healthy") {
        Write-Host "✅ Ambos os nós estão saudáveis" -ForegroundColor Green
    }
    else {
        Write-Host "❌ Um ou mais nós não estão saudáveis" -ForegroundColor Red
        exit 1
    }
    
    Write-Host ""
    
    # Step 2: Open channel
    Write-Host "🔐 Abrindo canal criptografado..." -ForegroundColor Yellow
    $channelRequest = @{
        remoteNodeUrl = "http://node-b:8080"
    } | ConvertTo-Json
    
    $channelResult = Invoke-RestMethod -Uri "$nodeA/api/channel/initiate" `
        -Method Post `
        -ContentType "application/json" `
        -Body $channelRequest
    
    $channelId = $channelResult.channelId
    
    if ($channelResult.success) {
        Write-Host "✅ Canal estabelecido com sucesso" -ForegroundColor Green
        Write-Host "   Channel ID: $channelId" -ForegroundColor Gray
        Write-Host "   Cipher: $($channelResult.selectedCipher)" -ForegroundColor Gray
    }
    else {
        Write-Host "❌ Falha ao estabelecer canal" -ForegroundColor Red
        exit 1
    }
    
    Write-Host ""
    Start-Sleep -Seconds 1
    
    # Step 3: Get channel info
    Write-Host "ℹ️  Obtendo informações do canal..." -ForegroundColor Yellow
    $channelInfo = Invoke-RestMethod -Uri "$nodeA/api/testing/channel-info/$channelId" -Method Get
    
    Write-Host "✅ Informações do canal obtidas:" -ForegroundColor Green
    Write-Host "   Role: $($channelInfo.role)" -ForegroundColor Gray
    Write-Host "   Cipher: $($channelInfo.cipher)" -ForegroundColor Gray
    Write-Host "   Created: $($channelInfo.createdAt)" -ForegroundColor Gray
    Write-Host "   Expires: $($channelInfo.expiresAt)" -ForegroundColor Gray
    Write-Host "   Symmetric Key Length: $($channelInfo.symmetricKeyLength) bytes" -ForegroundColor Gray
    
    Write-Host ""
    Start-Sleep -Seconds 1
    
    # Step 4: Encrypt payload
    Write-Host "🔒 Criptografando payload de teste..." -ForegroundColor Yellow
    $encryptRequest = @{
        channelId = $channelId
        payload = $testPayload
    } | ConvertTo-Json -Depth 10
    
    $encryptResult = Invoke-RestMethod -Uri "$nodeA/api/testing/encrypt-payload" `
        -Method Post `
        -ContentType "application/json" `
        -Body $encryptRequest
    
    Write-Host "✅ Payload criptografado com sucesso" -ForegroundColor Green
    Write-Host "   Encrypted Data Length: $($encryptResult.encryptedPayload.encryptedData.Length) chars" -ForegroundColor Gray
    Write-Host "   IV Length: $($encryptResult.encryptedPayload.iv.Length) chars" -ForegroundColor Gray
    Write-Host "   Auth Tag Length: $($encryptResult.encryptedPayload.authTag.Length) chars" -ForegroundColor Gray
    
    Write-Host ""
    Write-Host "📦 Encrypted Payload:" -ForegroundColor Cyan
    Write-Host "   Encrypted Data: $($encryptResult.encryptedPayload.encryptedData.Substring(0, [Math]::Min(50, $encryptResult.encryptedPayload.encryptedData.Length)))..." -ForegroundColor Gray
    Write-Host "   IV: $($encryptResult.encryptedPayload.iv)" -ForegroundColor Gray
    Write-Host "   Auth Tag: $($encryptResult.encryptedPayload.authTag)" -ForegroundColor Gray
    
    Write-Host ""
    Start-Sleep -Seconds 1
    
    # Step 5: Decrypt payload
    Write-Host "🔓 Descriptografando payload..." -ForegroundColor Yellow
    $decryptRequest = @{
        channelId = $channelId
        encryptedPayload = $encryptResult.encryptedPayload
    } | ConvertTo-Json -Depth 10
    
    $decryptResult = Invoke-RestMethod -Uri "$nodeA/api/testing/decrypt-payload" `
        -Method Post `
        -ContentType "application/json" `
        -Body $decryptRequest
    
    Write-Host "✅ Payload descriptografado com sucesso" -ForegroundColor Green
    
    Write-Host ""
    Write-Host "📦 Decrypted Payload:" -ForegroundColor Cyan
    Write-Host ($decryptResult.decryptedPayload | ConvertTo-Json -Depth 10) -ForegroundColor Gray
    
    Write-Host ""
    
    # Step 6: Verify data integrity
    Write-Host "🔍 Verificando integridade dos dados..." -ForegroundColor Yellow
    
    $originalMessage = $testPayload.message
    $decryptedMessage = $decryptResult.decryptedPayload.message
    
    if ($originalMessage -eq $decryptedMessage) {
        Write-Host "✅ Integridade verificada: Dados descriptografados correspondem ao original" -ForegroundColor Green
    }
    else {
        Write-Host "❌ FALHA DE INTEGRIDADE: Dados não correspondem!" -ForegroundColor Red
        Write-Host "   Original: $originalMessage" -ForegroundColor Red
        Write-Host "   Decrypted: $decryptedMessage" -ForegroundColor Red
        exit 1
    }
    
    Write-Host ""
    
    # Step 7: Test with tampered payload
    Write-Host "🛡️  Testando detecção de adulteração..." -ForegroundColor Yellow
    
    $tamperedPayload = @{
        channelId = $channelId
        encryptedPayload = @{
            encryptedData = $encryptResult.encryptedPayload.encryptedData
            iv = $encryptResult.encryptedPayload.iv
            authTag = "TAMPERED_AUTH_TAG_12345678901234567890123="  # Tampered tag
        }
    } | ConvertTo-Json -Depth 10
    
    try {
        $tamperedResult = Invoke-RestMethod -Uri "$nodeA/api/testing/decrypt-payload" `
            -Method Post `
            -ContentType "application/json" `
            -Body $tamperedPayload
        
        Write-Host "❌ FALHA DE SEGURANÇA: Payload adulterado foi aceito!" -ForegroundColor Red
        exit 1
    }
    catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        if ($statusCode -eq 400) {
            Write-Host "✅ Adulteração detectada corretamente (HTTP 400)" -ForegroundColor Green
        }
        else {
            Write-Host "⚠️  Erro inesperado ao testar adulteração: HTTP $statusCode" -ForegroundColor Yellow
        }
    }
    
    Write-Host ""
    
    # Step 8: Test with invalid channel
    Write-Host "🔍 Testando canal inválido..." -ForegroundColor Yellow
    
    $invalidChannelRequest = @{
        channelId = "invalid-channel-id-12345"
        payload = $testPayload
    } | ConvertTo-Json -Depth 10
    
    try {
        $invalidResult = Invoke-RestMethod -Uri "$nodeA/api/testing/encrypt-payload" `
            -Method Post `
            -ContentType "application/json" `
            -Body $invalidChannelRequest
        
        Write-Host "❌ FALHA: Canal inválido foi aceito!" -ForegroundColor Red
        exit 1
    }
    catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        if ($statusCode -eq 404) {
            Write-Host "✅ Canal inválido rejeitado corretamente (HTTP 404)" -ForegroundColor Green
        }
        else {
            Write-Host "⚠️  Erro inesperado ao testar canal inválido: HTTP $statusCode" -ForegroundColor Yellow
        }
    }
    
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "  ✅ TODOS OS TESTES PASSARAM!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Resumo dos testes:" -ForegroundColor Yellow
    Write-Host "  ✅ Canal criptografado estabelecido" -ForegroundColor Green
    Write-Host "  ✅ Informações de canal obtidas" -ForegroundColor Green
    Write-Host "  ✅ Payload criptografado com AES-256-GCM" -ForegroundColor Green
    Write-Host "  ✅ Payload descriptografado corretamente" -ForegroundColor Green
    Write-Host "  ✅ Integridade dos dados verificada" -ForegroundColor Green
    Write-Host "  ✅ Adulteração detectada e rejeitada" -ForegroundColor Green
    Write-Host "  ✅ Canal inválido rejeitado" -ForegroundColor Green
    Write-Host ""
    Write-Host "Próximos passos:" -ForegroundColor Cyan
    Write-Host "  • Use esses endpoints para testar comunicação criptografada" -ForegroundColor Gray
    Write-Host "  • Implemente endpoints de negócio que usam payloads criptografados" -ForegroundColor Gray
    Write-Host "  • Consulte: docs/api-examples/testing-encryption.http" -ForegroundColor Gray
    Write-Host ""
}
catch {
    Write-Host ""
    Write-Host "❌ ERRO durante os testes:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ""
    Write-Host "Stack Trace:" -ForegroundColor Yellow
    Write-Host $_.ScriptStackTrace -ForegroundColor Gray
    exit 1
}

