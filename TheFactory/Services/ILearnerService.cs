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
    Task<IReadOnlyCollection<SubjectDto>> GetAllSubjectsForCurrentTenantAsync(CancellationToken cancellationToken = default);
    Task<SubjectDto> CreateSubjectAsync(SubjectUpsertRequestDto request, CancellationToken cancellationToken = default);
    Task<SubjectDto?> UpdateSubjectAsync(int subjectId, SubjectUpsertRequestDto request, CancellationToken cancellationToken = default);
    Task<LearnerDetailDto?> GetLearnerDetailByIdAsync(int learnerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<LearnerAcademicRecordDto>> GetLearnerAcademicRecordsAsync(int learnerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<WorkDto>> GetWorkItemsForCurrentSchoolAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<WorkTypeLookupDto>> GetWorkTypesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<WorkDto>> GetWorkItemsForClassAsync(int classId, CancellationToken cancellationToken = default);
    Task<WorkDetailDto?> GetWorkDetailForClassAsync(int classId, int workId, CancellationToken cancellationToken = default);
    Task<bool> SaveWorkMarksAsync(int classId, int workId, IReadOnlyCollection<SaveLearnerMarkDto> marks, CancellationToken cancellationToken = default);
    Task<WorkDto> CreateWorkItemForClassAsync(int classId, WorkUpsertRequestDto request, CancellationToken cancellationToken = default);
    Task<WorkDto?> UpdateWorkItemForClassAsync(int classId, int workId, WorkUpsertRequestDto request, CancellationToken cancellationToken = default);
    Task<bool> ArchiveWorkItemForClassAsync(int classId, int workId, CancellationToken cancellationToken = default);
    Task<WorkDto> CreateWorkItemAsync(WorkUpsertRequestDto request, CancellationToken cancellationToken = default);
    Task<WorkDto?> UpdateWorkItemAsync(int workId, WorkUpsertRequestDto request, CancellationToken cancellationToken = default);
    Task<bool> ArchiveWorkItemAsync(int workId, CancellationToken cancellationToken = default);
    Task<LearnerDto?> GetLearnerByIdAsync(int learnerId, CancellationToken cancellationToken = default);
    Task<LearnerDto> CreateLearnerAsync(LearnerDto learner, CancellationToken cancellationToken = default);
    Task<LearnerDto?> UpdateLearnerAsync(int learnerId, LearnerDto learner, CancellationToken cancellationToken = default);
    Task<bool> ArchiveLearnerAsync(int learnerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<SubjectDto>> GetSubjectsForCurrentTenantAsync(CancellationToken cancellationToken = default);
    Task<SubjectScoreDto?> UpsertSubjectScoreAsync(int learnerId, SubjectScoreDto subjectScore, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<SubjectScoreDto>> GetSubjectScoresAsync(int learnerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<TermAssessmentReportItemDto>> GetLearnerTermAssessmentMarksAsync(int learnerId, string selectedTerm, int? year = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<LearnerReportPreviewDto>> GetLearnerReportPreviewsAsync(int learnerId, CancellationToken cancellationToken = default);
}