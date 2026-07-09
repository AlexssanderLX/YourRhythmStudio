namespace YourRhythmStudio.Application.Learning;

public static class LearningLevelCalculator
{
    // XP required to REACH each level (index = level number).
    // index 0 unused; index 1 = 0 XP (starting level); index 5 = 25 000 XP.
    private static readonly int[] Thresholds = { 0, 0, 500, 2_000, 8_000, 25_000 };

    /// <summary>Returns the maximum level reachable purely by XP (ignoring promotion gates).</summary>
    public static int CalculateLevel(int xp) => xp switch
    {
        < 500    => 1,
        < 2_000  => 2,
        < 8_000  => 3,
        < 25_000 => 4,
        _        => 5,
    };

    /// <summary>Returns the XP threshold required to be eligible for promotion FROM <paramref name="currentLevel"/>.</summary>
    public static int XpThresholdForNextLevel(int currentLevel)
    {
        if (currentLevel >= 5) return int.MaxValue;
        return Thresholds[currentLevel + 1];
    }

    /// <summary>True when the student has enough XP to be eligible for promotion to the next level.</summary>
    public static bool IsEligibleForPromotion(int xp, int currentLevel)
        => currentLevel < 5 && xp >= XpThresholdForNextLevel(currentLevel);

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
