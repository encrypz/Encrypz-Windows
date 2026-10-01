param([switch]$Installer)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$destination = Join-Path $root ('artifacts\windows-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
function Invoke-Checked([string]$File, [string[]]$Arguments) {
    & $File @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$File failed with exit code $LASTEXITCODE" }
}
Push-Location (Join-Path $root 'Encrypz.UI')
try {
    Invoke-Checked 'npm.cmd' @('ci', '--cache', (Join-Path $root 'artifacts\npm-cache'))
    Invoke-Checked 'npm.cmd' @('run', 'build')
} finally { Pop-Location }
Invoke-Checked 'dotnet' @('publish', (Join-Path $root 'Encrypz.Desktop\Encrypz.Desktop.csproj'), '-c', 'Release', '-r', 'win-x64', '--self-contained', 'true', '-o', $destination)
Invoke-Checked 'dotnet' @('publish', (Join-Path $root 'Encrypz.API\Encrypz.API.csproj'), '-c', 'Release', '-r', 'win-x64', '--self-contained', 'true', '-o', (Join-Path $destination 'api'))
foreach ($required in @('Encrypz.Desktop.exe', 'dist\index.html', 'api\Encrypz.API.exe', 'api\appsettings.json')) {
    if (!(Test-Path (Join-Path $destination $required))) { throw "Package missing $required" }
}
Copy-Item (Join-Path $root 'desktop-settings.example.json') $destination
Copy-Item (Join-Path $root 'WINDOWS.md') $destination
if ($Installer) {
    $compiler = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    $compilerPath = if ($compiler) { $compiler.Source } else { Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe' }
    if (!(Test-Path $compilerPath)) { throw 'Install Inno Setup 6 to build the installer.' }
    Invoke-Checked $compilerPath @("/DPublishDir=$destination", "/O$destination-installer", (Join-Path $root 'setup.iss'))
}
Write-Output "Windows package: $destination"
