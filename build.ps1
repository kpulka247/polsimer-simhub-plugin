param (
    [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"
$rootDir = $PSScriptRoot
$releaseDir = Join-Path $rootDir "release"
$stagingDir = Join-Path $releaseDir "staging"
$zipFile = Join-Path $releaseDir "Polsimer-F74LED-SimHub-v$Version.zip"

function Remove-StagingDirectory {
    if (-not (Test-Path -LiteralPath $stagingDir)) {
        return
    }

    $releaseRoot = [System.IO.Path]::GetFullPath($releaseDir).TrimEnd([char[]]@('\', '/')) +
        [System.IO.Path]::DirectorySeparatorChar
    $stagingPath = [System.IO.Path]::GetFullPath($stagingDir)

    if (-not $stagingPath.StartsWith($releaseRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to remove a staging path outside the release directory: $stagingPath"
    }

    Remove-Item -LiteralPath $stagingPath -Recurse -Force
}

New-Item -ItemType Directory -Path $releaseDir -Force | Out-Null
Remove-StagingDirectory
New-Item -ItemType Directory -Path $stagingDir | Out-Null

Write-Host "==> Building SimHub plugin DLL..." -ForegroundColor Cyan
dotnet build (Join-Path $rootDir "src\Polsimer.SimHub.Plugin.csproj") -c Release
if ($LASTEXITCODE -ne 0) { throw "Plugin compilation failed!" }

$pluginDll = Get-ChildItem -Path (Join-Path $rootDir "src\bin\Release") -Filter "Polsimer.SimHub.Plugin.dll" -Recurse | Select-Object -First 1
if (-not $pluginDll) {
    throw "Compiled binary 'Polsimer.SimHub.Plugin.dll' not found!"
}
Copy-Item $pluginDll.FullName -Destination $stagingDir -Force

Write-Host "==> Publishing .NET Framework installer (setup.exe)..." -ForegroundColor Cyan
$installerOut = Join-Path $stagingDir "tmp_installer"
dotnet publish (Join-Path $rootDir "installer\Polsimer.Installer.csproj") -c Release -p:AssemblyName=setup -o $installerOut
if ($LASTEXITCODE -ne 0) { throw "Installer publishing failed!" }

$builtExe = Get-ChildItem -Path $installerOut -Filter "*setup*.exe" | Select-Object -First 1
if (-not $builtExe) {
    throw "Installer binary not found in build directory!"
}
Move-Item $builtExe.FullName (Join-Path $stagingDir "setup.exe") -Force

$stagingRoot = [System.IO.Path]::GetFullPath($stagingDir).TrimEnd([char[]]@('\', '/')) +
    [System.IO.Path]::DirectorySeparatorChar
$resolvedInstallerOut = [System.IO.Path]::GetFullPath($installerOut)
if (-not $resolvedInstallerOut.StartsWith($stagingRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Refusing to remove an installer output path outside staging: $resolvedInstallerOut"
}
Remove-Item -LiteralPath $resolvedInstallerOut -Recurse -Force
Get-ChildItem -Path $stagingDir -Filter "*.pdb" -ErrorAction SilentlyContinue | Remove-Item -Force

Write-Host "==> Gathering assets and compressing release ZIP..." -ForegroundColor Cyan

$txtReadmePath = Join-Path $rootDir "installer\readme.txt"
if (Test-Path $txtReadmePath) {
    Copy-Item $txtReadmePath -Destination $stagingDir -Force
} else {
    Write-Warning "File 'installer\readme.txt' not found!"
}

Compress-Archive -Path "$stagingDir\*" -DestinationPath $zipFile -Force
Remove-StagingDirectory

Write-Host "Build complete. Package created: $zipFile" -ForegroundColor Green
