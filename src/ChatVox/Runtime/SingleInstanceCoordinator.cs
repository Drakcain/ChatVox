using System.Threading;

namespace ChatVox.Runtime;

/// <summary>Per-user-session ownership and local SHOW handoff for ChatVox.</summary>
public sealed class SingleInstanceCoordinator : IDisposable
{
    private const string MutexName = "Local\\ChatVox.SingleInstance.v1";
    private const string ShowEventName = "Local\\ChatVox.ShowExisting.v1";
    private readonly Action showExisting;
    private readonly CancellationTokenSource stopping = new();
    private Mutex? ownership;
    private EventWaitHandle? showEvent;
    private Task? listener;
    private bool primary;

    public SingleInstanceCoordinator(Action showExisting) => this.showExisting = showExisting;
    public bool IsPrimary => primary;

    public bool TryBecomePrimary()
    {
        ownership = new Mutex(true, MutexName, out var createdNew);
        primary = createdNew;
        if (primary) showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowEventName);
        return primary;
    }

    public bool SignalExistingInstance()
    {
        for (var attempt = 0; attempt < 12; attempt++)
        {
            try
            {
                using var existing = EventWaitHandle.OpenExisting(ShowEventName);
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
