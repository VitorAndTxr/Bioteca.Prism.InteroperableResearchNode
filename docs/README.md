# Documentação do IRN - Interoperable Research Node

Esta pasta contém toda a documentação técnica e de desenvolvimento do projeto IRN.

## 📊 Status do Projeto

**Última Atualização:** 2025-10-01

| Fase | Status | Descrição |
|------|--------|-----------|
| **Fase 1** | ✅ **Completa e Validada** | Canal criptografado com ECDH efêmero |
| **Fase 2** | ✅ **Completa e Validada** | Identificação e autorização de nós com X.509 |
| **Fase 3** | 📋 Planejado | Autenticação mútua com desafio/resposta |
| **Fase 4** | 📋 Planejado | Estabelecimento de sessão com capabilities |

## Estrutura da Documentação

### 1. Arquitetura e Design
- [`architecture/handshake-protocol.md`](architecture/handshake-protocol.md) - **⭐ PRINCIPAL** - Protocolo de handshake completo (Fases 1-4)
- [`architecture/node-communication.md`](architecture/node-communication.md) - Arquitetura de comunicação entre nós
- [`architecture/session-management.md`](architecture/session-management.md) - Gerenciamento de sessões entre nós

### 2. Testes
- [`testing/manual-testing-guide.md`](testing/manual-testing-guide.md) - **⭐ NOVO** - Guia completo de testes manuais e debugging
- [`testing/phase1-test-plan.md`](testing/phase1-test-plan.md) - Plano de testes da Fase 1 (Canal Criptografado)
- [`testing/phase2-test-plan.md`](testing/phase2-test-plan.md) - Plano de testes da Fase 2 (Identificação)
- [`testing/phase1-docker-test.md`](testing/phase1-docker-test.md) - Testes com Docker
- [`testing/phase1-two-nodes-test.md`](testing/phase1-two-nodes-test.md) - Testes com dois nós

### 3. Desenvolvimento
- [`development/ai-assisted-development.md`](development/ai-assisted-development.md) - Padrões de desenvolvimento assistido por IA
- [`development/implementation-roadmap.md`](development/implementation-roadmap.md) - Roadmap de implementação
- [`development/debugging-docker.md`](development/debugging-docker.md) - Debug de containers Docker

### 4. API e Protocolos ⚠️
- ~~[`api/node-endpoints.md`](api/node-endpoints.md) - Especificação dos endpoints de comunicação~~ (Desatualizado - ver Swagger)
- ~~[`api/message-formats.md`](api/message-formats.md) - Formatos de mensagens e payloads~~ (Desatualizado - ver Models)

## 🚀 Início Rápido

### Para Testar o Sistema

1. **Subir ambiente Docker:**
   ```powershell
   docker-compose up -d
   ```

2. **Executar testes automatizados:**
   ```powershell
   # Fase 1 + Fase 2
   .\test-phase2-full.ps1
   ```

3. **Testes manuais e debugging:**
   - Leia [`testing/manual-testing-guide.md`](testing/manual-testing-guide.md)

### Para Desenvolvimento

1. **Entender a arquitetura:**
   - Leia [`architecture/handshake-protocol.md`](architecture/handshake-protocol.md)

2. **Debug no Visual Studio:**
   - Selecione profile "Node A (Debug)" ou "Node B (Debug)"
   - Breakpoints sugeridos estão em [`testing/manual-testing-guide.md`](testing/manual-testing-guide.md)

3. **Endpoints disponíveis:**
   - Swagger: http://localhost:5000/swagger (Node A)
   - Swagger: http://localhost:5001/swagger (Node B)

## 📚 Endpoints Implementados

### Fase 1: Canal Criptografado
- `POST /api/channel/open` - Recebe solicitação de canal (servidor)
- `POST /api/channel/initiate` - Inicia canal com nó remoto (cliente)
- `GET /api/channel/{channelId}` - Informações do canal
- `GET /api/channel/health` - Health check

### Fase 2: Identificação e Autorização
- `POST /api/channel/identify` - Identifica nó com certificado
- `POST /api/node/register` - Registra novo nó desconhecido
- `GET /api/node/nodes` - Lista nós registrados (admin)
- `PUT /api/node/{nodeId}/status` - Atualiza status do nó (admin)

### Testing (apenas em Dev/NodeA/NodeB)
- `POST /api/testing/generate-certificate` - Gera certificado auto-assinado
- `POST /api/testing/sign-data` - Assina dados com certificado
- `POST /api/testing/verify-signature` - Verifica assinatura
- `POST /api/testing/generate-node-identity` - Gera identidade completa

## 🔧 Tecnologias Utilizadas

- **ASP.NET Core 8.0** - Framework web
- **ECDH P-384** - Troca de chaves efêmeras
- **HKDF-SHA256** - Derivação de chaves
- **AES-256-GCM** - Criptografia simétrica
- **RSA-2048** - Certificados e assinaturas
- **X.509** - Padrão de certificados
- **Docker** - Containerização
- **Swagger/OpenAPI** - Documentação de API

## 📖 Convenções

- ✅ Implementado e validado
- 🚧 Em desenvolvimento
- 📋 Planejado
- ⚠️ Desatualizado/Bloqueado
- ⭐ Documento principal/importante
