namespace LearnEasy.Core.Models;

/// <summary>
/// Ordered difficulty bands. The numeric value matters: the adaptive engine
/// moves a learner up/down by comparing these values, so keep them contiguous.
/// </summary>
public enum DifficultyLevel
{
    Starter = 1,   // 2-3 letter sight words: "cat", "sun"
    Easy = 2,      // common CVC / CVCC words: "frog", "milk"
    Medium = 3,    // two syllables: "rabbit", "garden"
    Hard = 4,      // tricky spellings: "because", "friend"
    Challenge = 5  // spelling-bee tier: "necessary", "rhythm"
}
