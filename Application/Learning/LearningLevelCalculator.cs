namespace YourRhythmStudio.Application.Learning;

public static class LearningLevelCalculator
{
    public static int CalculateLevel(int xp)
    {
        return xp switch
        {
            < 100 => 1,
            < 250 => 2,
            < 500 => 3,
            < 1000 => 4,
            _ => 5
        };
    }
}

