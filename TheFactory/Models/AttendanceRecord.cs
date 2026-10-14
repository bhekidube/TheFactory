namespace TheFactory.Models;

public sealed class AttendanceRecord
{
    public int RecordID { get; set; }
    public int SessionID { get; set; }
    public int LearnerID { get; set; }
    public string Status { get; set; } = "Present";
    public AttendanceSession? Session { get; set; }
}

public sealed class AttendanceLearnerReference
{
    public int Id { get; set; }
}
