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
        var learner = await _learnerService.GetLearnerByIdAsync(learnerId, cancellationToken);
        if (learner is null)
        {
            return null;
        }

        var subjects = await _learnerService.GetSubjectScoresAsync(learnerId, cancellationToken);
        if (subjects.Count == 0)
        {
            subjects =
            [
                new() { Subject = "English", PossibleMark = 100, PupilMark = 82, Grade = "A", TeacherComments = "Good comprehension and writing." },
                new() { Subject = "Ndebele", PossibleMark = 100, PupilMark = 76, Grade = "B", TeacherComments = "Participates well in class." },
                new() { Subject = "Mathematics", PossibleMark = 100, PupilMark = 78, Grade = "B", TeacherComments = "Shows consistent progress." },
                new() { Subject = "Agriculture / Science & Technology", PossibleMark = 100, PupilMark = 80, Grade = "B", TeacherComments = "Practical skills are developing." },
                new() { Subject = "Social Science", PossibleMark = 100, PupilMark = 74, Grade = "C", TeacherComments = "Needs more revision on key topics." },
                new() { Subject = "Physical Education & Arts", PossibleMark = 100, PupilMark = 88, Grade = "A", TeacherComments = "Excellent effort and creativity." }
            ];
        }

        var average = subjects.Count == 0 ? 0 : subjects.Average(s => s.PupilMark);

        return new LearnerReportDto
        {
            SchoolName = "ABC Primary School",
            LearnerId = learner.Id,
            FirstName = learner.FirstName,
            Surname = learner.Surname,
            Grade = learner.Grade,
            Subjects = subjects,
            Average = Convert.ToDecimal(average)
        };
    }
}