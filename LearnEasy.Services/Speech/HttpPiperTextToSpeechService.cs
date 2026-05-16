using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using LearnEasy.Core.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LearnEasy.Services.Speech;

/// <summary>
/// Talks to a piper1-gpl <c>piper.http_server</c> (typically an Aspire-managed
/// container). Synthesizes via <c>POST /</c> with a JSON body and gets back a
/// WAV stream. Results are content-addressed and cached on disk so repeated
/// words (very common in spelling practice) never hit the network twice.
///
/// Degrades gracefully: on any HTTP failure it uses the optional
/// <see cref="ISpeechFallback"/> (the WPF app supplies a Windows System.Speech
/// one) so a lesson is never blocked on the TTS container.
/// </summary>
public sealed class HttpPiperTextToSpeechService(
    HttpClient http,
    IOptions<PiperOptions> options,
    IAudioPlayer audioPlayer,
    ILogger<HttpPiperTextToSpeechService> logger,
    ISpeechFallback? speechFallback = null) : ITextToSpeechService
{
    private readonly PiperOptions _opt = options.Value;

    public async Task<string> SynthesizeToFileAsync(string text, CancellationToken ct = default)
    {
        Directory.CreateDirectory(_opt.CacheDirectory);

        // Cache key covers endpoint + voice so switching either invalidates.
        var key = Hash($"{_opt.Endpoint}|{_opt.VoiceName}|{text}");
        var wavPath = Path.Combine(_opt.CacheDirectory, key + ".wav");

        if (File.Exists(wavPath) && new FileInfo(wavPath).Length > 0)
            return wavPath;

        try
        {
            var payload = new SynthesizeRequest
            {
                Text = text,
                Voice = string.IsNullOrWhiteSpace(_opt.VoiceName) ? null : _opt.VoiceName,
            };

            using var resp = await http.PostAsJsonAsync("/", payload, ct);
            resp.EnsureSuccessStatusCode();

            // Stream straight to a temp file, then atomically move into place so
            // a cancelled/failed write never leaves a half WAV in the cache.
            var tmp = wavPath + ".tmp";
            await using (var fs = File.Create(tmp))
            {
                await resp.Content.CopyToAsync(fs, ct);
            }
            File.Move(tmp, wavPath, overwrite: true);
            return wavPath;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex,
                "Piper HTTP synthesis failed for {Length}-char text; using fallback.", text.Length);

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

    private static string Hash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes)[..20].ToLowerInvariant();
    }

    /// <summary>piper.http_server request body (only text + optional voice used).</summary>
    private sealed class SynthesizeRequest
    {
        [JsonPropertyName("text")] public required string Text { get; init; }
        [JsonPropertyName("voice")] public string? Voice { get; init; }
    }
}
