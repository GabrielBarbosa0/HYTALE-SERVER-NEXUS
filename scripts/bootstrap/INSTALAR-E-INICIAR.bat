@echo off
setlocal
title Bootstrap do Iniciador do Servidor Hytale

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0..\Install.ps1"
if errorlevel 1 (
    echo.
    echo O bootstrap nao foi concluido. Consulte a mensagem acima.
    pause
    exit /b 1
)

exit /b 0
