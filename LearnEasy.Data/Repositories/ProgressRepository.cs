using LearnEasy.Core.Abstractions;
using LearnEasy.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace LearnEasy.Data.Repositories;

public sealed class ProgressRepository(LearnEasyDbContext db) : IProgressRepository
{
    public Task<LearnerProfile?> GetProfileAsync(int id, CancellationToken ct = default)
        => db.Learners.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<LearnerProfile>> GetAllProfilesAsync(CancellationToken ct = default)
        => await db.Learners.OrderBy(p => p.DisplayName).ToListAsync(ct);

    public async Task<LearnerProfile> CreateProfileAsync(
        string displayName, string avatarKey, CancellationToken ct = default)
    {
        var profile = new LearnerProfile { DisplayName = displayName.Trim(), AvatarKey = avatarKey };
        db.Learners.Add(profile);
        await db.SaveChangesAsync(ct);
        return profile;
    }

    public async Task SaveProfileAsync(LearnerProfile profile, CancellationToken ct = default)
    {
        // Persist ONLY the profile's scalar progression (points, level, streak,
        // difficulty…). We must not use db.Learners.Update(profile): the lesson
        // service holds one long-lived detached profile whose navigation
        // collections get polluted by EF relationship fixup across scopes, so
        // Update would drag in stale SpellingAttempt instances and collide with
        // freshly-queried ones ("another instance with the same key value is
        // already being tracked"). CurrentValues.SetValues copies mapped
        // scalars only and never touches navigations — the correct pattern for
        // saving a detached entity in a per-operation-scope desktop app.
        var tracked = await db.Learners.FindAsync([profile.Id], ct);
        if (tracked is null)
            db.Learners.Add(profile);
        else
            db.Entry(tracked).CurrentValues.SetValues(profile);

        await db.SaveChangesAsync(ct);
    }

    public async Task AddAttemptAsync(SpellingAttempt attempt, CancellationToken ct = default)
    {
        // Guard: an attempt only ever carries FK scalars across scopes. If a
        // caller also set the Word/LearnerProfile navigation to an entity
        // loaded by another (disposed) context, this fresh context would treat
        // it as new and INSERT it — violating PK_Words / PK_Learners. Detach
        // the navigations so only WordId / LearnerProfileId are used.
        attempt.Word = null;
        attempt.LearnerProfile = null;

        db.Attempts.Add(attempt);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<SpellingAttempt>> GetRecentAttemptsAsync(
        int learnerId, int take, CancellationToken ct = default)
        => await db.Attempts
            .Where(a => a.LearnerProfileId == learnerId)
            .OrderByDescending(a => a.AttemptedUtc)
            .Take(take)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Badge>> GetEarnedBadgesAsync(int learnerId, CancellationToken ct = default)
        => await db.LearnerBadges
            .Where(lb => lb.LearnerProfileId == learnerId)
            .Select(lb => lb.Badge!)
            .ToListAsync(ct);

    public Task<Badge?> GetBadgeByCodeAsync(string code, CancellationToken ct = default)
        => db.Badges.FirstOrDefaultAsync(x => x.Code == code, ct);

    public async Task AwardBadgeAsync(int learnerId, int badgeId, CancellationToken ct = default)
    {
        bool exists = await db.LearnerBadges
            .AnyAsync(lb => lb.LearnerProfileId == learnerId && lb.BadgeId == badgeId, ct);
        if (exists) return;

        db.LearnerBadges.Add(new LearnerBadge { LearnerProfileId = learnerId, BadgeId = badgeId });
        await db.SaveChangesAsync(ct);
    }
}
