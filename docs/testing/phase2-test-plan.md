# Plano de Testes - Fase 2: Identificação e Autorização de Nós

Este documento lista todos os testes necessários para validar a Fase 2 do protocolo de handshake.

## Pré-requisitos

- Docker Compose rodando: `docker-compose up`
- Fase 1 validada e funcionando (canal criptografado estabelecido)
- Nó A: http://localhost:5000
- Nó B: http://localhost:5001

## Visão Geral da Fase 2

A Fase 2 implementa a **identificação e autorização de nós** após o canal criptografado estar estabelecido:

1. **Nó desconhecido** → Sistema retorna endpoint de registro
2. **Registro de nó** → Administrador aprova/rejeita
3. **Nó conhecido** → Sistema verifica status de autorização
4. **Nó autorizado** → Avança para Fase 3 (autenticação mútua)

## 1. Testes de Registro de Nós

### 1.1 Registrar Nó Desconhecido
**Objetivo:** Verificar que um nó desconhecido pode se registrar

```powershell
curl -X POST http://localhost:5001/api/node/register `
  -H "Content-Type: application/json" `
  -d '{
    "nodeId": "node-a-test-001",
    "nodeName": "Interoperable Research Node A - Test",
    "certificate": "LS0tLS1CRUdJTi...",
    "contactInfo": "admin@node-a.test",
    "institutionDetails": "Test Institution A",
    "nodeUrl": "http://node-a:8080",
    "requestedCapabilities": ["search", "retrieve"]
  }'
```

**Resultado Esperado:**
```json
{
  "success": true,
  "registrationId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "status": "Pending",
  "message": "Registration received. Pending administrator approval.",
  "estimatedApprovalTime": "1.00:00:00"
}
```

**✅ Validação:**
- `success: true`
- `status: Pending`
- Registro criado no sistema

### 1.2 Tentar Registrar Nó Duplicado
**Objetivo:** Verificar rejeição de NodeId duplicado

```powershell
# Registrar o mesmo nodeId novamente
curl -X POST http://localhost:5001/api/node/register `
  -H "Content-Type: application/json" `
  -d '{
    "nodeId": "node-a-test-001",
    "nodeName": "Duplicate Node",
    "certificate": "LS0tLS1CRUdJTi...",
    "contactInfo": "duplicate@test.com",
    "institutionDetails": "Duplicate Institution",
    "nodeUrl": "http://duplicate:8080",
    "requestedCapabilities": ["search"]
  }'
```

**Resultado Esperado:**
```json
{
  "success": false,
  "status": "Rejected",
  "message": "Node ID already registered"
}
```

### 1.3 Tentar Registrar com Certificado Duplicado
**Objetivo:** Verificar rejeição de certificado já usado

```powershell
curl -X POST http://localhost:5001/api/node/register `
  -H "Content-Type: application/json" `
  -d '{
    "nodeId": "node-different",
    "nodeName": "Different Node",
    "certificate": "LS0tLS1CRUdJTi...",  // Mesmo certificado do teste 1.1
    "contactInfo": "different@test.com",
    "institutionDetails": "Different Institution",
    "nodeUrl": "http://different:8080",
    "requestedCapabilities": ["search"]
  }'
```

**Resultado Esperado:**
```json
{
  "success": false,
  "status": "Rejected",
  "message": "Certificate already registered with another node"
}
```

## 2. Testes de Gerenciamento de Nós (Admin)

### 2.1 Listar Todos os Nós Registrados
**Objetivo:** Verificar listagem de nós

```powershell
curl http://localhost:5001/api/node/nodes
```

**Resultado Esperado:**
```json
[
  {
    "nodeId": "node-a-test-001",
    "nodeName": "Interoperable Research Node A - Test",
    "certificate": "LS0tLS1CRUdJTi...",
    "certificateFingerprint": "abc123...",
    "nodeUrl": "http://node-a:8080",
    "status": "Pending",
    "capabilities": ["search", "retrieve"],
    "contactInfo": "admin@node-a.test",
    "institutionDetails": "Test Institution A",
    "registeredAt": "2025-10-01T...",
    "updatedAt": "2025-10-01T..."
  }
]
```

### 2.2 Aprovar Nó Pendente
**Objetivo:** Administrador aprova nó para uso

```powershell
curl -X PUT http://localhost:5001/api/node/node-a-test-001/status `
  -H "Content-Type: application/json" `
  -d '{"status": 1}'  # 1 = Authorized
```

**Resultado Esperado:**
```json
{
  "message": "Node status updated successfully",
  "nodeId": "node-a-test-001",
  "status": "Authorized"
}
```

### 2.3 Revogar Nó Autorizado
**Objetivo:** Administrador revoga acesso de nó comprometido

```powershell
curl -X PUT http://localhost:5001/api/node/node-a-test-001/status `
  -H "Content-Type: application/json" `
  -d '{"status": 3}'  # 3 = Revoked
```

**Resultado Esperado:**
```json
{
  "message": "Node status updated successfully",
  "nodeId": "node-a-test-001",
  "status": "Revoked"
}
```

## 3. Testes de Identificação de Nós

### 3.1 Identificar Nó Desconhecido
**Objetivo:** Sistema informa que nó é desconhecido e fornece URL de registro

**Pré-requisito:** Canal estabelecido (Fase 1)

```powershell
# 1. Estabelecer canal
$channelResult = Invoke-RestMethod -Uri "http://localhost:5000/api/channel/initiate" `
  -Method Post -ContentType "application/json" `
  -Body '{"remoteNodeUrl": "http://node-b:8080"}'

$channelId = $channelResult.channelId

# 2. Tentar identificar nó desconhecido
curl -X POST http://localhost:5001/api/channel/identify `
  -H "Content-Type: application/json" `
  -d '{
    "channelId": "'$channelId'",
    "nodeId": "unknown-node-999",
    "nodeName": "Unknown Node",
    "certificate": "LS0tLS1CRUdJTi...",
    "timestamp": "2025-10-01T12:00:00Z",
    "signature": "..."
  }'
```

**Resultado Esperado:**
```json
{
  "isKnown": false,
  "status": "Unknown",
  "nodeId": "unknown-node-999",
  "timestamp": "2025-10-01T...",
  "registrationUrl": "http://localhost:5001/api/node/register",
  "message": "Node is not registered. Please register using the provided URL.",
  "nextPhase": null
}
```

### 3.2 Identificar Nó Autorizado
**Objetivo:** Nó autorizado pode prosseguir para Fase 3

**Pré-requisitos:**
- Nó registrado e aprovado (testes 1.1 e 2.2)
- Canal estabelecido (Fase 1)
- Certificado e assinatura válidos

```powershell
curl -X POST http://localhost:5001/api/channel/identify `
  -H "Content-Type: application/json" `
  -d '{
    "channelId": "'$channelId'",
    "nodeId": "node-a-test-001",
    "nodeName": "Interoperable Research Node A - Test",
    "certificate": "LS0tLS1CRUdJTi...",
    "timestamp": "2025-10-01T12:00:00Z",
    "signature": "VALID_SIGNATURE_HERE"
  }'
```

**Resultado Esperado:**
```json
{
  "isKnown": true,
  "status": "Authorized",
  "nodeId": "node-a-test-001",
  "nodeName": "Interoperable Research Node A - Test",
  "timestamp": "2025-10-01T...",
  "message": "Node is authorized. Proceed to Phase 3 (Mutual Authentication).",
  "nextPhase": "phase3_authenticate",
  "registrationUrl": null
}
```

### 3.3 Identificar Nó Pendente
**Objetivo:** Nó pendente não pode prosseguir até aprovação

```powershell
curl -X POST http://localhost:5001/api/channel/identify `
  -H "Content-Type: application/json" `
  -d '{
    "channelId": "'$channelId'",
    "nodeId": "node-pending-002",
    "nodeName": "Pending Node",
    "certificate": "LS0tLS1CRUdJTi...",
    "timestamp": "2025-10-01T12:00:00Z",
    "signature": "..."
  }'
```

**Resultado Esperado:**
```json
{
  "isKnown": true,
  "status": "Pending",
  "nodeId": "node-pending-002",
  "nodeName": "Pending Node",
  "timestamp": "2025-10-01T...",
  "message": "Node registration is pending approval.",
  "nextPhase": null,
  "registrationUrl": null
}
```

### 3.4 Identificar Nó Revogado
**Objetivo:** Nó revogado é bloqueado

```powershell
curl -X POST http://localhost:5001/api/channel/identify `
  -H "Content-Type: application/json" `
  -d '{
    "channelId": "'$channelId'",
    "nodeId": "node-revoked-003",
    "nodeName": "Revoked Node",
    "certificate": "LS0tLS1CRUdJTi...",
    "timestamp": "2025-10-01T12:00:00Z",
    "signature": "..."
  }'
```

**Resultado Esperado:**
```json
{
  "isKnown": true,
  "status": "Revoked",
  "nodeId": "node-revoked-003",
  "nodeName": "Revoked Node",
  "timestamp": "2025-10-01T...",
  "message": "Node authorization has been revoked.",
  "nextPhase": null,
  "registrationUrl": null
}
```

## 4. Testes de Segurança

### 4.1 Assinatura Inválida
**Objetivo:** Verificar rejeição de assinatura incorreta

```powershell
curl -X POST http://localhost:5001/api/channel/identify `
  -H "Content-Type: application/json" `
  -d '{
    "channelId": "'$channelId'",
    "nodeId": "node-a-test-001",
    "nodeName": "Test Node",
    "certificate": "LS0tLS1CRUdJTi...",
    "timestamp": "2025-10-01T12:00:00Z",
    "signature": "INVALID_SIGNATURE"
  }'
```

**Resultado Esperado:**
```json
{
  "error": {
    "code": "ERR_INVALID_SIGNATURE",
    "message": "Node signature verification failed",
    "retryable": false
  }
}
```

### 4.2 Canal Inválido ou Expirado
**Objetivo:** Verificar que identificação requer canal válido

```powershell
curl -X POST http://localhost:5001/api/channel/identify `
  -H "Content-Type: application/json" `
  -d '{
    "channelId": "invalid-channel-id",
    "nodeId": "node-a-test-001",
    "nodeName": "Test Node",
    "certificate": "LS0tLS1CRUdJTi...",
    "timestamp": "2025-10-01T12:00:00Z",
    "signature": "..."
  }'
```

**Resultado Esperado:**
```json
{
  "error": {
    "code": "ERR_INVALID_CHANNEL",
    "message": "Channel does not exist or has expired",
    "retryable": true
  }
}
```

## 5. Teste Automatizado

Execute o script de teste:

```powershell
.\test-phase2.ps1
```

**Resultado Esperado:**
```
=== Teste de Comunicação Fase 2 - Identificação de Nós ===

[Teste 1] Estabelecer Canal Criptografado (Fase 1)
  [OK] Canal estabelecido: {channelId}

[Teste 2] Registrar Nó A no Nó B (Unknown Node)
  [OK] Nó registrado com sucesso!
    Registration ID: {id}
    Status: Pending

[Teste 3] Listar Nós Registrados no Nó B
  [OK] Total de nós registrados: 1

[Teste 4] Aprovar Nó A no Nó B (Admin Operation)
  [OK] Node status updated successfully

[Teste 5] Identificar Nó A no Nó B (Fase 2)
  [INFO] Este teste requer certificado real e assinatura
  [INFO] Pulando por enquanto

=== Testes da Fase 2 Concluídos ===
```

## 6. Checklist de Validação Final

Marque cada item após validação:

- [ ] Nó desconhecido pode se registrar
- [ ] Sistema rejeita NodeId duplicado
- [ ] Sistema rejeita certificado duplicado
- [ ] Listagem de nós funciona corretamente
- [ ] Administrador pode aprovar nós pendentes
- [ ] Administrador pode revogar nós autorizados
- [ ] Identificação de nó desconhecido retorna URL de registro
- [ ] Identificação de nó autorizado permite avanço para Fase 3
- [ ] Identificação de nó pendente bloqueia progresso
- [ ] Identificação de nó revogado bloqueia acesso
- [ ] Assinatura inválida é rejeitada
- [ ] Canal inválido/expirado é rejeitado
- [ ] Logs mostram informações corretas

## 7. Testes Pendentes (Requerem Certificados Reais)

Os seguintes testes precisam de certificados X.509 auto-assinados reais:

- [ ] Geração de certificados auto-assinados
- [ ] Assinatura de dados com chave privada do certificado
- [ ] Verificação de assinatura com chave pública
- [ ] Teste completo de identificação com certificado e assinatura válidos

**Próximo passo:** Implementar endpoint `/api/testing/generate-certificate` para gerar certificados de teste.

## 8. Critérios de Sucesso

A Fase 2 está completa quando:

✅ **Registro de nós** funciona (desconhecidos podem se registrar)
✅ **Gerenciamento de nós** funciona (admin pode aprovar/revogar)
✅ **Identificação de nós** diferencia corretamente: unknown, pending, authorized, revoked
✅ **Validações de segurança** estão implementadas (assinatura, canal)
✅ **Integração com Fase 1** funciona (canal deve estar ativo)
✅ **Logs** fornecem informações úteis para debug
✅ **Todos os testes** deste documento passam

## Próximos Passos

Após validar a Fase 2:
1. Implementar endpoint para geração de certificados de teste
2. Implementar **Fase 3**: Autenticação Mútua com Desafio/Resposta
3. Implementar **Fase 4**: Estabelecimento de Sessão com Capabilities
