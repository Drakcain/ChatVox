namespace ChatVox.Speech;

public interface ISpeechEngine
{
    Task SpeakAsync(string text, string voice, float speed = 1, float volume = 1);
    void Stop();
}
