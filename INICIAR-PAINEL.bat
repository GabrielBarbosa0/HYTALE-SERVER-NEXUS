@echo off
setlocal
set "APP=%LOCALAPPDATA%\HytaleServerManager\app\HytaleServerManager.exe"

if exist "%APP%" (
    start "Iniciador do Servidor Hytale" "%APP%"
    exit /b 0
)

echo A interface ainda nao esta instalada neste computador.
echo Iniciando a instalacao local...
call "%~dp0INSTALAR-E-INICIAR.bat"
exit /b %ERRORLEVEL%
