# Guia Rápido de Testes - IRN

Este documento fornece um guia rápido para testar o sistema IRN.

## 🚀 Início Rápido

### Opção 1: Testes de Integração C# (Recomendado) ⭐

Os testes foram **migrados completamente** para C# usando xUnit. Esta é a forma recomendada de executar os testes.

```powershell
# Executar todos os testes
dotnet test

# Executar testes específicos
dotnet test --filter "FullyQualifiedName~Phase1"
dotnet test --filter "FullyQualifiedName~Phase2"
dotnet test --filter "FullyQualifiedName~Security"

# Com cobertura de código
dotnet test --collect:"XPlat Code Coverage"
```

**Vantagens:**
- ✅ Execução automática em CI/CD
- ✅ Debugging completo com breakpoints
- ✅ Cobertura de código detalhada
- ✅ Execução paralela (mais rápido)
- ✅ Isolamento completo entre testes
- ✅ Type safety e IntelliSense

**Documentação completa:** [Bioteca.Prism.InteroperableResearchNode.Test/README.md](Bioteca.Prism.InteroperableResearchNode.Test/README.md)

---

### Opção 2: Scripts PowerShell (Legacy)

Os scripts PowerShell ainda estão disponíveis para testes manuais e validação rápida.

#### 1. Subir o Ambiente

```powershell
# Subir containers Docker
docker-compose up -d

# Aguardar containers iniciarem
Start-Sleep -Seconds 5

# Verificar health
curl http://localhost:5000/api/channel/health
curl http://localhost:5001/api/channel/health
```

#### 2. Executar Testes Automatizados

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

## 🧪 Testes de Integração C# (Migrados)

### Status da Migração: ✅ COMPLETO

Todos os testes dos scripts PowerShell foram migrados para C# e **expandidos** com cenários adicionais de segurança e edge cases.

| Categoria | Arquivo C# | Testes | Status |
|-----------|-----------|--------|--------|
| Fase 1 - Canal | `Phase1ChannelEstablishmentTests.cs` | 7 | ✅ |
| Fase 2 - Identificação | `Phase2NodeIdentificationTests.cs` | 8 | ✅ |
| Integração Completa | `EncryptedChannelIntegrationTests.cs` | 3 | ✅ |
| Segurança & Edge Cases | `SecurityAndEdgeCaseTests.cs` | 13 | ✅ |
| Certificados | `CertificateAndSignatureTests.cs` | 13 | ✅ |
| Cliente | `NodeChannelClientTests.cs` | 7 | ✅ |
| **TOTAL** | **6 arquivos** | **51 testes** | ✅ |

### Mapeamento PowerShell → C#

| Script PowerShell | Arquivo C# | Cobertura |
|-------------------|------------|-----------|
| `test-docker.ps1` | `Phase1ChannelEstablishmentTests.cs` | 100% |
| `test-phase2.ps1` | `Phase2NodeIdentificationTests.cs` | 100% |
| `test-phase2-full.ps1` | `Phase2NodeIdentificationTests.cs` + `NodeChannelClientTests.cs` | 100% |
| `test-phase2-encrypted.ps1` | `EncryptedChannelIntegrationTests.cs` | 100% |
| Endpoints `/api/testing/*` | `CertificateAndSignatureTests.cs` | 100% |
| Cenários não cobertos | `SecurityAndEdgeCaseTests.cs` | ~30 novos testes |

**📖 Documentação completa dos testes:** [Bioteca.Prism.InteroperableResearchNode.Test/README.md](Bioteca.Prism.InteroperableResearchNode.Test/README.md)

---

## 📝 Scripts PowerShell (Legacy)

> **Nota:** Os scripts PowerShell foram mantidos para testes manuais rápidos e validação visual, mas os testes C# são a forma recomendada de teste automatizado.

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

### Testes de Criptografia (Novos Endpoints) 🔐

#### Obter Informações de Canal
```powershell
# Verifica se um canal está ativo e obtém suas informações
Invoke-RestMethod -Uri "http://localhost:5000/api/testing/channel-info/{channelId}" -Method Get
```

#### Criptografar Payload
```powershell
# Criptografa qualquer payload JSON usando a chave simétrica do canal
Invoke-RestMethod -Uri "http://localhost:5000/api/testing/encrypt-payload" `
  -Method Post `
  -ContentType "application/json" `
  -Body @"
{
  "channelId": "SEU_CHANNEL_ID",
  "payload": {
    "message": "Hello, World!",
    "data": {
      "temperature": 36.5,
      "heartRate": 72
    }
  }
}
"@
```

#### Descriptografar Payload
```powershell
# Descriptografa um payload criptografado para validação
Invoke-RestMethod -Uri "http://localhost:5000/api/testing/decrypt-payload" `
  -Method Post `
  -ContentType "application/json" `
  -Body @"
{
  "channelId": "SEU_CHANNEL_ID",
  "encryptedPayload": {
    "encryptedData": "BASE64_ENCRYPTED_DATA",
    "iv": "BASE64_IV",
    "authTag": "BASE64_AUTH_TAG"
  }
}
"@
```

**📖 Exemplos completos:** [docs/api-examples/testing-encryption.http](docs/api-examples/testing-encryption.http)

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

### Testes C#
- **[Test Suite README](Bioteca.Prism.InteroperableResearchNode.Test/README.md)** - Documentação completa dos testes C#
- **[Manual Testing Guide](docs/testing/manual-testing-guide.md)** - Guia completo com debugging

### Planos de Teste
- **[Phase 1 Test Plan](docs/testing/phase1-test-plan.md)** - Plano detalhado de testes da Fase 1
- **[Phase 2 Test Plan](docs/testing/phase2-test-plan.md)** - Plano detalhado de testes da Fase 2

### Status do Projeto
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
