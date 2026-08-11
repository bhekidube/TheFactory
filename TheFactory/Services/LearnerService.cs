using Microsoft.Data.SqlClient;
using TheFactory.Contracts;

namespace TheFactory.Services;

public sealed class LearnerService : ILearnerService
{
    private const int DefaultPossibleMark = 100;
    private readonly SqlConnectionService _sqlConnectionService;

    public LearnerService(SqlConnectionService sqlConnectionService)
    {
        _sqlConnectionService = sqlConnectionService;
    }

    public async Task<IReadOnlyCollection<LearnerDto>> GetLearnersForCurrentSchoolAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await _sqlConnectionService.GetSqlConnectionAsync();
        using var command = new SqlCommand(
            @"SELECT Id, FirstName, Surname, Grade
              FROM institution.Learner
              WHERE IsArchived = 0
              ORDER BY Id ASC;",
            connection);

        var learners = new List<LearnerDto>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            learners.Add(new LearnerDto
            {
                Id = reader.GetInt32(0),
                FirstName = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                Surname = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                Grade = reader.IsDBNull(3) ? string.Empty : reader.GetString(3)
            });
        }

        return learners;
    }

    public async Task<LearnerDto?> GetLearnerByIdAsync(int learnerId, CancellationToken cancellationToken = default)
    {
        using var connection = await _sqlConnectionService.GetSqlConnectionAsync();
        using var command = new SqlCommand(
            @"SELECT Id, FirstName, Surname, Grade
              FROM institution.Learner
              WHERE Id = @LearnerId AND IsArchived = 0;",
            connection);
        command.Parameters.AddWithValue("@LearnerId", learnerId);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new LearnerDto
        {
            Id = reader.GetInt32(0),
            FirstName = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
            Surname = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
            Grade = reader.IsDBNull(3) ? string.Empty : reader.GetString(3)
        };
    }

    public async Task<LearnerDto> CreateLearnerAsync(LearnerDto learner, CancellationToken cancellationToken = default)
    {
        using var connection = await _sqlConnectionService.GetSqlConnectionAsync();

        var tenantId = await GetDefaultTenantIdAsync(connection, cancellationToken);
        if (!tenantId.HasValue)
        {
            throw new InvalidOperationException("No tenant found in institution.Tenant.");
        }

        var nextId = await GetNextIdAsync(connection, "institution.Learner", cancellationToken);
        using var command = new SqlCommand(
            @"INSERT INTO institution.Learner (Id, TenantId, FirstName, Surname, Grade, IsArchived)
              VALUES (@Id, @TenantId, @FirstName, @Surname, @Grade, 0);",
            connection);

        command.Parameters.AddWithValue("@Id", nextId);
        command.Parameters.AddWithValue("@TenantId", tenantId.Value);
        command.Parameters.AddWithValue("@FirstName", learner.FirstName.Trim());
        command.Parameters.AddWithValue("@Surname", learner.Surname.Trim());
        command.Parameters.AddWithValue("@Grade", learner.Grade.Trim());

        await command.ExecuteNonQueryAsync(cancellationToken);

        return new LearnerDto
        {
            Id = nextId,
            FirstName = learner.FirstName.Trim(),
            Surname = learner.Surname.Trim(),
            Grade = learner.Grade.Trim()
        };
    }

    public async Task<LearnerDto?> UpdateLearnerAsync(int learnerId, LearnerDto learner, CancellationToken cancellationToken = default)
    {
        using var connection = await _sqlConnectionService.GetSqlConnectionAsync();
        using var command = new SqlCommand(
            @"UPDATE institution.Learner
              SET FirstName = @FirstName,
                  Surname = @Surname,
                  Grade = @Grade
              WHERE Id = @LearnerId AND IsArchived = 0;",
            connection);

        command.Parameters.AddWithValue("@LearnerId", learnerId);
        command.Parameters.AddWithValue("@FirstName", learner.FirstName.Trim());
        command.Parameters.AddWithValue("@Surname", learner.Surname.Trim());
        command.Parameters.AddWithValue("@Grade", learner.Grade.Trim());

        var affected = await command.ExecuteNonQueryAsync(cancellationToken);
        if (affected == 0)
        {
            return null;
        }

        return new LearnerDto
        {
            Id = learnerId,
            FirstName = learner.FirstName.Trim(),
            Surname = learner.Surname.Trim(),
            Grade = learner.Grade.Trim()
        };
    }

    public async Task<bool> ArchiveLearnerAsync(int learnerId, CancellationToken cancellationToken = default)
    {
        using var connection = await _sqlConnectionService.GetSqlConnectionAsync();
        using var command = new SqlCommand(
            @"UPDATE institution.Learner
              SET IsArchived = 1
              WHERE Id = @LearnerId AND IsArchived = 0;",
            connection);
        command.Parameters.AddWithValue("@LearnerId", learnerId);

        var affected = await command.ExecuteNonQueryAsync(cancellationToken);
        return affected > 0;
    }

    public async Task<SubjectScoreDto?> UpsertSubjectScoreAsync(int learnerId, SubjectScoreDto subjectScore, CancellationToken cancellationToken = default)
    {
        using var connection = await _sqlConnectionService.GetSqlConnectionAsync();

        var learnerTenantId = await GetLearnerTenantIdAsync(connection, learnerId, cancellationToken);
        if (!learnerTenantId.HasValue)
        {
            return null;
        }

        var normalizedSubject = subjectScore.Subject.Trim();
        using var checkCommand = new SqlCommand(
            @"SELECT Id
              FROM institution.Mark
              WHERE LearnerId = @LearnerId AND Subject = @Subject;",
            connection);
        checkCommand.Parameters.AddWithValue("@LearnerId", learnerId);
        checkCommand.Parameters.AddWithValue("@Subject", normalizedSubject);

        var existingMarkIdObj = await checkCommand.ExecuteScalarAsync(cancellationToken);
        if (existingMarkIdObj is not null && existingMarkIdObj != DBNull.Value)
        {
            using var updateCommand = new SqlCommand(
                @"UPDATE institution.Mark
                  SET Score = @Score,
                      TeacherComments = @TeacherComments,
                      TenantId = @TenantId
                  WHERE Id = @Id;",
                connection);
            updateCommand.Parameters.AddWithValue("@Id", Convert.ToInt32(existingMarkIdObj));
            updateCommand.Parameters.AddWithValue("@Score", Convert.ToDecimal(subjectScore.PupilMark));
            updateCommand.Parameters.AddWithValue("@TeacherComments", string.IsNullOrWhiteSpace(subjectScore.TeacherComments)
                ? DBNull.Value
                : subjectScore.TeacherComments.Trim());
            updateCommand.Parameters.AddWithValue("@TenantId", learnerTenantId.Value);

            await updateCommand.ExecuteNonQueryAsync(cancellationToken);
        }
        else
        {
            var nextId = await GetNextIdAsync(connection, "institution.Mark", cancellationToken);
            using var insertCommand = new SqlCommand(
                @"INSERT INTO institution.Mark (Id, TenantId, LearnerId, Subject, Score, TeacherComments)
                  VALUES (@Id, @TenantId, @LearnerId, @Subject, @Score, @TeacherComments);",
                connection);
            insertCommand.Parameters.AddWithValue("@Id", nextId);
            insertCommand.Parameters.AddWithValue("@TenantId", learnerTenantId.Value);
            insertCommand.Parameters.AddWithValue("@LearnerId", learnerId);
            insertCommand.Parameters.AddWithValue("@Subject", normalizedSubject);
            insertCommand.Parameters.AddWithValue("@Score", Convert.ToDecimal(subjectScore.PupilMark));
            insertCommand.Parameters.AddWithValue("@TeacherComments", string.IsNullOrWhiteSpace(subjectScore.TeacherComments)
                ? DBNull.Value
                : subjectScore.TeacherComments.Trim());

            await insertCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        return new SubjectScoreDto
        {
            Subject = normalizedSubject,
            PossibleMark = subjectScore.PossibleMark,
            PupilMark = subjectScore.PupilMark,
            Grade = subjectScore.Grade,
            TeacherComments = subjectScore.TeacherComments?.Trim() ?? string.Empty
        };
    }

    public async Task<IReadOnlyCollection<SubjectScoreDto>> GetSubjectScoresAsync(int learnerId, CancellationToken cancellationToken = default)
    {
        using var connection = await _sqlConnectionService.GetSqlConnectionAsync();

        var learnerTenantId = await GetLearnerTenantIdAsync(connection, learnerId, cancellationToken);
        if (!learnerTenantId.HasValue)
        {
            return Array.Empty<SubjectScoreDto>();
        }

        using var command = new SqlCommand(
            @"SELECT Subject, Score, TeacherComments
              FROM institution.Mark
              WHERE LearnerId = @LearnerId AND TenantId = @TenantId
              ORDER BY Subject ASC;",
            connection);
        command.Parameters.AddWithValue("@LearnerId", learnerId);
        command.Parameters.AddWithValue("@TenantId", learnerTenantId.Value);

        var results = new List<SubjectScoreDto>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var scoreValue = reader.IsDBNull(1) ? 0m : reader.GetDecimal(1);
            var pupilMark = Convert.ToInt32(Math.Round(scoreValue, MidpointRounding.AwayFromZero));
            var grade = ResolveGrade(pupilMark, DefaultPossibleMark);

            results.Add(new SubjectScoreDto
            {
                Subject = reader.IsDBNull(0) ? string.Empty : reader.GetString(0),
                PossibleMark = DefaultPossibleMark,
                PupilMark = pupilMark,
                Grade = grade,
                TeacherComments = reader.IsDBNull(2) ? string.Empty : reader.GetString(2)
            });
        }

        return results;
    }

    private static string ResolveGrade(int pupilMark, int possibleMark)
    {
        if (possibleMark <= 0)
        {
            return string.Empty;
        }

        var percentage = (decimal)pupilMark / possibleMark * 100m;
        if (percentage >= 80m) return "A";
        if (percentage >= 70m) return "B";
        if (percentage >= 60m) return "C";
        if (percentage >= 50m) return "D";
        return "F";
    }

    private static async Task<int> GetNextIdAsync(SqlConnection connection, string tableName, CancellationToken cancellationToken)
    {
        using var command = new SqlCommand($"SELECT ISNULL(MAX(Id), 0) + 1 FROM {tableName};", connection);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(value);
    }

    private static async Task<int?> GetDefaultTenantIdAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        using var command = new SqlCommand("SELECT TOP (1) Id FROM institution.Tenant ORDER BY Id ASC;", connection);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        if (value is null || value == DBNull.Value)
        {
            return null;
        }

        return Convert.ToInt32(value);
    }

    private static async Task<int?> GetLearnerTenantIdAsync(SqlConnection connection, int learnerId, CancellationToken cancellationToken)
    {
        using var command = new SqlCommand(
            @"SELECT TOP (1) TenantId
              FROM institution.Learner
              WHERE Id = @LearnerId AND IsArchived = 0;",
            connection);
        command.Parameters.AddWithValue("@LearnerId", learnerId);

        var value = await command.ExecuteScalarAsync(cancellationToken);
        if (value is not null && value != DBNull.Value)
        {
            return Convert.ToInt32(value);
        }

        return await GetDefaultTenantIdAsync(connection, cancellationToken);
    }
}