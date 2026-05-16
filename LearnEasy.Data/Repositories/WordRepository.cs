using LearnEasy.Core.Abstractions;
using LearnEasy.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace LearnEasy.Data.Repositories;

public sealed class WordRepository(LearnEasyDbContext db) : IWordRepository
{
    public Task<Word?> GetByIdAsync(int id, CancellationToken ct = default)
        => db.Words.FirstOrDefaultAsync(w => w.Id == id, ct);

    public async Task<Word?> GetRandomAsync(
        DifficultyLevel level, IEnumerable<int> excludeIds, CancellationToken ct = default)
    {
        var exclude = excludeIds.ToHashSet();

        var pool = await db.Words
            .Where(w => w.Difficulty == level && !exclude.Contains(w.Id))
            .Select(w => w.Id)
            .ToListAsync(ct);

        // Fall back to the whole band if every word was recently shown.
        if (pool.Count == 0)
        {
            pool = await db.Words
                .Where(w => w.Difficulty == level)
                .Select(w => w.Id)
                .ToListAsync(ct);
        }

        if (pool.Count == 0)
            return null;

        var pickId = pool[Random.Shared.Next(pool.Count)];
        return await db.Words.FirstOrDefaultAsync(w => w.Id == pickId, ct);
    }

    public async Task<IReadOnlyList<Word>> GetByDifficultyAsync(
        DifficultyLevel level, CancellationToken ct = default)
        => await db.Words.Where(w => w.Difficulty == level).ToListAsync(ct);
}
