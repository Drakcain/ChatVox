# Privacy

ChatVox is a local Windows Twitch chat text-to-speech application. After you
authorize it with Twitch, it receives chat messages needed for speech output.
Speech processing and audio playback occur locally on your computer.

ChatVox does not intentionally store chat transcripts in its operational logs.
OAuth authorization is protected using Windows DPAPI for the current Windows
user. Settings, authorization, update downloads, and bounded operational logs
are stored under `%LOCALAPPDATA%\ChatVox\`.

ChatVox checks GitHub Releases for software updates when the optional startup
check is enabled. GitHub receives ordinary network request metadata such as
your IP address. ChatVox contains no analytics, advertising, telemetry, or
sale of user information.

Uninstalling ChatVox preserves `%LOCALAPPDATA%\ChatVox\` so settings and
authorization survive a normal upgrade. To fully reset local data, close
ChatVox and delete that directory manually.
