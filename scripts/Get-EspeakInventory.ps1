[CmdletBinding()]
param(
    [string]$PublishDir = (Join-Path $PSScriptRoot '..\build\publish\current')
)

$resolvedPublishDir = (Resolve-Path -LiteralPath $PublishDir -ErrorAction Stop).Path
$espeakDir = Join-Path $resolvedPublishDir 'espeak'

if (-not (Test-Path -LiteralPath $espeakDir -PathType Container)) {
    throw "No eSpeak payload is present: $espeakDir"
}

Get-ChildItem -LiteralPath $espeakDir -Recurse -File |
    Sort-Object FullName |
    ForEach-Object {
        $relativePath = $_.FullName.Substring($espeakDir.Length).TrimStart('\')
        $kind = if ($_.Extension -eq '.dll') {
            'Native backend'
        }
        elseif ($relativePath -like 'espeak-ng-data\*') {
            'Language/data asset'
        }
        else {
            'License or support file'
        }

        [pscustomobject]@{
            File = "espeak\$relativePath"
            Bytes = $_.Length
            SourcePackage = 'KokoroSharp 0.8.0 content/espeak'
            Purpose = $kind
            License = 'GPL-3.0-or-later'
        }
    }
