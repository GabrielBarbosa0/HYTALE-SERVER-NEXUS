@echo off
setlocal
title Instalador do Iniciador do Servidor Hytale

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\Install.ps1"
if errorlevel 1 (
    echo.
    echo A instalacao nao foi concluida. Consulte a mensagem acima.
    pause
    exit /b 1
)

exit /b 0
