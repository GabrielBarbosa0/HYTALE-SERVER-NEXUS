@echo off
setlocal
set "APP=%LOCALAPPDATA%\HytaleServerManager\app\HytaleServerManager.exe"

if exist "%APP%" (
    start "Central do Servidor CAVALEIROS CLT" "%APP%"
    exit /b 0
)

echo ERRO: A interface grafica ainda nao foi publicada em:
echo %APP%
echo.
echo O iniciador antigo continua disponivel em INICIAR-CAVALEIROS-CLT.bat.
pause
exit /b 1
