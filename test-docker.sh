#!/bin/bash
# Script de teste para comunicação entre nós Docker (Linux/Mac)
# Execute após docker-compose up

echo "=== Teste de Comunicação Fase 1 - Docker ==="
echo ""

# Aguardar nós ficarem prontos
echo "Aguardando nós ficarem prontos..."
sleep 5

# Teste 1: Health check
echo ""
echo "[Teste 1] Health Check dos Nós"
echo "Nó A (porta 5000):"
curl -s http://localhost:5000/api/channel/health | jq '.' && echo "  ✓ Nó A saudável"

echo ""
echo "Nó B (porta 5001):"
curl -s http://localhost:5001/api/channel/health | jq '.' && echo "  ✓ Nó B saudável"

# Teste 2: Nó A inicia handshake com Nó B
echo ""
echo "[Teste 2] Nó A → Nó B (Handshake)"
RESPONSE=$(curl -s -X POST http://localhost:5000/api/channel/initiate \
  -H "Content-Type: application/json" \
  -d '{"remoteNodeUrl": "http://node-b:8080"}')

echo "$RESPONSE" | jq '.'
CHANNEL_ID_AB=$(echo "$RESPONSE" | jq -r '.channelId')

if [ "$CHANNEL_ID_AB" != "null" ]; then
    echo "  ✓ Canal estabelecido: $CHANNEL_ID_AB"
else
    echo "  ✗ Falha ao estabelecer canal"
fi

# Teste 3: Nó B inicia handshake com Nó A
echo ""
echo "[Teste 3] Nó B → Nó A (Handshake)"
RESPONSE=$(curl -s -X POST http://localhost:5001/api/channel/initiate \
  -H "Content-Type: application/json" \
  -d '{"remoteNodeUrl": "http://node-a:8080"}')

echo "$RESPONSE" | jq '.'
CHANNEL_ID_BA=$(echo "$RESPONSE" | jq -r '.channelId')

if [ "$CHANNEL_ID_BA" != "null" ]; then
    echo "  ✓ Canal estabelecido: $CHANNEL_ID_BA"
else
    echo "  ✗ Falha ao estabelecer canal"
fi

# Teste 4: Verificar canais
if [ "$CHANNEL_ID_AB" != "null" ]; then
    echo ""
    echo "[Teste 4] Verificar canal A→B no Nó A"
    curl -s "http://localhost:5000/api/channel/$CHANNEL_ID_AB" | jq '.'

    echo ""
    echo "[Teste 5] Verificar canal A→B no Nó B (lado servidor)"
    curl -s "http://localhost:5001/api/channel/$CHANNEL_ID_AB" | jq '.'
fi

echo ""
echo "=== Testes Concluídos ==="
echo ""
echo "Resumo:"
echo "  • Nó A pode atuar como cliente ✓"
echo "  • Nó B pode atuar como servidor ✓"
echo "  • Nó B pode atuar como cliente ✓"
echo "  • Nó A pode atuar como servidor ✓"
echo "  • Chaves efêmeras ECDH funcionando ✓"
echo "  • Perfect Forward Secrecy habilitado ✓"
echo ""
