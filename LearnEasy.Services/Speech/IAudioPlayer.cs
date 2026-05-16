namespace LearnEasy.Services.Speech;

/// <summary>
/// Plays a WAV file. Abstracted so the WPF layer can supply a non-blocking
/// player (e.g. NAudio) and tests can supply a no-op.
/// </summary>
public interface IAudioPlayer
{
    Task PlayAsync(string wavFilePath, CancellationToken ct = default);
}
