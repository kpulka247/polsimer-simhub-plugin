param (
    [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"
$rootDir = $PSScriptRoot
$releaseDir = Join-Path $rootDir "release"
$stagingDir = Join-Path $releaseDir "staging"
$zipFile = Join-Path $releaseDir "Polsimer-F74LED-SimHub-v$Version.zip"

Write-Host "==> [1/4] Cleaning release directory..." -ForegroundColor Cyan
if (Test-Path $releaseDir) { Remove-Item $releaseDir -Recurse -Force }
New-Item -ItemType Directory -Path $stagingDir | Out-Null

Write-Host "==> [2/4] Building SimHub plugin DLL..." -ForegroundColor Cyan
dotnet build (Join-Path $rootDir "src\Polsimer.SimHub.Plugin.csproj") -c Release
if ($LASTEXITCODE -ne 0) { throw "Plugin compilation failed!" }

$pluginDll = Get-ChildItem -Path (Join-Path $rootDir "src\bin\Release") -Filter "Polsimer.SimHub.Plugin.dll" -Recurse | Select-Object -First 1
if (-not $pluginDll) {
    throw "Compiled binary 'Polsimer.SimHub.Plugin.dll' not found!"
}
Copy-Item $pluginDll.FullName -Destination $stagingDir -Force

Write-Host "==> [3/4] Publishing standalone installer (setup.exe)..." -ForegroundColor Cyan
$installerOut = Join-Path $stagingDir "tmp_installer"
dotnet publish (Join-Path $rootDir "installer\Polsimer.Installer.csproj") `
    -c Release `
    -r win-x64 `
    --self-contained false `
    -p:PublishSingleFile=true `
    -p:AssemblyName=setup `
    -o $installerOut
if ($LASTEXITCODE -ne 0) { throw "Installer compilation failed!" }

$builtExe = Get-ChildItem -Path $installerOut -Filter "*setup*.exe" | Select-Object -First 1
if (-not $builtExe) {
    throw "Installer binary not found in build directory!"
}
Move-Item $builtExe.FullName (Join-Path $stagingDir "setup.exe") -Force
Remove-Item $installerOut -Recurse -Force
Get-ChildItem -Path $stagingDir -Filter "*.pdb" -ErrorAction SilentlyContinue | Remove-Item -Force

Write-Host "==> [4/4] Gathering assets and compressing release ZIP..." -ForegroundColor Cyan
$profilePath = Join-Path $rootDir "Polsimer_F74LED.ledsprofile"
if (Test-Path $profilePath) {
    Copy-Item $profilePath -Destination $stagingDir -Force
} else {
    Write-Warning "File 'Polsimer_F74LED.ledsprofile' not found in root directory!"
}

# Pobieranie readme.txt z katalogu installer
$txtReadmePath = Join-Path $rootDir "installer\readme.txt"
if (Test-Path $txtReadmePath) {
    Copy-Item $txtReadmePath -Destination $stagingDir -Force
} else {
    Write-Warning "File 'installer\readme.txt' not found!"
}

Compress-Archive -Path "$stagingDir\*" -DestinationPath $zipFile -Force
Remove-Item $stagingDir -Recurse -Force

Write-Host "`nBuild complete! Package created: $zipFile" -ForegroundColor Green