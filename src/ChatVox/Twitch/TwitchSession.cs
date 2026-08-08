namespace ChatVox.Twitch;

public enum SessionRestoreKind { MissingAuthorization, Valid, TransientFailure, PermanentAuthorizationFailure }
public sealed record SessionRestoreResult(SessionRestoreKind Kind, TokenResponse? Auth = null, TokenValidation? Identity = null, string? SafeDetail = null);

public sealed class TwitchSession(DeviceCodeClient oauth, ITokenLifecycle tokens, DpapiAuthStore store)
{
    public async Task<SessionRestoreResult> TryRestoreAsync(string clientId, CancellationToken ct)
    {
        var loaded = store.TryLoad();
        if (loaded.Kind != AuthBlobLoadKind.Loaded || loaded.Auth is null) return new(SessionRestoreKind.MissingAuthorization);

        var validation = await tokens.ValidateAsync(loaded.Auth.AccessToken, ct);
        if (validation.Kind == TokenValidationKind.Success) return new(SessionRestoreKind.Valid, loaded.Auth, validation.Identity);
        if (validation.Kind == TokenValidationKind.TransientFailure) return new(SessionRestoreKind.TransientFailure, loaded.Auth, null, validation.SafeDetail);

        var refresh = await tokens.RefreshAsync(clientId, loaded.Auth.RefreshToken, ct);
        if (refresh.Kind == TokenRefreshKind.TransientFailure) return new(SessionRestoreKind.TransientFailure, loaded.Auth, null, refresh.SafeDetail);
        if (refresh.Kind == TokenRefreshKind.PermanentAuthFailure || refresh.Auth is null)
        {
            store.Clear();
            return new(SessionRestoreKind.PermanentAuthorizationFailure, null, null, refresh.SafeDetail);
        }

        if (!store.TrySave(refresh.Auth)) return new(SessionRestoreKind.TransientFailure, loaded.Auth, null, "auth blob write failed");
        var refreshedValidation = await tokens.ValidateAsync(refresh.Auth.AccessToken, ct);
        return refreshedValidation.Kind switch
        {
            TokenValidationKind.Success => new(SessionRestoreKind.Valid, refresh.Auth, refreshedValidation.Identity),
            TokenValidationKind.TransientFailure => new(SessionRestoreKind.TransientFailure, refresh.Auth, null, refreshedValidation.SafeDetail),
            _ => PermanentAfterRefreshedTokenRejection()
        };
    }

    private SessionRestoreResult PermanentAfterRefreshedTokenRejection()
    {
        store.Clear();
        return new(SessionRestoreKind.PermanentAuthorizationFailure, null, null, "refreshed token rejected");
    }

    public async Task<(TokenResponse Auth, TokenValidation Identity)> AuthorizeAsync(string clientId, Action<DeviceCodeResponse>? show, CancellationToken ct)
    {
        var device = await oauth.StartAsync(clientId, ct);
        show?.Invoke(device);
        DeviceCodeClient.Open(device);
        var auth = await oauth.PollAsync(clientId, device, ct);
        var validation = await tokens.ValidateAsync(auth.AccessToken, ct);
        if (validation.Kind != TokenValidationKind.Success || validation.Identity is null) throw new InvalidOperationException("New Twitch authorization could not be validated.");
        store.Save(auth);
        return (auth, validation.Identity);
    }
}
