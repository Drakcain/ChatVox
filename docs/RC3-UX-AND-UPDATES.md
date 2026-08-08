# ChatVox RC.3 consumer behavior

ChatVox speaks readable viewer wording only. Twitch emotes and Unicode emoji are removed before speech; an emote-only message is ignored completely.

Authorize Twitch once. On later launches ChatVox loads the saved per-user authorization, reconnects automatically, and only shows **Connect Twitch** when authorization is missing or Twitch permanently rejects it. Transient network problems keep authorization intact and recover through the existing retry path.

Closing or minimizing ChatVox hides it in the system tray. Use the tray menu to open ChatVox, pause/resume speech, or choose **Exit ChatVox** for a full shutdown.

The optional **Start ChatVox with Windows** setting is per-user and uses only `HKCU\Software\Microsoft\Windows\CurrentVersion\Run\ChatVox`. **Start minimized to tray** is a separate saved preference.

Program files are installed under `C:\Program Files\ChatVox`. Per-user settings, authorization, logs, and future update downloads are kept under `%LOCALAPPDATA%\ChatVox`; normal upgrades and normal uninstall do not remove that data.

The update UI is safe groundwork only until ChatVox has a controlled release feed. It can check status without blocking Twitch startup, never installs silently, and never uses an invented download URL.
