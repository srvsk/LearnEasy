using LearnEasy.Core.Models;

namespace LearnEasy.Core.Abstractions;

public interface IProgressRepository
{
    Task<LearnerProfile?> GetProfileAsync(int id, CancellationToken ct = default);

    Task<IReadOnlyList<LearnerProfile>> GetAllProfilesAsync(CancellationToken ct = default);

    Task<LearnerProfile> CreateProfileAsync(string displayName, string avatarKey, CancellationToken ct = default);

    Task SaveProfileAsync(LearnerProfile profile, CancellationToken ct = default);

    Task AddAttemptAsync(SpellingAttempt attempt, CancellationToken ct = default);

    /// <summary>Most recent attempts (newest first) for the adaptive engine.</summary>
    Task<IReadOnlyList<SpellingAttempt>> GetRecentAttemptsAsync(int learnerId, int take, CancellationToken ct = default);

    Task<IReadOnlyList<Badge>> GetEarnedBadgesAsync(int learnerId, CancellationToken ct = default);

    Task<Badge?> GetBadgeByCodeAsync(string code, CancellationToken ct = default);

    Task AwardBadgeAsync(int learnerId, int badgeId, CancellationToken ct = default);
}
