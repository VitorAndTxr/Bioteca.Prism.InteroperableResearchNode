# Arquitetura de Comunicação entre Nós IRN

**Status**: 📋 Planejado
**Última atualização**: 2025-10-01

## Visão Geral

Define a arquitetura de comunicação entre instâncias do Interoperable Research Node (IRN), permitindo que múltiplos nós formem uma rede federada para compartilhamento de dados de pesquisa biomédica.

## Princípios Arquiteturais

### 1. Descentralização
- Não há servidor central; cada nó é autônomo
- Comunicação peer-to-peer (P2P)
- Descoberta de nós via registry distribuído ou DNS

### 2. Segurança por Design
- Todos os dados são criptografados em trânsito (TLS 1.3+)
- Autenticação mútua obrigatória
- Zero-trust: verificação contínua durante a sessão

### 3. Interoperabilidade
- API RESTful padronizada
- Versionamento de protocolo
- Suporte a múltiplos formatos (JSON, Protocol Buffers)

### 4. Resiliência
- Timeout e retry configuráveis
- Circuit breaker para nós indisponíveis
- Fallback para cache local quando necessário

## Componentes da Arquitetura

```
┌─────────────────────────────────────────────────────────────┐
│                       IRN Instance A                        │
│  ┌────────────────┐  ┌─────────────────┐  ┌──────────────┐ │
│  │ API Gateway    │  │ Node Registry   │  │ Auth Manager │ │
│  └────────────────┘  └─────────────────┘  └──────────────┘ │
│  ┌────────────────┐  ┌─────────────────┐  ┌──────────────┐ │
│  │ Session Mgr    │  │ Data Exchange   │  │ Query Router │ │
│  └────────────────┘  └─────────────────┘  └──────────────┘ │
│  ┌────────────────────────────────────────────────────────┐ │
│  │            Local Data Store & Validation                │ │
│  └────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
                              │
                              │ HTTPS/TLS 1.3
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                       IRN Instance B                        │
│                      (mesma estrutura)                      │
└─────────────────────────────────────────────────────────────┘
```

## Camadas de Comunicação

### Camada 1: Transporte
- **Protocolo**: HTTPS (HTTP/2 ou HTTP/3)
- **Porta padrão**: 8443 (configurável)
- **TLS**: Versão 1.3 mínima
- **Compressão**: gzip, brotli

### Camada 2: Autenticação
- **Handshake**: Protocolo customizado sobre HTTPS
- **Tokens**: JWT com curta duração (15 min)
- **Refresh**: Tokens de refresh para renovação
- **Revogação**: Blacklist distribuída de tokens

### Camada 3: Sessão
- **Gerenciamento**: Estado de sessão em memória ou Redis
- **Timeout**: 30 minutos de inatividade
- **Keep-alive**: Heartbeat a cada 5 minutos

### Camada 4: Aplicação
- **API REST**: Endpoints padronizados
- **Versionamento**: Semântico (v1, v2, etc.)
- **Rate Limiting**: Configurável por nó e endpoint

## Fluxo de Comunicação Típico

### 1. Descoberta de Nós
```
Cliente --> [DNS/Registry] --> Lista de Nós IRN disponíveis
```

### 2. Estabelecimento de Conexão
```
Nó A --> [Handshake] --> Nó B
      <-- [Session ID] <--
```

### 3. Troca de Dados
```
Nó A --> [Query Request] --> Nó B
      <-- [Query Results] <--
```

### 4. Encerramento
```
Nó A --> [Session Close] --> Nó B
      <-- [ACK] <--
```

## Endpoints Principais da API Node-to-Node

### Handshake e Autenticação
- `POST /api/node/v1/handshake/hello` - Iniciar handshake
- `POST /api/node/v1/handshake/auth` - Autenticação mútua
- `POST /api/node/v1/session/create` - Criar sessão
- `DELETE /api/node/v1/session/{id}` - Encerrar sessão

### Descoberta e Informações
- `GET /api/node/v1/info` - Informações do nó
- `GET /api/node/v1/capabilities` - Capacidades suportadas
- `GET /api/node/v1/health` - Status de saúde

### Troca de Dados
- `POST /api/node/v1/query/metadata` - Consultar metadados
- `POST /api/node/v1/query/biosignal` - Consultar biossinais
- `GET /api/node/v1/data/{id}` - Recuperar dados específicos

## Descoberta de Nós

### Opção 1: Registry Centralizado (MVP)
- Registro manual de nós conhecidos
- Configuração em `appsettings.json`

```json
{
  "NodeRegistry": {
    "KnownNodes": [
      {
        "nodeId": "uuid-node-b",
        "name": "IRN-Lab-Beta",
        "endpoint": "https://irn-beta.example.com:8443",
        "publicKey": "base64-encoded-key",
        "trusted": true
      }
    ]
  }
}
```

### Opção 2: DNS-SD (Futuro)
- Service Discovery via DNS
- Registro automático usando mDNS/DNS-SD

### Opção 3: Blockchain/DLT (Pesquisa)
- Registro imutável de nós
- Reputação distribuída

## Segurança e Confiança

### Modelo de Confiança

1. **Whitelist**: Apenas nós pré-aprovados (MVP)
2. **Web of Trust**: Nós confiam em nós confiáveis por seus pares
3. **Certificate Authority**: PKI institucional

### Auditoria

Todas as comunicações entre nós devem ser registradas:

```json
{
  "eventType": "node-communication",
  "timestamp": "2025-10-01T12:34:56Z",
  "sourceNode": "uuid-node-a",
  "targetNode": "uuid-node-b",
  "action": "query-metadata",
  "result": "success",
  "dataTransferred": 1024,
  "userId": "researcher-xyz"
}
```

## Escalabilidade

### Limites Recomendados (MVP)
- **Conexões simultâneas**: 50 por nó
- **Requisições por minuto**: 1000
- **Tamanho máximo de payload**: 10 MB
- **Sessões ativas**: 100

### Estratégias de Escala
- Load balancing entre múltiplas instâncias IRN
- Cache distribuído (Redis)
- Compressão de dados
- Paginação em queries grandes

## Monitoramento

### Métricas Importantes
- Latência de handshake
- Taxa de sucesso de autenticação
- Throughput de dados
- Número de sessões ativas
- Taxa de erro por endpoint

### Health Checks
```json
{
  "status": "healthy",
  "version": "1.0.0",
  "uptime": 86400,
  "activeSessions": 23,
  "lastSync": "2025-10-01T12:00:00Z",
  "dependencies": {
    "database": "healthy",
    "certificateValidation": "healthy",
    "nodeRegistry": "healthy"
  }
}
```

## Implementação

### Estado Atual
📋 **Planejado** - Arquitetura definida, implementação pendente

### Estrutura de Pastas Proposta

```
Bioteca.Prism.InteroperableResearchNode/
├── Controllers/
│   └── Node/
│       ├── NodeHandshakeController.cs
│       ├── NodeSessionController.cs
│       ├── NodeQueryController.cs
│       └── NodeInfoController.cs
├── Services/
│   └── Node/
│       ├── INodeCommunicationService.cs
│       ├── NodeCommunicationService.cs
│       ├── INodeRegistryService.cs
│       ├── NodeRegistryService.cs
│       └── ISessionManagementService.cs
├── Models/
│   └── Node/
│       ├── NodeInfo.cs
│       ├── SessionInfo.cs
│       ├── QueryRequest.cs
│       └── NodeCapabilities.cs
├── Middleware/
│   └── NodeAuthenticationMiddleware.cs
└── Configuration/
    └── NodeConfiguration.cs
```

### Próximos Passos

1. Implementar protocolo de handshake (ver `handshake-protocol.md`)
2. Implementar gerenciamento de sessões (ver `session-management.md`)
3. Criar endpoints de query de dados
4. Implementar sistema de auditoria
5. Adicionar monitoramento e métricas

## Contexto para IA

### Prompt Sugerido

```
Com base na arquitetura definida em docs/architecture/node-communication.md,
criar a estrutura inicial de pastas e classes para comunicação entre nós.
Começar pelos Models e Interfaces de serviços.
```

## Referências

- REST API Best Practices
- Microservices Communication Patterns
- Federated Learning System Architecture
- FHIR Server-to-Server Communication
