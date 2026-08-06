using TheFactory.Contracts;

namespace TheFactory.Services;

public sealed class ReportService : IReportService
{
    private readonly ILearnerService _learnerService;

    public ReportService(ILearnerService learnerService)
    {
        _learnerService = learnerService;
    }

    public async Task<LearnerReportDto?> GetLearnerReportAsync(int learnerId, CancellationToken cancellationToken = default)
    {
        var learners = await _learnerService.GetLearnersForCurrentSchoolAsync(cancellationToken);
        var learner = learners.FirstOrDefault(x => x.Id == learnerId);
        if (learner is null)
        {
            return null;
        }

        var subjects = new List<SubjectScoreDto>
        {
            new() { Subject = "Mathematics", Score = 78 },
            new() { Subject = "English", Score = 82 }
        };

        return new LearnerReportDto
        {
            SchoolName = "ABC Primary School",
            LearnerId = learner.Id,
            FirstName = learner.FirstName,
            Surname = learner.Surname,
            Grade = learner.Grade,
            Subjects = subjects,
            Average = Convert.ToDecimal(subjects.Average(s => s.Score))
        };
    }
}