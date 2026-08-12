namespace TheFactory.Contracts;

public sealed class LearnerDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string Surname { get; set; } = string.Empty;
    public string Grade { get; set; } = string.Empty;
}

public sealed class SubjectScoreDto
{
    public int SubjectId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public int PossibleMark { get; set; } = 100;
    public int PupilMark { get; set; }
    public string Grade { get; set; } = string.Empty;
    public string TeacherComments { get; set; } = string.Empty;

    // Keep backward compatibility for existing consumers that still use Score.
    public int Score
    {
        get => PupilMark;
        set => PupilMark = value;
    }
}

public sealed class SubjectDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class LearnerReportDto
{
    public string SchoolName { get; set; } = string.Empty;
    public int LearnerId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string Surname { get; set; } = string.Empty;
    public string Grade { get; set; } = string.Empty;
    public IReadOnlyCollection<SubjectScoreDto> Subjects { get; set; } = Array.Empty<SubjectScoreDto>();
    public decimal Average { get; set; }
}

public sealed class LearnerReportResponseDto
{
    public string SchoolName { get; set; } = string.Empty;
    public string LearnerName { get; set; } = string.Empty;
    public string Grade { get; set; } = string.Empty;
    public IReadOnlyCollection<SubjectScoreDto> Subjects { get; set; } = Array.Empty<SubjectScoreDto>();
    public decimal Average { get; set; }
}