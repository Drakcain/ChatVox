namespace ChatVox.Twitch;

public sealed class TokenMonitor(ITokenLifecycle tokens, TimeSpan? interval = null)
{
    private readonly TimeSpan interval = interval ?? TimeSpan.FromHours(1);

    public async Task RunAsync(
        Func<TokenResponse> currentAuth,
        Func<TokenResponse, Task> refreshSucceeded,
        Func<TokenValidationResult, Task> validationObserved,
        Func<TokenRefreshResult, Task> refreshObserved,
        CancellationToken ct)
    {
        using var timer = new PeriodicTimer(interval);
        while (await timer.WaitForNextTickAsync(ct))
        {
            var validation = await ValidateWithRetryAsync(currentAuth, validationObserved, ct);
            if (validation.Kind != TokenValidationKind.Unauthorized) continue;

            var refresh = await RefreshWithRetryAsync(currentAuth, refreshObserved, ct);
            if (refresh.Kind == TokenRefreshKind.Success && refresh.Auth is not null) await refreshSucceeded(refresh.Auth);
        }
    }

    private async Task<TokenValidationResult> ValidateWithRetryAsync(Func<TokenResponse> currentAuth, Func<TokenValidationResult, Task> observed, CancellationToken ct)
    {
        for (var attempt = 0; ; attempt++)
        {
            TokenValidationResult result;
            try { result = await tokens.ValidateAsync(currentAuth().AccessToken, ct); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex) { result = TokenValidationResult.Transient("validation exception: " + ex.GetType().Name); }
            await observed(result);
            if (result.Kind != TokenValidationKind.TransientFailure || attempt >= 2) return result;
            await Task.Delay(RetryPolicy.Delay(attempt), ct);
        }
    }

    private async Task<TokenRefreshResult> RefreshWithRetryAsync(Func<TokenResponse> currentAuth, Func<TokenRefreshResult, Task> observed, CancellationToken ct)
    {
        for (var attempt = 0; ; attempt++)
        {
            TokenRefreshResult result;
            try { result = await tokens.RefreshAsync(TwitchAppConfiguration.ClientId, currentAuth().RefreshToken, ct); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex) { result = TokenRefreshResult.Transient("refresh exception: " + ex.GetType().Name); }
            await observed(result);
            if (result.Kind != TokenRefreshKind.TransientFailure || attempt >= 2) return result;
            await Task.Delay(RetryPolicy.Delay(attempt), ct);
        }
    }
}
