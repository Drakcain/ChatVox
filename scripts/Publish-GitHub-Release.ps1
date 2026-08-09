[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
Set-Location $root
$version = (Get-Content .\VERSION -Raw).Trim()
$tag = "v$version"
$installer = Join-Path $root "READY TO INSTALL\ChatVox-$version-Setup.exe"
$sha = "$installer.sha256"
if ((git status --porcelain)) { throw 'Git working tree must be clean before publishing.' }
if (!(Test-Path $installer) -or !(Test-Path $sha)) { throw 'Expected installer or SHA asset is missing.' }
$actual = (Get-FileHash $installer -Algorithm SHA256).Hash
$expected = (Get-Content $sha -Raw).Trim() -split '\s+' | Select-Object -First 1
if ($actual -ne $expected) { throw 'Installer SHA-256 does not match its SHA asset.' }
$repo = 'Drakcain/ChatVox'
if ((gh repo view $repo --json nameWithOwner --jq .nameWithOwner) -ne $repo) { throw 'Unexpected GitHub repository.' }
if (!(git tag -l $tag)) { git tag -a $tag -m "ChatVox $version"; git push origin $tag }
$notes = Get-Content -LiteralPath (Join-Path $root 'docs\RC6-RELEASE-NOTES.md') -Raw
$args = @('release','create',$tag,$installer,$sha,'--repo',$repo,'--title',"ChatVox $version",'--notes',$notes)
if ($version -match '-rc\.') { $args += '--prerelease' }
gh @args
gh release view $tag --repo $repo --json url --jq .url
