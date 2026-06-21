namespace Foundation.Assistant.Models;

public sealed record AssistantBusinessHours(TimeOnly Start, TimeOnly End)
{
    public bool IsOpenAt(TimeOnly time)
    {
        if (Start <= End)
        {
            return time >= Start && time <= End;
        }

        return time >= Start || time <= End;
    }
}
