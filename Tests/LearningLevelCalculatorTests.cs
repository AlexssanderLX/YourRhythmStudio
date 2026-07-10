using YourRhythmStudio.Application.Learning;

namespace YourRhythmStudio.Tests;

/// <summary>
/// Validates per-level XP logic in LearningLevelCalculator.
///
/// New model: each level's XP bar runs independently from 0 to MaxXp.
///   Iniciante:     0 → 500 XP
///   Aprendiz:      0 → 2500 XP
///   Intermediário: 0 → 7500 XP
///   Avançado:      0 → 15000 XP
///   Lendário:      0 → 25000 XP
///
/// CurrentLevelXp (not cumulative total) is passed to CalculateProgress / IsEligibleForPromotion.
/// </summary>
public sealed class LearningLevelCalculatorTests
{
    // ── MinXp is 0 for all levels in the new model ────────────────────────

    [Fact]
    public void Levels_AllHaveMinXpZero()
    {
        foreach (var def in LearningLevelCalculator.Levels)
            Assert.Equal(0, def.MinXp);
    }

    [Fact]
    public void Levels_AreExactlyFive()
        => Assert.Equal(5, LearningLevelCalculator.Levels.Count);

    [Fact]
    public void Levels_NamesAreCorrect()
    {
        Assert.Equal("Iniciante",     LearningLevelCalculator.Levels[0].Name);
        Assert.Equal("Aprendiz",      LearningLevelCalculator.Levels[1].Name);
        Assert.Equal("Intermediário", LearningLevelCalculator.Levels[2].Name);
        Assert.Equal("Avançado",      LearningLevelCalculator.Levels[3].Name);
        Assert.Equal("Lendário",      LearningLevelCalculator.Levels[4].Name);
    }

    [Fact]
    public void Levels_MaxXpAreCorrect()
    {
        Assert.Equal(500,    LearningLevelCalculator.Levels[0].MaxXp);
        Assert.Equal(2_500,  LearningLevelCalculator.Levels[1].MaxXp);
        Assert.Equal(7_500,  LearningLevelCalculator.Levels[2].MaxXp);
        Assert.Equal(15_000, LearningLevelCalculator.Levels[3].MaxXp);
        Assert.Equal(25_000, LearningLevelCalculator.Levels[4].MaxXp);
    }

    // ── IsEligibleForPromotion (currentLevelXp, currentLevel) ─────────────

    [Theory]
    [InlineData(0,     1, false)]
    [InlineData(499,   1, false)]
    [InlineData(500,   1, true)]
    [InlineData(600,   1, true)]  // over target — blocked by skill until grant
    [InlineData(0,     2, false)]
    [InlineData(2499,  2, false)]
    [InlineData(2500,  2, true)]
    [InlineData(2501,  2, true)]
    [InlineData(0,     3, false)]
    [InlineData(7499,  3, false)]
    [InlineData(7500,  3, true)]
    [InlineData(0,     4, false)]
    [InlineData(14999, 4, false)]
    [InlineData(15000, 4, true)]
    [InlineData(25000, 5, false)] // max level — no promotion
    public void IsEligibleForPromotion_BoundaryValues(int levelXp, int currentLevel, bool expected)
        => Assert.Equal(expected, LearningLevelCalculator.IsEligibleForPromotion(levelXp, currentLevel));

    // ── CalculateProgress (currentLevelXp, currentLevel) ─────────────────

    [Fact]
    public void CalculateProgress_AtZero_IsZeroPercent()
    {
        var p = LearningLevelCalculator.CalculateProgress(0, 1);
        Assert.Equal(0, p.XpInCurrentRange);
        Assert.Equal(500, p.XpRequiredInCurrentRange);
        Assert.Equal(0, p.Percent);
        Assert.False(p.AwaitingPromotion);
    }

    [Fact]
    public void CalculateProgress_Level1_AtTarget_FullBarAndAwaiting()
    {
        var p = LearningLevelCalculator.CalculateProgress(500, 1);
        Assert.Equal(500, p.XpInCurrentRange);
        Assert.Equal(500, p.XpRequiredInCurrentRange);
        Assert.Equal(100, p.Percent);
        Assert.True(p.AwaitingPromotion);
        Assert.Equal("Aprendiz", p.NextLevel!.Name);
    }

    [Fact]
    public void CalculateProgress_Level2_AtStart_ZeroPercent()
    {
        // Student just promoted to level 2 — their CurrentLevelXp reset to 0.
        var p = LearningLevelCalculator.CalculateProgress(0, 2);
        Assert.Equal(0, p.XpInCurrentRange);
        Assert.Equal(2500, p.XpRequiredInCurrentRange);
        Assert.Equal(0, p.Percent);
    }

    [Fact]
    public void CalculateProgress_Level2_Midpoint()
    {
        var p = LearningLevelCalculator.CalculateProgress(1250, 2);
        Assert.Equal(1250, p.XpInCurrentRange);
        Assert.Equal(2500, p.XpRequiredInCurrentRange);
        Assert.Equal(50, p.Percent);
        Assert.False(p.AwaitingPromotion);
    }

    [Fact]
    public void CalculateProgress_Level2_AtTarget_FullBar()
    {
        var p = LearningLevelCalculator.CalculateProgress(2500, 2);
        Assert.Equal(2500, p.XpInCurrentRange);
        Assert.Equal(100, p.Percent);
        Assert.True(p.AwaitingPromotion);
    }

    [Fact]
    public void CalculateProgress_Level3_ExampleFromSpec()
    {
        // Example: Avançado (level 4) student with 4200 per-level XP → 4200/15000 = 28%
        var p = LearningLevelCalculator.CalculateProgress(4200, 4);
        Assert.Equal(4200, p.XpInCurrentRange);
        Assert.Equal(15000, p.XpRequiredInCurrentRange);
        Assert.Equal(28, p.Percent); // 4200*100/15000 = 28
        Assert.False(p.AwaitingPromotion);
        Assert.Equal("Lendário", p.NextLevel!.Name);
    }

    [Fact]
    public void CalculateProgress_Lendario_MaxLevel_NoNextLevel()
    {
        var p = LearningLevelCalculator.CalculateProgress(25000, 5);
        Assert.False(p.AwaitingPromotion);
        Assert.Null(p.NextLevel);
        Assert.Equal(100, p.Percent);
    }

    [Fact]
    public void CalculateProgress_Lendario_AboveTarget_CappedAt100()
    {
        // Even if CurrentLevelXp somehow exceeds target, percent is capped.
        var p = LearningLevelCalculator.CalculateProgress(99999, 5);
        Assert.Equal(100, p.Percent);
        Assert.False(p.AwaitingPromotion);
    }

    [Fact]
    public void CalculateProgress_WhenBlockedBySkill_EffectiveXpCapsAt100()
    {
        // ProgressService caps CurrentLevelXp to MaxXp when blocked; verify result shows 100%.
        var maxXp = LearningLevelCalculator.GetLevel(2).MaxXp; // 2500
        var p = LearningLevelCalculator.CalculateProgress(maxXp, 2);
        Assert.Equal(100, p.Percent);
        Assert.True(p.AwaitingPromotion);
    }

    // ── XpRequiredInRange sanity ───────────────────────────────────────────

    [Theory]
    [InlineData(1,  500)]
    [InlineData(2, 2500)]
    [InlineData(3, 7500)]
    [InlineData(4, 15000)]
    [InlineData(5, 25000)]
    public void CalculateProgress_RequiredRangeEqualsMaxXp(int level, int expectedRequired)
    {
        var p = LearningLevelCalculator.CalculateProgress(0, level);
        Assert.Equal(expectedRequired, p.XpRequiredInCurrentRange);
    }

    // ── CumulativeXpFloorForLevel (migration helper) ──────────────────────

    [Fact]
    public void CumulativeXpFloor_IsCorrect()
    {
        Assert.Equal(0,      LearningLevelCalculator.CumulativeXpFloorForLevel(1));
        Assert.Equal(500,    LearningLevelCalculator.CumulativeXpFloorForLevel(2));
        Assert.Equal(3_000,  LearningLevelCalculator.CumulativeXpFloorForLevel(3));
        Assert.Equal(10_500, LearningLevelCalculator.CumulativeXpFloorForLevel(4));
        Assert.Equal(25_500, LearningLevelCalculator.CumulativeXpFloorForLevel(5));
    }
}
