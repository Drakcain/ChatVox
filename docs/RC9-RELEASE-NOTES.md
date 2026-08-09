# ChatVox 1.0.0-rc.9

## Scope

RC.9 adds safer spoken Twitch username normalization and clarifies the two
local speech test actions.

## Username pronunciation

- Recognized common wrappers, word fragments, and high-confidence leetspeak
  render naturally for speech.
- The included user-name corpus is test data only; it does not ship a
  channel-specific alias list.
- Streamers can add a local `twitch_handle = spoken name` override when a
  particular handle needs a preferred pronunciation.
- Normalization changes only the speech text. Twitch identity, message IDs,
  filtering, and EventSub handling retain the original Twitch data.

## Test controls

- **Test Message** speaks the entered text exactly as written.
- **Test Username** sends the entered text through the same username
  normalization path used by live Twitch chat.

## Packaging gates

Before public publication, validate the RC.9 installer over the current
installation, confirm settings and authorization remain intact, and run both
test controls from the installed application.
