# 🐳 Docker Quick Start - Fase 1

Teste rápido da Fase 1 do protocolo de handshake usando Docker.

## Iniciar

```bash
# 1. Build e iniciar os dois nós
docker-compose up --build

# 2. Em outro terminal, executar testes
.\test-docker.ps1   # Windows
# ou
./test-docker.sh    # Linux/Mac
```

## Resultado Esperado

```
=== Teste de Comunicação Fase 1 - Docker ===

[Teste 1] Health Check dos Nós
Nó A (porta 5000):
  ✓ Status: healthy

Nó B (porta 5001):
  ✓ Status: healthy

[Teste 2] Nó A → Nó B (Handshake)
  ✓ Canal estabelecido!
    Channel ID: 3fa85f64-5717-4562-b3fc-2c963f66afa6
    Cipher: AES-256-GCM

[Teste 3] Nó B → Nó A (Handshake)
  ✓ Canal estabelecido!
    Channel ID: 8b2c1a93-6428-4e81-a5df-9e7f32d1b0c5
    Cipher: AES-256-GCM

=== Testes Concluídos ===
```

## Acessar os Nós

- **Nó A**: http://localhost:5000
- **Nó B**: http://localhost:5001

## Parar

```bash
docker-compose down
```

## Documentação Completa

Ver `docs/testing/phase1-docker-test.md` para:
- Testes manuais detalhados
- Troubleshooting
- Comandos úteis
- Validações de segurança
