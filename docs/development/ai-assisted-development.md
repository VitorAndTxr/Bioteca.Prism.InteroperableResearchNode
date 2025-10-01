# Padrões de Desenvolvimento Assistido por IA

**Última atualização**: 2025-10-01

## Visão Geral

Este documento define padrões e práticas para desenvolvimento do IRN com assistência de IA (Claude Code), garantindo que o contexto seja mantido e o desenvolvimento seja eficiente e consistente.

## Princípios Fundamentais

### 1. Documentação como Fonte da Verdade
- Toda decisão arquitetural importante é documentada em Markdown
- A IA deve sempre consultar a documentação antes de implementar
- O código implementado deve referenciar a documentação correspondente

### 2. Contexto Incremental
- Cada sessão de desenvolvimento deve ter escopo claro e limitado
- Documentação de progresso após cada implementação significativa
- Uso de TODOs e status para rastreamento

### 3. Rastreabilidade
- Commits devem referenciar a documentação relevante
- Issues/PRs devem linkar para docs específicos
- Código deve incluir comentários com referências aos docs

## Estrutura de Documentação

### Tipos de Documentos

1. **Arquitetura** (`docs/architecture/`)
   - Decisões de design de alto nível
   - Fluxos de comunicação
   - Diagramas e especificações

2. **Desenvolvimento** (`docs/development/`)
   - Guias de implementação
   - Padrões de código
   - Configurações de ambiente

3. **API** (`docs/api/`)
   - Especificações de endpoints
   - Formatos de mensagens
   - Exemplos de requests/responses

4. **Features** (`docs/features/`)
   - Descrição de funcionalidades específicas
   - Estado de implementação
   - Testes e validação

### Template de Documento

Cada documento deve seguir esta estrutura:

```markdown
# Título da Feature/Componente

**Status**: 📋 Planejado | 🚧 Em desenvolvimento | ✅ Implementado | ⚠️ Bloqueado
**Última atualização**: YYYY-MM-DD
**Responsável**: Nome/IA

## Visão Geral
[Descrição concisa do que é e por que existe]

## Objetivos
[Lista de objetivos mensuráveis]

## Arquitetura/Design
[Detalhes técnicos, diagramas, fluxos]

## Implementação

### Estado Atual
[O que já foi feito]

### Próximos Passos
- [ ] Tarefa 1
- [ ] Tarefa 2

### Referências de Código
[Links para arquivos e linhas específicas]

## Testes
[Cenários de teste e validação]

## Contexto para IA

### Prompt Sugerido
```
[Prompt específico para a IA continuar o trabalho]
```

### Dependências
- Depende de: [links para docs relacionados]
- É dependência de: [links para docs relacionados]

## Referências
[Links externos, RFCs, papers, etc.]
```

## Workflow com Claude Code

### 1. Planejamento de Feature

```bash
# Passo 1: Criar documento da feature
# A IA deve criar o documento com status 📋 Planejado

# Passo 2: Revisar e aprovar o design
# Desenvolvedor revisa e aprova/ajusta

# Passo 3: Atualizar status para 🚧 Em desenvolvimento
```

### 2. Implementação Iterativa

```bash
# Sessão 1: Criar models e interfaces
claude "Implementar os models definidos em docs/architecture/handshake-protocol.md,
        começando por NodeInfo, HandshakeRequest e HandshakeResponse"

# Sessão 2: Implementar serviços
claude "Implementar NodeAuthenticationService conforme especificado em
        docs/architecture/handshake-protocol.md seção Implementação"

# Sessão 3: Criar controller
claude "Criar NodeHandshakeController com os endpoints da Fase 1 do protocolo
        definido em docs/architecture/handshake-protocol.md"

# Sessão 4: Adicionar testes
claude "Criar testes unitários para NodeAuthenticationService baseado nos
        cenários em docs/architecture/handshake-protocol.md seção Testes"
```

### 3. Documentação de Progresso

Após cada implementação significativa:

```markdown
## Implementação

### Estado Atual
✅ Models criados (NodeInfo, HandshakeRequest, HandshakeResponse)
✅ Interface INodeAuthenticationService definida
🚧 NodeAuthenticationService em implementação (50%)
📋 Controller pendente

### Referências de Código
- Models/Node/NodeInfo.cs:1-25
- Models/Node/HandshakeRequest.cs:1-18
- Services/Node/INodeAuthenticationService.cs:1-12
- Services/Node/NodeAuthenticationService.cs:1-45 (parcial)
```

## Padrões de Prompts

### Iniciar Nova Feature

```
Analisar docs/architecture/[nome-do-doc].md e criar a estrutura inicial
de pastas, interfaces e models conforme especificado. Seguir os padrões
do projeto e referenciar a documentação nos comentários do código.
```

### Continuar Implementação

```
Continuar implementação de [componente] conforme docs/[caminho].md.
O estado atual está documentado na seção "Estado Atual". Implementar
os próximos itens da lista de "Próximos Passos".
```

### Debugging

```
Investigar erro em [componente]. Consultar docs/[caminho].md para
entender o comportamento esperado e identificar a causa raiz.
```

### Refatoração

```
Refatorar [componente] para melhorar [aspecto]. Manter compatibilidade
com a especificação em docs/[caminho].md e atualizar a documentação
se necessário.
```

## Boas Práticas

### 1. Comentários no Código

```csharp
/// <summary>
/// Implementa a Fase 1 do protocolo de handshake entre nós IRN.
/// Especificação: docs/architecture/handshake-protocol.md#fase-1-descoberta-inicial
/// </summary>
public class NodeHandshakeController : ControllerBase
{
    /// <summary>
    /// Endpoint inicial do handshake. Recebe HELLO e retorna HELLO_ACK.
    /// Ver: docs/architecture/handshake-protocol.md (Payload HELLO)
    /// </summary>
    [HttpPost("hello")]
    public async Task<ActionResult<HelloAckResponse>> Hello([FromBody] HelloRequest request)
    {
        // Implementação
    }
}
```

### 2. Commits

```bash
git commit -m "feat(node): implement handshake Phase 1 - HELLO/HELLO_ACK

Implements initial discovery phase of IRN node handshake protocol.

References: docs/architecture/handshake-protocol.md

- Add NodeInfo, HelloRequest, HelloAckResponse models
- Add POST /api/node/v1/handshake/hello endpoint
- Add basic validation for protocol version

🤖 Generated with Claude Code
Co-Authored-By: Claude <noreply@anthropic.com>"
```

### 3. Issues e Pull Requests

```markdown
## Descrição
Implementa a Fase 1 do protocolo de handshake conforme especificado em
`docs/architecture/handshake-protocol.md`.

## Checklist
- [x] Models criados
- [x] Controller implementado
- [x] Testes unitários adicionados
- [ ] Testes de integração (pendente)

## Documentação
- Especificação: [docs/architecture/handshake-protocol.md](../docs/architecture/handshake-protocol.md)
- Status atualizado no documento

## Testes
Todos os cenários da seção "Testes" do documento foram implementados.
```

## Gerenciamento de Contexto

### Contexto por Sessão

A IA deve sempre começar uma sessão:

1. **Lendo o estado atual** do documento relevante
2. **Verificando referências de código** existentes
3. **Consultando dependências** documentadas
4. **Identificando próximos passos** claros

### Exemplo de Início de Sessão

```
# IA inicia lendo:
1. docs/architecture/handshake-protocol.md (feature principal)
2. docs/architecture/node-communication.md (arquitetura geral)
3. Código existente referenciado na seção "Estado Atual"

# IA identifica:
- Status: 🚧 Em desenvolvimento (60%)
- Última implementação: NodeAuthenticationService parcial
- Próximo passo: Completar método ValidateHandshake()

# IA propõe:
"Vou continuar a implementação de NodeAuthenticationService.ValidateHandshake(),
seguindo a especificação da Fase 2 em docs/architecture/handshake-protocol.md"
```

### Salvando Contexto ao Final

Ao final de cada sessão, a IA deve atualizar:

1. **Status do documento** (progresso %)
2. **Seção "Estado Atual"** com o que foi feito
3. **Seção "Próximos Passos"** com TODOs atualizados
4. **Referências de Código** com novos arquivos/linhas

## Trabalhando com Features Grandes

### Quebra em Sub-Features

Feature grande: "Sistema de Comunicação entre Nós"

```
docs/architecture/
├── node-communication.md (overview)
├── handshake-protocol.md (sub-feature 1)
├── session-management.md (sub-feature 2)
└── data-exchange.md (sub-feature 3)
```

Cada sub-feature pode ser implementada independentemente.

### Roadmap de Implementação

```markdown
## Roadmap

### Fase 1: Handshake (2 semanas)
- [x] Documentação completa
- [ ] Implementação Fase 1 (HELLO)
- [ ] Implementação Fase 2 (AUTH)
- [ ] Implementação Fase 3 (SESSION_CREATE)
- [ ] Testes end-to-end

### Fase 2: Session Management (1 semana)
- [x] Documentação completa
- [ ] Stores (InMemory e Redis)
- [ ] Controllers e endpoints
- [ ] Background services
- [ ] Testes

### Fase 3: Data Exchange (2 semanas)
- [ ] Documentação
- [ ] Query routing
- [ ] Data serialization
- [ ] Testes de performance
```

## Sincronização com Projeto Claude

### Criar Projeto no Claude

1. Acessar [claude.ai](https://claude.ai)
2. Criar novo projeto "IRN Development"
3. Adicionar documentos principais:
   - `docs/README.md`
   - `docs/architecture/*.md`
   - `docs/development/*.md`

### Uso do Projeto

```bash
# Durante desenvolvimento, referenciar o projeto
@IRN-Development "Implementar próxima fase do handshake"

# O projeto fornece contexto automático dos docs
```

### Atualização do Projeto

Sempre que documentos são significativamente alterados:

1. Fazer commit das mudanças
2. Atualizar documentos no projeto Claude
3. Verificar que o contexto está sincronizado

## Checklist de Qualidade

Antes de considerar uma feature completa:

- [ ] Documentação atualizada com status ✅
- [ ] Código implementado conforme especificação
- [ ] Comentários referenciam documentação
- [ ] Testes cobrem cenários documentados
- [ ] Commit message referencia docs
- [ ] "Estado Atual" e "Referências de Código" atualizados
- [ ] Próxima feature identificada ou documentada

## Exemplo Completo: Feature do Início ao Fim

### 1. Criação

```bash
claude "Documentar protocolo de handshake entre nós IRN. Criar documento
        em docs/architecture/handshake-protocol.md seguindo o template
        de docs/development/ai-assisted-development.md"
```

**Resultado**: `docs/architecture/handshake-protocol.md` criado com status 📋

### 2. Implementação - Sessão 1

```bash
claude "Implementar models do handshake conforme docs/architecture/handshake-protocol.md
        seção Fase 1. Criar NodeInfo, HelloRequest e HelloAckResponse."
```

**Resultado**:
- `Models/Node/NodeInfo.cs` criado
- Documento atualizado: status 🚧, progresso 20%

### 3. Implementação - Sessão 2

```bash
claude "Continuar handshake. Implementar NodeHandshakeController com endpoint
        /api/node/v1/handshake/hello conforme docs/architecture/handshake-protocol.md"
```

**Resultado**:
- `Controllers/Node/NodeHandshakeController.cs` criado
- Documento atualizado: progresso 40%

### 4. Implementação - Sessão 3

```bash
claude "Implementar Fase 2 (autenticação) do handshake. Ver
        docs/architecture/handshake-protocol.md seção Fase 2"
```

**Resultado**:
- `Services/Node/NodeAuthenticationService.cs` criado
- Endpoints AUTH_CHALLENGE e AUTH_RESPONSE adicionados
- Documento atualizado: progresso 70%

### 5. Testes

```bash
claude "Criar testes para o handshake baseado nos cenários em
        docs/architecture/handshake-protocol.md seção Testes"
```

**Resultado**:
- `Tests/Node/HandshakeProtocolTests.cs` criado
- Documento atualizado: progresso 90%

### 6. Finalização

```bash
claude "Revisar implementação do handshake, adicionar logging e tratamento
        de erros conforme docs/architecture/handshake-protocol.md"
```

**Resultado**:
- Refinamentos aplicados
- Documento atualizado: status ✅, progresso 100%
- "Próximos Passos" atualizado para apontar próxima feature

## Ferramentas e Scripts

### Script para Verificar Sincronização

```bash
#!/bin/bash
# check-doc-sync.sh

# Verifica se código implementado tem documentação correspondente
echo "Verificando sincronização docs <-> código..."

# Lista controllers sem documentação
# Lista TODOs não implementados
# Lista referências quebradas em docs
```

### Script para Gerar Resumo de Progresso

```bash
#!/bin/bash
# progress-report.sh

# Gera relatório de progresso baseado nos status dos docs
echo "# Relatório de Progresso IRN"
echo ""

for doc in docs/**/*.md; do
    status=$(grep "^\*\*Status\*\*:" "$doc" | head -1)
    echo "- [$doc]: $status"
done
```

## Conclusão

Seguindo esses padrões:

✅ **Contexto sempre disponível** - Docs servem como memória persistente
✅ **Desenvolvimento incremental** - Cada sessão tem escopo claro
✅ **Rastreabilidade** - Fácil entender estado e decisões passadas
✅ **Colaboração humano-IA** - IA e desenvolvedor trabalham no mesmo contexto
✅ **Qualidade consistente** - Padrões garantem código uniforme

**Lembre-se**: A documentação é para humanos E para a IA. Mantenha-a atualizada!
