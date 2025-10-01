# Roadmap de Implementação - IRN

**Última atualização**: 2025-10-01
**Versão alvo**: 1.0.0

## Visão Geral

Roadmap para desenvolvimento do Interoperable Research Node desde o estado inicial (projeto ASP.NET template) até um sistema funcional de comunicação federada entre nós de pesquisa.

## Status Geral

- **Fase atual**: 0 - Preparação
- **Progresso global**: 5%
- **Versão**: 0.1.0-alpha

## Fases de Desenvolvimento

### Fase 0: Preparação e Fundação (1 semana)

**Status**: 🚧 Em andamento

**Objetivos**:
- Estruturar documentação do projeto
- Definir padrões de desenvolvimento
- Configurar ambiente base

**Tarefas**:
- [x] Criar estrutura de documentação
- [x] Documentar arquitetura de comunicação entre nós
- [x] Documentar protocolo de handshake
- [x] Documentar gerenciamento de sessões
- [x] Definir padrões de desenvolvimento assistido por IA
- [ ] Configurar logging estruturado (Serilog)
- [ ] Configurar health checks básicos
- [ ] Adicionar versionamento de API
- [ ] Remover código template (WeatherForecast)

**Entregável**: Documentação completa + projeto base configurado

---

### Fase 1: Protocolo de Handshake (2-3 semanas)

**Status**: 📋 Planejado

**Objetivos**:
- Implementar autenticação mútua entre nós
- Estabelecer confiança criptográfica
- Criar base para comunicação segura

**Tarefas**:

#### 1.1 Models e DTOs (2 dias)
- [ ] `Models/Node/NodeInfo.cs`
- [ ] `Models/Node/HelloRequest.cs`
- [ ] `Models/Node/HelloAckResponse.cs`
- [ ] `Models/Node/AuthChallenge.cs`
- [ ] `Models/Node/AuthResponse.cs`
- [ ] `Models/Node/HandshakeError.cs`
- [ ] `Models/Node/NodeCapabilities.cs`

#### 1.2 Serviços de Segurança (3-4 dias)
- [ ] `Services/Security/ICertificateService.cs`
- [ ] `Services/Security/CertificateService.cs` (geração, validação)
- [ ] `Services/Security/ICryptographyService.cs`
- [ ] `Services/Security/CryptographyService.cs` (sign, verify, encrypt)
- [ ] Configuração de certificados auto-assinados para desenvolvimento

#### 1.3 Serviço de Autenticação (3-4 dias)
- [ ] `Services/Node/INodeAuthenticationService.cs`
- [ ] `Services/Node/NodeAuthenticationService.cs`
- [ ] Implementar validação de nonce e timestamp
- [ ] Implementar prevenção de replay attacks

#### 1.4 Registry de Nós (2 dias)
- [ ] `Services/Node/INodeRegistryService.cs`
- [ ] `Services/Node/InMemoryNodeRegistryService.cs`
- [ ] Carregar nós conhecidos de configuração
- [ ] API para adicionar/remover nós confiáveis

#### 1.5 Controller de Handshake (3-4 dias)
- [ ] `Controllers/Node/NodeHandshakeController.cs`
- [ ] `POST /api/node/v1/handshake/hello`
- [ ] `POST /api/node/v1/handshake/auth/challenge`
- [ ] `POST /api/node/v1/handshake/auth/response`
- [ ] `POST /api/node/v1/handshake/auth/verify`
- [ ] Tratamento completo de erros

#### 1.6 Testes (2-3 dias)
- [ ] Testes unitários de serviços
- [ ] Testes de integração do fluxo completo
- [ ] Testes de segurança (certificado inválido, replay, etc.)
- [ ] Testes de performance (latência de handshake)

**Entregável**: Handshake funcional entre dois nós IRN

**Documentação**: `docs/architecture/handshake-protocol.md`

---

### Fase 2: Gerenciamento de Sessões (1-2 semanas)

**Status**: 📋 Planejado

**Objetivos**:
- Manter estado de conexões ativas
- Implementar heartbeat e renovação
- Controlar ciclo de vida de sessões

**Tarefas**:

#### 2.1 Models e Stores (2-3 dias)
- [ ] `Models/Node/SessionInfo.cs`
- [ ] `Models/Node/SessionState.cs` (enum)
- [ ] `Models/Node/SessionMetrics.cs`
- [ ] `Services/Node/ISessionStore.cs`
- [ ] `Services/Node/InMemorySessionStore.cs`

#### 2.2 Serviço de Sessão (3-4 dias)
- [ ] `Services/Node/ISessionManagementService.cs`
- [ ] `Services/Node/SessionManagementService.cs`
- [ ] Criação e validação de sessões
- [ ] Rate limiting por sessão
- [ ] Metrics tracking

#### 2.3 Background Services (2-3 dias)
- [ ] `Services/Background/SessionCleanupService.cs`
- [ ] `Services/Background/SessionMonitoringService.cs`
- [ ] Configurar timers e cleanup policies

#### 2.4 Controller e Middleware (2 dias)
- [ ] `Controllers/Node/NodeSessionController.cs`
- [ ] `Middleware/NodeSessionMiddleware.cs`
- [ ] Validação de sessão em requests

#### 2.5 Testes (2 dias)
- [ ] Testes de criação/renovação/encerramento
- [ ] Testes de expiração e cleanup
- [ ] Testes de rate limiting
- [ ] Testes de múltiplas sessões simultâneas

**Entregável**: Gerenciamento robusto de sessões

**Documentação**: `docs/architecture/session-management.md`

---

### Fase 3: Troca de Dados Básica (2-3 semanas)

**Status**: 📋 Planejado

**Objetivos**:
- Permitir queries de metadados entre nós
- Implementar roteamento de requisições
- Estabelecer formato padrão de dados

**Tarefas**:

#### 3.1 Modelos de Dados (3-4 dias)
- [ ] `Models/Data/ResearchProject.cs`
- [ ] `Models/Data/BiosignalMetadata.cs`
- [ ] `Models/Data/DeviceInfo.cs`
- [ ] `Models/Query/QueryRequest.cs`
- [ ] `Models/Query/QueryResponse.cs`
- [ ] `Models/Query/QueryFilter.cs`

#### 3.2 Armazenamento Local (3-4 dias)
- [ ] Configurar Entity Framework Core
- [ ] `Data/IRNDbContext.cs`
- [ ] Migrations para schema inicial
- [ ] Seed data para desenvolvimento

#### 3.3 Serviços de Query (4-5 dias)
- [ ] `Services/Query/ILocalQueryService.cs`
- [ ] `Services/Query/LocalQueryService.cs` (queries no nó local)
- [ ] `Services/Query/IRemoteQueryService.cs`
- [ ] `Services/Query/RemoteQueryService.cs` (queries em outros nós)
- [ ] `Services/Query/QueryRoutingService.cs` (decidir onde buscar)

#### 3.4 Controllers (3 dias)
- [ ] `Controllers/Node/NodeQueryController.cs` (para outros nós chamarem)
- [ ] `Controllers/QueryController.cs` (para aplicações locais)

#### 3.5 Validação e Segurança (2-3 dias)
- [ ] Validação de queries (prevenir injection)
- [ ] Autorização por tipo de dados
- [ ] Auditoria de acessos

#### 3.6 Testes (3-4 dias)
- [ ] Testes de queries locais
- [ ] Testes de queries federadas (mock de nó remoto)
- [ ] Testes de routing
- [ ] Testes de performance

**Entregável**: Query básico de metadados entre nós

**Documentação**: `docs/architecture/data-exchange.md` (a criar)

---

### Fase 4: Identidade e Autorização (2 semanas)

**Status**: 📋 Planejado

**Objetivos**:
- Implementar autenticação de usuários
- Gerenciar permissões e papéis
- Integrar com queries federadas

**Tarefas**:

#### 4.1 Identity Framework (3-4 dias)
- [ ] Configurar ASP.NET Core Identity
- [ ] `Models/Identity/ResearchUser.cs`
- [ ] `Models/Identity/Role.cs`
- [ ] `Models/Identity/Permission.cs`
- [ ] Migrations

#### 4.2 Autenticação (3 dias)
- [ ] JWT tokens para usuários
- [ ] Refresh token flow
- [ ] `Controllers/AuthController.cs` (já existe estrutura básica)
- [ ] Login/logout/register

#### 4.3 Autorização (3-4 dias)
- [ ] Policies baseadas em claims
- [ ] Autorização por projeto/dataset
- [ ] Middleware de autorização

#### 4.4 Federação de Identidade (3-4 dias)
- [ ] Validação de identidade cross-node
- [ ] Delegação de permissões
- [ ] Auditoria de acessos federados

#### 4.5 Testes (2-3 dias)
- [ ] Testes de autenticação
- [ ] Testes de autorização
- [ ] Testes de federação

**Entregável**: Sistema de identidade e autorização funcional

**Documentação**: `docs/architecture/identity-authorization.md` (a criar)

---

### Fase 5: Ingestão de Dados (2 semanas)

**Status**: 📋 Planejado

**Objetivos**:
- Permitir Devices enviarem biossinais
- Validar dados contra PRISM standards
- Armazenar com metadados completos

**Tarefas**:

#### 5.1 Modelos de Ingestão (2 dias)
- [ ] `Models/Ingestion/BiosignalRecord.cs`
- [ ] `Models/Ingestion/IngestionRequest.cs`
- [ ] `Models/Ingestion/ValidationResult.cs`

#### 5.2 Validação (4-5 dias)
- [ ] `Services/Validation/IBiosignalValidationService.cs`
- [ ] Validação de formato
- [ ] Validação de range
- [ ] Validação de consistência temporal

#### 5.3 Armazenamento (3-4 dias)
- [ ] Schema para biossinais (time-series?)
- [ ] `Services/Storage/IBiosignalStorageService.cs`
- [ ] Otimização de armazenamento

#### 5.4 Controller (2 dias)
- [ ] `Controllers/IngestionController.cs`
- [ ] Autenticação de Devices
- [ ] Rate limiting

#### 5.5 Testes (3-4 dias)
- [ ] Testes de validação
- [ ] Testes de armazenamento
- [ ] Testes de performance (throughput)

**Entregável**: API de ingestão funcional

**Documentação**: `docs/architecture/data-ingestion.md` (a criar)

---

### Fase 6: Dashboard e Monitoramento (1-2 semanas)

**Status**: 📋 Planejado

**Objetivos**:
- Interface web para administração
- Visualização de métricas
- Monitoramento de nós federados

**Tarefas**:

#### 6.1 Frontend Base (3-4 dias)
- [ ] Configurar Blazor Server ou React SPA
- [ ] Layout e navegação básicos
- [ ] Autenticação no frontend

#### 6.2 Dashboard Admin (4-5 dias)
- [ ] Visualização de nós conectados
- [ ] Métricas de sessões ativas
- [ ] Logs e auditoria
- [ ] Gerenciamento de usuários

#### 6.3 Monitoring (3-4 dias)
- [ ] Integração com Prometheus/Grafana
- [ ] Métricas customizadas
- [ ] Alertas

**Entregável**: Dashboard administrativo

**Documentação**: `docs/features/admin-dashboard.md` (a criar)

---

### Fase 7: Polimento e Produção (2-3 semanas)

**Status**: 📋 Planejado

**Objetivos**:
- Preparar para deployment
- Documentação completa
- Testes de carga

**Tarefas**:

#### 7.1 Deployment (4-5 dias)
- [ ] Docker Compose para múltiplos nós
- [ ] Kubernetes manifests
- [ ] CI/CD pipeline
- [ ] Guia de deployment

#### 7.2 Documentação (3-4 dias)
- [ ] API documentation (Swagger + docs)
- [ ] Guia de instalação
- [ ] Guia de configuração
- [ ] Guia de operação

#### 7.3 Testes Finais (5-6 dias)
- [ ] Testes de carga
- [ ] Testes de segurança (pen testing)
- [ ] Testes de resiliência (chaos engineering)
- [ ] User acceptance testing

#### 7.4 Performance (3-4 dias)
- [ ] Profiling e otimização
- [ ] Caching strategies
- [ ] Database tuning

**Entregável**: Sistema pronto para produção

---

## Milestones

| Milestone | Data alvo | Entregável |
|-----------|-----------|------------|
| M0 - Documentação | Semana 1 | Docs completos |
| M1 - Handshake | Semana 4 | Nós se autenticam |
| M2 - Sessões | Semana 6 | Sessões gerenciadas |
| M3 - Query Básico | Semana 9 | Query de metadados funciona |
| M4 - Identidade | Semana 11 | Autenticação de usuários |
| M5 - Ingestão | Semana 13 | Devices enviam dados |
| M6 - Dashboard | Semana 15 | Interface administrativa |
| M7 - Produção | Semana 18 | Deploy em produção |

## Métricas de Sucesso

### Técnicas
- [ ] 2+ nós IRN se comunicando
- [ ] Latência de handshake < 500ms
- [ ] Throughput de queries > 100 req/s por nó
- [ ] Cobertura de testes > 80%

### Funcionais
- [ ] Usuários autenticam e fazem queries
- [ ] Devices ingerem biossinais
- [ ] Dados são validados e armazenados
- [ ] Queries federadas retornam resultados corretos

### Qualidade
- [ ] Documentação completa e atualizada
- [ ] Zero vulnerabilidades críticas
- [ ] Logs e monitoramento funcionais

## Dependências Técnicas

### Pacotes NuGet Necessários
- [x] Microsoft.AspNetCore.App
- [x] Swashbuckle.AspNetCore
- [ ] Serilog.AspNetCore
- [ ] Microsoft.EntityFrameworkCore
- [ ] Microsoft.AspNetCore.Identity
- [ ] System.IdentityModel.Tokens.Jwt
- [ ] StackExchange.Redis (Fase 2+)
- [ ] Polly (circuit breaker, retry)

### Infraestrutura
- [ ] PostgreSQL ou SQL Server (database)
- [ ] Redis (opcional, para sessões distribuídas)
- [ ] Prometheus + Grafana (monitoramento)
- [ ] Docker (containerização)
- [ ] Kubernetes (opcional, para produção)

## Riscos e Mitigações

| Risco | Probabilidade | Impacto | Mitigação |
|-------|---------------|---------|-----------|
| Complexidade do handshake | Média | Alto | Seguir RFC estabelecidos, testes extensivos |
| Performance de queries | Média | Médio | Indexação, caching, paginação |
| Sincronização de sessões | Alta | Médio | Usar Redis ou aceitar eventual consistency |
| Validação de biossinais | Alta | Alto | Definir esquemas claros, feedback rápido |

## Próximos Passos Imediatos

1. **Concluir Fase 0** (esta semana):
   - [ ] Configurar Serilog
   - [ ] Adicionar health checks
   - [ ] Remover código template
   - [ ] Configurar versionamento de API

2. **Iniciar Fase 1** (próxima semana):
   - [ ] Criar models de handshake
   - [ ] Implementar CertificateService
   - [ ] Setup de certificados para dev

## Como Usar Este Roadmap

### Para Desenvolvimento Manual
1. Escolher próxima tarefa da fase atual
2. Consultar documentação correspondente
3. Implementar e testar
4. Atualizar checkboxes neste documento
5. Commitar com referência ao roadmap

### Para Desenvolvimento com IA

```bash
# Exemplo: trabalhar na próxima tarefa da Fase 1
claude "Implementar próxima tarefa não concluída da Fase 1 conforme
        docs/development/implementation-roadmap.md. Consultar documentação
        relacionada e atualizar o roadmap ao finalizar."
```

## Atualizações

- **2025-10-01**: Roadmap inicial criado, Fase 0 em andamento
