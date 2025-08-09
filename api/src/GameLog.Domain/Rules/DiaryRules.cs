using GameLog.Domain.Enums;

namespace GameLog.Domain.Rules;

public readonly record struct RuleViolation(string Field, string Message);

/// <summary>Pure validation of diary data; no I/O so it is trivially unit-testable.</summary>
public static class DiaryRules
{
    public const int MaxPlaytimeMinutes = 1_000_000;

    public static IReadOnlyList<RuleViolation> Validate(
        DiaryStatus status,
        DateOnly? startedOn,
        DateOnly? completedOn,
        int playtimeMinutes,
        DateOnly today)
    {
        var errors = new List<RuleViolation>();

        if (playtimeMinutes < 0 || playtimeMinutes > MaxPlaytimeMinutes)
        {
            errors.Add(new("totalPlaytimeMinutes", $"Playtime must be between 0 and {MaxPlaytimeMinutes} minutes."));
        }

        if (completedOn is not null && status != DiaryStatus.Completed)
        {
            errors.Add(new("completedOn", "A completion date is only valid for completed games."));
        }

        if (startedOn is not null && completedOn is not null && completedOn < startedOn)
        {
            errors.Add(new("completedOn", "The completion date cannot be before the start date."));
        }

        if (startedOn > today)
        {
            errors.Add(new("startedOn", "The start date cannot be in the future."));
        }

        if (completedOn > today)
        {
            errors.Add(new("completedOn", "The completion date cannot be in the future."));
        }

        if (status == DiaryStatus.WantToPlay && (startedOn is not null || playtimeMinutes > 0))
        {
            errors.Add(new("status", "A game you want to play cannot have a start date or playtime."));
        }

        return errors;
    }
}
