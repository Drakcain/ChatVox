using System.Text.Json.Serialization;
namespace ChatVox.Twitch;
public sealed record DeviceCodeResponse([property:JsonPropertyName("device_code")]string DeviceCode,[property:JsonPropertyName("user_code")]string UserCode,[property:JsonPropertyName("verification_uri")]string VerificationUri,[property:JsonPropertyName("expires_in")]int ExpiresIn,[property:JsonPropertyName("interval")]int Interval);
public sealed record TokenResponse([property:JsonPropertyName("access_token")]string AccessToken,[property:JsonPropertyName("refresh_token")]string RefreshToken,[property:JsonPropertyName("expires_in")]int ExpiresIn);
public sealed record ChatEvent(string MessageId, string Chatter, string Text, DateTimeOffset Received, string? ChatterUserId = null, string? ChatterLogin = null);
