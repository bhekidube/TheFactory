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
    public string Subject { get; set; } = string.Empty;
    public int Score { get; set; }
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