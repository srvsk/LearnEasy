namespace LearnEasy.Core.Models;

/// <summary>
/// A single spelling target. Seeded into Postgres and selected by the
/// adaptive engine based on the learner's current band.
/// </summary>
public class Word
{
    public int Id { get; set; }

    /// <summary>The canonical spelling the learner must produce (lower-case).</summary>
    public required string Text { get; set; }

    public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Starter;

    /// <summary>
    /// Hyphen-separated syllables, e.g. "rab-bit". Used to build progressive
    /// hints without calling the LLM. Optional.
    /// </summary>
    public string? Syllables { get; set; }

    /// <summary>A kid-friendly sentence that uses the word in context.</summary>
    public string? ExampleSentence { get; set; }

    /// <summary>Loose grouping for themed lessons: "animals", "food", ...</summary>
    public string? Category { get; set; }

    /// <summary>Number of letters; convenient for the "how many letters?" hint.</summary>
    public int LetterCount => Text.Length;
}
