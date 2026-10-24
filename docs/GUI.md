# Interface gráfica — arquitetura e operação

## Stack

- C# e .NET 10 LTS
- WinUI 3 / Windows App SDK 2.5.1 estável
- XAML com padrão MVVM
- publicação Windows x64, desempacotada e autocontida

O aplicativo é exclusivo para Windows e não precisa de Visual Studio nem do SDK .NET depois de publicado.

## Estrutura

```text
src/HytaleServerManager/
├── Infrastructure/       ObservableObject e comandos
├── Models/               configuração e snapshots
├── Services/             processos e persistência
├── ViewModels/           estado e ações da tela
├── App.xaml              recursos visuais globais
├── MainWindow.xaml       dashboard
└── HytaleServerManager.csproj
```

## Fluxo de inicialização

1. Carrega `%LOCALAPPDATA%\HytaleServerManager\settings.json` ou cria padrões em memória.
2. Descobre processos existentes do Java e Playit.gg.
3. Atualiza status, porta e métricas a cada segundo.
4. Ao iniciar, valida mundo, Java, JAR, assets, Playit.gg e limites de memória.
5. Abre o túnel se necessário.
6. Executa o Hytale sem janela de CMD, capturando stdout e stderr.
7. Encaminha as linhas para o console visual e identifica entrada/saída de jogadores.

## Encerramento

O botão **Salvar e parar servidor** escreve `/stop` na entrada padrão do processo e aguarda até 30 segundos. Se o servidor não encerrar, a interface informa o problema e não força a finalização. Isso evita corrupção do mundo.

Nesta primeira versão, a interface só consegue enviar `/stop` para a instância iniciada por ela. Uma instância preexistente é monitorada, mas deve ser encerrada no console em que foi aberta.

## Recursos

- `-Xms`: memória inicial configurável, padrão 768 MB.
- `-Xmx`: limite máximo configurável, padrão 4096 MB.
- CPU: diferença de tempo de processador do Java entre amostras.
- RAM: working set físico do processo Java.
- Uptime: diferença entre horário atual e início do processo.

## Segurança

- Nenhuma credencial é exibida ou salva pela interface.
- `auth.enc` continua na pasta do mundo.
- O agente Playit.gg usa a instalação local já autorizada.
- O firewall não é desligado nem alterado.
- A interface não oferece encerramento forçado do mundo.

## Build local

O SDK usado está definido em `global.json`. Para compilar:

```powershell
dotnet restore HytaleServerManager.slnx
dotnet build HytaleServerManager.slnx -c Debug
```

Para publicar de forma autocontida:

```powershell
dotnet publish src/HytaleServerManager/HytaleServerManager.csproj `
  -c Release -r win-x64 --self-contained true `
  -o "$env:LOCALAPPDATA\HytaleServerManager\app"
```

## Próximas etapas

1. gráficos históricos de CPU, RAM e rede;
2. backup manual e agendado;
3. editor completo de caminhos e seleção de mundo;
4. pesquisa e filtros do console;
5. notificações e ícone na bandeja;
6. diagnóstico detalhado do túnel e firewall;
7. instalador e atualização automática da interface;
8. testes unitários e de integração dos serviços.
