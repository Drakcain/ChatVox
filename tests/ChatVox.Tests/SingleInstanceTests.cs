using ChatVox.Runtime;

namespace ChatVox.Tests;

public sealed class SingleInstanceTests
{
    [Fact]
    public void SecondaryLaunchSignalsThePrimaryAndDoesNotAcquireOwnership()
    {
        using var shown = new ManualResetEventSlim();
        using var primary = new SingleInstanceCoordinator(() => shown.Set());
        Assert.True(primary.TryBecomePrimary());
        primary.StartListening();
        Thread.Sleep(100);
        using var secondary = new SingleInstanceCoordinator(() => throw new InvalidOperationException("Secondary must not own the listener."));
        Assert.False(secondary.TryBecomePrimary());
        Assert.True(secondary.SignalExistingInstance());
        Assert.True(shown.Wait(TimeSpan.FromSeconds(3)));
    }
}
