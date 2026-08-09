# ChatVox

## Live Chat TTS Reader for Twitch

ChatVox is a Windows x64 companion app that reads authorized Twitch chat aloud using local Kokoro text-to-speech.

Speech generation runs locally on the PC. ChatVox does not require a cloud TTS account, paid speech API, or external voice service.

## Features

- 28 local American and British English voices.
- Twitch EventSub chat reading using authorized Twitch access.
- Saved Twitch authorization is restored automatically between normal launches and upgrades.
- Automatic username pronunciation for Twitch-style handles.
- Separate **Test Message** and **Test Username** controls for speech testing.
- Words-only speech with Twitch emotes and optional Unicode emoji filtering removed before TTS.
- Editable ignored-user list.
- Optional filtering for chat commands and URLs.
- Bounded fresh-message queue to prevent old chat from piling up.
- Pause, Clear Queue, and Stop Speaking controls.
- System tray support.
- Start ChatVox with Windows.
- Optional start-minimized behavior.
- True single-instance handling: launching ChatVox again restores the existing instance instead of starting a duplicate.
- Responsive, resizable interface with monitor- and DPI-aware window placement restoration.
- Local Kokoro voice runtime cache stored per Windows user while installed Program Files assets remain read-only.
- Optional startup update checks using published ChatVox GitHub releases.

## Project docs

- [Twitch setup](docs/TWITCH-SETUP.md)
- [Installer and build guide](docs/INSTALLER.md)
- [Update behavior](docs/UPDATES.md)
- [Voice compatibility](docs/VOICE-COMPATIBILITY.md)
- [Changelog](CHANGELOG.md)

## Support and legal

- [Security policy](SECURITY.md)
- [Privacy policy](PRIVACY.md)
- [Third-party notices](THIRD-PARTY-NOTICES.txt)
- [MIT license](LICENSE)

## Username pronunciation

When **Read usernames** is enabled, ChatVox attempts to turn Twitch-style usernames into natural spoken text before sending them to Kokoro.

Examples of supported normalization include:

```text
Squ1rr3lM0m      -> squirrel mom
N3onNinja        -> neon ninja
CaptainN00b      -> captain noob
number1redstar   -> number 1 red star
```

ChatVox uses conservative local normalization rules and managed English pronunciation fallback.

Username pronunciation is best-effort. Stylized, abbreviated, invented, or intentionally unusual usernames may not always match the pronunciation intended by their owner.

Use **Test Username** to hear how ChatVox will process a username through the same normalization path used for live Twitch chat.

## Test controls

### Test Message

Speaks the text exactly as a normal message would be sent to the speech engine.

### Test Username

Processes the entered text as a Twitch username using ChatVox's username-normalization path before speaking it.

## Download and install

Download the latest ChatVox installer from [GitHub Releases](https://github.com/Drakcain/ChatVox/releases).

Then:

1. Run the installer.
2. Launch ChatVox.
3. Connect Twitch once.
4. Choose a voice and preferred settings.
5. Leave ChatVox running or minimize it to the system tray.

Normal upgrades preserve local settings and saved Twitch authorization.

## Twitch connection

ChatVox uses Twitch EventSub to receive authorized chat messages.

ChatVox:

- reads chat only
- does not send Twitch chat messages
- does not moderate chat
- does not require a Twitch Client Secret from the user
- does not require users to create their own Twitch developer application

## Speech and queue behavior

ChatVox uses a bounded queue designed for live chat rather than long-form narration.

Current defaults include:

- Maximum pending messages: 6
- Maximum queued-message age: 30 seconds
- Gap between spoken messages: 500 ms
- Maximum message length: 200 characters

When the queue is full, older pending messages may be discarded so ChatVox stays close to current chat activity.

The message currently being spoken is not interrupted by normal queue overflow.

## Filters

ChatVox can ignore:

- Twitch emotes
- Unicode emoji
- `!commands`
- URLs
- configured ignored users
- empty messages

The ignored-user list is editable and matched case-insensitively.

## Voice support

ChatVox currently supports:

- American English
- British English

The release includes 28 supported Kokoro voices.

ChatVox does **not** include:

- eSpeak
- eSpeak NG
- multilingual Kokoro voice packs
- `voices-zh`

Unsupported or historical voice selections are migrated to a supported English voice when necessary.

## Local data

ChatVox stores runtime data under the current Windows user profile. This can include:

- settings
- saved Twitch authorization
- logs
- cached speech assets
- update working files

Authentication data is protected for the current Windows user.

Release installers do not include user authorization, settings, logs, tokens, or other per-user runtime data.

## Updates

ChatVox can optionally check GitHub Releases for newer published versions.

Update behavior includes:

- semantic version comparison
- draft releases ignored
- matching installer and SHA-256 checksum required
- checksum verification before installation
- user approval before installing an update

Update failures do not prevent Twitch chat reading or local TTS from continuing.

## Privacy

ChatVox performs speech synthesis locally.

Chat messages are not sent to a third-party cloud TTS provider for voice generation.

Twitch communication still occurs with Twitch services as required for authorization and EventSub chat delivery.

See [PRIVACY.md](PRIVACY.md) for additional details.

## Licenses

ChatVox-owned source code is licensed under the MIT License.

Third-party libraries, models, and other components retain their own licenses.

See:

- [LICENSE](LICENSE)
- [THIRD-PARTY-NOTICES.txt](THIRD-PARTY-NOTICES.txt)

## Current release

Current public prerelease: **ChatVox 1.0.0-rc.11**.

See the [GitHub Releases page](https://github.com/Drakcain/ChatVox/releases) for the current installer and checksum.
