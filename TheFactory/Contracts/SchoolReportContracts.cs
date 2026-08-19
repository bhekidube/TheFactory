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

public sealed class ClassAssignedSubjectDto
{
    public int SubjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

public sealed class ClassDetailDto
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Grade { get; set; } = string.Empty;
    public int TeacherId { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public IReadOnlyCollection<LearnerLookupDto> Learners { get; set; } = Array.Empty<LearnerLookupDto>();
    public IReadOnlyCollection<ClassAssignedSubjectDto> Subjects { get; set; } = Array.Empty<ClassAssignedSubjectDto>();
}

public sealed class AssignClassSubjectRequestDto
{
    public int SubjectId { get; set; }
}

public sealed class SubjectUpsertRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class LearnerAcademicRecordDto
{
    public string SubjectName { get; set; } = string.Empty;
    public string SubjectCode { get; set; } = string.Empty;
    public string TermOrPeriod { get; set; } = string.Empty;
    public decimal GradeOrMarkPercent { get; set; }
    public string TeacherRemarks { get; set; } = string.Empty;
}

public sealed class LearnerDetailDto
{
    public int LearnerId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string Surname { get; set; } = string.Empty;
    public string Grade { get; set; } = string.Empty;
    public string DateOfBirth { get; set; } = string.Empty;
    public string ParentGuardianContact { get; set; } = string.Empty;
    public IReadOnlyCollection<LearnerAcademicRecordDto> AcademicRecords { get; set; } = Array.Empty<LearnerAcademicRecordDto>();
}

public sealed class WorkDto
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int WorkTypeId { get; set; }
    public string WorkType { get; set; } = string.Empty;
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string DueDate { get; set; } = string.Empty;
    public int TotalMark { get; set; }
    public bool IsArchived { get; set; }
}

public sealed class WorkUpsertRequestDto
{
    public string Title { get; set; } = string.Empty;
    public int SubjectId { get; set; }
    public string Description { get; set; } = string.Empty;
    public int WorkTypeId { get; set; }
    public int ClassId { get; set; }
    public string DueDate { get; set; } = string.Empty;
    public int TotalMark { get; set; }
}

public sealed class WorkTypeLookupDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class WorkLearnerMarkDto
{
    public int LearnerId { get; set; }
    public string LearnerName { get; set; } = string.Empty;
    public string Grade { get; set; } = string.Empty;
    public int TotalMark { get; set; }
    public int? MarkObtained { get; set; }
    public string Comment { get; set; } = string.Empty;
}

public sealed class WorkDetailDto
{
    public int WorkId { get; set; }
    public int ClassId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public string WorkType { get; set; } = string.Empty;
    public string DueDate { get; set; } = string.Empty;
    public int TotalMark { get; set; }
    public IReadOnlyCollection<WorkLearnerMarkDto> Learners { get; set; } = Array.Empty<WorkLearnerMarkDto>();
}

public sealed class WorkLearnerMarkUpsertDto
{
    public int LearnerId { get; set; }
    public int? MarkObtained { get; set; }
    public string Comment { get; set; } = string.Empty;
}

public sealed class SaveWorkMarksRequestDto
{
    public IReadOnlyCollection<WorkLearnerMarkUpsertDto> Marks { get; set; } = Array.Empty<WorkLearnerMarkUpsertDto>();
}