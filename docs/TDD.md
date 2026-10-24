# TDD — Iniciador do servidor Hytale

## 1. Identificação

- **Documento:** Technical Design Document (Documento de Projeto Técnico)
- **Projeto:** INICIADOR-SERVIDOR-HYTALE
- **Plataforma:** Windows
- **Mundos detectados:** qualquer save válido; CAVALEIROS CLT é o padrão legado
- **Porta local:** UDP `5520`
- **Endereço público atual:** `cone-dq.tun.ply.gg:2172`
- **Transporte do Hytale:** QUICHE/UDP
- **Agente de túnel validado:** Playit.gg `1.0.12`

## 2. Contexto e objetivo

O servidor dedicado do Hytale exige uma linha de comando extensa, o diretório correto do mundo, os arquivos da instalação e um túnel UDP ativo para aceitar jogadores externos. O objetivo deste projeto é reduzir a inicialização a dois cliques, mantendo validações simples e mensagens compreensíveis.

O iniciador deve:

1. validar os arquivos essenciais;
2. iniciar o agente Playit.gg se ele ainda não estiver executando;
3. impedir uma segunda instância do servidor na porta `5520`;
4. permitir escolher um save válido e executar o servidor dentro da pasta desse mundo;
5. exibir o endereço que deve ser enviado aos amigos;
6. manter o console visível para operação e diagnóstico.

Não fazem parte do escopo: baixar o Hytale, criar uma conta, gerar mundos, publicar este repositório remotamente, copiar o mundo para o Git ou armazenar credenciais.

## 3. Arquitetura

Fluxo de rede:

```text
Jogador externo
    │
    │ QUIC/UDP em cone-dq.tun.ply.gg:2172
    ▼
Rede do Playit.gg
    │
    │ túnel autenticado
    ▼
Agente Playit.gg no computador do servidor
    │
    │ encaminhamento para 127.0.0.1:5520/UDP
    ▼
HytaleServer.jar
    │
    ▼
Mundo selecionado / universo default
```

Componentes:

| Componente | Responsabilidade |
|---|---|
| Interface WinUI 3 | Descobre mundos, guarda a seleção e controla os processos |
| `INICIAR-CAVALEIROS-CLT.bat` | Orquestra validações e inicialização |
| Java incluído no Hytale | Executa o servidor dedicado |
| `HytaleServer.jar` | Hospeda o mundo e autentica jogadores |
| Playit.gg | Expõe a porta UDP local sem encaminhamento manual no roteador |
| `auth.enc` | Guarda localmente a autenticação persistida do servidor |

## 4. Caminhos e configuração

O script usa variáveis do Windows para não depender do nome do usuário:

| Recurso | Caminho esperado |
|---|---|
| Raiz dos mundos | `%APPDATA%\Hytale\UserData\Saves` |
| Java | `%APPDATA%\Hytale\install\release\package\jre\latest\bin\java.exe` |
| Servidor | `%APPDATA%\Hytale\install\release\package\game\latest\Server\HytaleServer.jar` |
| Assets | `%APPDATA%\Hytale\install\release\package\game\latest\Assets.zip` |
| Mods globais | `%APPDATA%\Hytale\UserData\Mods` |
| Playit.gg | `%LOCALAPPDATA%\PlayitGG\playit-windows-x86_64-signed.exe` |
| Cópia local de recuperação | `tools\playit-windows-x86_64-signed.exe` |

Cada subpasta que contém `config.json` é exibida como opção. O diretório de trabalho do Java é a pasta do mundo selecionado. Isso é essencial: `config.json`, `auth.enc`, `universe`, `mods`, backups e `.world.lock` são resolvidos a partir desse local.

## 5. Sequência de inicialização

1. A interface descobre os mundos com `config.json` e restaura a última seleção.
2. O iniciador verifica `config.json`, Java, servidor e assets.
3. Se o agente estiver ausente em `%LOCALAPPDATA%`, tenta restaurá-lo usando a cópia local em `tools`.
4. Procura um processo chamado `playit-windows-x86_64-signed`.
5. Se não existir, abre o agente Playit.gg minimizado.
6. Consulta os endpoints UDP do Windows para saber se a porta `5520` já está em uso.
7. Se estiver em uso, informa que o servidor já está ligado e não cria outra instância.
8. Se estiver livre, muda o diretório para a pasta do mundo selecionado.
9. Executa o servidor com bind em `0.0.0.0:5520` e transporte `QUICHE`.
10. O servidor carrega o universo e o mundo interno `default` pertencente ao save selecionado.

Parâmetros relevantes:

- `-Xms768M`: memória inicial da JVM;
- `--bind 0.0.0.0:5520`: escuta UDP na porta definida;
- `--assets`: aponta para os assets da versão atual;
- `--mods`, `--prefab-cache` e `--early-plugins`: reutilizam os dados do usuário;
- `--transport QUICHE`: usa o transporte esperado pelo servidor e pelo túnel.

## 6. Autenticação e confirmações

O servidor Hytale opera em modo autenticado. No primeiro uso, ou se a sessão for revogada, o console pode solicitar `/auth login` e uma confirmação no navegador. Depois de concluída com persistência criptografada, o arquivo `auth.enc` fica na pasta do mundo.

Na interface gráfica, **Gerar código** envia `/auth login device`. O iniciador reconhece na saída o código e a URL de confirmação, apresenta ambos ao operador e pode abrir a página oficial. O operador ainda precisa entrar e confirmar com sua própria conta; o iniciador não captura senha, token ou sessão do navegador.

Regras de segurança:

- nunca adicionar `auth.enc` ao Git;
- nunca adicionar `auth.key` ou `%LOCALAPPDATA%\playit_gg\playit.toml` ao Git;
- nunca registrar tokens, URLs de confirmação ou segredos do Playit.gg na documentação;
- realizar confirmações somente nas páginas oficiais abertas pelo fluxo de autenticação;
- se a autenticação expirar, refazer o login pelo console em vez de copiar credenciais de outro computador.

## 7. Rede, firewall e Playit.gg

O Hytale usa UDP/QUIC, não uma conexão TCP comum. O túnel público recebe tráfego em `cone-dq.tun.ply.gg:2172` e o agente o entrega localmente em `127.0.0.1:5520`.

O firewall do Windows deve permanecer ativado. Desativá-lo pode servir apenas como diagnóstico momentâneo, mas não é uma correção. Como o tráfego chega pelo agente local do Playit.gg, uma regra permitida para os executáveis envolvidos é preferível a liberar portas amplas ou desligar o firewall.

O firewall do amigo normalmente não precisa aceitar conexões de entrada: o cliente inicia uma conexão de saída para o endereço do túnel.

### Incidente já observado

O agente Playit.gg `0.17.1` apresentou erro de socket do Windows `OS code 10040`, associado a um datagrama UDP maior que o buffer disponível. O sintoma para o jogador era expiração da conexão. A atualização para o agente `1.0.12` eliminou o erro no teste realizado. Não voltar para a versão antiga.

## 8. Catálogo de falhas e diagnóstico

### “A conexão expirou” no computador do amigo

Verificar, nesta ordem:

1. se a janela do servidor continua aberta;
2. se o console informa que o universo está pronto;
3. se o Playit.gg está executando e conectado;
4. se o endereço foi digitado como `cone-dq.tun.ply.gg:2172`;
5. se o túnel aponta para UDP `127.0.0.1:5520`;
6. se há mensagens de conexão no console quando o amigo tenta entrar;
7. se cliente e servidor usam versões compatíveis.

Se não aparecer tentativa alguma no console, o problema provavelmente está antes do servidor: endereço incorreto, agente desconectado ou configuração do túnel. Se aparecer autenticação e depois desconexão, investigar conta, versão, mods ou estabilidade da rede.

### Erro `OS code 10040`

Confirmar que o agente em uso é o `1.0.12` no caminho documentado. Encerrar instâncias antigas do Playit.gg antes de abrir a nova.

### O mundo não aparece no seletor

Confirmar que ele está diretamente dentro de `%APPDATA%\Hytale\UserData\Saves` e contém `config.json`. Reiniciar a interface para refazer a descoberta. O `.bat` legado continua fixo no CAVALEIROS CLT.

### Java, servidor ou `Assets.zip` não encontrado

Abrir o launcher oficial para verificar ou reparar a instalação. Os caminhos usam a pasta `latest`; uma mudança futura na estrutura do Hytale pode exigir atualização do script.

### Playit.gg não encontrado

O iniciador primeiro procura `%LOCALAPPDATA%\PlayitGG\playit-windows-x86_64-signed.exe`. Se estiver ausente, ele tenta restaurar a partir de `tools\playit-windows-x86_64-signed.exe`. Se ambas as cópias estiverem ausentes, reinstalar a versão validada. O executável local é ignorado pelo Git e não deve ser publicado sem revisar licença, procedência e política de distribuição.

Na interface gráfica, **Baixar Playit** abre a página oficial e **Vincular agente** abre o programa junto ao painel de agentes. Um novo computador precisa usar sua própria conta, seu próprio agente, seu próprio túnel UDP e seu próprio endereço público.

### Porta `5520` já em uso

O comportamento normal do script é não iniciar outra instância. Verificar no Gerenciador de Tarefas se o servidor existente está saudável. Não encerrar Java indiscriminadamente se outros programas Java estiverem abertos.

### Arquivo `.world.lock`

Esse arquivo indica que o mundo pode estar aberto. Não apagá-lo enquanto o servidor estiver executando. Depois de uma queda, primeiro confirmar que não existe processo do servidor antes de considerar qualquer correção.

### Autenticação ausente ou expirada

Usar o comando de autenticação indicado pelo próprio console, concluir a confirmação e preservar o novo `auth.enc`. Não publicar o arquivo.

### Mods incompatíveis

Revisar os avisos no console. Mods desativados pela configuração não são necessariamente uma falha. Se o servidor cair durante o carregamento, testar apenas depois de fazer backup e identificar o mod mencionado no final do log.

### Hostname público alterado

Se o túnel for recriado, o domínio ou a porta pública podem mudar. Atualizar `PUBLIC_ADDRESS` no `.bat`, este documento e o `README.md` na mesma alteração.

## 9. Operação segura

### Iniciar

1. Dar dois cliques no `.bat`.
2. Aguardar o carregamento completo.
3. Manter as janelas do servidor e do agente abertas.
4. Compartilhar apenas o endereço público, nunca arquivos de autenticação.

### Encerrar

1. Avisar os jogadores.
2. Pressionar `Ctrl+C` na janela do servidor.
3. Confirmar o encerramento, caso o Windows pergunte.
4. Aguardar a gravação do mundo.
5. O agente Playit.gg pode ser fechado depois do servidor.

Evitar finalizar o Java à força, desligar o computador durante salvamento ou abrir o mesmo mundo simultaneamente em outra instância.

## 10. Testes e critérios de aceitação

| ID | Cenário | Resultado esperado |
|---|---|---|
| T01 | Todos os arquivos existem e nada está aberto | Playit.gg e servidor iniciam |
| T02 | Playit.gg já está aberto | O agente não é duplicado |
| T03 | UDP `5520` já está em uso | O servidor não é duplicado |
| T04 | Mundo ausente | Mensagem mostra o caminho esperado |
| T05 | Agente ausente | Mensagem mostra o caminho esperado |
| T06 | Jogador local entra | Mundo `default` do save correto é carregado |
| T07 | Jogador externo entra | Autenticação termina e o jogador aparece no mundo |
| T08 | Firewall permanece ativado | Jogador externo continua conectando pelo túnel |
| T09 | Encerramento com `Ctrl+C` | Servidor salva e encerra sem corrupção aparente |
| T10 | Quatro saves válidos instalados | Os quatro aparecem no seletor em ordem alfabética |
| T11 | Mundo selecionado e aplicativo reaberto | A última seleção é restaurada |
| T12 | Servidor em execução | O seletor de mundo fica desabilitado |
| T13 | Mundo sem `auth.enc` | Assistente indica autorização Hytale pendente |
| T14 | Saída contém código e URL de autorização | Interface apresenta código e abre apenas URL HTTPS oficial |
| T15 | Comando digitado com servidor iniciado pela GUI | Comando chega à entrada padrão do servidor |
| T16 | Clone sem aplicação publicada | Instalador publica localmente e cria o atalho |
| T17 | Perfil sem `playit.toml` | Interface indica vinculação Playit pendente |
| T18 | Servidor iniciado por outra janela | Mundo ativo é exibido e comandos/troca ficam desabilitados |
| T19 | Servidor iniciado pela interface | Botões de status e persistência enviam os comandos correspondentes |
| T20 | Playit iniciado na operação normal | Agente executa sem console ou segundo ícone na barra |
| T21 | Operador escolhe vincular agente | Console do Playit permanece visível durante a configuração |
| T22 | Aplicativo e atalho publicados | Ambos exibem o ícone azul com “H” |
| T23 | Servidor online e túnel desligado | Botão permite ligar somente o Playit sem duplicar o servidor |

O teste externo validado teve entrada bem-sucedida do jogador remoto `GleamingFjord808` e entrada local de `Saga`, com o firewall reativado.

O modo `--check` executa as validações de caminhos e a recuperação local do agente sem iniciar Playit.gg ou Hytale. Ele deve ser usado em testes automatizados e diagnósticos que não podem abrir o servidor.

Em testes isolados, `HYTALE_PLAYIT_PATH` e `HYTALE_PLAYIT_BACKUP_PATH` podem substituir os dois caminhos do agente. Essas variáveis não são necessárias no uso normal.

## 11. Observabilidade

A fonte principal de diagnóstico é a janela do servidor. Os logs persistentes ficam na pasta do mundo, dentro de `logs`, e não devem ser versionados. Procurar especialmente por:

- conclusão do carregamento do universo;
- autenticação mútua concluída;
- jogador adicionado ao mundo `default`;
- exceções de rede, mods ou salvamento;
- avisos de token ausente.

## 12. Limitações e melhorias futuras

- O endereço público é configurado diretamente no `.bat`; mudanças no painel Playit.gg exigem edição manual.
- A verificação de porta detecta qualquer processo usando UDP `5520`, não apenas o Hytale.
- O script não verifica automaticamente a versão do agente Playit.gg.
- O encerramento do servidor e do agente ainda é manual.
- Uma evolução possível é um script PowerShell com logs próprios, validação do processo dono da porta e configuração externa em JSON.

## 12.1 Interface gráfica

O projeto agora inclui uma central em C# com WinUI 3. Ela substitui o CMD na operação cotidiana, mantendo este `.bat` como fallback. A arquitetura, o fluxo de processos e o roadmap estão documentados em [`GUI.md`](GUI.md).

## 13. Controle de mudanças

Qualquer mudança de porta, túnel, nome do mundo, estrutura da instalação ou transporte deve atualizar em conjunto:

1. `INICIAR-CAVALEIROS-CLT.bat`;
2. `README.md`;
3. este TDD;
4. configuração do túnel no Playit.gg, quando aplicável.
