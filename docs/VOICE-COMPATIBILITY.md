# ChatVox RC.4 English Voice Compatibility

ChatVox 1.0.0-rc.4 bundles the Kokoro v1.0 FP16 model and **28** local American and British English voice assets. The selector is intentionally English-only. Voice assets are installed locally and do not require per-voice downloads.

## Supported

- Model family: Kokoro v1.0
- Verified voice assets: 28
- Categories: American Female, American Male, British Female, British Male
- Selector behavior: grouped by category; settings persist the internal voice ID.
- Asset location after installation: `voices\*.npy`

## Removed voice assets and eSpeak

KokoroSharp supplies additional multilingual and `voices\voices-zh\*.npy` assets. They are outside this release's American/British English product scope and are not shipped. eSpeak NG is also not shipped.

The RC.4 build removes non-English voice assets and eSpeak from its runtime/publish payload. A prior setting that names a removed voice safely falls back to `af_heart`; unrelated settings and Twitch authorization are retained.
