# ChatVox installer and build

ChatVox is installer-first for normal Windows users.

## Install

1. Download the versioned `ChatVox-<version>-Setup.exe` asset from
   [GitHub Releases](https://github.com/Drakcain/ChatVox/releases).
2. Run the installer.
3. Optionally select **Create a desktop shortcut**.
4. Launch ChatVox from the Start Menu or desktop shortcut.
5. Select **Connect Twitch** and authorize through Twitch's browser page.

The installer is self-contained for Windows x64. It includes the supported
local Kokoro model and English voice assets, so it does not need a first-use
voice-model download.

The installer may place ChatVox under the Windows Program Files location. User
data stays separate under `%LOCALAPPDATA%\ChatVox`.

## Upgrade

Normal installer upgrades preserve local settings and the protected Twitch
authorization for the same Windows user. Close ChatVox before a manual upgrade
if the installer asks to do so.

## Uninstall and reset

Use **Settings > Apps > Installed apps > ChatVox** to uninstall the program.
Uninstalling the program does not automatically delete `%LOCALAPPDATA%\ChatVox`
so normal upgrades can retain settings and authorization.

To fully reset ChatVox, close it and delete `%LOCALAPPDATA%\ChatVox`. This
removes local settings, logs, cached speech assets, update files, and saved
Twitch authorization.

## Developer build

See [BUILD.md](../BUILD.md) for validation, publish, and installer commands.
