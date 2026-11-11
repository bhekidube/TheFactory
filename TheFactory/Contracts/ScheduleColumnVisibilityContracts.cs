namespace TheFactory.Contracts;

/// <summary>Shared visibility settings for the timetable columns.</summary>
public sealed record ScheduleColumnVisibilityResponse(
    bool Bus,
    bool Departure,
    bool Price,
    bool Action);

/// <summary>Administrator update for the shared timetable column visibility settings.</summary>
public sealed record UpdateScheduleColumnVisibilityRequest
{
    public required bool Bus { get; init; }
    public required bool Departure { get; init; }
    public required bool Price { get; init; }
    public required bool Action { get; init; }
}
