using TheFactory.Contracts;

namespace TheFactory.Services;

public interface ILearnerService
{
    Task<IReadOnlyCollection<LearnerDto>> GetLearnersForCurrentSchoolAsync(CancellationToken cancellationToken = default);
}