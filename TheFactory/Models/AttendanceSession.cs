namespace TheFactory.Models;

public sealed class AttendanceSession
{
    public int SessionID { get; set; }
    public int ClassID { get; set; }
    public DateTime SessionDate { get; set; }
    public bool IsSubmitted { get; set; }
    public ICollection<AttendanceRecord> Records { get; set; } = new List<AttendanceRecord>();
}

public sealed class AttendanceClassReference
{
    public int Id { get; set; }
}
