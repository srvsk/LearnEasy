using System.Runtime.Versioning;
using System.Speech.Synthesis;
using LearnEasy.Services.Speech;

namespace LearnEasy.Wpf.Infrastructure;

/// <summary>
/// Windows System.Speech fallback used only when Piper is unavailable, so the
/// app still talks to the child during setup / on machines without Piper.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class SystemSpeechFallback : ISpeechFallback
{
    public void SynthesizeToWav(string text, string wavPath)
    {
        using var synth = new SpeechSynthesizer();
        synth.Rate = -1; // a touch slower — easier for young learners
        synth.SetOutputToWaveFile(wavPath);
        synth.Speak(text);
        synth.SetOutputToNull();
    }
}
