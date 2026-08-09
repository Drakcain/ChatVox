using System.Threading;

namespace ChatVox.Runtime;

/// <summary>Per-user-session ownership and local SHOW handoff for ChatVox.</summary>
public sealed class SingleInstanceCoordinator : IDisposable
{
    private readonly string mutexName;
    private readonly string showEventName;
    private readonly Action showExisting;
    private readonly CancellationTokenSource stopping = new();
    private Mutex? ownership;
    private EventWaitHandle? showEvent;
    private Task? listener;
    private bool primary;

    public SingleInstanceCoordinator(Action showExisting, string? identifier = null)
    {
        this.showExisting = showExisting;
        var suffix = string.IsNullOrWhiteSpace(identifier) ? "ChatVox" : identifier;
        mutexName = $"Local\\{suffix}.SingleInstance.v1";
        showEventName = $"Local\\{suffix}.ShowExisting.v1";
    }
    public bool IsPrimary => primary;

    public bool TryBecomePrimary()
    {
        ownership = new Mutex(true, mutexName, out var createdNew);
        primary = createdNew;
        if (primary) showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, showEventName);
        return primary;
    }

    public bool SignalExistingInstance()
    {
        for (var attempt = 0; attempt < 12; attempt++)
        {
            try
            {
                using var existing = EventWaitHandle.OpenExisting(showEventName);
                existing.Set();
                return true;
            }
            catch (WaitHandleCannotBeOpenedException) { Thread.Sleep(100); }
        }
        return false;
    }

    public void StartListening()
    {
        if (!primary || listener is not null || showEvent is null) return;
        listener = Task.Run(() =>
        {
            while (!stopping.IsCancellationRequested)
            {
                if (showEvent.WaitOne(100)) showExisting();
            }
        });
    }

    public void Dispose()
    {
        stopping.Cancel();
        try { listener?.Wait(TimeSpan.FromSeconds(1)); } catch { }
        showEvent?.Dispose();
        if (primary) ownership?.ReleaseMutex();
        ownership?.Dispose();
        stopping.Dispose();
    }
}
