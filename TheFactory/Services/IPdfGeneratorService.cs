using TheFactory.Contracts;

namespace TheFactory.Services;

public interface IPdfGeneratorService
{
    Task<byte[]> GenerateLearnerReportPdfAsync(LearnerReportDto report, CancellationToken cancellationToken = default);
}