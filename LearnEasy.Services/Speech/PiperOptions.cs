namespace LearnEasy.Services.Speech;

/// <summary>
/// Bound from the "Piper" config section.
///
/// Two transports are supported:
///  • <b>HTTP (preferred)</b> — piper1-gpl's <c>piper.http_server</c> running
///    in a container that Aspire orchestrates. Set <see cref="Endpoint"/>
///    (Aspire injects it as the "piper" connection string).
///  • <b>Local CLI</b> — a piper executable on disk, used when no endpoint is
///    configured (standalone dev without Docker).
/// See https://github.com/OHF-Voice/piper1-gpl.
/// </summary>
public sealed class PiperOptions
{
    public const string SectionName = "Piper";

    /// <summary>
    /// Base URL of the piper1-gpl HTTP server (e.g. http://localhost:5000).
    /// When set, the HTTP transport is used and the CLI options are ignored.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Optional voice name sent on each HTTP request. Leave empty to use the
    /// container's baked-in default voice (set via the Dockerfile VOICE arg).
    /// </summary>
    public string? VoiceName { get; set; }

    /// <summary>Full path to piper.exe — local CLI transport only.</summary>
    public string ExecutablePath { get; set; } = "piper";

    /// <summary>Path to the .onnx voice model — local CLI transport only.</summary>
    public string VoiceModelPath { get; set; } = "voices/en_US-amy-medium.onnx";

    /// <summary>Folder for synthesized + cached WAV files.</summary>
    public string CacheDirectory { get; set; } =
        Path.Combine(Path.GetTempPath(), "LearnEasy", "tts-cache");

    /// <summary>
    /// When true and Piper is unavailable, fall back to the OS speech engine
    /// (System.Speech on Windows) so lessons still have audio.
    /// </summary>
    public bool FallbackToSystemSpeech { get; set; } = true;
}
