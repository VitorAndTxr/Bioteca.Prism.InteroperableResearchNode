# 🐛 Debug Quick Start

Guia rápido para debugar os nós IRN localmente.

## Visual Studio

### Opção 1: Debug de Um Nó (Mais Simples)

1. Abra a solução no Visual Studio
2. Na barra de ferramentas, selecione o perfil de debug:
   - **"Node A (Debug)"** → Roda Node A na porta 5000
   - **"Node B (Debug)"** → Roda Node B na porta 5001
3. Pressione **F5**
4. Swagger abre automaticamente

### Opção 2: Debug de Ambos os Nós Simultaneamente

**Terminal 1:**
```powershell
cd Bioteca.Prism.InteroperableResearchNode
$env:ASPNETCORE_ENVIRONMENT="NodeA"
dotnet run
```

**No Visual Studio:**
1. Pressione **Ctrl+Alt+P** (Attach to Process)
2. Procure por `Bioteca.Prism.InteroperableResearchNode.exe`
3. Selecione a instância do **Node B**
4. Clique **Attach**

Agora você pode debugar Node B enquanto Node A roda normalmente!

### Opção 3: Duas Instâncias do Visual Studio

1. Abra **2 instâncias** do Visual Studio
2. Na primeira, selecione **"Node A (Debug)"** e pressione F5
3. Na segunda, selecione **"Node B (Debug)"** e pressione F5
4. Ambos podem ser debugados simultaneamente

## VS Code

### Debug de Um Nó

1. Abra a pasta no VS Code
2. Vá em **Run and Debug** (Ctrl+Shift+D)
3. Selecione no dropdown:
   - **"Debug Node A"** → Roda Node A na porta 5000
   - **"Debug Node B"** → Roda Node B na porta 5001
4. Pressione **F5**

### Debug de Ambos os Nós

1. Selecione **"Debug Both Nodes"** no dropdown
2. Pressione **F5**
3. Ambos os nós iniciam e podem ser debugados juntos!

## Testando a Comunicação

Com os nós rodando em debug:

### Via Swagger
- **Node A**: http://localhost:5000/swagger
- **Node B**: http://localhost:5001/swagger

### Via PowerShell

```powershell
# Node A inicia handshake com Node B
Invoke-RestMethod -Uri "http://localhost:5000/api/channel/initiate" `
  -Method Post `
  -ContentType "application/json" `
  -Body '{"remoteNodeUrl": "http://localhost:5001"}'
```

Coloque breakpoints em:
- `ChannelController.cs:168` - Endpoint `InitiateHandshake` (cliente)
- `ChannelController.cs:49` - Endpoint `OpenChannel` (servidor)

## Breakpoints Úteis

### Cliente (Nó que inicia handshake)
```
ChannelController.cs:168    - InitiateHandshake (entrada)
NodeChannelClient.cs:40     - OpenChannelAsync (lógica do cliente)
EphemeralKeyService.cs:18   - GenerateEphemeralKeyPair
ChannelEncryptionService.cs:19 - DeriveKey (HKDF)
```

### Servidor (Nó que recebe handshake)
```
ChannelController.cs:49     - OpenChannel (entrada)
EphemeralKeyService.cs:48   - ExportPublicKey
EphemeralKeyService.cs:74   - DeriveSharedSecret
```

## Variáveis Interessantes para Inspecionar

Durante debug, inspecione:
- `clientEcdh` / `serverEcdh` - Chaves ECDH efêmeras
- `sharedSecret` - Segredo compartilhado derivado
- `symmetricKey` - Chave simétrica final (AES-256)
- `channelId` - ID do canal estabelecido

## Hot Reload

Durante debug, você pode editar código e ele recarrega automaticamente:
- ✅ Edições em controllers
- ✅ Edições em services
- ✅ Mudanças em appsettings.json

## Troubleshooting

### "Port already in use"
Outro processo está usando a porta 5000 ou 5001:
```powershell
# Ver o que está usando a porta
netstat -ano | findstr :5000
# Matar o processo
taskkill /PID <PID> /F
```

### Breakpoints não estão sendo atingidos
1. Certifique-se que está em modo **Debug** (não Release)
2. Reconstrua a solução (Ctrl+Shift+B)
3. Limpe a solução e rebuild

### "Cannot find Bioteca.Prism.Service"
As dependências não foram restauradas:
```powershell
dotnet restore
```

## Performance

Para debug mais rápido:
- Use **"Node A (Debug)"** ou **"Node B (Debug)"**
- Evite Docker durante desenvolvimento intensivo
- Use Docker apenas para testes de integração

## Próximos Passos

- Adicione breakpoints condicionais
- Use Tracepoints para logging sem parar execução
- Configure IntelliTrace (Visual Studio Enterprise)
