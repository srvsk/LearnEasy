using LearnEasy.Core.Models;

namespace LearnEasy.Core.Dtos;

/// <summary>Outcome of grading one submission, ready for the UI to celebrate.</summary>
public sealed record AttemptResult
{
    public required bool IsCorrect { get; init; }

    /// <summary>Warm, encouraging line the teacher speaks back to the child.</summary>
    public required string Feedback { get; init; }

    public int PointsAwarded { get; init; }

    /// <summary>Badges newly unlocked by this attempt (may be empty).</summary>
    public IReadOnlyList<Badge> NewBadges { get; init; } = [];

    /// <summary>True when the adaptive band changed because of this attempt.</summary>
    public bool DifficultyChanged { get; init; }

    public DifficultyLevel CurrentDifficulty { get; init; }

    /// <summary>Correct spelling, surfaced only after a wrong final answer.</summary>
    public string? CorrectSpelling { get; init; }
}
