using System.Net;
using System.Net.Http;
using System.Text;
using ChatVox.Logging;
using ChatVox.Twitch;

namespace ChatVox.Tests;

public sealed class ReliabilityTests
{
    [Theory]
    [InlineData(3600, 1)]
    [InlineData(86400, 24)]
    [InlineData(2592000, 720)]
    public void AccessTokenExpiryUsesSeconds(int expiresIn, int expectedHours) => Assert.Equal(TimeSpan.FromHours(expectedHours), TwitchTokenTiming.AccessTokenLifetime(expiresIn));

    [Fact] public void PublicRefreshLifetimeIsThirtyCalendarDays() => Assert.Equal(TimeSpan.FromDays(30), TwitchTokenTiming.PublicRefreshLifetime);

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests, TokenValidationKind.TransientFailure)]
    [InlineData(HttpStatusCode.InternalServerError, TokenValidationKind.TransientFailure)]
    [InlineData(HttpStatusCode.Unauthorized, TokenValidationKind.Unauthorized)]
    public async Task ValidationClassifiesOnly401AsUnauthorized(HttpStatusCode code, TokenValidationKind expected)
    {
        var lifecycle = new TokenLifecycle(new HttpClient(new StaticHandler(_ => new HttpResponseMessage(code))));
        Assert.Equal(expected, (await lifecycle.ValidateAsync("fake", CancellationToken.None)).Kind);
    }

    [Fact]
    public async Task ValidationNetworkExceptionIsTransient()
    {
        var lifecycle = new TokenLifecycle(new HttpClient(new StaticHandler(_ => throw new HttpRequestException("offline"))));
        Assert.Equal(TokenValidationKind.TransientFailure, (await lifecycle.ValidateAsync("fake", CancellationToken.None)).Kind);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests, TokenRefreshKind.TransientFailure)]
    [InlineData(HttpStatusCode.InternalServerError, TokenRefreshKind.TransientFailure)]
    [InlineData(HttpStatusCode.BadRequest, TokenRefreshKind.PermanentAuthFailure)]
    [InlineData(HttpStatusCode.Unauthorized, TokenRefreshKind.PermanentAuthFailure)]
    public async Task RefreshClassifiesTransientAndPermanentFailures(HttpStatusCode code, TokenRefreshKind expected)
    {
        var lifecycle = new TokenLifecycle(new HttpClient(new StaticHandler(_ => new HttpResponseMessage(code))));
        Assert.Equal(expected, (await lifecycle.RefreshAsync("client", "fake-refresh", CancellationToken.None)).Kind);
    }

    [Fact]
    public async Task TransientValidationDoesNotClearPersistedAuthorization()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".bin");
        var store = new DpapiAuthStore(path); var original = new TokenResponse("fake-access", "fake-refresh", 3600); store.Save(original);
        var session = new TwitchSession(new DeviceCodeClient(new HttpClient(new StaticHandler(_ => throw new InvalidOperationException()))), new FakeTokens([TokenValidationResult.Transient("timeout")]), store);
        var result = await session.TryRestoreAsync("client", CancellationToken.None);
        Assert.Equal(SessionRestoreKind.TransientFailure, result.Kind);
        Assert.Equal("fake-access", store.Load()!.AccessToken);
        File.Delete(path);
    }

    [Fact]
    public async Task UnauthorizedValidationRefreshesAndRotatesToken()
    {
        var fake = new FakeTokens([TokenValidationResult.Unauthorized()], [TokenRefreshResult.Success(new TokenResponse("new-access", "new-refresh", 3600))]);
        using var cancelled = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var monitor = new TokenMonitor(fake, TimeSpan.FromMilliseconds(5));
        TokenResponse? replaced = null;
        var task = monitor.RunAsync(() => new TokenResponse("old-access", "old-refresh", 3600), auth => { replaced = auth; cancelled.Cancel(); return Task.CompletedTask; }, _ => Task.CompletedTask, _ => Task.CompletedTask, cancelled.Token);
        try { await task; } catch (OperationCanceledException) { }
        Assert.Equal("new-access", replaced!.AccessToken);
        Assert.Equal("new-refresh", replaced.RefreshToken);
    }

    [Fact]
    public async Task MonitorSurvivesTransientValidationThenSucceeds()
    {
        var fake = new FakeTokens([TokenValidationResult.Transient("timeout"), TokenValidationResult.Success(new TokenValidation("app", "user", 3600))]);
        using var cancelled = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var observed = new List<TokenValidationKind>();
        var monitor = new TokenMonitor(fake, TimeSpan.FromMilliseconds(5));
        var task = monitor.RunAsync(() => new TokenResponse("fake-access", "fake-refresh", 3600), _ => Task.CompletedTask, result => { observed.Add(result.Kind); if (result.Kind == TokenValidationKind.Success) cancelled.Cancel(); return Task.CompletedTask; }, _ => Task.CompletedTask, cancelled.Token);
        try { await task; } catch (OperationCanceledException) { }
        Assert.Equal([TokenValidationKind.TransientFailure, TokenValidationKind.Success], observed);
    }

    [Fact]
    public void FailedAtomicUpdatePreservesExistingAuthBlob()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".bin");
        var store = new DpapiAuthStore(path); store.Save(new TokenResponse("old-access", "old-refresh", 3600));
        using (File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read)) Assert.False(store.TrySave(new TokenResponse("new-access", "new-refresh", 3600)));
        Assert.Equal("old-access", store.Load()!.AccessToken);
        File.Delete(path);
    }

    [Fact]
    public void LogsAreBoundedAndSuppressSecretsAndChatBodies()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var log = new OperationalLog(directory);
        log.Write("AUTH", "access_token fake-secret");
        log.Write("CHAT", "viewer said private message body");
        var text = File.ReadAllText(Path.Combine(directory, "chatvox.log"));
        Assert.DoesNotContain("fake-secret", text);
        Assert.DoesNotContain("private message body", text);
        for (var i = 0; i < 6; i++) log.Write("TEST", new string('x', (int)OperationalLog.MaxBytesPerFile));
        Assert.True(Directory.GetFiles(directory, "chatvox*.log").Length <= OperationalLog.MaxFiles);
        Directory.Delete(directory, true);
    }

    [Fact]
    public void SubscriptionUsesOnlyAuthorizedBroadcasterIdentity()
    {
        var subscription = ChatSubscription.Create("authorized-user", "session-id");
        Assert.Equal("authorized-user", subscription.BroadcasterUserId);
        Assert.Equal("authorized-user", subscription.UserId);
        Assert.Equal("session-id", subscription.SessionId);
    }

    private sealed class FakeTokens(IEnumerable<TokenValidationResult>? validations = null, IEnumerable<TokenRefreshResult>? refreshes = null) : ITokenLifecycle
    {
        private readonly Queue<TokenValidationResult> validations = new(validations ?? [TokenValidationResult.Success(new TokenValidation("app", "user", 3600))]);
        private readonly Queue<TokenRefreshResult> refreshes = new(refreshes ?? [TokenRefreshResult.Transient("not configured")]);
        public Task<TokenValidationResult> ValidateAsync(string token, CancellationToken ct) => Task.FromResult(validations.Count > 1 ? validations.Dequeue() : validations.Peek());
        public Task<TokenRefreshResult> RefreshAsync(string clientId, string refreshToken, CancellationToken ct) => Task.FromResult(refreshes.Count > 1 ? refreshes.Dequeue() : refreshes.Peek());
    }

    private sealed class StaticHandler(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(response(request));
    }
}
