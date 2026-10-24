# INICIADOR HYTALE — CAVALEIROS CLT

Iniciador local para abrir o servidor dedicado do mundo **CAVALEIROS CLT** e o túnel do Playit.gg com dois cliques.

## Uso rápido

1. Execute `INICIAR-CAVALEIROS-CLT.bat`.
2. Aguarde a mensagem de que o universo está pronto.
3. Entre localmente pelo Hytale.
4. Envie aos amigos o endereço `cone-dq.tun.ply.gg:2172`.
5. Para desligar o servidor corretamente, pressione `Ctrl+C` na janela dele e aguarde o encerramento.

O firewall do Windows deve permanecer ativado. O iniciador reconhece quando o Playit.gg ou a porta UDP `5520` já estão em uso e evita iniciar uma segunda instância.

Para validar caminhos e restaurar o Playit.gg sem iniciar o servidor, execute no terminal:

```bat
INICIAR-CAVALEIROS-CLT.bat --check
```

## Pré-requisitos

- Hytale instalado pelo launcher oficial.
- Mundo em `%APPDATA%\Hytale\UserData\Saves\CAVALEIROS CLT`.
- Agente Playit.gg `1.0.12` em `%LOCALAPPDATA%\PlayitGG\playit-windows-x86_64-signed.exe`.
- Cópia local de recuperação em `tools\playit-windows-x86_64-signed.exe` (ignorada pelo Git).
- Túnel Playit.gg configurado para encaminhar o endereço público à porta local UDP `5520`.
- Autenticação do servidor persistida no arquivo `auth.enc` do mundo.

## Documentação

Consulte [`docs/TDD.md`](docs/TDD.md) para arquitetura, decisões técnicas, operação, segurança, testes e solução de problemas.

## Segurança

Este repositório não contém o mundo, backups, `auth.enc`, segredo do agente Playit.gg nem qualquer token. Esses dados devem continuar fora do Git.

O executável do Playit.gg também não entra nos commits. Nesta instalação existe uma cópia local ignorada em `tools\`; se o arquivo em `%LOCALAPPDATA%` desaparecer, o iniciador o restaura automaticamente.
