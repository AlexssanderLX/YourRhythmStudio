namespace YourRhythmStudio.Application.Learning;

public static class LearningLevelCalculator
{
    public static readonly IReadOnlyList<LevelDefinition> Levels =
    [
        new(1, "Iniciante", 0, 500),
        new(2, "Aprendiz", 500, 2_500),
        new(3, "Intermediário", 2_500, 7_500),
        new(4, "Avançado", 7_500, 15_000),
        new(5, "Lendário", 15_000, 25_000),
    ];

    /// <summary>
    /// Returns the maximum level suggested by XP only. This is informational:
    /// real level changes still require the promotion skill gate.
    /// </summary>
    public static int CalculateLevel(int xp)
        => Levels.FirstOrDefault(level => xp <= level.MaxXp)?.Level ?? Levels[^1].Level;

    /// <summary>Returns the XP threshold required to be eligible for promotion FROM <paramref name="currentLevel"/>.</summary>
    public static int XpThresholdForNextLevel(int currentLevel) => GetLevel(currentLevel).MaxXp;

    public static string LevelName(int level) => GetLevel(level).Name;

    public static LevelDefinition GetLevel(int level)
    {
        var normalized = Math.Clamp(level, Levels[0].Level, Levels[^1].Level);
        return Levels.First(item => item.Level == normalized);
    }

    public static LevelProgress CalculateProgress(int xp, int currentLevel)
    {
        var level = GetLevel(currentLevel);
        var xpInRange = Math.Clamp(xp - level.MinXp, 0, level.MaxXp - level.MinXp);
        var requiredInRange = level.MaxXp - level.MinXp;
        var percent = requiredInRange > 0
            ? Math.Min(100, xpInRange * 100 / requiredInRange)
            : 100;

        return new LevelProgress(
            level,
            xpInRange,
            requiredInRange,
            percent,
            currentLevel < Levels[^1].Level && xp >= level.MaxXp,
            currentLevel >= Levels[^1].Level ? null : GetLevel(currentLevel + 1));
    }

    /// <summary>True when the student has enough XP to be eligible for promotion to the next level.</summary>
    public static bool IsEligibleForPromotion(int xp, int currentLevel)
        => currentLevel < Levels[^1].Level && xp >= XpThresholdForNextLevel(currentLevel);

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
