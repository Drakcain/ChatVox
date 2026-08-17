using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace ChatVox.Twitch;

public sealed record TokenValidation([property: JsonPropertyName("client_id")] string ClientId, [property: JsonPropertyName("user_id")] string UserId, [property: JsonPropertyName("expires_in")] int ExpiresIn, [property: JsonPropertyName("login")] string? Login = null);
public enum TokenValidationKind { Success, Unauthorized, TransientFailure }
public sealed record TokenValidationResult(TokenValidationKind Kind, TokenValidation? Identity = null, int? HttpStatus = null, string? SafeDetail = null)
{
    public static TokenValidationResult Success(TokenValidation identity) => new(TokenValidationKind.Success, identity);
    public static TokenValidationResult Unauthorized(int? status = 401) => new(TokenValidationKind.Unauthorized, null, status, "validation rejected");
    public static TokenValidationResult Transient(string detail, int? status = null) => new(TokenValidationKind.TransientFailure, null, status, detail);
}

public enum TokenRefreshKind { Success, PermanentAuthFailure, TransientFailure }
public sealed record TokenRefreshResult(TokenRefreshKind Kind, TokenResponse? Auth = null, int? HttpStatus = null, string? SafeDetail = null)
{
    public static TokenRefreshResult Success(TokenResponse auth) => new(TokenRefreshKind.Success, auth);
    public static TokenRefreshResult Permanent(string detail, int? status = null) => new(TokenRefreshKind.PermanentAuthFailure, null, status, detail);
    public static TokenRefreshResult Transient(string detail, int? status = null) => new(TokenRefreshKind.TransientFailure, null, status, detail);
}

public interface ITokenLifecycle
{
    Task<TokenValidationResult> ValidateAsync(string token, CancellationToken ct);
    Task<TokenRefreshResult> RefreshAsync(string clientId, string refreshToken, CancellationToken ct);
}

public static class TwitchTokenTiming
{
    public static readonly TimeSpan PublicRefreshLifetime = TimeSpan.FromDays(30);
    public static TimeSpan AccessTokenLifetime(int expiresInSeconds) => TimeSpan.FromSeconds(expiresInSeconds);
}

public sealed class TokenLifecycle(HttpClient http) : ITokenLifecycle
{
    public async Task<TokenValidationResult> ValidateAsync(string token, CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://id.twitch.tv/oauth2/validate");
            request.Headers.Add("Authorization", "OAuth " + token);
            using var response = await http.SendAsync(request, ct);
            if (response.IsSuccessStatusCode)
            {
                var identity = await response.Content.ReadFromJsonAsync<TokenValidation>(cancellationToken: ct);
                return identity is null ? TokenValidationResult.Transient("validation response malformed", (int)response.StatusCode) : TokenValidationResult.Success(identity);
            }
            return response.StatusCode == HttpStatusCode.Unauthorized
                ? TokenValidationResult.Unauthorized((int)response.StatusCode)
                : TokenValidationResult.Transient("validation HTTP " + (int)response.StatusCode, (int)response.StatusCode);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) { return TokenValidationResult.Transient("validation transport " + ex.GetType().Name); }
    }

    public async Task<TokenRefreshResult> RefreshAsync(string clientId, string refreshToken, CancellationToken ct)
    {
        try
        {
            using var response = await http.PostAsync("https://id.twitch.tv/oauth2/token", new FormUrlEncodedContent([new("client_id", clientId), new("refresh_token", refreshToken), new("grant_type", "refresh_token")]), ct);
            if (response.IsSuccessStatusCode)
            {
                var auth = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: ct);
                return auth is null || string.IsNullOrWhiteSpace(auth.AccessToken) || string.IsNullOrWhiteSpace(auth.RefreshToken)
                    ? TokenRefreshResult.Transient("refresh response malformed", (int)response.StatusCode)
                    : TokenRefreshResult.Success(auth);
            }
            var status = (int)response.StatusCode;
            return response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized
                ? TokenRefreshResult.Permanent("refresh rejected", status)
                : TokenRefreshResult.Transient("refresh HTTP " + status, status);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) { return TokenRefreshResult.Transient("refresh transport " + ex.GetType().Name); }
    }
}
