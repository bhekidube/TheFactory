using TheFactory.Contracts;

namespace TheFactory.Services;

public interface ILearnerService
{
    Task<IReadOnlyCollection<LearnerDto>> GetLearnersForCurrentSchoolAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ClassDto>> GetClassesForCurrentSchoolAsync(CancellationToken cancellationToken = default);
    Task<ClassDetailDto?> GetClassByIdAsync(int classId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<TeacherLookupDto>> SearchTeachersAsync(string query, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<LearnerLookupDto>> SearchLearnersAsync(string query, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ClassAssignedSubjectDto>> SearchSubjectsAsync(string query, CancellationToken cancellationToken = default);
    Task<ClassDto> CreateClassAsync(CreateClassRequestDto request, CancellationToken cancellationToken = default);
    Task<ClassAssignedSubjectDto?> AssignSubjectToClassAsync(int classId, int subjectId, CancellationToken cancellationToken = default);
    Task<LearnerDto?> GetLearnerByIdAsync(int learnerId, CancellationToken cancellationToken = default);
    Task<LearnerDto> CreateLearnerAsync(LearnerDto learner, CancellationToken cancellationToken = default);
    Task<LearnerDto?> UpdateLearnerAsync(int learnerId, LearnerDto learner, CancellationToken cancellationToken = default);
    Task<bool> ArchiveLearnerAsync(int learnerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<SubjectDto>> GetSubjectsForCurrentTenantAsync(CancellationToken cancellationToken = default);
    Task<SubjectScoreDto?> UpsertSubjectScoreAsync(int learnerId, SubjectScoreDto subjectScore, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<SubjectScoreDto>> GetSubjectScoresAsync(int learnerId, CancellationToken cancellationToken = default);
}