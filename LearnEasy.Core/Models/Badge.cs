namespace LearnEasy.Core.Models;

/// <summary>Catalogue of earnable badges. Seeded; rarely changes at runtime.</summary>
public class Badge
{
    public int Id { get; set; }

    /// <summary>Stable machine code used by the gamification rules.</summary>
    public required string Code { get; set; }

    public required string Name { get; set; }

    public required string Description { get; set; }

    /// <summary>Key into the badge icon resources.</summary>
    public string IconKey { get; set; } = "star";
}

/// <summary>Join row: which learner earned which badge and when.</summary>
public class LearnerBadge
{
    public int Id { get; set; }

    public int LearnerProfileId { get; set; }
    public LearnerProfile? LearnerProfile { get; set; }

    public int BadgeId { get; set; }
    public Badge? Badge { get; set; }

    public DateTime AwardedUtc { get; set; } = DateTime.UtcNow;
}
