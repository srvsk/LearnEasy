using LearnEasy.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace LearnEasy.Data;

/// <summary>
/// EF Core context for the whole app. Entity shape is configured fluently in
/// <see cref="OnModelCreating"/> and the starter content is seeded via
/// <see cref="SeedData"/> so a fresh database is immediately usable.
/// </summary>
public class LearnEasyDbContext(DbContextOptions<LearnEasyDbContext> options) : DbContext(options)
{
    public DbSet<Word> Words => Set<Word>();
    public DbSet<LearnerProfile> Learners => Set<LearnerProfile>();
    public DbSet<SpellingAttempt> Attempts => Set<SpellingAttempt>();
    public DbSet<Badge> Badges => Set<Badge>();
    public DbSet<LearnerBadge> LearnerBadges => Set<LearnerBadge>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Word>(e =>
        {
            e.Property(w => w.Text).HasMaxLength(64).IsRequired();
            e.Property(w => w.Syllables).HasMaxLength(96);
            e.Property(w => w.ExampleSentence).HasMaxLength(256);
            e.Property(w => w.Category).HasMaxLength(48);
            // The adaptive selector filters by band constantly — index it.
            e.HasIndex(w => w.Difficulty);
            e.Ignore(w => w.LetterCount); // computed, not stored
        });

        b.Entity<LearnerProfile>(e =>
        {
            e.Property(p => p.DisplayName).HasMaxLength(48).IsRequired();
            e.Property(p => p.AvatarKey).HasMaxLength(32);
            e.HasIndex(p => p.DisplayName).IsUnique();
        });

        b.Entity<SpellingAttempt>(e =>
        {
            e.Property(a => a.SubmittedText).HasMaxLength(128);
            e.HasOne(a => a.LearnerProfile)
             .WithMany(p => p.Attempts)
             .HasForeignKey(a => a.LearnerProfileId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(a => a.Word)
             .WithMany()
             .HasForeignKey(a => a.WordId)
             .OnDelete(DeleteBehavior.Restrict);
            // Hot path for the adaptive engine: newest attempts per learner.
            e.HasIndex(a => new { a.LearnerProfileId, a.AttemptedUtc });
        });

        b.Entity<Badge>(e =>
        {
            e.Property(x => x.Code).HasMaxLength(32).IsRequired();
            e.Property(x => x.Name).HasMaxLength(64).IsRequired();
            e.Property(x => x.Description).HasMaxLength(160).IsRequired();
            e.HasIndex(x => x.Code).IsUnique();
        });

        b.Entity<LearnerBadge>(e =>
        {
            e.HasOne(x => x.LearnerProfile)
             .WithMany(p => p.Badges)
             .HasForeignKey(x => x.LearnerProfileId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Badge)
             .WithMany()
             .HasForeignKey(x => x.BadgeId)
             .OnDelete(DeleteBehavior.Cascade);
            // A learner can only earn each badge once.
            e.HasIndex(x => new { x.LearnerProfileId, x.BadgeId }).IsUnique();
        });

        SeedData.Apply(b);
    }
}
