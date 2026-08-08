# Twitch setup
Create a Twitch Developer application and provide its Client ID to the app. Authorize the streamer with the minimum `user:read:chat` scope. Use EventSub WebSocket and `channel.chat.message`; no chat write scope is requested. Tokens must be DPAPI-protected and never logged. Live validation is required after the streamer authorizes.
