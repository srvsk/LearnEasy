using LearnEasy.Core.Abstractions;
using LearnEasy.Core.Dtos;
using LearnEasy.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LearnEasy.Services.Learning;

/// <summary>
/// The "teacher". Holds in-memory session state for one active learner and
/// coordinates the repositories, speech, NLP, scoring and adaptive engine.
///
/// Registered as a singleton (the desktop app has one learner at a time) and
/// opens a short-lived DI scope per data operation so the EF Core DbContext is
/// never long-lived — the correct pattern for a long-running desktop process.
/// </summary>
public sealed class SpellingLessonService(
    IServiceScopeFactory scopeFactory,
    ITextToSpeechService tts,
    INlpService nlp,
    IAdaptiveDifficultySelector adaptive,
    IGamificationEngine gamification,
    ILogger<SpellingLessonService> logger) : ISpellingLessonService
{
    private const int MaxHintLevel = (int)HintLevel.Phonetic;
    private const int AdaptiveWindow = 8;

    // --- session state ------------------------------------------------------
    private LearnerProfile? _profile;
    private Word? _currentWord;
    private HintLevel _hintLevel = HintLevel.None;
    private int _hintsUsedThisWord;
    private readonly Queue<int> _recentWordIds = new();

    public async Task StartSessionAsync(int learnerId, CancellationToken ct = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var progress = scope.ServiceProvider.GetRequiredService<IProgressRepository>();

        _profile = await progress.GetProfileAsync(learnerId, ct)
                   ?? throw new InvalidOperationException($"Learner {learnerId} not found.");
        _currentWord = null;
        _hintLevel = HintLevel.None;
        _hintsUsedThisWord = 0;
        _recentWordIds.Clear();
        logger.LogInformation("Session started for learner {Id} at {Band}.",
            learnerId, _profile.CurrentDifficulty);
    }

    public async Task<LessonStep> NextWordAsync(CancellationToken ct = default)
    {
        var profile = RequireProfile();

        await using var scope = scopeFactory.CreateAsyncScope();
        var words = scope.ServiceProvider.GetRequiredService<IWordRepository>();

        var word = await words.GetRandomAsync(profile.CurrentDifficulty, _recentWordIds, ct)
                   ?? throw new InvalidOperationException(
                       $"No words seeded for band {profile.CurrentDifficulty}.");

        _currentWord = word;
        _hintLevel = HintLevel.None;
        _hintsUsedThisWord = 0;

        RememberWord(word.Id);

        var prompt = $"Can you spell… {word.Text}?";
        await SpeakSafelyAsync(prompt, ct);

        return new LessonStep { Word = word, SpokenPrompt = prompt };
    }

    public async Task<LessonStep> RequestHintAsync(CancellationToken ct = default)
    {
        var word = RequireWord();

        if ((int)_hintLevel < MaxHintLevel)
        {
            _hintLevel = (HintLevel)((int)_hintLevel + 1);
            _hintsUsedThisWord++;
        }

        var hintText = await nlp.GenerateHintAsync(word, _hintLevel, ct);
        await SpeakSafelyAsync(hintText, ct);

        return new LessonStep
        {
            Word = word,
            SpokenPrompt = hintText,
            RevealedHint = _hintLevel,
            HintText = hintText,
        };
    }

    public Task RepeatWordAsync(CancellationToken ct = default)
        => SpeakSafelyAsync($"The word is… {RequireWord().Text}.", ct);

    public async Task<AttemptResult> SubmitAsync(
        string submittedText, int durationMs, CancellationToken ct = default)
    {
        var profile = RequireProfile();
        var word = RequireWord();

        bool correct = string.Equals(
            submittedText.Trim(), word.Text, StringComparison.OrdinalIgnoreCase);

        // FK scalars only — never attach the cross-scope `word`/`profile`
        // entities via navigation properties. They were loaded by a different
        // (now-disposed) DbContext scope; a fresh context would see them as
        // new and try to INSERT, violating PK_Words.
        var attempt = new SpellingAttempt
        {
            LearnerProfileId = profile.Id,
            WordId = word.Id,
            SubmittedText = submittedText.Trim(),
            IsCorrect = correct,
            HintsUsed = _hintsUsedThisWord,
            DurationMs = durationMs,
        };

        // --- streak + score -------------------------------------------------
        profile.CurrentStreak = correct ? profile.CurrentStreak + 1 : 0;
        profile.BestStreak = Math.Max(profile.BestStreak, profile.CurrentStreak);

        int points = 0;
        if (correct)
        {
            points = gamification.ScoreAttempt(word, _hintsUsedThisWord, profile.CurrentStreak);
            profile.TotalPoints += points;
        }

        int previousLevel = profile.Level;
        profile.Level = gamification.ComputeLevel(profile.TotalPoints);
        bool leveledUp = profile.Level > previousLevel;

        // --- persist attempt, then adapt difficulty from history ------------
        var newBadges = new List<Badge>();
        bool difficultyChanged = false;

        await using (var scope = scopeFactory.CreateAsyncScope())
        {
            var progress = scope.ServiceProvider.GetRequiredService<IProgressRepository>();
            var sp = scope.ServiceProvider;

            await progress.AddAttemptAsync(attempt, ct);

            var recent = await progress.GetRecentAttemptsAsync(profile.Id, AdaptiveWindow, ct);
            var newBand = adaptive.Evaluate(profile.CurrentDifficulty, recent);
            if (newBand != profile.CurrentDifficulty)
            {
                profile.CurrentDifficulty = newBand;
                difficultyChanged = true;
            }

            // Badge evaluation (level-up handled here too).
            var earnedCodes = (await progress.GetEarnedBadgesAsync(profile.Id, ct))
                .Select(x => x.Code).ToHashSet();

            var toAward = gamification
                .EvaluateBadges(profile, attempt, word, earnedCodes)
                .ToList();
            if (leveledUp && !earnedCodes.Contains("level-up"))
                toAward.Add("level-up");

            foreach (var code in toAward.Distinct())
            {
                var badge = await progress.GetBadgeByCodeAsync(code, ct);
                if (badge is null) continue;
                await progress.AwardBadgeAsync(profile.Id, badge.Id, ct);
                newBadges.Add(badge);
            }

            await progress.SaveProfileAsync(profile, ct);
        }

        var feedback = await nlp.GenerateEncouragementAsync(
            word, correct, profile.CurrentStreak, ct);
        await SpeakSafelyAsync(feedback, ct);

        return new AttemptResult
        {
            IsCorrect = correct,
            Feedback = feedback,
            PointsAwarded = points,
            NewBadges = newBadges,
            DifficultyChanged = difficultyChanged,
            CurrentDifficulty = profile.CurrentDifficulty,
            CorrectSpelling = correct ? null : word.Text,
        };
    }

    // --- helpers ------------------------------------------------------------
    private LearnerProfile RequireProfile() =>
        _profile ?? throw new InvalidOperationException("Call StartSessionAsync first.");

    private Word RequireWord() =>
        _currentWord ?? throw new InvalidOperationException("Call NextWordAsync first.");

    private void RememberWord(int id)
    {
        _recentWordIds.Enqueue(id);
        while (_recentWordIds.Count > 5)
            _recentWordIds.Dequeue();
    }

    /// <summary>Speech is a nice-to-have; a TTS failure must not break a lesson.</summary>
    private async Task SpeakSafelyAsync(string text, CancellationToken ct)
    {
        try
        {
            await tts.SpeakAsync(text, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Speech failed; continuing silently.");
        }
    }
}
