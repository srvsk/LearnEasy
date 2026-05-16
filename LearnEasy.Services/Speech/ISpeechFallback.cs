namespace LearnEasy.Services.Speech;

/// <summary>
/// Optional last-resort synthesizer used when Piper is missing or fails.
/// Registered by the host layer (the WPF app supplies a Windows
/// System.Speech implementation). If none is registered, Piper failures
/// surface to the caller.
/// </summary>
public interface ISpeechFallback
{
    void SynthesizeToWav(string text, string wavPath);
}
