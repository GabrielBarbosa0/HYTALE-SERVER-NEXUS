param(
    [string]$Version = '1.1.0'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repositoryRoot 'src\HytaleServerManager\HytaleServerManager.csproj'
$publishDirectory = Join-Path $repositoryRoot 'artifacts\publish\win-x64'
$installerScript = Join-Path $repositoryRoot 'installer\HytaleServerManager.iss'
$installerDirectory = Join-Path $repositoryRoot 'artifacts\installer'

function Find-DotNet {
    $userDotNet = Join-Path $env:LOCALAPPDATA 'Microsoft\dotnet\dotnet.exe'
    if (Test-Path -LiteralPath $userDotNet) { return $userDotNet }
    $command = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($command) { return $command.Source }
    throw 'O SDK .NET 10 nao foi encontrado.'
}

function Find-InnoCompiler {
    $candidates = @(
        (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'),
        (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe')
    )
    foreach ($candidate in $candidates) {
        if (Test-Path -LiteralPath $candidate) { return $candidate }
    }
    throw 'Inno Setup 6 nao encontrado. Instale com: winget install JRSoftware.InnoSetup'
}

$dotnet = Find-DotNet
$iscc = Find-InnoCompiler
$artifactsRoot = [IO.Path]::GetFullPath((Join-Path $repositoryRoot 'artifacts'))
$publishDirectory = [IO.Path]::GetFullPath($publishDirectory)
if (-not $publishDirectory.StartsWith($artifactsRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'O diretório de publicação precisa permanecer dentro de artifacts.'
}
if (Test-Path -LiteralPath $publishDirectory) {
    Get-ChildItem -LiteralPath $publishDirectory -Force | Remove-Item -Recurse -Force
}
New-Item -ItemType Directory -Path $publishDirectory -Force | Out-Null
New-Item -ItemType Directory -Path $installerDirectory -Force | Out-Null

Write-Host "Publicando a versao $Version..." -ForegroundColor Cyan
& $dotnet restore $projectPath -r win-x64
if ($LASTEXITCODE -ne 0) { throw 'Falha ao restaurar as dependencias.' }
& $dotnet publish $projectPath -c Release -r win-x64 --self-contained true -o $publishDirectory --no-restore -p:Version=$Version
if ($LASTEXITCODE -ne 0) { throw 'Falha ao publicar a aplicacao.' }

Write-Host 'Gerando o instalador executavel...' -ForegroundColor Cyan
& $iscc "/DMyAppVersion=$Version" $installerScript
if ($LASTEXITCODE -ne 0) { throw 'Falha ao gerar o instalador.' }

$installer = Get-ChildItem -LiteralPath $installerDirectory -Filter "HTServer-Nexus-Setup-$Version.exe" | Select-Object -First 1
if (-not $installer) { throw 'O instalador esperado nao foi criado.' }
Write-Host "Instalador pronto: $($installer.FullName)" -ForegroundColor Green
