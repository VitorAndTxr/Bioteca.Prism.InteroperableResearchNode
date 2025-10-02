# Testes de Integração - Interoperable Research Node

Este diretório contém os testes de integração do IRN, migrando e expandindo os testes originalmente escritos em scripts PowerShell.

## 📋 Visão Geral

Os testes cobrem todo o protocolo de comunicação entre nós, incluindo:
- **Fase 1**: Estabelecimento de Canal Criptografado (ECDH + AES-GCM)
- **Fase 2**: Identificação e Autorização de Nós (Certificados X.509)
- **Fase 3**: [Planejado] Autenticação e Operações

## 🗂️ Estrutura dos Testes

### 1. `Phase1ChannelEstablishmentTests.cs`
**Origem**: Migrado de `test-docker.ps1` e scripts de fase 1

Testa o estabelecimento de canal criptografado:
- ✅ Health check do endpoint
- ✅ Abertura de canal com ECDH-P384
- ✅ Negociação de cifras (AES-256-GCM, ChaCha20-Poly1305)
- ✅ Validação de versão de protocolo
- ✅ Validação de chaves públicas
- ✅ Recuperação de informações do canal
- ✅ Tratamento de canais inexistentes

**Cobertura**: ~95% dos cenários de Fase 1

### 2. `Phase2NodeIdentificationTests.cs`
**Origem**: Migrado de `test-phase2.ps1` e `test-phase2-full.ps1`

Testa identificação e registro de nós **com criptografia de payload**:
- ✅ Registro de nó com payload criptografado
- ✅ Identificação de nó desconhecido
- ✅ Identificação de nó pendente (Pending)
- ✅ Identificação de nó autorizado (Authorized)
- ✅ Listagem de nós registrados
- ✅ Atualização de status de nós (aprovação/rejeição)
- ✅ Validação de cabeçalho X-Channel-Id
- ✅ Verificação de transição para Fase 3

**Cobertura**: 100% dos cenários de Fase 2 dos scripts PowerShell

### 3. `EncryptedChannelIntegrationTests.cs`
**Origem**: Migrado de `test-phase2-encrypted.ps1`

Testa fluxo completo entre dois nós:
- ✅ Workflow completo: Canal → Registro → Identificação → Aprovação
- ✅ Criptografia end-to-end de payloads
- ✅ Derivação de chaves simétricas (HKDF)
- ✅ Detecção de chave incorreta
- ✅ Validação de canal expirado/inválido

**Cobertura**: Testes de integração completos mencionados nos scripts

### 4. `SecurityAndEdgeCaseTests.cs`
**Origem**: Novos testes (expandindo cobertura além dos scripts)

Testa segurança e casos extremos:

#### Validação de Timestamp
- ✅ Timestamp muito antigo (> 5 minutos)
- ✅ Timestamp futuro
- ✅ Proteção contra replay attacks

#### Validação de Nonce
- ✅ Nonce inválido (formato base64)
- ✅ Nonce muito curto (< 16 bytes)

#### Proteção contra Tampering
- ✅ Dados criptografados adulterados
- ✅ Authentication tag adulterada
- ✅ Detecção de modificação de payload

#### Validação de Certificados
- ✅ Certificado expirado
- ✅ Formato de certificado inválido
- ✅ Certificado sem chave privada

#### Estado do Canal
- ✅ Canal inexistente
- ✅ Requisição sem cabeçalho X-Channel-Id
- ✅ Algoritmo de troca de chaves não suportado

#### Gestão de Nós
- ✅ Atualização de status para nó inexistente
- ✅ Status inválido
- ✅ Registro duplicado

#### Validação de Dados
- ✅ NodeId vazio
- ✅ NodeName vazio
- ✅ Campos obrigatórios ausentes

**Cobertura**: ~30 cenários adicionais de segurança e edge cases

### 5. `CertificateAndSignatureTests.cs`
**Origem**: Testa endpoints `/api/testing/*` usados pelos scripts PowerShell

Testa geração e validação de certificados:

#### Geração de Certificados
- ✅ Geração com parâmetros válidos
- ✅ Validade customizada (anos)
- ✅ Validação de nome vazio

#### Assinatura Digital
- ✅ Assinatura de dados com chave privada
- ✅ Algoritmo RSA-SHA256
- ✅ Senha incorreta
- ✅ Certificado inválido

#### Verificação de Assinatura
- ✅ Assinatura válida retorna true
- ✅ Dados adulterados retornam false
- ✅ Certificado errado retorna false

#### Geração de Identidade Completa
- ✅ Pacote completo (certificado + assinatura)
- ✅ Signature válida pode ser verificada
- ✅ Formato pronto para `/api/channel/identify`

#### Testes Diretos do CertificateHelper
- ✅ Geração de certificado auto-assinado
- ✅ Export/Import com chave privada (PFX)
- ✅ Assinatura e verificação
- ✅ Detecção de adulteração

**Cobertura**: 100% dos endpoints de teste usados pelos scripts

### 6. `NodeChannelClientTests.cs`
**Origem**: Simula o lado cliente dos scripts PowerShell

Testa o cliente de comunicação entre nós:

#### Iniciação de Canal
- ✅ Estabelecimento com URL válida
- ✅ Erro com URL inválida

#### Registro de Nós
- ✅ Registro após estabelecimento de canal
- ✅ Payload criptografado automaticamente

#### Identificação de Nós
- ✅ Nó desconhecido retorna Unknown
- ✅ Nó registrado retorna Pending
- ✅ Transição de status

#### Workflow Completo
- ✅ Fluxo end-to-end simulando scripts PowerShell:
  1. Iniciar canal (Node A → Node B)
  2. Registrar nó
  3. Identificar (Pending)
  4. Aprovar (Admin)
  5. Identificar novamente (Authorized)
  6. Verificar NextPhase = "phase3_authenticate"

#### Tratamento de Erros
- ✅ ChannelId inválido
- ✅ Assinatura inválida

**Cobertura**: Equivalente aos scripts PowerShell completos

### 7. `TestWebApplicationFactory.cs`
Factory para criação de instâncias de teste da aplicação Web.

## 📊 Mapeamento: Scripts PowerShell → Testes C#

| Script PowerShell | Arquivo de Teste C# | Status |
|-------------------|---------------------|---------|
| `test-docker.ps1` | `Phase1ChannelEstablishmentTests.cs` | ✅ Migrado |
| `test-phase2.ps1` | `Phase2NodeIdentificationTests.cs` | ✅ Migrado |
| `test-phase2-full.ps1` | `Phase2NodeIdentificationTests.cs` + `NodeChannelClientTests.cs` | ✅ Migrado |
| `test-phase2-encrypted.ps1` | `EncryptedChannelIntegrationTests.cs` | ✅ Migrado |
| Cenários não testados nos scripts | `SecurityAndEdgeCaseTests.cs` | ✅ Adicionado |
| Endpoints `/api/testing/*` | `CertificateAndSignatureTests.cs` | ✅ Adicionado |

## 🚀 Como Executar os Testes

### Via Visual Studio
1. Abra o Test Explorer (`Test` > `Test Explorer`)
2. Clique em "Run All"

### Via CLI (.NET)
```powershell
# Todos os testes
dotnet test

# Testes específicos
dotnet test --filter "FullyQualifiedName~Phase1"
dotnet test --filter "FullyQualifiedName~Phase2"
dotnet test --filter "FullyQualifiedName~Security"
dotnet test --filter "FullyQualifiedName~Certificate"

# Com cobertura de código
dotnet test --collect:"XPlat Code Coverage"
```

### Via Rider
1. Clique com botão direito no projeto de testes
2. Selecione "Run Unit Tests"

## 📝 Vantagens dos Testes C# sobre Scripts PowerShell

### ✅ Integração com CI/CD
- Execução automática em pipelines
- Relatórios de cobertura
- Integração com ferramentas de qualidade

### ✅ Debugging
- Breakpoints em qualquer linha
- Inspeção de variáveis
- Stack traces completos

### ✅ Manutenibilidade
- IntelliSense e autocompletar
- Refatoração segura
- Type safety

### ✅ Performance
- Execução paralela de testes
- Reutilização de fixtures
- Otimização pelo compilador

### ✅ Cobertura de Código
- Métricas precisas
- Identificação de código não testado
- Relatórios visuais

### ✅ Isolamento
- Cada teste é independente
- State limpo entre testes
- Mock e stub fáceis

## 🔍 Cobertura de Código

### Estimativa Atual:
- **Fase 1 (Channel)**: ~95%
- **Fase 2 (Identification)**: ~100%
- **Services (Encryption)**: ~85%
- **Services (Certificate)**: ~90%
- **Controllers**: ~80%

### Áreas Não Cobertas:
- Cenários de alta concorrência
- Testes de carga/stress
- Persistência em banco de dados (quando implementada)
- Logging e auditoria (parcial)

## 🛠️ Tecnologias Utilizadas

- **xUnit**: Framework de testes
- **FluentAssertions**: Asserções fluentes e legíveis
- **WebApplicationFactory**: Testes de integração ASP.NET Core
- **System.Security.Cryptography**: Operações criptográficas

## 📚 Próximos Passos

### Testes a Adicionar:
1. ✅ ~~Fase 2 com criptografia completa~~ (Concluído)
2. ✅ ~~Testes de tampering e segurança~~ (Concluído)
3. ✅ ~~Testes de certificados e assinaturas~~ (Concluído)
4. ⏳ Fase 3: Autenticação e operações
5. ⏳ Testes de performance/carga
6. ⏳ Testes de persistência (quando implementado)
7. ⏳ Testes de auditoria e logging

### Melhorias:
- Adicionar testes de concorrência (múltiplos clientes simultâneos)
- Implementar testes de stress (milhares de requisições)
- Adicionar testes de latência e throughput
- Criar testes de cenários de rede instável
- Implementar testes de recuperação de falhas

## 📖 Documentação Relacionada

- `/docs/testing/manual-testing-guide.md` - Guia de testes manuais
- `/docs/architecture/handshake-protocol.md` - Protocolo de handshake
- `/docs/development/channel-encryption-implementation.md` - Implementação de criptografia
- `/TESTING.md` - Visão geral de testes do projeto

## 🤝 Contribuindo

Ao adicionar novos testes:
1. Siga o padrão AAA (Arrange, Act, Assert)
2. Use nomes descritivos para os testes
3. Adicione comentários explicando cenários complexos
4. Mantenha testes independentes (sem dependências entre eles)
5. Limpe recursos (Dispose) quando necessário
6. Atualize este README com novos testes adicionados

## ⚠️ Notas Importantes

- Todos os certificados gerados são **auto-assinados** e apenas para testes
- Não use certificados de teste em produção
- Os endpoints `/api/testing/*` devem ser **desabilitados em produção**
- Chaves simétricas são limpas da memória após uso (Array.Clear)
- Os testes criam instâncias isoladas da aplicação (TestWebApplicationFactory)

## 📞 Suporte

Para dúvidas ou problemas com os testes:
1. Consulte a documentação em `/docs/testing/`
2. Verifique os logs de execução dos testes
3. Execute testes individuais para isolar problemas
4. Revise os scripts PowerShell originais para comparação

