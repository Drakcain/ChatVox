$ErrorActionPreference='Stop'; $root=Split-Path $PSScriptRoot -Parent; dotnet test "$root\ChatVox.slnx" -c Release
