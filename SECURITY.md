# Security policy

## Reporting a security issue

Please report suspected security issues privately through GitHub's security
advisory feature. Do not post credentials, tokens, DPAPI authorization blobs,
or unredacted logs in a public issue.

## Twitch authorization

ChatVox does not require, request, or store a Twitch password or Client Secret
from a streamer. Twitch authorization is performed on Twitch's own
authorization page.

Saved authorization is protected with Windows DPAPI for the current Windows
user. Do not copy `%LOCALAPPDATA%\ChatVox\auth\auth.bin` to another computer or
share it with anyone.

## Download safety

Download ChatVox only from this repository's
[GitHub Releases](https://github.com/Drakcain/ChatVox/releases) page. Each
release includes a versioned installer and a matching `.sha256` checksum file.
ChatVox verifies the matching checksum before it runs an updater-downloaded
installer.

## Local data and logs

ChatVox local data can include settings, protected authorization, diagnostics,
and update working files under `%LOCALAPPDATA%\ChatVox\`. Review diagnostics
before sharing them publicly and remove account-specific details when possible.

ChatVox does not include analytics, advertising, telemetry, or a cloud TTS
service. Twitch communication is limited to the Twitch services needed for
authorization and EventSub chat delivery.

See [PRIVACY.md](PRIVACY.md) for the full local-data description.
