# Teste de Comunicação Fase 1 - Docker

Este guia mostra como testar a Fase 1 do protocolo de handshake usando Docker Compose para orquestrar dois nós.

## Pré-requisitos

- Docker Desktop instalado
- Docker Compose (geralmente incluído no Docker Desktop)

## Estrutura Docker

```
docker-compose.yml          # Orquestração de 2 nós
├── node-a (container)      # Nó A na porta 5000
│   └── Porta interna: 8080
│   └── Porta externa: 5000
└── node-b (container)      # Nó B na porta 5001
    └── Porta interna: 8080
    └── Porta externa: 5001
```

## Passo 1: Build e Iniciar os Nós

No diretório raiz do projeto, execute:

```bash
docker-compose up --build
```

Aguarde até ver as mensagens:
```
irn-node-a  | info: Microsoft.Hosting.Lifetime[14]
irn-node-a  |       Now listening on: http://[::]:8080

irn-node-b  | info: Microsoft.Hosting.Lifetime[14]
irn-node-b  |       Now listening on: http://[::]:8080
```

## Passo 2: Executar Testes Automatizados

### Windows (PowerShell):
```powershell
.\test-docker.ps1
```

### Linux/Mac:
```bash
chmod +x test-docker.sh
./test-docker.sh
```

## Passo 3: Testes Manuais

### Teste 1: Health Check

**Nó A:**
```bash
curl http://localhost:5000/api/channel/health
```

**Nó B:**
```bash
curl http://localhost:5001/api/channel/health
```

**Resposta esperada:**
```json
{
  "status": "healthy",
  "timestamp": "2025-10-01T12:00:00Z"
}
```

### Teste 2: Handshake Nó A → Nó B

```bash
curl -X POST http://localhost:5000/api/channel/initiate \
  -H "Content-Type: application/json" \
  -d '{"remoteNodeUrl": "http://node-b:8080"}'
```

**⚠️ Importante:** Note que usamos `http://node-b:8080` (nome do container interno), não `localhost:5001`.

**Resposta esperada:**
```json
{
  "success": true,
  "channelId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "selectedCipher": "AES-256-GCM",
  "remoteNodeUrl": "http://node-b:8080"
}
```

### Teste 3: Handshake Nó B → Nó A

```bash
curl -X POST http://localhost:5001/api/channel/initiate \
  -H "Content-Type: application/json" \
  -d '{"remoteNodeUrl": "http://node-a:8080"}'
```

### Teste 4: Verificar Canal

Usando o `channelId` retornado:

```bash
# No Nó A (lado cliente)
curl http://localhost:5000/api/channel/{channelId}

# No Nó B (lado servidor)
curl http://localhost:5001/api/channel/{channelId}
```

## Visualizar Logs

### Ver logs de ambos os nós:
```bash
docker-compose logs -f
```

### Ver logs apenas do Nó A:
```bash
docker-compose logs -f node-a
```

### Ver logs apenas do Nó B:
```bash
docker-compose logs -f node-b
```

## Comandos Úteis

### Parar os nós:
```bash
docker-compose down
```

### Reconstruir e reiniciar:
```bash
docker-compose up --build --force-recreate
```

### Ver status dos containers:
```bash
docker-compose ps
```

### Acessar shell de um container:
```bash
docker exec -it irn-node-a /bin/bash
# ou
docker exec -it irn-node-b /bin/bash
```

## Rede Docker

Os nós estão na mesma rede Docker (`irn-network`), permitindo comunicação direta:
- **node-a** → pode acessar **node-b** via `http://node-b:8080`
- **node-b** → pode acessar **node-a** via `http://node-a:8080`

Do host (sua máquina):
- **Nó A**: `http://localhost:5000`
- **Nó B**: `http://localhost:5001`

## Troubleshooting

### Erro: "address already in use"
Algum serviço já está usando as portas 5000 ou 5001. Altere as portas no `docker-compose.yml`:
```yaml
ports:
  - "5002:8080"  # Mudar para porta disponível
```

### Erro: "Cannot connect to Docker daemon"
Certifique-se de que o Docker Desktop está rodando.

### Nós não conseguem se comunicar
Verifique se estão na mesma rede:
```bash
docker network inspect interoperableresearchnode_irn-network
```

### Limpar tudo e recomeçar
```bash
docker-compose down -v
docker-compose up --build
```

## Validações de Segurança

### 1. Perfect Forward Secrecy
Reinicie os containers:
```bash
docker-compose restart
```
Os canais antigos não existem mais e novas chaves efêmeras são geradas.

### 2. Chaves Efêmeras Diferentes
Execute o handshake múltiplas vezes. Cada vez, novas chaves efêmeras são geradas:
```bash
curl -X POST http://localhost:5000/api/channel/initiate \
  -H "Content-Type: application/json" \
  -d '{"remoteNodeUrl": "http://node-b:8080"}'
```
Os `channelId` serão diferentes a cada vez.

### 3. Validação de Chaves
Os logs devem mostrar:
```
Generated ephemeral ECDH key pair with curve P384
Derived shared secret (48 bytes)
Channel {guid} established successfully with cipher AES-256-GCM
```

## Próximos Passos

Após validar a Fase 1 no Docker:
1. Implementar Fase 2 (Identificação e Autorização)
2. Adicionar persistência com volumes Docker
3. Configurar Docker para produção com HTTPS
4. Implementar testes de integração automatizados

## Vantagens do Docker

✅ Isolamento completo entre nós
✅ Fácil replicação do ambiente
✅ Testes reproduzíveis
✅ Simula ambiente de produção
✅ Fácil adicionar mais nós (escalar para 3, 4, N nós)
