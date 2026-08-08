$ErrorActionPreference = 'Stop'

$root = Split-Path $PSScriptRoot -Parent
$publishRoot = Join-Path $root 'build\publish'
$publishPath = Join-Path $publishRoot 'current'
$modelPath = Join-Path $root 'kokoro-fp16.onnx'
$modelHash = '027A25B14AEF7D3AE57FD09301EBEFBEC868E79D55213D07E4F3AF442F5BA352'

if (-not (Test-Path -LiteralPath $modelPath)) {
    Write-Host 'Downloading the build-time Kokoro model...'
    Invoke-WebRequest -Uri 'https://huggingface.co/hexgrad/Kokoro-82M/resolve/main/kokoro-v1.0.fp16.onnx' -OutFile $modelPath
}
if ((Get-FileHash -LiteralPath $modelPath -Algorithm SHA256).Hash -ne $modelHash) { throw 'Kokoro model checksum mismatch.' }

# This script owns only build\publish\current. Source, releases, and runtime data are never cleaned here.
if (Test-Path -LiteralPath $publishPath) {
    Remove-Item -LiteralPath $publishPath -Recurse -Force
}

New-Item -ItemType Directory -Path $publishRoot -Force | Out-Null
dotnet publish "$root\src\ChatVox\ChatVox.csproj" -c Release -r win-x64 --self-contained true -o $publishPath
