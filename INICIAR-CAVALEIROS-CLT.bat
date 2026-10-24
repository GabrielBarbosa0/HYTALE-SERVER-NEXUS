@echo off
setlocal
title Servidor Hytale - CAVALEIROS CLT

set "WORLD=%APPDATA%\Hytale\UserData\Saves\CAVALEIROS CLT"
set "HYTALE_PACKAGE=%APPDATA%\Hytale\install\release\package"
set "JAVA=%HYTALE_PACKAGE%\jre\latest\bin\java.exe"
set "SERVER=%HYTALE_PACKAGE%\game\latest\Server\HytaleServer.jar"
set "ASSETS=%HYTALE_PACKAGE%\game\latest\Assets.zip"
set "MODS=%APPDATA%\Hytale\UserData\Mods"
set "PREFABS=%APPDATA%\Hytale\UserData\PrefabCache"
set "EARLY_PLUGINS=%APPDATA%\Hytale\UserData\EarlyPlugins"
set "PLAYIT=%LOCALAPPDATA%\PlayitGG\playit-windows-x86_64-signed.exe"
set "PLAYIT_BACKUP=%~dp0tools\playit-windows-x86_64-signed.exe"
if defined HYTALE_PLAYIT_PATH set "PLAYIT=%HYTALE_PLAYIT_PATH%"
if defined HYTALE_PLAYIT_BACKUP_PATH set "PLAYIT_BACKUP=%HYTALE_PLAYIT_BACKUP_PATH%"
set "SERVER_PORT=5520"
set "PUBLIC_ADDRESS=cone-dq.tun.ply.gg:2172"

if not exist "%WORLD%\config.json" goto :missing_world
if not exist "%JAVA%" goto :missing_java
if not exist "%SERVER%" goto :missing_server
if not exist "%ASSETS%" goto :missing_assets

if not exist "%PLAYIT%" if exist "%PLAYIT_BACKUP%" (
    echo Restaurando o programa Playit.gg...
    if not exist "%LOCALAPPDATA%\PlayitGG" mkdir "%LOCALAPPDATA%\PlayitGG"
    copy /Y "%PLAYIT_BACKUP%" "%PLAYIT%" >NUL
)

if not exist "%PLAYIT%" goto :missing_playit

if /I "%~1"=="--check" goto :check_ok

powershell.exe -NoProfile -Command "if (Get-Process -Name 'playit-windows-x86_64-signed' -ErrorAction SilentlyContinue) { exit 0 } else { exit 1 }"
if errorlevel 1 (
    echo Iniciando o tunel do Playit.gg...
    start "Playit.gg - CAVALEIROS CLT" /min "%PLAYIT%"
) else (
    echo O Playit.gg ja esta ligado.
)

powershell.exe -NoProfile -Command "if (Get-NetUDPEndpoint -LocalPort %SERVER_PORT% -ErrorAction SilentlyContinue) { exit 0 } else { exit 1 }"
if not errorlevel 1 goto :already_running

echo.
echo Iniciando o mundo CAVALEIROS CLT...
echo Endereco para os amigos: %PUBLIC_ADDRESS%
echo.
echo Para desligar corretamente, pressione Ctrl+C nesta janela.
echo Nao desligue o computador enquanto o mundo estiver salvando.
echo.

cd /d "%WORLD%"
"%JAVA%" -Xms768M -XX:+IgnoreUnrecognizedVMOptions -XX:+UseCompactObjectHeaders -XX:+UseShenandoahGC -XX:ShenandoahGCMode=generational -XX:+UnlockExperimentalVMOptions -jar "%SERVER%" --bind 0.0.0.0:%SERVER_PORT% --assets="%ASSETS%" --mods="%MODS%" --prefab-cache="%PREFABS%" --early-plugins="%EARLY_PLUGINS%" --transport QUICHE

echo.
echo O servidor foi encerrado.
pause
exit /b

:check_ok
echo OK: mundo, Hytale e Playit.gg foram encontrados.
echo Porta local: UDP %SERVER_PORT%
echo Endereco publico: %PUBLIC_ADDRESS%
exit /b 0

:already_running
echo.
echo O servidor Hytale ja esta ligado na porta %SERVER_PORT%.
echo Endereco para os amigos: %PUBLIC_ADDRESS%
pause
exit /b

:missing_world
echo ERRO: O mundo CAVALEIROS CLT nao foi encontrado em:
echo %WORLD%
goto :error_end
:missing_java
echo ERRO: O Java do Hytale nao foi encontrado em:
echo %JAVA%
goto :error_end
:missing_server
echo ERRO: O arquivo do servidor Hytale nao foi encontrado em:
echo %SERVER%
goto :error_end
:missing_assets
echo ERRO: O arquivo Assets.zip do Hytale nao foi encontrado em:
echo %ASSETS%
goto :error_end
:missing_playit
echo ERRO: O programa Playit.gg nao foi encontrado em:
echo %PLAYIT%
echo.
echo A copia de recuperacao tambem nao foi encontrada em:
echo %PLAYIT_BACKUP%
goto :error_end
:error_end
echo Consulte docs\TDD.md para instalacao e diagnostico.
pause
exit /b 1
