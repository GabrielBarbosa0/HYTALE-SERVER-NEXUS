@echo off
setlocal
set "APP=%LOCALAPPDATA%\HytaleServerManager\app\HTServerNexus.exe"

if exist "%APP%" (
    start "HTServer Nexus" "%APP%"
    exit /b 0
)

echo A interface ainda nao esta instalada neste computador.
echo Iniciando o bootstrap de desenvolvimento...
call "%~dp0INSTALAR-E-INICIAR.bat"
exit /b %ERRORLEVEL%
