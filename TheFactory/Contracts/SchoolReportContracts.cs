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

public sealed class CreateClassRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string Grade { get; set; } = string.Empty;
    public int TeacherId { get; set; }
    public IReadOnlyCollection<int> LearnerIds { get; set; } = Array.Empty<int>();
}

public sealed class ClassDto
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Grade { get; set; } = string.Empty;
    public int TeacherId { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public IReadOnlyCollection<int> LearnerIds { get; set; } = Array.Empty<int>();
}

public sealed class TeacherLookupDto
{
    public int TeacherId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public sealed class LearnerLookupDto
{
    public int LearnerId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string Surname { get; set; } = string.Empty;
    public string Grade { get; set; } = string.Empty;
}