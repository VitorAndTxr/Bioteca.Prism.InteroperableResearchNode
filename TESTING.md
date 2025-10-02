# Guia Rápido de Testes - IRN

Este documento fornece um guia rápido para testar o sistema IRN.

## 🚀 Início Rápido

### 1. Subir o Ambiente

```powershell
# Subir containers Docker
docker-compose up -d

# Aguardar containers iniciarem
Start-Sleep -Seconds 5

# Verificar health
curl http://localhost:5000/api/channel/health
curl http://localhost:5001/api/channel/health
```

### 2. Executar Testes Automatizados

```powershell
# Teste completo (Fases 1 + 2)
.\test-phase2-full.ps1
```

**Resultado esperado:**
```
✅ Canal criptografado estabelecido (Fase 1)
✅ Certificados auto-assinados gerados
✅ Nó desconhecido pode se registrar
✅ Identificação com status Pending funciona
✅ Aprovação de nós funciona
✅ Identificação com status Authorized funciona
✅ Sistema indica Fase 3 como próximo passo
✅ Nó desconhecido recebe URL de registro

Fase 2 COMPLETA! Pronto para implementar Fase 3.
```

## 📝 Scripts de Teste Disponíveis

### `test-docker.ps1` - Teste da Fase 1

**O que testa:**
- Health check de ambos os nós
- Handshake Node A → Node B
- Handshake Node B → Node A
- Verificação de canal em ambos os lados
- Validação de roles (client/server)

**Como executar:**
```powershell
.\test-docker.ps1
```

**Duração:** ~10 segundos

---

### `test-phase2.ps1` - Teste Básico da Fase 2

**O que testa:**
- Canal criptografado (Fase 1)
- Registro de nó com dados mock
- Listagem de nós registrados
- Aprovação de nó (admin)

**Como executar:**
```powershell
.\test-phase2.ps1
```

**Duração:** ~15 segundos

**Nota:** Usa certificados mock, não testa assinaturas reais.

---

### `test-phase2-full.ps1` - Teste Completo da Fase 2 ⭐

**O que testa:**
- ✅ Fase 1: Canal criptografado com ECDH
- ✅ Geração de certificado X.509 auto-assinado
- ✅ Assinatura digital RSA-SHA256
- ✅ Verificação de assinatura
- ✅ Registro de nó desconhecido
- ✅ Identificação com status Pending
- ✅ Aprovação de nó (admin)
- ✅ Identificação com status Authorized
- ✅ Nó desconhecido recebe URL de registro
- ✅ Listagem de nós registrados

**Como executar:**
```powershell
.\test-phase2-full.ps1
```

**Duração:** ~20 segundos

**Este é o teste mais completo disponível!**

---

## 🔍 Testes Manuais

Para testes manuais passo a passo com debugging, consulte:

📖 **[Manual Testing Guide](docs/testing/manual-testing-guide.md)**

Este guia inclui:
- Preparação do ambiente
- Breakpoints sugeridos para debug
- Inspeção de variáveis importantes
- Fluxo detalhado de cada fase
- Cenários de teste adicionais
- Análise de segurança

## 🧪 Teste Individual de Endpoints

### Fase 1: Canal Criptografado

#### Health Check
```powershell
curl http://localhost:5000/api/channel/health
```

#### Iniciar Handshake
```powershell
Invoke-RestMethod -Uri "http://localhost:5000/api/channel/initiate" `
  -Method Post `
  -ContentType "application/json" `
  -Body '{"remoteNodeUrl": "http://node-b:8080"}'
```

#### Consultar Canal
```powershell
# Substitua {channelId} pelo ID retornado
curl http://localhost:5000/api/channel/{channelId}
```

---

### Fase 2: Identificação

#### Gerar Certificado
```powershell
Invoke-RestMethod -Uri "http://localhost:5000/api/testing/generate-certificate" `
  -Method Post `
  -ContentType "application/json" `
  -Body '{"subjectName": "test-node", "validityYears": 2, "password": "test123"}'
```

#### Gerar Identidade Completa
```powershell
# Primeiro, estabeleça um canal e obtenha o channelId

Invoke-RestMethod -Uri "http://localhost:5000/api/testing/generate-node-identity" `
  -Method Post `
  -ContentType "application/json" `
  -Body '{
    "nodeId": "test-node-001",
    "nodeName": "Test Node",
    "channelId": "SEU_CHANNEL_ID_AQUI",
    "validityYears": 2,
    "password": "test123"
  }'
```

#### Registrar Nó
```powershell
Invoke-RestMethod -Uri "http://localhost:5001/api/node/register" `
  -Method Post `
  -ContentType "application/json" `
  -Body '{
    "nodeId": "test-node-001",
    "nodeName": "Test Node",
    "certificate": "SEU_CERTIFICADO_BASE64",
    "contactInfo": "admin@test.com",
    "institutionDetails": "Test Institution",
    "nodeUrl": "http://test:8080",
    "requestedCapabilities": ["search"]
  }'
```

#### Listar Nós Registrados
```powershell
Invoke-RestMethod -Uri "http://localhost:5001/api/node/nodes" -Method Get
```

#### Aprovar Nó
```powershell
Invoke-RestMethod -Uri "http://localhost:5001/api/node/test-node-001/status" `
  -Method Put `
  -ContentType "application/json" `
  -Body '{"status": 1}'  # 1 = Authorized
```

#### Identificar Nó
```powershell
Invoke-RestMethod -Uri "http://localhost:5001/api/channel/identify" `
  -Method Post `
  -ContentType "application/json" `
  -Body '{
    "channelId": "SEU_CHANNEL_ID",
    "nodeId": "test-node-001",
    "nodeName": "Test Node",
    "certificate": "SEU_CERTIFICADO",
    "timestamp": "2025-10-01T12:00:00Z",
    "signature": "SUA_ASSINATURA"
  }'
```

---

## 🌐 Swagger UI

Acesse a interface interativa do Swagger:

- **Node A:** http://localhost:5000/swagger
- **Node B:** http://localhost:5001/swagger

**Vantagens:**
- Interface gráfica para testar endpoints
- Documentação automática
- Validação de schemas
- Exemplos de requisições

---

## 🐳 Comandos Docker Úteis

```powershell
# Verificar status dos containers
docker ps

# Ver logs em tempo real
docker logs -f irn-node-a
docker logs -f irn-node-b

# Ver últimas 50 linhas de log
docker logs --tail 50 irn-node-a

# Parar containers
docker-compose down

# Rebuild após alterações
docker-compose down
docker-compose build --no-cache
docker-compose up -d

# Executar comando dentro do container
docker exec -it irn-node-a /bin/bash
```

---

## 📊 Validação de Resultados

### Fase 1 - Sucesso
- ✅ `success: true`
- ✅ `channelId` é um GUID válido
- ✅ `selectedCipher` contém uma cifra suportada
- ✅ Mesmo `channelId` em ambos os nós
- ✅ Roles diferentes (`client` vs `server`)

### Fase 2 - Registro
- ✅ `success: true`
- ✅ `status: 0` (Pending)
- ✅ `registrationId` é um GUID
- ✅ Mensagem indica aprovação pendente

### Fase 2 - Identificação (Pending)
- ✅ `isKnown: true`
- ✅ `status: 2` (Pending)
- ✅ `nextPhase: null` (bloqueado)

### Fase 2 - Identificação (Authorized)
- ✅ `isKnown: true`
- ✅ `status: 1` (Authorized)
- ✅ `nextPhase: "phase3_authenticate"` ⭐
- ✅ Mensagem indica progressão para Fase 3

---

## 🐛 Troubleshooting

### Erro: "The remote server returned an error: (404) Not Found"

**Causa:** Container não está rodando ou endpoint não existe.

**Solução:**
```powershell
# Verificar se containers estão rodando
docker ps

# Reiniciar containers
docker-compose restart

# Verificar logs
docker logs irn-node-a
```

---

### Erro: "Channel does not exist or has expired"

**Causa:** Canal expirou (30 minutos) ou `channelId` incorreto.

**Solução:**
```powershell
# Estabelecer novo canal
$result = Invoke-RestMethod -Uri "http://localhost:5000/api/channel/initiate" `
  -Method Post `
  -ContentType "application/json" `
  -Body '{"remoteNodeUrl": "http://node-b:8080"}'

$channelId = $result.channelId
```

---

### Erro: "Node signature verification failed"

**Causa:** Assinatura digital incorreta ou timestamp não bate.

**Solução:**
1. Verificar formato do timestamp: `DateTime.UtcNow.ToString("O")`
2. Verificar ordem dos campos: `channelId + nodeId + timestamp`
3. Usar endpoint `/api/testing/sign-data` para gerar assinatura válida

---

### Containers em estado "unhealthy"

**Causa:** Health check usa `curl` que pode não estar disponível.

**Solução:**
- Ignorar (containers funcionam mesmo com health check falho)
- Ou remover health check do `docker-compose.yml`

---

## 📚 Documentação Adicional

- **[Phase 1 Test Plan](docs/testing/phase1-test-plan.md)** - Plano detalhado de testes da Fase 1
- **[Phase 2 Test Plan](docs/testing/phase2-test-plan.md)** - Plano detalhado de testes da Fase 2
- **[Manual Testing Guide](docs/testing/manual-testing-guide.md)** - Guia completo com debugging
- **[Project Status](docs/PROJECT_STATUS.md)** - Status completo do projeto

---

## ✅ Checklist de Validação

Após executar os testes, verifique:

### Fase 1
- [ ] Health check funciona em ambos os nós
- [ ] Node A consegue iniciar handshake com Node B
- [ ] Node B consegue iniciar handshake com Node A
- [ ] Canal aparece em ambos os nós (cliente e servidor)
- [ ] Chaves efêmeras são diferentes a cada handshake
- [ ] Logs mostram informações corretas

### Fase 2
- [ ] Certificado auto-assinado gerado corretamente
- [ ] Assinatura RSA-SHA256 criada
- [ ] Nó desconhecido pode se registrar
- [ ] Status inicial é Pending
- [ ] Identificação com Pending bloqueia progresso
- [ ] Admin pode aprovar nós
- [ ] Identificação com Authorized permite Fase 3
- [ ] Nó desconhecido recebe URL de registro
- [ ] Listagem de nós funciona

---

**Última atualização:** 2025-10-01
