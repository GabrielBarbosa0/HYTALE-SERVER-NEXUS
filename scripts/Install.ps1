$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repositoryRoot 'src\HytaleServerManager\HytaleServerManager.csproj'
$installDirectory = Join-Path $env:LOCALAPPDATA 'HytaleServerManager\app'
$applicationPath = Join-Path $installDirectory 'HytaleServerManager.exe'
$desktopDirectory = [Environment]::GetFolderPath('Desktop')
$shortcutPath = Join-Path $desktopDirectory 'Iniciador do Servidor Hytale.lnk'

function Find-DotNet {
    $userDotNet = Join-Path $env:LOCALAPPDATA 'Microsoft\dotnet\dotnet.exe'
    if (Test-Path -LiteralPath $userDotNet) { return $userDotNet }

    $command = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($command) { return $command.Source }
    return $null
}

Write-Host 'Verificando o SDK .NET 10...' -ForegroundColor Cyan
$dotnet = Find-DotNet
$hasSdk10 = $false
if ($dotnet) {
    $hasSdk10 = (& $dotnet --list-sdks 2>$null) -match '^10\.'
}

if (-not $hasSdk10) {
    $winget = Get-Command winget -ErrorAction SilentlyContinue
    if (-not $winget) {
        throw 'O SDK .NET 10 nao foi encontrado. Instale-o em https://dotnet.microsoft.com/download/dotnet/10.0 e execute novamente.'
    }

    Write-Host 'Instalando o SDK .NET 10 oficial com o Windows Package Manager...' -ForegroundColor Yellow
    & $winget.Source install --exact --id Microsoft.DotNet.SDK.10 --accept-package-agreements --accept-source-agreements
    if ($LASTEXITCODE -ne 0) { throw 'O Windows Package Manager nao conseguiu instalar o SDK .NET 10.' }
    $dotnet = Find-DotNet
}

if (-not $dotnet) { throw 'O executavel dotnet nao foi localizado apos a instalacao.' }

Write-Host 'Compilando e publicando o iniciador...' -ForegroundColor Cyan
& $dotnet restore $projectPath
if ($LASTEXITCODE -ne 0) { throw 'Falha ao restaurar as dependências do projeto.' }
& $dotnet publish $projectPath -c Release -r win-x64 --self-contained true -o $installDirectory --no-restore
if ($LASTEXITCODE -ne 0) { throw 'Falha ao publicar o aplicativo.' }

Write-Host 'Criando o atalho na Area de Trabalho...' -ForegroundColor Cyan
$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $applicationPath
$shortcut.WorkingDirectory = $installDirectory
$shortcut.Description = 'Iniciador gráfico dos servidores Hytale'
$shortcut.IconLocation = "$applicationPath,0"
$shortcut.Save()

Write-Host 'Instalacao concluida. Abrindo o iniciador...' -ForegroundColor Green
Start-Process -FilePath $applicationPath
