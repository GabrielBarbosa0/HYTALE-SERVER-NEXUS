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

O ícone multipropósito em `Assets/HytaleServerManager.ico` é incorporado ao executável e aplicado ao `AppWindow` e ao atalho. `tools/GenerateIcon.ps1` regenera o `.ico` e a prévia PNG a partir da identidade azul com “H”.

## Fluxo de inicialização

1. Carrega `%LOCALAPPDATA%\HytaleServerManager\settings.json` ou cria padrões em memória.
2. Procura subpastas com `config.json` em `%APPDATA%\Hytale\UserData\Saves` e monta o seletor de mundos.
3. Restaura o último mundo selecionado; se ele não existir, escolhe o primeiro save válido encontrado.
4. Descobre processos existentes do Java e Playit.gg.
5. Atualiza status, porta e métricas a cada segundo.
6. Ao iniciar, valida o mundo selecionado, Java, JAR, assets, Playit.gg e limites de memória.
7. Abre o túnel se necessário.
8. Executa o Hytale sem janela de CMD, capturando stdout e stderr.
9. Encaminha as linhas para o console visual e identifica entrada/saída de jogadores.

O seletor é desabilitado enquanto o servidor está ligado. A escolha é persistida automaticamente em `settings.json`, e o nome exibido no topo acompanha o mundo atual.

## Assistente de primeira execução

A interface verifica somente a presença dos arquivos, sem ler credenciais:

- Java, servidor e assets confirmam a instalação local do Hytale;
- `auth.enc` na pasta do mundo indica que já houve persistência de autorização;
- o executável confirma a instalação do agente Playit.gg;
- `%LOCALAPPDATA%\playit_gg\playit.toml` indica que o agente já foi vinculado neste perfil do Windows.

O botão **Gerar código** envia `/auth login device` pela entrada padrão do servidor. A saída é analisada para detectar o código e a URL `accounts.hytale.com/device`; a confirmação oficial é aberta automaticamente uma vez e continua disponível pelo botão da interface. A mensagem de sucesso atualiza o diagnóstico.

O Playit.gg continua com sua própria autenticação. A interface abre apenas páginas oficiais e o agente local; cada operador vincula sua conta, cria seu túnel UDP para `127.0.0.1:5520` e salva o endereço público correspondente.

Na operação normal, o agente é criado com `CreateNoWindow` e sua saída é drenada em segundo plano, evitando console e segundo ícone na barra de tarefas. Uma instância antiga com janela também é ocultada. O fluxo **Vincular agente** reinicia em modo visível quando necessário, e a configuração **Mostrar console do Playit.gg no próximo início** permite diagnóstico manual.

Os controles do túnel são independentes do servidor: **Ligar somente o túnel** permanece disponível quando o Hytale já está online, enquanto **Desligar somente o túnel** fica disponível apenas com o agente ativo.

O console visual aceita comandos somente quando o processo foi iniciado pela própria interface. Isso evita fingir que um comando foi entregue a uma instância Java externa que não possui entrada padrão anexada.

O cartão do servidor diferencia **Controlado por este painel** de **Detectado externamente**. No primeiro caso, a parada usa `/stop` e aguarda o salvamento. No segundo, a interface oferece **Encerrar processo externo** com diálogo de confirmação; essa ação finaliza a árvore do Java e é explicitamente tratada como recuperação de emergência, pois não garante um salvamento final.

Os comandos frequentes `/auth status`, `/auth persistence` e `/auth persistence Encrypted` possuem botões próprios. Quando um servidor externo é detectado, a interface desabilita os comandos, o início e a troca de mundo, exibe o mundo identificado pelo `.world.lock` e orienta o operador a encerrar a instância no console original.

## Instalação a partir do repositório

`INSTALAR-E-INICIAR.bat` chama `scripts/Install.ps1`, que:

1. localiza um SDK .NET 10 existente;
2. quando necessário, instala o SDK oficial pelo `winget`;
3. restaura e publica a aplicação autocontida em `%LOCALAPPDATA%\HytaleServerManager\app`;
4. cria o atalho na Área de Trabalho;
5. abre o aplicativo publicado.

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
- `auth.key` e `playit.toml` também permanecem locais e fora do Git.
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
3. editor completo de caminhos e atualização manual da lista de mundos;
4. pesquisa e filtros do console;
5. notificações e ícone na bandeja;
6. diagnóstico detalhado do túnel e firewall;
7. instalador e atualização automática da interface;
8. testes unitários e de integração dos serviços;
9. leitura local, sem segredos, do endereço público anunciado pelo agente Playit.
