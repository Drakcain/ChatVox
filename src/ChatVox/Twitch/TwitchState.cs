namespace ChatVox.Twitch;

public enum TwitchState
{
    Disconnected,
    Connecting,
    Connected,
    Reconnecting,
    TwitchError,
    NetworkError,
    AuthorizationRequired
}

public sealed class TwitchHealth
{
    public TwitchState State { get; private set; } = TwitchState.Disconnected;
    public DateTimeOffset? LastSuccessfulValidation { get; private set; }
    public string LastValidationResult { get; private set; } = "not run";
    public DateTimeOffset? LastSuccessfulEventSubConnection { get; private set; }
    public DateTimeOffset? LastEventSubReconnect { get; private set; }
    public int ReconnectAttempt { get; private set; }
    public string? LastSafeError { get; private set; }

    public void SetState(TwitchState state, string? safeError = null)
    {
        State = state;
        if (!string.IsNullOrWhiteSpace(safeError)) LastSafeError = safeError;
        if (state == TwitchState.Reconnecting) { LastEventSubReconnect = DateTimeOffset.UtcNow; ReconnectAttempt++; }
        if (state == TwitchState.Connected) { LastSuccessfulEventSubConnection = DateTimeOffset.UtcNow; ReconnectAttempt = 0; }
    }

    public void Validation(TokenValidationResult result)
    {
        LastValidationResult = result.Kind.ToString();
        if (result.Kind == TokenValidationKind.Success) LastSuccessfulValidation = DateTimeOffset.UtcNow;
        if (result.Kind == TokenValidationKind.TransientFailure && result.SafeDetail is not null) LastSafeError = result.SafeDetail;
    }
}
