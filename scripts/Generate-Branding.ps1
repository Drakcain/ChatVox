param(
    [string]$SourcePng = 'C:\Users\Administrator\Downloads\ChatVox.png'
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$root = Split-Path $PSScriptRoot -Parent
$branding = Join-Path $root 'assets\branding'
New-Item -ItemType Directory -Path $branding -Force | Out-Null

$png = Join-Path $branding 'ChatVox.png'
$ico = Join-Path $branding 'ChatVox.ico'
Copy-Item -LiteralPath $SourcePng -Destination $png -Force

$source = [System.Drawing.Image]::FromFile($png)
try {
    $sizes = 16,24,32,48,64,128,256
    $images = foreach ($size in $sizes) {
        $bitmap = [System.Drawing.Bitmap]::new($size, $size)
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        try {
            $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $graphics.DrawImage($source, 0, 0, $size, $size)
            $stream = [System.IO.MemoryStream]::new()
            $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
            ,$stream.ToArray()
        }
        finally { $graphics.Dispose(); $bitmap.Dispose() }
    }

    $stream = [System.IO.File]::Create($ico)
    $writer = [System.IO.BinaryWriter]::new($stream)
    try {
        $writer.Write([UInt16]0); $writer.Write([UInt16]1); $writer.Write([UInt16]$sizes.Count)
        $offset = 6 + (16 * $sizes.Count)
        for ($i = 0; $i -lt $sizes.Count; $i++) {
            $dimension = $sizes[$i]; $bytes = $images[$i]
            $writer.Write([Byte]($(if ($dimension -eq 256) { 0 } else { $dimension })))
            $writer.Write([Byte]($(if ($dimension -eq 256) { 0 } else { $dimension })))
            $writer.Write([Byte]0); $writer.Write([Byte]0); $writer.Write([UInt16]1); $writer.Write([UInt16]32)
            $writer.Write([UInt32]$bytes.Length); $writer.Write([UInt32]$offset); $offset += $bytes.Length
        }
        foreach ($bytes in $images) { $writer.Write($bytes) }
    }
    finally { $writer.Dispose(); $stream.Dispose() }
}
finally { $source.Dispose() }
