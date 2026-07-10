using YourRhythmStudio.Application.Learning;

namespace YourRhythmStudio.Tests;

/// <summary>
/// Validates XP boundary logic in LearningLevelCalculator.
/// Boundaries: Iniciante 0–500 | Aprendiz 501–2500 | Intermediário 2501–7500 | Avançado 7501–15000 | Lendário 15001–25000
/// </summary>
public sealed class LearningLevelCalculatorTests
{
    // ── CalculateLevel ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0,     1)]
    [InlineData(250,   1)]
    [InlineData(500,   1)]
    [InlineData(501,   2)]
    [InlineData(2500,  2)]
    [InlineData(2501,  3)]
    [InlineData(7500,  3)]
    [InlineData(7501,  4)]
    [InlineData(15000, 4)]
    [InlineData(15001, 5)]
    [InlineData(25000, 5)]
    [InlineData(99999, 5)]
    public void CalculateLevel_ReturnsCorrectLevel(int xp, int expected)
        => Assert.Equal(expected, LearningLevelCalculator.CalculateLevel(xp));

    // ── IsEligibleForPromotion ─────────────────────────────────────────────────

    [Theory]
    [InlineData(499,   1, false)]
    [InlineData(500,   1, true)]
    [InlineData(501,   1, true)]
    [InlineData(2499,  2, false)]
    [InlineData(2500,  2, true)]
    [InlineData(2501,  2, true)]
    [InlineData(7499,  3, false)]
    [InlineData(7500,  3, true)]
    [InlineData(7501,  3, true)]
    [InlineData(14999, 4, false)]
    [InlineData(15000, 4, true)]
    [InlineData(15001, 4, true)]
    [InlineData(25000, 5, false)] // max level — no promotion available
    public void IsEligibleForPromotion_BoundaryValues(int xp, int currentLevel, bool expected)
        => Assert.Equal(expected, LearningLevelCalculator.IsEligibleForPromotion(xp, currentLevel));

    // ── CalculateProgress — XP within range ───────────────────────────────────

    [Fact]
    public void CalculateProgress_Iniciante_AtStart_ZeroPercent()
    {
        var p = LearningLevelCalculator.CalculateProgress(0, 1);
        Assert.Equal(0, p.XpInCurrentRange);
        Assert.Equal(500, p.XpRequiredInCurrentRange);
        Assert.Equal(0, p.Percent);
        Assert.False(p.AwaitingPromotion);
    }

    [Fact]
    public void CalculateProgress_Iniciante_AtCap_FullBar_AwaitingPromotion()
    {
        var p = LearningLevelCalculator.CalculateProgress(500, 1);
        Assert.Equal(500, p.XpInCurrentRange);
        Assert.Equal(100, p.Percent);
        Assert.True(p.AwaitingPromotion);
        Assert.Equal("Aprendiz", p.NextLevel!.Name);
    }

    [Fact]
    public void CalculateProgress_Aprendiz_AtMinXp_ZeroPercent()
    {
        // Student just promoted to level 2 with 500 XP — progress within level 2 = 0
        var p = LearningLevelCalculator.CalculateProgress(500, 2);
        Assert.Equal(0, p.XpInCurrentRange);
        Assert.Equal(0, p.Percent);
    }

    [Fact]
    public void CalculateProgress_Aprendiz_AtFirstXpInRange()
    {
        var p = LearningLevelCalculator.CalculateProgress(501, 2);
        Assert.Equal(0, p.XpInCurrentRange); // 501 - 501 = 0
        Assert.Equal(1999, p.XpRequiredInCurrentRange); // 2500 - 501
    }

    [Fact]
    public void CalculateProgress_Avancado_DisplaysCorrectRange()
    {
        // Spec example: Avançado 8010 XP
        var p = LearningLevelCalculator.CalculateProgress(8010, 4);
        Assert.Equal(509, p.XpInCurrentRange);          // 8010 - 7501
        Assert.Equal(7499, p.XpRequiredInCurrentRange); // 15000 - 7501
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
    public void CalculateProgress_Lendario_AboveMaxXp_CappedAt100()
    {
        var p = LearningLevelCalculator.CalculateProgress(99999, 5);
        Assert.Equal(100, p.Percent);
        Assert.False(p.AwaitingPromotion);
    }

    // ── CalculateProgress — XP frozen at cap when blocked by skill ────────────

    [Fact]
    public void CalculateProgress_WhenXpExceedsCap_AndBlockedBySkill_ShowsFullBarOnly()
    {
        // When blocked, ProgressService clamps XP to MaxXp before calling CalculateProgress.
        // Verify that clamped value (MaxXp) shows 100% and AwaitingPromotion = true.
        var maxXp = LearningLevelCalculator.GetLevel(2).MaxXp; // 2500
        var p = LearningLevelCalculator.CalculateProgress(maxXp, 2);
        Assert.Equal(100, p.Percent);
        Assert.True(p.AwaitingPromotion);
    }

    [Fact]
    public void CalculateProgress_XpBeyondCap_NotBlockedBySkill_ShouldHaveBeenPromoted()
    {
        // When NOT blocked, raw XP > cap means promotion already happened (or should have).
        // CalculateProgress clamps xpInRange to MaxXp - MinXp, so still shows 100%.
        var p = LearningLevelCalculator.CalculateProgress(3000, 2);
        Assert.Equal(100, p.Percent);
        Assert.True(p.AwaitingPromotion);
    }

    // ── Level definitions integrity ────────────────────────────────────────────

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
    public void Levels_MinXpAreCorrect()
    {
        Assert.Equal(0,      LearningLevelCalculator.Levels[0].MinXp);
        Assert.Equal(501,    LearningLevelCalculator.Levels[1].MinXp);
        Assert.Equal(2_501,  LearningLevelCalculator.Levels[2].MinXp);
        Assert.Equal(7_501,  LearningLevelCalculator.Levels[3].MinXp);
        Assert.Equal(15_001, LearningLevelCalculator.Levels[4].MinXp);
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

    // ── XpRequiredInRange sanity ───────────────────────────────────────────────

    [Theory]
    [InlineData(1,  500)]
    [InlineData(2, 1999)]
    [InlineData(3, 4999)]
    [InlineData(4, 7499)]
    [InlineData(5, 9999)]
    public void CalculateProgress_RequiredRanges_AreCorrect(int level, int expectedRequired)
    {
        var xp = LearningLevelCalculator.GetLevel(level).MinXp;
        var p  = LearningLevelCalculator.CalculateProgress(xp, level);
        Assert.Equal(expectedRequired, p.XpRequiredInCurrentRange);
    }
}
