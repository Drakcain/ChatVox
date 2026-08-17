# ChatVox 1.0.0-rc.12

## What changed

- Added **Ignore my own messages**, enabled by default. ChatVox no longer speaks messages sent by the Twitch account connected to the app.
- Increased the default queue size for new profiles from 6 to 8 pending messages to handle short chat bursts more smoothly.

## Existing settings

- Existing queue limits are preserved.
- The new own-message filter defaults to enabled for existing settings files.

## Validation

- Release build: 0 errors, 0 warnings.
- Automated tests: 135 passed, 0 failed.

This is a prerelease. ChatVox remains local Kokoro text-to-speech; no cloud TTS, eSpeak runtime, new Twitch scopes, or client secret were added.
