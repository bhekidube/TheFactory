using TheFactory.Contracts;

namespace TheFactory.Services;

public interface IReportService
{
    Task<LearnerReportDto?> GetLearnerReportAsync(int learnerId, CancellationToken cancellationToken = default);
}