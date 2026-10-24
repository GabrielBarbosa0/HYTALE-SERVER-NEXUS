# INICIADOR DO SERVIDOR HYTALE

Aplicativo WinUI 3 em C# para escolher e controlar mundos do servidor dedicado do Hytale e o túnel do Playit.gg. O iniciador em lote original do **CAVALEIROS CLT** permanece disponível como fallback.

## Interface gráfica

Na primeira vez, execute `INSTALAR-E-INICIAR.bat`. Nas próximas vezes, use `INICIAR-PAINEL.bat` ou o atalho **Iniciador do Servidor Hytale** na Área de Trabalho. Se a interface ainda não estiver instalada, `INICIAR-PAINEL.bat` chama o instalador automaticamente.

O MVP atual oferece:

- status do servidor, túnel e porta UDP;
- início conjunto do Hytale e Playit.gg;
- controle independente para ligar ou desligar somente o túnel, inclusive com o servidor já ativo;
- descoberta automática e seleção dos mundos válidos na pasta de saves;
- título dinâmico com o nome do mundo selecionado;
- encerramento seguro do servidor pelo comando `/stop`;
- endereço público com cópia para a área de transferência;
- CPU, memória, uptime e jogadores detectados nos logs;
- console ao vivo com rolagem automática;
- configuração persistente de memória, porta e endereço;
- acesso rápido à pasta do mundo.
- diagnóstico da instalação, autorização Hytale e vinculação do Playit.gg;
- assistente para gerar e copiar o código de autorização Hytale, abrindo automaticamente a confirmação oficial;
- entrada de comandos no console visual;
- ações rápidas para consultar status e persistência da autenticação;
- indicação explícita do mundo em execução e do motivo de o seletor estar bloqueado;
- indicação se o processo está controlado pelo painel ou foi detectado externamente;
- encerramento de emergência de servidor externo, protegido por confirmação e aviso de salvamento;
- links oficiais para baixar e configurar o Playit.gg;
- execução normal do Playit.gg em segundo plano, sem uma segunda janela na barra de tarefas;
- ícone próprio do aplicativo no executável, na janela e no atalho.

## Primeiro uso em outro computador

1. Instale o Hytale pelo launcher oficial e tenha pelo menos um save com `config.json`.
2. Baixe este repositório e execute `INSTALAR-E-INICIAR.bat`. Se necessário, ele instala o SDK .NET 10 oficial pelo `winget`, publica a aplicação localmente e cria o atalho.
3. No painel, use **Baixar Playit** e depois **Vincular agente**. Entre na sua própria conta e crie um túnel UDP para `127.0.0.1:5520`.
4. Copie o endereço público desse túnel para **Configuração > Endereço público** e salve.
5. Selecione o mundo e inicie o servidor.
6. Se a autorização Hytale estiver pendente, pressione **Gerar código**, abra a confirmação e autorize com a sua própria conta Hytale.

O mundo só pode ser trocado com o servidor desligado. Se um servidor foi iniciado por outra janela ou pelo `.bat`, o painel consegue monitorá-lo, mas não consegue assumir sua entrada de console; encerre-o no console original antes de selecionar outro mundo.

Quando o console original não estiver disponível, **Encerrar processo externo** substitui o Gerenciador de Tarefas. Essa é uma opção de emergência: diferentemente de **Salvar e parar servidor**, ela não consegue enviar `/stop` e pode perder alterações posteriores ao último salvamento.

O console do Playit.gg fica oculto por padrão. Para vincular um agente, o iniciador o mostra temporariamente; para diagnóstico contínuo, ative **Mostrar console do Playit.gg no próximo início** nas configurações.

Contas, tokens, `auth.enc`, `auth.key`, `playit.toml`, mundos e endereços pessoais não são distribuídos pelo repositório.

## Uso rápido

1. Execute `INICIAR-PAINEL.bat` (recomendado) ou `INICIAR-CAVALEIROS-CLT.bat` (fallback exclusivo desse mundo).
2. Selecione o mundo e pressione **Iniciar tudo**.
3. Aguarde a mensagem de que o universo está pronto.
4. Entre localmente pelo Hytale.
5. Envie aos amigos o endereço `cone-dq.tun.ply.gg:2172`.
6. Para desligar com segurança, use **Salvar e parar servidor**.

O firewall do Windows deve permanecer ativado. O iniciador reconhece quando o Playit.gg ou a porta UDP `5520` já estão em uso e evita iniciar uma segunda instância.

Para validar caminhos e restaurar o Playit.gg sem iniciar o servidor, execute no terminal:

```bat
INICIAR-CAVALEIROS-CLT.bat --check
```

## Pré-requisitos

- Hytale instalado pelo launcher oficial.
- Um ou mais mundos com `config.json` em `%APPDATA%\Hytale\UserData\Saves`.
- Agente Playit.gg em `%LOCALAPPDATA%\PlayitGG\playit-windows-x86_64-signed.exe`.
- Cópia local de recuperação em `tools\playit-windows-x86_64-signed.exe` (ignorada pelo Git).
- Túnel Playit.gg configurado para encaminhar o endereço público à porta local UDP `5520`.
- Autenticação do servidor persistida no arquivo `auth.enc` do mundo.

## Documentação

Consulte [`docs/TDD.md`](docs/TDD.md) para o servidor e [`docs/GUI.md`](docs/GUI.md) para a arquitetura da interface.

## Segurança

Este repositório não contém o mundo, backups, `auth.enc`, segredo do agente Playit.gg nem qualquer token. Esses dados devem continuar fora do Git.

O executável do Playit.gg também não entra nos commits. Nesta instalação existe uma cópia local ignorada em `tools\`; se o arquivo em `%LOCALAPPDATA%` desaparecer, o iniciador o restaura automaticamente.
