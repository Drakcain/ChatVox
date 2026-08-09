# Building ChatVox

## Requirements

- Windows 10 or 11 x64
- .NET 10 SDK
- PowerShell 7 or Windows PowerShell
- Inno Setup 6 for installer builds
- Git

Install Inno Setup with:

```powershell
winget install --id JRSoftware.InnoSetup -e
```

Inno Setup is needed only to produce the Windows installer. End users do not
need the .NET SDK, PowerShell, Git, or Inno Setup to run the self-contained
ChatVox installer.

## Validate source

From the repository root:

```powershell
dotnet restore .\ChatVox.slnx
dotnet build .\ChatVox.slnx -c Release --no-restore
dotnet test .\ChatVox.slnx -c Release --no-build --no-restore
```

## Create a self-contained publish

```powershell
.\scripts\Build.ps1
```

The script creates `build\publish\current`. If the Kokoro model is not already
present, the script downloads it from the configured build source and verifies
its SHA-256 checksum before publishing.

## Create an installer

```powershell
.\scripts\Build-Installer.ps1
```

The installer build creates a versioned archive in `releases` and the current
local validation installer in `READY TO INSTALL`, with a matching `.sha256`
file for each.

## Do not commit

- `build/`
- `READY TO INSTALL/`
- `releases/`
- Kokoro model assets downloaded for local builds
- `%LOCALAPPDATA%\ChatVox` runtime data
- logs, authorization blobs, tokens, settings, crash dumps, or local update files
