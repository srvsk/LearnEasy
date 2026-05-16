using LearnEasy.Core.Models;

namespace LearnEasy.Core.Dtos;

/// <summary>
/// What the UI needs to present one turn of the lesson. The actual spelling
/// (<see cref="Word.Text"/>) is intentionally still on the word so the UI can
/// grade locally; never bind it to a visible control.
/// </summary>
public sealed record LessonStep
{
    public required Word Word { get; init; }

    /// <summary>Friendly line the teacher speaks, e.g. "Can you spell… rabbit?"</summary>
    public required string SpokenPrompt { get; init; }

    /// <summary>Highest hint already revealed this turn.</summary>
    public HintLevel RevealedHint { get; init; } = HintLevel.None;

    /// <summary>Text of the most recently revealed hint, if any.</summary>
    public string? HintText { get; init; }
}
