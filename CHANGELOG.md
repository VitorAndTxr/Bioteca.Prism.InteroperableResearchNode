# Changelog

Todas as mudanças notáveis neste projeto serão documentadas neste arquivo.

O formato é baseado em [Keep a Changelog](https://keepachangelog.com/pt-BR/1.0.0/),
e este projeto adere ao [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.3.1] - 2025-10-02

### 🔧 Corrigido
- **Deserialização JSON de payloads criptografados**: Corrigido problema de incompatibilidade entre camelCase/PascalCase
  - Adicionado `PropertyNameCaseInsensitive = true` em `Program.cs`
  - Adicionado atributos `[JsonPropertyName]` em `EncryptedPayload`
  - Alinhado `JsonSerializerOptions` entre ASP.NET Core e `ChannelEncryptionService`
- **Causa raiz**: Cliente enviava JSON em camelCase, servidor esperava PascalCase
- **Impacto**: Agora aceita ambos os formatos (camelCase e PascalCase)

### 📚 Documentação
- Criado guia completo de teste manual: `docs/testing/TESTE-MANUAL-FASE2-CRIPTOGRAFADA.md`
  - 9 passos detalhados com Swagger UI
  - Troubleshooting de erros comuns
  - Validação de todos os cenários de Fase 2
- Criado script PowerShell automatizado: `test-fase2-manual.ps1`
  - Executa fluxo completo de teste automaticamente
  - Output colorido com progresso passo-a-passo
  - Validação de todos os payloads criptografados
- Atualizado `README.md` com 3 opções de teste:
  - Teste automatizado (PowerShell)
  - Teste manual via Swagger
  - Testes de integração (xUnit)

### 🎯 Melhorias
- **Configuração Global de JSON** (`Program.cs`):
  ```csharp
  builder.Services.AddControllers()
      .AddJsonOptions(options => {
          options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
          options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
          options.JsonSerializerOptions.AllowTrailingCommas = true;
      });
  ```
- **Atributos Explícitos** (`EncryptedPayload`):
  ```csharp
  [JsonPropertyName("encryptedData")]
  public string EncryptedData { get; set; }

  [JsonPropertyName("iv")]
  public string Iv { get; set; }

  [JsonPropertyName("authTag")]
  public string AuthTag { get; set; }
  ```

---

## [0.3.0] - 2025-10-02

### ✨ Adicionado
- **Criptografia de Canal para Fase 2+**
  - Todos os payloads após estabelecimento de canal são criptografados com AES-256-GCM
  - Header `X-Channel-Id` obrigatório para validação de canal
  - Métodos `EncryptPayload`/`DecryptPayload` em `IChannelEncryptionService`
- **Gerenciamento Centralizado de Canais**
  - Interface `IChannelStore` para armazenamento de contextos de canal
  - Implementação `ChannelStore` com `ConcurrentDictionary`
  - Validação automática de expiração (30 minutos)
- **Endpoints de Teste de Criptografia** (`TestingController`)
  - `POST /api/testing/encrypt-payload` - Criptografa qualquer payload JSON
  - `POST /api/testing/decrypt-payload` - Descriptografa payload recebido
  - `GET /api/testing/channel-info/{channelId}` - Informações do canal ativo

### 🔒 Segurança
- **Breaking Change**: Endpoints `/api/node/identify` e `/api/node/register` agora **exigem** payload criptografado
  - Formato: `{"encryptedData": "...", "iv": "...", "authTag": "..."}`
  - Header obrigatório: `X-Channel-Id`
- **Proteção contra replay attacks**: Channel ID vincula requests ao canal específico
- **Perfect Forward Secrecy mantido**: Chaves simétricas derivadas de chaves efêmeras ECDH

### 📚 Documentação
- Criado `docs/development/channel-encryption-implementation.md`
- Criado `docs/development/testing-endpoints-criptografia.md`
- Atualizado `docs/testing/manual-testing-guide.md` com instruções de criptografia
- Criado `docs/api-examples/testing-encryption.http` com exemplos de requisições

### 🔧 Alterações Técnicas
- **Controllers**:
  - Criado `NodeConnectionController.cs` (endpoints de Fase 2 criptografados)
  - Mantido `ChannelController.cs` (apenas Fase 1)
  - Adicionado `TestingController.cs` (helpers de teste)
- **Services**:
  - Estendido `ChannelEncryptionService` com métodos de payload
  - Criado `ChannelStore` para gerenciamento de contextos
  - Atualizado `NodeChannelClient` para usar criptografia
- **Program.cs**:
  - Registrado `IChannelStore` como Singleton
  - Configuração de ambientes NodeA/NodeB/Development para Swagger

---

## [0.2.0] - 2025-10-01

### ✨ Adicionado
- **Fase 2: Identificação e Registro de Nós** (SEM criptografia - versão inicial)
  - Endpoint `POST /api/channel/identify` - Identifica nó com certificado
  - Endpoint `POST /api/node/register` - Registra novo nó
  - Endpoint `PUT /api/node/{nodeId}/status` - Atualiza status de autorização
  - Endpoint `GET /api/node/nodes` - Lista todos os nós registrados
- **Node Registry Service**
  - Armazenamento in-memory de nós registrados
  - Estados de autorização: Unknown, Pending, Authorized, Revoked
  - Verificação de assinaturas RSA
  - Cálculo de fingerprints de certificados (SHA-256)
- **Certificate Helper**
  - Geração de certificados auto-assinados X.509
  - Suporte a RSA-2048
  - Validade configurável

### 📚 Documentação
- Criado `docs/architecture/handshake-protocol.md`
- Criado `docs/testing/phase2-test-plan.md`
- Criado `docs/PROJECT_STATUS.md`
- Atualizado `README.md` com status da Fase 2

### 🧪 Testes
- Criado `test-phase2-full.ps1` - Script PowerShell para teste completo
- Cenários testados:
  - Nó desconhecido → Registro → Pending → Aprovação → Authorized
  - Verificação de assinatura digital
  - Fluxo de próxima fase (`phase3_authenticate`)

---

## [0.1.0] - 2025-09-30

### ✨ Adicionado
- **Fase 1: Estabelecimento de Canal Criptografado**
  - Endpoint `POST /api/channel/open` - Servidor aceita handshake
  - Endpoint `POST /api/channel/initiate` - Cliente inicia handshake
  - Endpoint `GET /api/channel/health` - Health check
  - Endpoint `GET /api/channel/{channelId}` - Informações do canal
- **Serviços de Criptografia**
  - `EphemeralKeyService` - Geração de chaves ECDH P-384
  - `ChannelEncryptionService` - HKDF + AES-256-GCM
  - `NodeChannelClient` - Cliente HTTP para handshake
- **Arquitetura Limpa**
  - `Bioteca.Prism.Domain` - Entities, DTOs, Requests, Responses
  - `Bioteca.Prism.Service` - Business logic
  - `Bioteca.Prism.InteroperableResearchNode` - API layer
- **Docker Deployment**
  - `docker-compose.yml` - Orchestração de Node A e Node B
  - Configurações separadas: `appsettings.NodeA.json`, `appsettings.NodeB.json`
  - Health checks e networking configurados

### 🔒 Segurança
- **Perfect Forward Secrecy (PFS)**: Chaves efêmeras ECDH P-384
- **HKDF-SHA256**: Derivação de chaves simétricas
- **AES-256-GCM**: Criptografia autenticada (preparada para Fase 2+)

### 📚 Documentação
- Criado `README.md` inicial
- Criado `docs/README.md` - Índice de documentação
- Criado `docs/testing/manual-testing-guide.md`

### 🧪 Testes
- Criado `test-docker.ps1` - Script PowerShell para teste de Fase 1
- Testes básicos de:
  - Health check
  - Handshake ECDH
  - Derivação de chave simétrica

---

## Formato de Versões

- **MAJOR** (X.0.0): Mudanças incompatíveis na API
- **MINOR** (0.X.0): Novas funcionalidades compatíveis
- **PATCH** (0.0.X): Correções de bugs compatíveis

## Categorias de Mudanças

- **Adicionado** - Novas funcionalidades
- **Alterado** - Mudanças em funcionalidades existentes
- **Descontinuado** - Funcionalidades que serão removidas
- **Removido** - Funcionalidades removidas
- **Corrigido** - Correções de bugs
- **Segurança** - Correções de vulnerabilidades
