namespace YourRhythmStudio.Application.Learning;

public static class LearningLevelCalculator
{
    // Single source of truth for per-level XP targets.
    // Each level's XP bar runs independently from 0 to MaxXp.
    // On promotion the student's CurrentLevelXp resets to 0.
    public static readonly IReadOnlyList<LevelDefinition> Levels =
    [
        new(1, "Iniciante",     0, 500),
        new(2, "Aprendiz",      0, 2_500),
        new(3, "Intermediário", 0, 7_500),
        new(4, "Avançado",      0, 15_000),
        new(5, "Lendário",      0, 25_000),
    ];

    /// <summary>XP needed within a level to become eligible for promotion.</summary>
    public static int XpThresholdForNextLevel(int currentLevel) => GetLevel(currentLevel).MaxXp;

    public static string LevelName(int level) => GetLevel(level).Name;

    public static LevelDefinition GetLevel(int level)
    {
        var normalized = Math.Clamp(level, Levels[0].Level, Levels[^1].Level);
        return Levels.First(item => item.Level == normalized);
    }

    /// <summary>
    /// Calculates progress from <paramref name="currentLevelXp"/> — XP earned within the current
    /// level only (resets to 0 on each promotion). Never pass cumulative/total XP here.
    /// </summary>
    public static LevelProgress CalculateProgress(int currentLevelXp, int currentLevel)
    {
        var level = GetLevel(currentLevel);
        var xpInRange = Math.Clamp(currentLevelXp, 0, level.MaxXp);
        var percent = level.MaxXp > 0
            ? Math.Min(100, xpInRange * 100 / level.MaxXp)
            : 100;

        return new LevelProgress(
            level,
            xpInRange,
            level.MaxXp,
            percent,
            currentLevel < Levels[^1].Level && currentLevelXp >= level.MaxXp,
            currentLevel >= Levels[^1].Level ? null : GetLevel(currentLevel + 1));
    }

    /// <summary>True when the student has enough per-level XP to be eligible for promotion.</summary>
    public static bool IsEligibleForPromotion(int currentLevelXp, int currentLevel)
        => currentLevel < Levels[^1].Level && currentLevelXp >= XpThresholdForNextLevel(currentLevel);

    /// <summary>Cumulative total XP that a student would have accumulated to REACH a given level
    /// under the per-level model (sum of all previous levels' targets). Used only for migration and stats.</summary>
    public static int CumulativeXpFloorForLevel(int level) => level switch
    {
        1 => 0,
        2 => 500,
        3 => 500 + 2_500,
        4 => 500 + 2_500 + 7_500,
        5 => 500 + 2_500 + 7_500 + 15_000,
        _ => 0,
    };

    /// <summary>Default XP reward for a new assignment of the given rarity.</summary>
    public static int DefaultXpForRarity(Domain.Learning.Enums.AssignmentRarity rarity) => rarity switch
    {
        Domain.Learning.Enums.AssignmentRarity.Comum     => 100,
        Domain.Learning.Enums.AssignmentRarity.Rara      => 400,
        Domain.Learning.Enums.AssignmentRarity.MuitoRara => 1_500,
        Domain.Learning.Enums.AssignmentRarity.Epica     => 5_000,
        Domain.Learning.Enums.AssignmentRarity.Lendaria  => 25_000,
        _ => 100,
    };
}

public sealed record LevelDefinition(int Level, string Name, int MinXp, int MaxXp);

public sealed record LevelProgress(
    LevelDefinition CurrentLevel,
    int XpInCurrentRange,
    int XpRequiredInCurrentRange,
    int Percent,
    bool AwaitingPromotion,
    LevelDefinition? NextLevel);
