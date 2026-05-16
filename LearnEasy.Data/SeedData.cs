using LearnEasy.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace LearnEasy.Data;

/// <summary>
/// Starter content baked into the first migration via <c>HasData</c>. Extend
/// the word list freely — ids are stable and explicit so migrations stay clean.
/// </summary>
internal static class SeedData
{
    public static void Apply(ModelBuilder b)
    {
        b.Entity<Badge>().HasData(
            new Badge { Id = 1, Code = "first-word", Name = "First Word!", Description = "Spelled your very first word correctly.", IconKey = "seedling" },
            new Badge { Id = 2, Code = "streak-5", Name = "On Fire", Description = "Got 5 in a row right.", IconKey = "flame" },
            new Badge { Id = 3, Code = "streak-10", Name = "Spelling Star", Description = "Got 10 in a row right.", IconKey = "star" },
            new Badge { Id = 4, Code = "no-hints", Name = "Brain Power", Description = "Spelled a Hard word with no hints.", IconKey = "brain" },
            new Badge { Id = 5, Code = "level-up", Name = "Level Up", Description = "Reached a brand new level.", IconKey = "trophy" },
            new Badge { Id = 6, Code = "challenge-master", Name = "Champion", Description = "Conquered a Challenge word.", IconKey = "crown" }
        );

        // (id, text, difficulty, syllables, sentence, category)
        var seed = new (int, string, DifficultyLevel, string?, string, string)[]
        {
            (1,  "cat",       DifficultyLevel.Starter,   "cat",        "The cat sat on the mat.",            "animals"),
            (2,  "dog",       DifficultyLevel.Starter,   "dog",        "My dog likes to run.",               "animals"),
            (3,  "sun",       DifficultyLevel.Starter,   "sun",        "The sun is warm and bright.",        "nature"),
            (4,  "hat",       DifficultyLevel.Starter,   "hat",        "She wore a red hat.",                "clothes"),
            (5,  "bus",       DifficultyLevel.Starter,   "bus",        "We ride the bus to school.",         "things"),
            (6,  "frog",      DifficultyLevel.Easy,      "frog",       "The frog jumped into the pond.",     "animals"),
            (7,  "milk",      DifficultyLevel.Easy,      "milk",       "I drink milk every morning.",        "food"),
            (8,  "tree",      DifficultyLevel.Easy,      "tree",       "A bird sat in the tall tree.",       "nature"),
            (9,  "ship",      DifficultyLevel.Easy,      "ship",       "The big ship sailed away.",          "things"),
            (10, "play",      DifficultyLevel.Easy,      "play",       "We play in the park.",               "actions"),
            (11, "rabbit",    DifficultyLevel.Medium,    "rab-bit",    "The rabbit has soft ears.",          "animals"),
            (12, "garden",    DifficultyLevel.Medium,    "gar-den",    "Flowers grow in the garden.",        "nature"),
            (13, "yellow",    DifficultyLevel.Medium,    "yel-low",    "The banana is yellow.",              "colors"),
            (14, "pencil",    DifficultyLevel.Medium,    "pen-cil",    "I write with a pencil.",             "school"),
            (15, "monkey",    DifficultyLevel.Medium,    "mon-key",    "The monkey swings on the vine.",     "animals"),
            (16, "because",   DifficultyLevel.Hard,      "be-cause",   "I smiled because I was happy.",      "tricky"),
            (17, "friend",    DifficultyLevel.Hard,      "friend",     "My best friend is kind.",            "people"),
            (18, "school",    DifficultyLevel.Hard,      "school",     "We learn a lot at school.",          "places"),
            (19, "beautiful", DifficultyLevel.Hard,      "beau-ti-ful","The sunset is beautiful.",           "describing"),
            (20, "elephant",  DifficultyLevel.Hard,      "el-e-phant", "The elephant has a long trunk.",     "animals"),
            (21, "necessary", DifficultyLevel.Challenge, "nec-es-sar-y","Sleep is necessary to feel good.",  "tricky"),
            (22, "rhythm",    DifficultyLevel.Challenge, "rhy-thm",    "Clap to the rhythm of the song.",    "music"),
            (23, "separate",  DifficultyLevel.Challenge, "sep-a-rate", "Please separate the red blocks.",    "tricky"),
            (24, "knowledge", DifficultyLevel.Challenge, "know-ledge", "Reading gives you knowledge.",       "tricky"),
        };

        b.Entity<Word>().HasData(seed.Select(s => new Word
        {
            Id = s.Item1,
            Text = s.Item2,
            Difficulty = s.Item3,
            Syllables = s.Item4,
            ExampleSentence = s.Item5,
            Category = s.Item6,
        }));
    }
}
