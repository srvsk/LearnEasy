using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using LearnEasy.Core.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LearnEasy.Services.Speech;

/// <summary>
/// Drives the Piper CLI to synthesize speech. Piper reads text from stdin and
/// writes a WAV file. Results are content-addressed and cached so repeated
/// words (very common in spelling practice) are instant and silent on disk.
/// </summary>
public sealed class PiperTextToSpeechService(
    IOptions<PiperOptions> options,
    IAudioPlayer audioPlayer,
    ILogger<PiperTextToSpeechService> logger,
    ISpeechFallback? speechFallback = null) : ITextToSpeechService
{
    private readonly PiperOptions _opt = options.Value;

    public async Task<string> SynthesizeToFileAsync(string text, CancellationToken ct = default)
    {
        Directory.CreateDirectory(_opt.CacheDirectory);

        // Cache key = hash of (voice model + text) so changing the voice
        // invalidates old files automatically.
        var key = Hash($"{_opt.VoiceModelPath}|{text}");
        var wavPath = Path.Combine(_opt.CacheDirectory, key + ".wav");

        if (File.Exists(wavPath) && new FileInfo(wavPath).Length > 0)
            return wavPath;

        try
        {
            await RunPiperAsync(text, wavPath, ct);
            return wavPath;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex,
                "Piper synthesis failed for {Length}-char text; using fallback.", text.Length);

            if (_opt.FallbackToSystemSpeech && speechFallback is not null)
            {
                speechFallback.SynthesizeToWav(text, wavPath);
                return wavPath;
            }
            throw;
        }
    }

    public async Task SpeakAsync(string text, CancellationToken ct = default)
    {
        var wav = await SynthesizeToFileAsync(text, ct);
        await audioPlayer.PlayAsync(wav, ct);
    }

    private async Task RunPiperAsync(string text, string wavPath, CancellationToken ct)
    {
        // piper --model <voice>.onnx --output_file out.wav   (text via stdin)
        var psi = new ProcessStartInfo
        {
            FileName = _opt.ExecutablePath,
            RedirectStandardInput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        psi.ArgumentList.Add("--model");
        psi.ArgumentList.Add(_opt.VoiceModelPath);
        psi.ArgumentList.Add("--output_file");
        psi.ArgumentList.Add(wavPath);

        using var proc = Process.Start(psi)
            ?? throw new InvalidOperationException($"Could not start Piper at '{_opt.ExecutablePath}'.");

        await proc.StandardInput.WriteLineAsync(text.AsMemory(), ct);
        proc.StandardInput.Close();

        var stderr = await proc.StandardError.ReadToEndAsync(ct);
        await proc.WaitForExitAsync(ct);

        if (proc.ExitCode != 0)
            throw new InvalidOperationException($"Piper exited with code {proc.ExitCode}: {stderr}");
    }

    private static string Hash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes)[..20].ToLowerInvariant();
    }
}
