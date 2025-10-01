# Debugging com Docker no Visual Studio

Este guia explica como fazer debug dos nós rodando em Docker usando Visual Studio.

## Opção 1: Visual Studio Docker Tools (Recomendado)

O Visual Studio tem suporte nativo para debug em containers Docker.

### Passo 1: Configurar o Projeto

O projeto já está configurado com:
- ✅ `Dockerfile` no projeto
- ✅ `docker-compose.yml` na raiz

### Passo 2: Abrir a Solução

Abra `Bioteca.Prism.InteroperableResearchNode.sln` no Visual Studio.

### Passo 3: Configurar Docker Compose como Startup Project

1. No **Solution Explorer**, clique com botão direito em `docker-compose`
2. Selecione **"Set as Startup Project"**

### Passo 4: Escolher qual nó debugar

Edite `docker-compose.yml` para comentar o nó que você NÃO quer debugar:

```yaml
services:
  node-a:
    # ... configuração do node-a

  # node-b:  # Comentar se quiser debugar apenas node-a
  #   ...
```

### Passo 5: Iniciar Debug

1. Pressione **F5** ou clique em **"Docker Compose"** no botão de play
2. Visual Studio irá:
   - Build da imagem Docker
   - Iniciar o container
   - Anexar o debugger automaticamente

### Passo 6: Definir Breakpoints

- Coloque breakpoints normalmente no código (ex: `ChannelController.cs`)
- Faça requisições HTTP para os endpoints
- O debugger irá parar nos breakpoints! 🎯

## Opção 2: Attach to Process (Manual)

Se o método acima não funcionar, você pode anexar manualmente:

### Passo 1: Iniciar containers normalmente

```powershell
docker-compose up
```

### Passo 2: No Visual Studio

1. Vá em **Debug** → **Attach to Process** (ou `Ctrl+Alt+P`)
2. Em **Connection type**, selecione **"Docker (Linux Container)"**
3. Em **Connection target**, clique em **Find...**
4. Selecione o container `irn-node-a` ou `irn-node-b`
5. Selecione o processo `dotnet`
6. Clique **Attach**

### Passo 3: Código deve estar sincronizado

Certifique-se que o código no container é o mesmo do seu ambiente local.

## Opção 3: Remote Debugging (Avançado)

Para debug mais avançado, configure remote debugging:

### Passo 1: Instalar vsdbg no container

Adicione ao `Dockerfile`:

```dockerfile
# Add vsdbg for remote debugging
RUN apt-get update && apt-get install -y curl
RUN curl -sSL https://aka.ms/getvsdbgsh | /bin/sh /dev/stdin -v latest -l /vsdbg
```

### Passo 2: Expor porta de debug

No `docker-compose.yml`:

```yaml
ports:
  - "5000:8080"
  - "5010:5010"  # Debug port
```

### Passo 3: Configurar launch.json (VS Code)

Se estiver usando VS Code:

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Docker: Attach to Node A",
      "type": "coreclr",
      "request": "attach",
      "processId": "${command:pickRemoteProcess}",
      "pipeTransport": {
        "pipeCwd": "${workspaceRoot}",
        "pipeProgram": "docker",
        "pipeArgs": ["exec", "-i", "irn-node-a"],
        "debuggerPath": "/vsdbg/vsdbg",
        "quoteArgs": false
      },
      "sourceFileMap": {
        "/src": "${workspaceFolder}"
      }
    }
  ]
}
```

## Troubleshooting

### Breakpoints não estão sendo atingidos

**Causa**: Código no container não corresponde ao código local.

**Solução**:
```powershell
docker-compose down
docker-compose up --build --force-recreate
```

### "Unable to attach to process"

**Causa**: Container não está em modo debug ou vsdbg não instalado.

**Solução**: Use o `Dockerfile` com `BUILD_CONFIGURATION=Debug`

### Simbolos de debug não carregados

**Causa**: Build em Release mode.

**Solução**: Certifique-se que está usando Debug:
```dockerfile
ARG BUILD_CONFIGURATION=Debug
```

### Performance lenta durante debug

**Causa**: Docker no Windows tem overhead de I/O.

**Solução**:
- Use volumes apenas quando necessário
- Considere rodar localmente sem Docker para debug intensivo

## Dicas

✅ **Use logs**: `docker-compose logs -f node-a`
✅ **Inspecione variáveis**: Funciona normalmente no Visual Studio
✅ **Hot reload**: Funciona com volumes montados
✅ **Debug de ambos os nós**: Rode em 2 instâncias do Visual Studio

## Alternativa: Debug Local (Sem Docker)

Para desenvolvimento rápido, considere rodar localmente:

**Terminal 1:**
```powershell
cd Bioteca.Prism.InteroperableResearchNode
$env:ASPNETCORE_ENVIRONMENT="NodeA"
dotnet run --no-launch-profile
```

**Terminal 2:**
```powershell
cd Bioteca.Prism.InteroperableResearchNode
$env:ASPNETCORE_ENVIRONMENT="NodeB"
dotnet run --no-launch-profile
```

Então anexe o debugger aos processos `dotnet.exe` rodando.

## Próximos Passos

- Configure `.vscode/launch.json` para VS Code
- Configure perfis de debug no Visual Studio
- Adicione testes de integração com containers de teste
