using System.IO;
using System.Media;
using LearnEasy.Services.Speech;

namespace LearnEasy.Wpf.Infrastructure;

/// <summary>
/// Plays Piper's WAV output via the built-in <see cref="SoundPlayer"/> (no
/// extra dependency). For richer playback (MP3, overlap, volume) swap in an
/// NAudio-based implementation — that's why this is behind an interface.
/// </summary>
public sealed class SoundPlayerAudioPlayer : IAudioPlayer
{
    public Task PlayAsync(string wavFilePath, CancellationToken ct = default)
    {
        if (!File.Exists(wavFilePath))
            return Task.CompletedTask;

        // PlaySync blocks the calling thread until audio finishes, so push it
        // off the UI thread; honour cancellation between play requests.
        return Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();
            using var player = new SoundPlayer(wavFilePath);
            player.PlaySync();
        }, ct);
    }
}
