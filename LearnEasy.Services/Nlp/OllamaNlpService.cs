using System.Net.Http.Json;
using System.Text.Json.Serialization;
using LearnEasy.Core.Abstractions;
using LearnEasy.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LearnEasy.Services.Nlp;

/// <summary>
/// NLP via a local Ollama model (phi4-mini) over its stable REST API
/// (<c>POST /api/generate</c>, non-streaming). Every public method has a
/// deterministic rule-based fallback so a missing/cold model never blocks a
/// child mid-lesson — the LLM only makes the experience warmer, not required.
/// </summary>
public sealed class OllamaNlpService(
    HttpClient http,
    IOptions<OllamaOptions> options,
    ILogger<OllamaNlpService> logger) : INlpService
{
    private readonly OllamaOptions _opt = options.Value;

    private const string SystemPrompt =
        "You are a cheerful, patient spelling teacher for a 6-9 year old child. " +
        "Always reply with ONE short sentence, simple words, warm and encouraging. " +
        "Never reveal the full spelling unless explicitly asked. No emojis.";

    public async Task<string> GenerateHintAsync(Word word, HintLevel level, CancellationToken ct = default)
    {
        // Levels 1-3 are deterministic by design — fast, free, always correct.
        switch (level)
        {
            case HintLevel.LetterCount:
                return $"This word has {word.LetterCount} letters.";
            case HintLevel.FirstLetter:
                return $"It starts with the letter \"{char.ToUpperInvariant(word.Text[0])}\".";
            case HintLevel.Syllables when !string.IsNullOrWhiteSpace(word.Syllables):
                return $"Say it in parts: {word.Syllables!.Replace("-", " - ")}.";
        }

        // Phonetic hint benefits from the model "sounding it out".
        var prompt =
            $"The word is \"{word.Text}\". Without spelling it out letter by letter, " +
            "give a gentle clue that helps a child hear how it sounds.";

        var fallback = string.IsNullOrWhiteSpace(word.Syllables)
            ? $"Sound it out slowly: {string.Join(" ", word.Text.ToCharArray())}."
            : $"Sound out each part: {word.Syllables!.Replace("-", " ... ")}.";

        return await CompleteAsync(prompt, fallback, ct);
    }

    public async Task<string> GenerateEncouragementAsync(
        Word word, bool wasCorrect, int currentStreak, CancellationToken ct = default)
    {
        var prompt = wasCorrect
            ? $"The child just spelled \"{word.Text}\" correctly (streak: {currentStreak}). Cheer them on."
            : $"The child spelled \"{word.Text}\" wrong. Encourage them to try again, stay positive.";

        var fallback = wasCorrect
            ? (currentStreak >= 3 ? $"Amazing! {currentStreak} in a row — you're on fire!" : "Great job, that's correct!")
            : "So close! Take a breath and try once more — you've got this.";

        return await CompleteAsync(prompt, fallback, ct);
    }

    public async Task<string> ExplainWordAsync(Word word, CancellationToken ct = default)
    {
        var prompt = $"In one simple sentence a young child understands, explain what \"{word.Text}\" means.";
        var fallback = word.ExampleSentence is { Length: > 0 } s
            ? $"Here's a sentence: {s}"
            : $"\"{word.Text}\" is a word we use when we talk and write.";
        return await CompleteAsync(prompt, fallback, ct);
    }

    /// <summary>One non-streaming completion; returns <paramref name="fallback"/> on any failure.</summary>
    private async Task<string> CompleteAsync(string userPrompt, string fallback, CancellationToken ct)
    {
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(_opt.TimeoutSeconds));

            var request = new GenerateRequest
            {
                Model = _opt.Model,
                System = SystemPrompt,
                Prompt = userPrompt,
                Stream = false,
                Options = new GenerateOptions { Temperature = _opt.Temperature },
            };

            var resp = await http.PostAsJsonAsync("/api/generate", request, timeout.Token);
            resp.EnsureSuccessStatusCode();

            var body = await resp.Content.ReadFromJsonAsync<GenerateResponse>(timeout.Token);
            var text = body?.Response?.Trim();

            return string.IsNullOrWhiteSpace(text) ? fallback : Sanitize(text);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            logger.LogInformation(ex, "Ollama unavailable; using rule-based fallback.");
            return fallback;
        }
    }

    /// <summary>Keep model output to a single tidy sentence for young readers.</summary>
    private static string Sanitize(string text)
    {
        text = text.Replace("\n", " ").Trim();
        var end = text.IndexOfAny(['.', '!', '?']);
        if (end >= 0 && end < text.Length - 1)
            text = text[..(end + 1)];
        return text.Length > 160 ? text[..160].Trim() + "…" : text;
    }

    // --- Ollama REST DTOs ---------------------------------------------------
    private sealed class GenerateRequest
    {
        [JsonPropertyName("model")] public required string Model { get; init; }
        [JsonPropertyName("system")] public string? System { get; init; }
        [JsonPropertyName("prompt")] public required string Prompt { get; init; }
        [JsonPropertyName("stream")] public bool Stream { get; init; }
        [JsonPropertyName("options")] public GenerateOptions? Options { get; init; }
    }

    private sealed class GenerateOptions
    {
        [JsonPropertyName("temperature")] public float Temperature { get; init; }
    }

    private sealed class GenerateResponse
    {
        [JsonPropertyName("response")] public string? Response { get; init; }
    }
}
