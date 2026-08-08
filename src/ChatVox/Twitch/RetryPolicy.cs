namespace ChatVox.Twitch;
public static class RetryPolicy { public static TimeSpan Delay(int attempt)=>TimeSpan.FromSeconds(Math.Min(30,Math.Pow(2,Math.Clamp(attempt,0,5)))); }
