namespace YourRhythmStudio.Domain.Learning.Enums;

public enum SkillType
{
    /// <summary>Must be mastered to unlock the next level (teacher grants it).</summary>
    PromotionRequired = 1,

    /// <summary>Granted as a reward for completing a legendary mission.</summary>
    MissionReward = 2,

    /// <summary>A special recognition the teacher grants freely.</summary>
    ProfessorSpecial = 3,
}
