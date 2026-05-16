using LearnEasy.Core.Models;

namespace LearnEasy.Core.Abstractions;

public interface IWordRepository
{
    Task<Word?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// A random word in the given band, excluding ids the learner just saw
    /// (to avoid immediate repeats within a session).
    /// </summary>
    Task<Word?> GetRandomAsync(DifficultyLevel level, IEnumerable<int> excludeIds, CancellationToken ct = default);

    Task<IReadOnlyList<Word>> GetByDifficultyAsync(DifficultyLevel level, CancellationToken ct = default);
}
