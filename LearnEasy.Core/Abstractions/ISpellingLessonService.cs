using LearnEasy.Core.Dtos;

namespace LearnEasy.Core.Abstractions;

/// <summary>
/// The "teacher". Orchestrates word selection, speech, hints, grading,
/// scoring and adaptive progression for one active learner. The WPF layer
/// only ever talks to this interface.
/// </summary>
public interface ISpellingLessonService
{
    /// <summary>Bind the lesson to a learner (loads profile + adaptive state).</summary>
    Task StartSessionAsync(int learnerId, CancellationToken ct = default);

    /// <summary>Pick the next word, speak it, and return the prompt.</summary>
    Task<LessonStep> NextWordAsync(CancellationToken ct = default);

    /// <summary>Reveal the next progressive hint for the current word.</summary>
    Task<LessonStep> RequestHintAsync(CancellationToken ct = default);

    /// <summary>Speak the current word again (child pressed the speaker).</summary>
    Task RepeatWordAsync(CancellationToken ct = default);

    /// <summary>Grade a submission, update progress and return the result.</summary>
    Task<AttemptResult> SubmitAsync(string submittedText, int durationMs, CancellationToken ct = default);
}
