namespace TheFactory.Contracts;

public sealed class InitiateAttendanceRequestDto
{
    public DateTime SessionDate { get; set; }
}

public sealed class AttendanceRecordDto
{
    public int RecordID { get; set; }
    public int LearnerID { get; set; }
    public string LearnerName { get; set; } = string.Empty;
    public string Status { get; set; } = "Present";
}

public sealed class AttendanceSessionDto
{
    public int SessionID { get; set; }
    public int ClassID { get; set; }
    public DateTime SessionDate { get; set; }
    public bool IsSubmitted { get; set; }
    public IReadOnlyCollection<AttendanceRecordDto> Records { get; set; } = Array.Empty<AttendanceRecordDto>();
}

public sealed class SaveAttendanceRecordDto
{
    public int LearnerID { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class SaveAttendanceRequestDto
{
    public IReadOnlyCollection<SaveAttendanceRecordDto> Records { get; set; } = Array.Empty<SaveAttendanceRecordDto>();
}
