namespace LearnEasy.Core.Models;

/// <summary>
/// Immutable record of one spelling try. The adaptive engine reads the most
/// recent N of these to decide whether to move the learner up or down.
/// </summary>
public class SpellingAttempt
{
    public long Id { get; set; }

    public int LearnerProfileId { get; set; }
    public LearnerProfile? LearnerProfile { get; set; }

    public int WordId { get; set; }
    public Word? Word { get; set; }

    public required string SubmittedText { get; set; }

    public bool IsCorrect { get; set; }

    /// <summary>How many progressive hints were revealed before submitting (0-4).</summary>
    public int HintsUsed { get; set; }

    /// <summary>Wall-clock time from prompt to submit; feeds pacing analytics.</summary>
    public int DurationMs { get; set; }

    public DateTime AttemptedUtc { get; set; } = DateTime.UtcNow;
}
