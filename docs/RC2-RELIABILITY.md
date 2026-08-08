# ChatVox RC.2 Reliability Decision

## Confirmed RC.1 failure path

The RC.1 hourly token monitor treated every unsuccessful token-validation call as invalid. Its caller then disposed EventSub and set the UI to `Authorization Required`. This included transient transport errors, timeouts, 429 responses, and 5xx responses. Startup restoration also deleted the DPAPI authorization blob on any validation failure.

## RC.2 behavior

- Token validation has explicit `Success`, `Unauthorized`, and `TransientFailure` outcomes.
- Only confirmed validation 401 triggers refresh; only a permanently rejected refresh can lead to `Authorization Required`.
- Transient validation/refresh failures retain DPAPI authorization and use bounded retry/backoff.
- The monitor uses a one-hour production interval; automated tests inject accelerated intervals without changing production timing.
- Access-token expiry is interpreted with `TimeSpan.FromSeconds`; the documented Public refresh lifetime is `TimeSpan.FromDays(30)` and is not used to locally force reauthorization.
- EventSub reconnects after close/socket failures with bounded exponential backoff and recreates the chat subscription after each `session_welcome`.
- Subscription identity uses only the authorized broadcaster/user ID and WebSocket session ID. Game/category/title metadata is not part of the subscription path.
- Logs are bounded to five 2 MiB files under `%LOCALAPPDATA%\ChatVox\logs\`. Tokens, device codes, auth headers, DPAPI blobs, and chat bodies are suppressed.
