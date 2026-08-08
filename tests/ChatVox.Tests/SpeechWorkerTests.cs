using ChatVox.Queue;
using ChatVox.Speech;

namespace ChatVox.Tests;

public sealed class SpeechWorkerTests
{
    [Fact] public async Task TwoMessagesProcessSequentially()
    {
        var fake = new FakeSpeech(); var queue = QueueOf("one", "two");
        await using var worker = Start(queue, fake);
        await Eventually(() => fake.Calls.Count == 2);
        Assert.Equal(["one", "two"], fake.Calls);
        Assert.Equal(0, queue.Count);
    }

    [Fact] public async Task ThreeMessagesProcessSequentially()
    {
        var fake = new FakeSpeech(); var queue = QueueOf("one", "two", "three");
        await using var worker = Start(queue, fake);
        await Eventually(() => fake.Calls.Count == 3);
        Assert.Equal(["one", "two", "three"], fake.Calls);
    }

    [Fact] public async Task TwelveRapidMessagesProcessSequentially()
    {
        var fake = new FakeSpeech(); var queue = new FreshQueue(20, TimeSpan.FromMinutes(1)); foreach (var text in Enumerable.Range(1, 12).Select(x => x.ToString())) queue.Add(text);
        await using var worker = Start(queue, fake);
        await Eventually(() => fake.Calls.Count == 12);
        Assert.Equal(Enumerable.Range(1, 12).Select(x => x.ToString()), fake.Calls);
    }

    [Fact] public async Task PausedWorkerResumesWithoutLosingQueuedMessage()
    {
        var paused = true; var fake = new FakeSpeech(); var queue = QueueOf("after resume");
        await using var worker = new SpeechWorker(queue, fake, () => ("af_heart", 1f, 1f, 0), () => paused);
        await Task.Delay(150); Assert.Empty(fake.Calls);
        paused = false;
        await Eventually(() => fake.Calls.Count == 1);
    }

    [Fact] public async Task ClearRemovesPendingMessages()
    {
        var paused = true; var fake = new FakeSpeech(); var queue = QueueOf("one", "two");
        await using var worker = new SpeechWorker(queue, fake, () => ("af_heart", 1f, 1f, 0), () => paused);
        queue.Clear(); paused = false;
        await Task.Delay(150); Assert.Empty(fake.Calls); Assert.Equal(0, queue.Count);
    }

    [Fact] public async Task ItemStateIsReleasedAfterEverySpeech()
    {
        var fake = new FakeSpeech(); var queue = QueueOf(); var diagnostics = new List<string>();
        await using var worker = Start(queue, fake, diagnostics);
        queue.Add("one"); queue.Add("two");
        await Eventually(() => fake.Calls.Count == 2);
        Assert.Equal(2, diagnostics.Count(x => x.Contains("worker item state released")));
    }

    [Fact] public async Task TtsExceptionDoesNotKillConsumer()
    {
        var fake = new FakeSpeech(new InvalidOperationException("expected")); var queue = QueueOf("bad", "good");
        await using var worker = Start(queue, fake);
        await Eventually(() => fake.Calls.Count == 2);
        Assert.False(worker.Completion.IsCompleted);
        Assert.Equal("good", fake.Calls[1]);
    }

    [Fact] public async Task CancelledSpeechDoesNotKillLaterSpeech()
    {
        var fake = new FakeSpeech(new OperationCanceledException("expected")); var queue = QueueOf("cancelled", "later");
        await using var worker = Start(queue, fake);
        await Eventually(() => fake.Calls.Count == 2);
        Assert.Equal("later", fake.Calls[1]);
    }

    [Fact] public async Task QueueStaysUsableAfterStopSpeaking()
    {
        var fake = new FakeSpeech(); var queue = QueueOf("first");
        await using var worker = Start(queue, fake);
        await Eventually(() => fake.Calls.Count == 1);
        fake.Stop(); queue.Add("second");
        await Eventually(() => fake.Calls.Count == 2);
        Assert.Equal("second", fake.Calls[1]);
    }

    [Fact] public async Task NextItemWaitsForActualPlaybackCompletion()
    {
        var speech = new CompletionControlledSpeech(); var queue = QueueOf("one", "two");
        await using var worker = new SpeechWorker(queue, speech, () => ("af_heart", 1f, 1f, 0), () => false);
        await speech.FirstStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.Single(speech.Calls);
        speech.CompleteCurrent();
        await Eventually(() => speech.Calls.Count == 2);
        speech.CompleteCurrent();
    }

    private static FreshQueue QueueOf(params string[] values) { var q = new FreshQueue(10, TimeSpan.FromMinutes(1)); foreach (var value in values) q.Add(value); return q; }
    private static SpeechWorker Start(FreshQueue q, FakeSpeech fake, List<string>? diagnostics = null)
    {
        var worker = new SpeechWorker(q, fake, () => ("af_heart", 1f, 1f, 0), () => false);
        if (diagnostics is not null) worker.Diagnostic += diagnostics.Add;
        return worker;
    }
    private static async Task Eventually(Func<bool> condition)
    {
        var until = DateTime.UtcNow.AddSeconds(3);
        while (!condition() && DateTime.UtcNow < until) await Task.Delay(20);
        Assert.True(condition());
    }

    private sealed class FakeSpeech(params Exception[] failures) : ISpeechEngine
    {
        private readonly Queue<Exception> failures = new(failures);
        public List<string> Calls { get; } = [];
        public Task SpeakAsync(string text, string voice, float speed = 1, float volume = 1)
        {
            Calls.Add(text);
            if (failures.TryDequeue(out var error)) return Task.FromException(error);
            return Task.CompletedTask;
        }
        public void Stop() { }
    }

    private sealed class CompletionControlledSpeech : ISpeechEngine
    {
        private TaskCompletionSource? current;
        public TaskCompletionSource FirstStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public List<string> Calls { get; } = [];
        public Task SpeakAsync(string text, string voice, float speed = 1, float volume = 1)
        {
            Calls.Add(text); current = new(TaskCreationOptions.RunContinuationsAsynchronously); FirstStarted.TrySetResult(); return current.Task;
        }
        public void CompleteCurrent() => current?.TrySetResult();
        public void Stop() => CompleteCurrent();
    }
}
