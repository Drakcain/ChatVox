using ChatVox.Runtime;

namespace ChatVox.Tests;

public sealed class SingleInstanceTests
{
    [Fact]
    public void SecondaryLaunchSignalsThePrimaryAndDoesNotAcquireOwnership()
    {
        using var shown = new ManualResetEventSlim();
        var identifier = "ChatVox.Tests." + Guid.NewGuid().ToString("N");
        using var primary = new SingleInstanceCoordinator(() => shown.Set(), identifier);
        Assert.True(primary.TryBecomePrimary());
        primary.StartListening();
        Thread.Sleep(100);
        using var secondary = new SingleInstanceCoordinator(() => throw new InvalidOperationException("Secondary must not own the listener."), identifier);
        Assert.False(secondary.TryBecomePrimary());
        Assert.True(secondary.SignalExistingInstance());
        Assert.True(shown.Wait(TimeSpan.FromSeconds(3)));
    }
}
