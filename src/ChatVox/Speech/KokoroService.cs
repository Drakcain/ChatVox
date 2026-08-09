using KokoroSharp;
using KokoroSharp.Core;
using KokoroSharp.Processing;
using System.IO;
namespace ChatVox.Speech;
public sealed class KokoroService : ISpeechEngine, IDisposable {
 private KokoroTTS? tts; private readonly SemaphoreSlim gate=new(1,1);
 private TaskCompletionSource? activePlayback;
 public event Action<string>? Diagnostic;
 public async Task SpeakAsync(string text,string voice,float speed=1,float volume=1) { await gate.WaitAsync(); Diagnostic?.Invoke("semaphore acquired"); Action<SpeechCompletionPacket>? completed=null;Action<SpeechCancellationPacket>? cancelled=null;try { var phonemization=EnglishPhonemizer.Phonemize(text,Voices.IsBritish(voice)); if(!phonemization.HasSpeech){Diagnostic?.Invoke("TTS input skipped: no supported English phonemes.");return;} tts ??= LoadBundledModel(); EnsureUserWritableVoiceCache(); var done=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);activePlayback=done;completed=_=>done.TrySetResult();cancelled=_=>done.TrySetResult();tts.OnSpeechCompleted+=completed;tts.OnSpeechCanceled+=cancelled;tts.SetVolume(Math.Clamp(volume,0,1));tts.Speak_Phonemes(phonemization.NormalizedText,phonemization.Tokens,KokoroVoiceManager.GetVoice(voice),new KokoroTTSPipelineConfig{Speed=Math.Clamp(speed,.5f,2f)},fast:true);Diagnostic?.Invoke("waiting for Kokoro playback completion");await done.Task.WaitAsync(TimeSpan.FromMinutes(2)); } finally { if(tts is not null){if(completed is not null)tts.OnSpeechCompleted-=completed;if(cancelled is not null)tts.OnSpeechCanceled-=cancelled;}activePlayback=null;gate.Release(); Diagnostic?.Invoke("semaphore released"); } }
 private static KokoroTTS LoadBundledModel(){var path=Path.Combine(AppContext.BaseDirectory,"kokoro-fp16.onnx");if(!File.Exists(path))throw new FileNotFoundException("Bundled Kokoro model is missing.",path);return KokoroTTS.LoadModel(path,null);}
 private void EnsureUserWritableVoiceCache()
 {
     if (KokoroVoiceManager.Voices.Count > 0) return;
     var source = Path.Combine(AppContext.BaseDirectory, "voices");
     var cache = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ChatVox", "speech", "voices");
     var result = VoiceAssetCache.Ensure(source, cache);
     if (result.RefreshedAssets > 0 || result.RemovedObsoleteAssets > 0)
         Diagnostic?.Invoke($"voice cache repaired: {result.RefreshedAssets} refreshed, {result.RemovedObsoleteAssets} obsolete removed");
     KokoroVoiceManager.LoadVoicesFromPath(cache);
 }
 public void Stop(){activePlayback?.TrySetResult();tts?.StopPlayback();} public void Dispose(){Stop();tts?.Dispose();gate.Dispose();}
}
