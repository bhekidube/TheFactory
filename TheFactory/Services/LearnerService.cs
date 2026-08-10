using TheFactory.Contracts;

namespace TheFactory.Services;

public sealed class LearnerService : ILearnerService
{
    private static readonly object SyncRoot = new();
    private static readonly List<LearnerRecord> Learners =
    [
        new LearnerRecord { Id = 1, FirstName = "Thabo", Surname = "Dube", Grade = "7B" },
        new LearnerRecord { Id = 2, FirstName = "Amahle", Surname = "Khumalo", Grade = "6A" }
    ];
    private static readonly Dictionary<int, List<SubjectScoreDto>> LearnerScores = new();

    public Task<IReadOnlyCollection<LearnerDto>> GetLearnersForCurrentSchoolAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (SyncRoot)
        {
            var activeLearners = Learners
                .Where(x => !x.IsArchived)
                .Select(MapLearner)
                .ToArray();

            return Task.FromResult<IReadOnlyCollection<LearnerDto>>(activeLearners);
        }
    }

    public Task<LearnerDto?> GetLearnerByIdAsync(int learnerId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (SyncRoot)
        {
            var learner = Learners.FirstOrDefault(x => x.Id == learnerId && !x.IsArchived);
            return Task.FromResult(learner is null ? null : MapLearner(learner));
        }
    }

    public Task<LearnerDto> CreateLearnerAsync(LearnerDto learner, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (SyncRoot)
        {
            var nextId = Learners.Count == 0 ? 1 : Learners.Max(x => x.Id) + 1;
            var record = new LearnerRecord
            {
                Id = nextId,
                FirstName = learner.FirstName.Trim(),
                Surname = learner.Surname.Trim(),
                Grade = learner.Grade.Trim()
            };

            Learners.Add(record);
            return Task.FromResult(MapLearner(record));
        }
    }

    public Task<LearnerDto?> UpdateLearnerAsync(int learnerId, LearnerDto learner, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (SyncRoot)
        {
            var record = Learners.FirstOrDefault(x => x.Id == learnerId && !x.IsArchived);
            if (record is null)
            {
                return Task.FromResult<LearnerDto?>(null);
            }

            record.FirstName = learner.FirstName.Trim();
            record.Surname = learner.Surname.Trim();
            record.Grade = learner.Grade.Trim();

            return Task.FromResult<LearnerDto?>(MapLearner(record));
        }
    }

    public Task<bool> ArchiveLearnerAsync(int learnerId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (SyncRoot)
        {
            var record = Learners.FirstOrDefault(x => x.Id == learnerId && !x.IsArchived);
            if (record is null)
            {
                return Task.FromResult(false);
            }

            record.IsArchived = true;
            return Task.FromResult(true);
        }
    }

    public Task<SubjectScoreDto?> UpsertSubjectScoreAsync(int learnerId, SubjectScoreDto subjectScore, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (SyncRoot)
        {
            var learnerExists = Learners.Any(x => x.Id == learnerId && !x.IsArchived);
            if (!learnerExists)
            {
                return Task.FromResult<SubjectScoreDto?>(null);
            }

            if (!LearnerScores.TryGetValue(learnerId, out var scores))
            {
                scores = [];
                LearnerScores[learnerId] = scores;
            }

            var existingScore = scores.FirstOrDefault(x =>
                string.Equals(x.Subject, subjectScore.Subject, StringComparison.OrdinalIgnoreCase));

            if (existingScore is null)
            {
                existingScore = CloneScore(subjectScore);
                scores.Add(existingScore);
            }
            else
            {
                existingScore.PossibleMark = subjectScore.PossibleMark;
                existingScore.PupilMark = subjectScore.PupilMark;
                existingScore.Grade = subjectScore.Grade;
                existingScore.TeacherComments = subjectScore.TeacherComments;
            }

            return Task.FromResult<SubjectScoreDto?>(CloneScore(existingScore));
        }
    }

    public Task<IReadOnlyCollection<SubjectScoreDto>> GetSubjectScoresAsync(int learnerId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (SyncRoot)
        {
            if (!LearnerScores.TryGetValue(learnerId, out var scores))
            {
                return Task.FromResult<IReadOnlyCollection<SubjectScoreDto>>(Array.Empty<SubjectScoreDto>());
            }

            var clonedScores = scores.Select(CloneScore).ToArray();
            return Task.FromResult<IReadOnlyCollection<SubjectScoreDto>>(clonedScores);
        }
    }

    private static LearnerDto MapLearner(LearnerRecord learner)
        => new()
        {
            Id = learner.Id,
            FirstName = learner.FirstName,
            Surname = learner.Surname,
            Grade = learner.Grade
        };

    private static SubjectScoreDto CloneScore(SubjectScoreDto score)
        => new()
        {
            Subject = score.Subject,
            PossibleMark = score.PossibleMark,
            PupilMark = score.PupilMark,
            Grade = score.Grade,
            TeacherComments = score.TeacherComments
        };

    private sealed class LearnerRecord
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
        public bool IsArchived { get; set; }
    }
}