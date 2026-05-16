namespace LearnEasy.Core.Models;

/// <summary>
/// Progressive scaffolding. Each request to the lesson service bumps this one
/// step so the child always gets a *little* more help, never the answer at once.
/// </summary>
public enum HintLevel
{
    None = 0,
    LetterCount = 1,   // "This word has 5 letters."
    FirstLetter = 2,   // "It starts with the letter R."
    Syllables = 3,     // "Say it in parts: rab - bit."
    Phonetic = 4       // LLM sounds it out: "rrr-a-b-b-i-t"
}
