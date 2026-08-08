using System.Net.Http.Json;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.IO;

namespace ChatVox.Twitch;

public sealed record ChatSubscription(string BroadcasterUserId, string UserId, string SessionId)
{
    public static ChatSubscription Create(string authorizedUserId, string sessionId) => new(authorizedUserId, authorizedUserId, sessionId);
}

public sealed class EventSubClient(HttpClient http, Action<string>? log = null) : IAsyncDisposable
{
    private readonly object sync = new();
    private ClientWebSocket? ws;
    private CancellationTokenSource? run;
    private Task? receiveLoop;
    public event Action<ChatEvent>? Chat;
    public event Action<string>? Status;
    public event Action<string>? Diagnostic;

    public Task ConnectAsync(string token, string clientId, string user, CancellationToken ct)
    {
        lock (sync)
        {
            if (run is not null) return Task.CompletedTask;
            run = CancellationTokenSource.CreateLinkedTokenSource(ct);
            receiveLoop = RunAsync(token, clientId, user, run.Token);
        }
        return Task.CompletedTask;
    }

    private async Task RunAsync(string token, string clientId, string user, CancellationToken ct)
    {
        var target = new Uri("wss://eventsub.wss.twitch.tv/ws");
        var attempt = 0;
        while (!ct.IsCancellationRequested)
        {
            ClientWebSocket? socket = null;
            try
            {
                Status?.Invoke(attempt == 0 ? "Connecting" : "Reconnecting");
                if (attempt > 0)
                {
                    var delay = RetryPolicy.Delay(attempt - 1);
                    Diagnostic?.Invoke($"EventSub reconnect backoff {delay.TotalSeconds:0}s attempt {attempt}");
                    log?.Invoke($"EventSub reconnect backoff {delay.TotalSeconds:0}s attempt {attempt}");
                    await Task.Delay(delay, ct);
                }

                socket = new ClientWebSocket();
                lock (sync) ws = socket;
                await socket.ConnectAsync(target, ct);
                Diagnostic?.Invoke("EventSub WebSocket connected");
                log?.Invoke("EventSub WebSocket connected");
                var reconnectTarget = await ReceiveAsync(socket, token, clientId, user, ct);
                if (ct.IsCancellationRequested) return;
                target = reconnectTarget ?? new Uri("wss://eventsub.wss.twitch.tv/ws");
                attempt++;
                Status?.Invoke("Reconnecting");
                log?.Invoke("EventSub socket closed; recovery scheduled");
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { return; }
            catch (Exception ex)
            {
                attempt++;
                Status?.Invoke("Twitch Error");
                var safe = "EventSub " + ex.GetType().Name;
                Diagnostic?.Invoke(safe);
                log?.Invoke(safe);
            }
            finally
            {
                if (socket is not null)
                {
                    lock (sync) { if (ReferenceEquals(ws, socket)) ws = null; }
                    socket.Dispose();
                }
            }
        }
    }

    private async Task<Uri?> ReceiveAsync(ClientWebSocket socket, string token, string clientId, string user, CancellationToken ct)
    {
        while (socket.State == WebSocketState.Open && !ct.IsCancellationRequested)
        {
            var json = await ReceiveTextAsync(socket, ct);
            if (json is null) return null;
            using var document = JsonDocument.Parse(json);
            var type = document.RootElement.GetProperty("metadata").GetProperty("message_type").GetString();
            if (type == "session_welcome")
            {
                var session = EventSubParser.WelcomeSession(json) ?? throw new InvalidOperationException("EventSub welcome had no session.");
                Diagnostic?.Invoke("EventSub session_welcome received");
                log?.Invoke("EventSub session_welcome received");
                await Subscribe(token, clientId, ChatSubscription.Create(user, session), ct);
                Status?.Invoke("Connected");
                log?.Invoke("EventSub subscription created");
            }
            else if (type == "session_keepalive") { Diagnostic?.Invoke("EventSub keepalive received"); }
            else if (type == "notification")
            {
                var chat = EventSubParser.Chat(json, DateTimeOffset.UtcNow);
                Diagnostic?.Invoke(chat is null ? "EventSub notification ignored: unsupported payload" : $"EventSub notification received id={chat.MessageId}");
                if (chat is not null) Chat?.Invoke(chat);
            }
            else if (type == "session_reconnect")
            {
                var url = EventSubParser.ReconnectUrl(json);
                if (url is null) throw new InvalidOperationException("EventSub reconnect had no URL.");
                Status?.Invoke("Reconnecting");
                Diagnostic?.Invoke("EventSub reconnect requested");
                log?.Invoke("EventSub reconnect requested");
                return new Uri(url);
            }
        }
        return null;
    }

    private static async Task<string?> ReceiveTextAsync(ClientWebSocket socket, CancellationToken ct)
    {
        var buffer = new byte[8192];
        using var message = new MemoryStream();
        while (true)
        {
            var result = await socket.ReceiveAsync(buffer, ct);
            if (result.MessageType == WebSocketMessageType.Close) return null;
            if (result.MessageType != WebSocketMessageType.Text) continue;
            message.Write(buffer, 0, result.Count);
            if (message.Length > 1_048_576) throw new InvalidOperationException("EventSub message exceeded 1 MB.");
            if (result.EndOfMessage) return Encoding.UTF8.GetString(message.ToArray());
        }
    }

    private async Task Subscribe(string token, string clientId, ChatSubscription subscription, CancellationToken ct)
    {
        var body = new { type = "channel.chat.message", version = "1", condition = new { broadcaster_user_id = subscription.BroadcasterUserId, user_id = subscription.UserId }, transport = new { method = "websocket", session_id = subscription.SessionId } };
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.twitch.tv/helix/eventsub/subscriptions") { Content = JsonContent.Create(body) };
        request.Headers.Add("Authorization", "Bearer " + token);
        request.Headers.Add("Client-Id", clientId);
        using var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException("EventSub subscription HTTP " + (int)response.StatusCode);
    }

    public async ValueTask DisposeAsync()
    {
        CancellationTokenSource? source;
        Task? loop;
        ClientWebSocket? socket;
        lock (sync) { source = run; loop = receiveLoop; socket = ws; run = null; receiveLoop = null; ws = null; }
        source?.Cancel();
        try { if (socket?.State == WebSocketState.Open) await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "shutdown", CancellationToken.None); } catch { }
        socket?.Dispose();
        if (loop is not null) try { await loop; } catch { }
        source?.Dispose();
    }
}
