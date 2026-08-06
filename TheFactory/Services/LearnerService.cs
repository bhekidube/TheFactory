using TheFactory.Contracts;

namespace TheFactory.Services;

public sealed class LearnerService : ILearnerService
{
    private static readonly IReadOnlyCollection<LearnerDto> Learners =
    [
        new LearnerDto { Id = 1, FirstName = "Thabo", Surname = "Dube", Grade = "7B" },
        new LearnerDto { Id = 2, FirstName = "Amahle", Surname = "Khumalo", Grade = "6A" }
    ];

    public Task<IReadOnlyCollection<LearnerDto>> GetLearnersForCurrentSchoolAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(Learners);
}