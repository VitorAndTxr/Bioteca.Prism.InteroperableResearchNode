# Documentação do IRN - Interoperable Research Node

Esta pasta contém toda a documentação técnica e de desenvolvimento do projeto IRN.

## Estrutura da Documentação

### 1. Arquitetura e Design
- [`architecture/node-communication.md`](architecture/node-communication.md) - Arquitetura de comunicação entre nós
- [`architecture/handshake-protocol.md`](architecture/handshake-protocol.md) - Protocolo de handshake e autenticação
- [`architecture/session-management.md`](architecture/session-management.md) - Gerenciamento de sessões entre nós

### 2. Desenvolvimento
- [`development/ai-assisted-development.md`](development/ai-assisted-development.md) - Padrões de desenvolvimento assistido por IA
- [`development/implementation-roadmap.md`](development/implementation-roadmap.md) - Roadmap de implementação
- [`development/coding-standards.md`](development/coding-standards.md) - Padrões de código e boas práticas

### 3. API e Protocolos
- [`api/node-endpoints.md`](api/node-endpoints.md) - Especificação dos endpoints de comunicação
- [`api/message-formats.md`](api/message-formats.md) - Formatos de mensagens e payloads

### 4. Contexto de Desenvolvimento
Cada documento mantém o contexto necessário para retomar o desenvolvimento com assistência de IA, incluindo:
- Estado atual da implementação
- Decisões arquiteturais tomadas
- Próximos passos
- Exemplos de código e testes

## Como usar com Claude Code

Ao trabalhar em uma feature específica, referencie o documento relevante:

```bash
# Exemplo: trabalhando no handshake
claude "Implemente o próximo passo do @docs/architecture/handshake-protocol.md"
```

## Convenções

- ✅ Itens implementados
- 🚧 Em desenvolvimento
- 📋 Planejado
- ⚠️ Bloqueado/Pendente decisão
