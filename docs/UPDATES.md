# Updates

ChatVox checks the public `Drakcain/ChatVox` GitHub Releases feed at startup
when enabled. Stable receives final releases only; Preview receives release
candidates and final releases. The update path downloads only the exact
versioned installer plus its matching SHA-256 file over HTTPS. A mismatched or
malformed checksum is never executable.

Updates are user-approved. Normal upgrades preserve `%LOCALAPPDATA%\ChatVox`
including Twitch authorization, settings, and logs.
