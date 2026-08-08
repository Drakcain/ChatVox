using ChatVox.Speech;

namespace ChatVox.Queue;

/// <summary>Single durable consumer for the speech queue. Recoverable item failures never end the loop.</summary>
public sealed class SpeechWorker : IAsyncDisposable
{
    private readonly FreshQueue queue;
    private readonly ISpeechEngine speech;
    private readonly Func<(string Voice, float Speed, float Volume, int GapMs)> options;
    private readonly Func<bool> paused;
    private readonly CancellationTokenSource stop = new();
    private readonly Task run;
    private volatile bool speaking;

    public event Action<string>? Diagnostic;
    public event Action? StateChanged;
    public bool IsSpeaking => speaking;
    public Task Completion => run;

    public SpeechWorker(FreshQueue queue, ISpeechEngine speech, Func<(string Voice, float Speed, float Volume, int GapMs)> options, Func<bool> paused)
    {
        this.queue = queue; this.speech = speech; this.options = options; this.paused = paused;
        run = Task.Run(RunAsync);
    }

    private async Task RunAsync()
    {
        Log("worker started");
        try
        {
            while (!stop.IsCancellationRequested)
            {
                if (paused()) { await Task.Delay(50, stop.Token); continue; }
                var item = queue.Take();
                if (item is null) { await Task.Delay(50, stop.Token); continue; }
                Log($"dequeue depth={queue.Count}");
                speaking = true; StateChanged?.Invoke();
                try
                {
                    var current = options();
                    Log("speech start");
                    await speech.SpeakAsync(item.Text, current.Voice, current.Speed, current.Volume);
                    Log("speech complete");
                }
                catch (OperationCanceledException) when (!stop.IsCancellationRequested) { Log("speech cancelled; continuing"); }
                catch (Exception ex) { Log("worker item exception: " + ex.Message); }
                finally { speaking = false; Log("worker item state released"); StateChanged?.Invoke(); }
                var gap = Math.Max(0, options().GapMs);
                if (gap > 0) await Task.Delay(gap, stop.Token);
            }
        }
        catch (OperationCanceledException) when (stop.IsCancellationRequested) { Log("worker exit: shutdown"); }
        catch (Exception ex) { Log("worker fatal exception: " + ex.Message); }
    }

    private void Log(string text) => Diagnostic?.Invoke($"{DateTime.Now:HH:mm:ss} {text}");
    public async ValueTask DisposeAsync() { stop.Cancel(); speech.Stop(); try { await run; } finally { stop.Dispose(); } }
}
