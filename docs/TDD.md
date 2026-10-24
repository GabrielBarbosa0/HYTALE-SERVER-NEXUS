# TDD — Iniciador do servidor Hytale CAVALEIROS CLT

## 1. Identificação

- **Documento:** Technical Design Document (Documento de Projeto Técnico)
- **Projeto:** INICIADOR-HYTALE-CAVALEIROS-CLT
- **Plataforma:** Windows
- **Mundo:** CAVALEIROS CLT
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
4. executar o servidor dentro da pasta do mundo CAVALEIROS CLT;
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
Mundo CAVALEIROS CLT / universo default
```

Componentes:

| Componente | Responsabilidade |
|---|---|
| `INICIAR-CAVALEIROS-CLT.bat` | Orquestra validações e inicialização |
| Java incluído no Hytale | Executa o servidor dedicado |
| `HytaleServer.jar` | Hospeda o mundo e autentica jogadores |
| Playit.gg | Expõe a porta UDP local sem encaminhamento manual no roteador |
| `auth.enc` | Guarda localmente a autenticação persistida do servidor |

## 4. Caminhos e configuração

O script usa variáveis do Windows para não depender do nome do usuário:

| Recurso | Caminho esperado |
|---|---|
| Mundo | `%APPDATA%\Hytale\UserData\Saves\CAVALEIROS CLT` |
| Java | `%APPDATA%\Hytale\install\release\package\jre\latest\bin\java.exe` |
| Servidor | `%APPDATA%\Hytale\install\release\package\game\latest\Server\HytaleServer.jar` |
| Assets | `%APPDATA%\Hytale\install\release\package\game\latest\Assets.zip` |
| Mods globais | `%APPDATA%\Hytale\UserData\Mods` |
| Playit.gg | `%LOCALAPPDATA%\PlayitGG\playit-windows-x86_64-signed.exe` |
| Cópia local de recuperação | `tools\playit-windows-x86_64-signed.exe` |

O diretório de trabalho do Java é a pasta do mundo. Isso é essencial: `config.json`, `auth.enc`, `universe`, `mods`, backups e `.world.lock` são resolvidos a partir desse local.

## 5. Sequência de inicialização

1. O script verifica `config.json`, Java, servidor e assets.
2. Se o agente estiver ausente em `%LOCALAPPDATA%`, tenta restaurá-lo usando a cópia local em `tools`.
3. Procura um processo chamado `playit-windows-x86_64-signed`.
4. Se não existir, abre o agente Playit.gg minimizado.
5. Consulta os endpoints UDP do Windows para saber se a porta `5520` já está em uso.
6. Se estiver em uso, informa que o servidor já está ligado e não cria outra instância.
7. Se estiver livre, muda o diretório para a pasta do mundo.
8. Executa o servidor com bind em `0.0.0.0:5520` e transporte `QUICHE`.
9. O servidor carrega o universo e o mundo interno `default` pertencente ao save CAVALEIROS CLT.

Parâmetros relevantes:

- `-Xms768M`: memória inicial da JVM;
- `--bind 0.0.0.0:5520`: escuta UDP na porta definida;
- `--assets`: aponta para os assets da versão atual;
- `--mods`, `--prefab-cache` e `--early-plugins`: reutilizam os dados do usuário;
- `--transport QUICHE`: usa o transporte esperado pelo servidor e pelo túnel.

## 6. Autenticação e confirmações

O servidor Hytale opera em modo autenticado. No primeiro uso, ou se a sessão for revogada, o console pode solicitar `/auth login` e uma confirmação no navegador. Depois de concluída com persistência criptografada, o arquivo `auth.enc` fica na pasta do mundo.

Regras de segurança:

- nunca adicionar `auth.enc` ao Git;
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

### “O mundo CAVALEIROS CLT não foi encontrado”

Confirmar a existência de `%APPDATA%\Hytale\UserData\Saves\CAVALEIROS CLT\config.json`. Se o mundo tiver sido renomeado, ajustar a variável `WORLD` no script.

### Java, servidor ou `Assets.zip` não encontrado

Abrir o launcher oficial para verificar ou reparar a instalação. Os caminhos usam a pasta `latest`; uma mudança futura na estrutura do Hytale pode exigir atualização do script.

### Playit.gg não encontrado

O iniciador primeiro procura `%LOCALAPPDATA%\PlayitGG\playit-windows-x86_64-signed.exe`. Se estiver ausente, ele tenta restaurar a partir de `tools\playit-windows-x86_64-signed.exe`. Se ambas as cópias estiverem ausentes, reinstalar a versão validada. O executável local é ignorado pelo Git e não deve ser publicado sem revisar licença, procedência e política de distribuição.

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

## 13. Controle de mudanças

Qualquer mudança de porta, túnel, nome do mundo, estrutura da instalação ou transporte deve atualizar em conjunto:

1. `INICIAR-CAVALEIROS-CLT.bat`;
2. `README.md`;
3. este TDD;
4. configuração do túnel no Playit.gg, quando aplicável.
