$ErrorActionPreference = 'Stop'

$root = Split-Path $PSScriptRoot -Parent
$version = (Get-Content -LiteralPath (Join-Path $root 'VERSION') -Raw).Trim()
if ([string]::IsNullOrWhiteSpace($version)) { throw 'VERSION is empty.' }

$iscc = 'C:\Users\Administrator\AppData\Local\Programs\Inno Setup 6\ISCC.exe'
if (!(Test-Path -LiteralPath $iscc)) { throw 'ISCC.exe unavailable' }

$installerTemp = Join-Path $root 'build\temp\installer'
if (Test-Path -LiteralPath $installerTemp) {
    Remove-Item -LiteralPath $installerTemp -Recurse -Force
}
New-Item -ItemType Directory -Path $installerTemp -Force | Out-Null

& $iscc "$root\installer\ChatVox.iss"
if ($LASTEXITCODE -ne 0) { throw "Inno Setup failed with exit code $LASTEXITCODE." }

$builtInstaller = Join-Path $installerTemp 'ChatVox-Setup.exe'
if (!(Test-Path -LiteralPath $builtInstaller)) { throw "Expected installer was not created: $builtInstaller" }

$releases = Join-Path $root 'releases'
$ready = Join-Path $root 'READY TO INSTALL'
New-Item -ItemType Directory -Path $releases -Force | Out-Null
New-Item -ItemType Directory -Path $ready -Force | Out-Null

$installerName = "ChatVox-$version-Setup.exe"
$releaseInstaller = Join-Path $releases $installerName
$readyInstaller = Join-Path $ready $installerName
$releaseSha = "$releaseInstaller.sha256"
$readySha = "$readyInstaller.sha256"

Copy-Item -LiteralPath $builtInstaller -Destination $releaseInstaller -Force
Get-ChildItem -LiteralPath $ready -File -Filter 'ChatVox-*-Setup.exe*' | Remove-Item -Force
Copy-Item -LiteralPath $builtInstaller -Destination $readyInstaller -Force

$releaseHash = (Get-FileHash -LiteralPath $releaseInstaller -Algorithm SHA256).Hash
$readyHash = (Get-FileHash -LiteralPath $readyInstaller -Algorithm SHA256).Hash
if ($releaseHash -ne $readyHash) { throw 'READY TO INSTALL hash does not match the archived release hash.' }
"$releaseHash  $installerName" | Set-Content -LiteralPath $releaseSha -NoNewline
Copy-Item -LiteralPath $releaseSha -Destination $readySha -Force

Write-Host "Release: $releaseInstaller"
Write-Host "Ready:   $readyInstaller"
Write-Host "SHA:     $readySha"
Write-Host "SHA256:  $releaseHash"
