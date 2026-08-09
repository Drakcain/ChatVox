# Twitch setup

ChatVox is already configured as a public Twitch desktop application. Normal
streamers do **not** need to create a Twitch developer application, provide a
Client ID, or create a Client Secret.

## Connect ChatVox

1. Open ChatVox.
2. Select **Connect Twitch**.
3. Follow the Twitch authorization page shown by ChatVox.
4. Sign in to Twitch only on Twitch's own page and approve the requested
   read-only access.
5. Return to ChatVox and wait for the status to show **Twitch Connected**.

ChatVox requests only the `user:read:chat` scope. It uses Twitch EventSub and
`channel.chat.message` to receive chat from the authorized channel.

## What ChatVox does not do

- It never asks for your Twitch password.
- It does not send Twitch chat messages.
- It does not request chat-write or moderation access.
- It does not require a Client Secret from the streamer.

Saved authorization is protected for the current Windows user and is restored
on normal launches and upgrades. To connect a different Twitch account, reset
the local ChatVox authorization and connect again.
