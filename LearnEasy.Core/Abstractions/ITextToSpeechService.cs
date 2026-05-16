namespace LearnEasy.Core.Abstractions;

/// <summary>
/// Speech synthesis. Default implementation drives Piper; swap in Microsoft
/// VibeVoice (or Azure Speech) by registering a different implementation.
/// </summary>
public interface ITextToSpeechService
{
    /// <summary>
    /// Synthesize <paramref name="text"/> to a cached WAV file and return its
    /// path. Repeated calls for the same text should hit the cache.
    /// </summary>
    Task<string> SynthesizeToFileAsync(string text, CancellationToken ct = default);

    /// <summary>Synthesize and play through the default output device.</summary>
    Task SpeakAsync(string text, CancellationToken ct = default);
}
