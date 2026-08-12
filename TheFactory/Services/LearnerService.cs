using Microsoft.Data.SqlClient;
using System.Data;
using Microsoft.AspNetCore.Http;
using TheFactory.Contracts;

namespace TheFactory.Services;

public sealed class LearnerService : ILearnerService
{
    private const int DefaultPossibleMark = 100;
    private readonly SqlConnectionService _sqlConnectionService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LearnerService(SqlConnectionService sqlConnectionService, IHttpContextAccessor httpContextAccessor)
    {
        _sqlConnectionService = sqlConnectionService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IReadOnlyCollection<LearnerDto>> GetLearnersForCurrentSchoolAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await _sqlConnectionService.GetSqlConnectionAsync(cancellationToken);
        var tenantId = await ResolveTenantIdAsync(connection, cancellationToken);
        if (!tenantId.HasValue)
        {
            return Array.Empty<LearnerDto>();
        }

        using var command = new SqlCommand(
            @"SELECT Id, FirstName, Surname, Grade
              FROM institution.Learner
              WHERE TenantId = @TenantId
                AND IsArchived = 0
              ORDER BY Id ASC;",
            connection);
        command.Parameters.AddWithValue("@TenantId", tenantId.Value);

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
                using var connection = await _sqlConnectionService.GetSqlConnectionAsync(cancellationToken);
                var tenantId = await ResolveTenantIdAsync(connection, cancellationToken);
                if (!tenantId.HasValue)
                {
                        return null;
                }

        using var command = new SqlCommand(
            @"SELECT Id, FirstName, Surname, Grade
              FROM institution.Learner
                            WHERE Id = @LearnerId
                                AND TenantId = @TenantId
                                AND IsArchived = 0;",
            connection);
        command.Parameters.AddWithValue("@LearnerId", learnerId);
                command.Parameters.AddWithValue("@TenantId", tenantId.Value);

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
        using var connection = await _sqlConnectionService.GetSqlConnectionAsync(cancellationToken);
        var tenantId = await ResolveTenantIdAsync(connection, cancellationToken);
        if (!tenantId.HasValue)
        {
            throw new InvalidOperationException("No tenant found in institution.Tenant.");
        }

        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            var nextId = await GetNextIdAsync(connection, transaction, "institution.Learner", cancellationToken);
            using var command = new SqlCommand(
                @"INSERT INTO institution.Learner (Id, TenantId, FirstName, Surname, Grade, IsArchived)
                  VALUES (@Id, @TenantId, @FirstName, @Surname, @Grade, 0);",
                connection,
                transaction);

            command.Parameters.AddWithValue("@Id", nextId);
            command.Parameters.AddWithValue("@TenantId", tenantId.Value);
            command.Parameters.AddWithValue("@FirstName", learner.FirstName.Trim());
            command.Parameters.AddWithValue("@Surname", learner.Surname.Trim());
            command.Parameters.AddWithValue("@Grade", learner.Grade.Trim());

            await command.ExecuteNonQueryAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new LearnerDto
            {
                Id = nextId,
                FirstName = learner.FirstName.Trim(),
                Surname = learner.Surname.Trim(),
                Grade = learner.Grade.Trim()
            };
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<LearnerDto?> UpdateLearnerAsync(int learnerId, LearnerDto learner, CancellationToken cancellationToken = default)
    {
        using var connection = await _sqlConnectionService.GetSqlConnectionAsync(cancellationToken);
        var tenantId = await ResolveTenantIdAsync(connection, cancellationToken);
        if (!tenantId.HasValue)
        {
            return null;
        }

        using var command = new SqlCommand(
            @"UPDATE institution.Learner
              SET FirstName = @FirstName,
                  Surname = @Surname,
                  Grade = @Grade
              WHERE Id = @LearnerId
                AND TenantId = @TenantId
                AND IsArchived = 0;",
            connection);

        command.Parameters.AddWithValue("@LearnerId", learnerId);
        command.Parameters.AddWithValue("@TenantId", tenantId.Value);
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
                using var connection = await _sqlConnectionService.GetSqlConnectionAsync(cancellationToken);
                var tenantId = await ResolveTenantIdAsync(connection, cancellationToken);
                if (!tenantId.HasValue)
                {
                        return false;
                }

        using var command = new SqlCommand(
            @"UPDATE institution.Learner
              SET IsArchived = 1
                            WHERE Id = @LearnerId
                                AND TenantId = @TenantId
                                AND IsArchived = 0;",
            connection);
        command.Parameters.AddWithValue("@LearnerId", learnerId);
                command.Parameters.AddWithValue("@TenantId", tenantId.Value);

        var affected = await command.ExecuteNonQueryAsync(cancellationToken);
        return affected > 0;
    }

    public async Task<SubjectScoreDto?> UpsertSubjectScoreAsync(int learnerId, SubjectScoreDto subjectScore, CancellationToken cancellationToken = default)
    {
        using var connection = await _sqlConnectionService.GetSqlConnectionAsync(cancellationToken);
        var tenantId = await ResolveTenantIdAsync(connection, cancellationToken);
        if (!tenantId.HasValue)
        {
            return null;
        }

        var supportsTeacherComments = await HasColumnAsync(connection, "institution", "Mark", "TeacherComments", cancellationToken);

        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var normalizedSubject = subjectScore.Subject.Trim();
        try
        {
            using var learnerCheckCommand = new SqlCommand(
                @"SELECT COUNT(1)
                  FROM institution.Learner
                  WHERE Id = @LearnerId
                    AND TenantId = @TenantId
                    AND IsArchived = 0;",
                connection,
                transaction);
            learnerCheckCommand.Parameters.AddWithValue("@LearnerId", learnerId);
            learnerCheckCommand.Parameters.AddWithValue("@TenantId", tenantId.Value);

            var learnerExists = Convert.ToInt32(await learnerCheckCommand.ExecuteScalarAsync(cancellationToken)) > 0;
            if (!learnerExists)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            using var updateCommand = new SqlCommand(
                supportsTeacherComments
                    ? @"UPDATE institution.Mark
                          SET Score = @Score,
                              TeacherComments = @TeacherComments
                          WHERE LearnerId = @LearnerId
                            AND TenantId = @TenantId
                            AND Subject = @Subject;"
                    : @"UPDATE institution.Mark
                          SET Score = @Score
                          WHERE LearnerId = @LearnerId
                            AND TenantId = @TenantId
                            AND Subject = @Subject;",
                connection,
                transaction);
            updateCommand.Parameters.AddWithValue("@Score", Convert.ToDecimal(subjectScore.PupilMark));
            if (supportsTeacherComments)
            {
                updateCommand.Parameters.AddWithValue("@TeacherComments", string.IsNullOrWhiteSpace(subjectScore.TeacherComments)
                    ? DBNull.Value
                    : subjectScore.TeacherComments.Trim());
            }
            updateCommand.Parameters.AddWithValue("@LearnerId", learnerId);
            updateCommand.Parameters.AddWithValue("@TenantId", tenantId.Value);
            updateCommand.Parameters.AddWithValue("@Subject", normalizedSubject);

            var affected = await updateCommand.ExecuteNonQueryAsync(cancellationToken);
            if (affected == 0)
            {
                var nextId = await GetNextIdAsync(connection, transaction, "institution.Mark", cancellationToken);
                using var insertCommand = new SqlCommand(
                    supportsTeacherComments
                        ? @"INSERT INTO institution.Mark (Id, TenantId, LearnerId, Subject, Score, TeacherComments)
                            VALUES (@Id, @TenantId, @LearnerId, @Subject, @Score, @TeacherComments);"
                        : @"INSERT INTO institution.Mark (Id, TenantId, LearnerId, Subject, Score)
                            VALUES (@Id, @TenantId, @LearnerId, @Subject, @Score);",
                    connection,
                    transaction);
                insertCommand.Parameters.AddWithValue("@Id", nextId);
                insertCommand.Parameters.AddWithValue("@TenantId", tenantId.Value);
                insertCommand.Parameters.AddWithValue("@LearnerId", learnerId);
                insertCommand.Parameters.AddWithValue("@Subject", normalizedSubject);
                insertCommand.Parameters.AddWithValue("@Score", Convert.ToDecimal(subjectScore.PupilMark));
                if (supportsTeacherComments)
                {
                    insertCommand.Parameters.AddWithValue("@TeacherComments", string.IsNullOrWhiteSpace(subjectScore.TeacherComments)
                        ? DBNull.Value
                        : subjectScore.TeacherComments.Trim());
                }

                await insertCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        var possibleMark = subjectScore.PossibleMark > 0 ? subjectScore.PossibleMark : DefaultPossibleMark;
        return new SubjectScoreDto
        {
            Subject = normalizedSubject,
            PossibleMark = possibleMark,
            PupilMark = subjectScore.PupilMark,
            Grade = ResolveGrade(subjectScore.PupilMark, possibleMark),
            TeacherComments = subjectScore.TeacherComments?.Trim() ?? string.Empty
        };
    }

    public async Task<IReadOnlyCollection<SubjectScoreDto>> GetSubjectScoresAsync(int learnerId, CancellationToken cancellationToken = default)
    {
        using var connection = await _sqlConnectionService.GetSqlConnectionAsync(cancellationToken);
        var tenantId = await ResolveTenantIdAsync(connection, cancellationToken);
        if (!tenantId.HasValue)
        {
            return Array.Empty<SubjectScoreDto>();
        }

        var supportsTeacherComments = await HasColumnAsync(connection, "institution", "Mark", "TeacherComments", cancellationToken);

        using var command = new SqlCommand(
            supportsTeacherComments
                ? @"SELECT Subject, Score, TeacherComments
                    FROM institution.Mark
                    WHERE LearnerId = @LearnerId
                      AND TenantId = @TenantId
                    ORDER BY Subject ASC;"
                : @"SELECT Subject, Score, CAST(NULL AS NVARCHAR(MAX)) AS TeacherComments
                    FROM institution.Mark
                    WHERE LearnerId = @LearnerId
                      AND TenantId = @TenantId
                    ORDER BY Subject ASC;",
            connection);
        command.Parameters.AddWithValue("@LearnerId", learnerId);
        command.Parameters.AddWithValue("@TenantId", tenantId.Value);

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
                TeacherComments = supportsTeacherComments && !reader.IsDBNull(2) ? reader.GetString(2) : string.Empty
            });
        }

        return results;
    }

    private static async Task<bool> HasColumnAsync(SqlConnection connection, string schemaName, string tableName, string columnName, CancellationToken cancellationToken)
    {
        using var command = new SqlCommand(
            @"SELECT CASE WHEN EXISTS (
                    SELECT 1
                    FROM sys.columns c
                    INNER JOIN sys.tables t ON t.object_id = c.object_id
                    INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
                    WHERE s.name = @SchemaName
                      AND t.name = @TableName
                      AND c.name = @ColumnName)
                THEN 1 ELSE 0 END;",
            connection);

        command.Parameters.AddWithValue("@SchemaName", schemaName);
        command.Parameters.AddWithValue("@TableName", tableName);
        command.Parameters.AddWithValue("@ColumnName", columnName);

        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is int intValue ? intValue == 1 : Convert.ToInt32(value) == 1;
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

    private static async Task<int> GetNextIdAsync(SqlConnection connection, SqlTransaction transaction, string tableName, CancellationToken cancellationToken)
    {
        using var command = new SqlCommand($"SELECT ISNULL(MAX(Id), 0) + 1 FROM {tableName} WITH (UPDLOCK, HOLDLOCK);", connection, transaction);
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

    private async Task<int?> ResolveTenantIdAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        var request = _httpContextAccessor.HttpContext?.Request;
        if (request is not null)
        {
            if (request.Headers.TryGetValue("X-Tenant-Id", out var headerValues)
                && int.TryParse(headerValues.FirstOrDefault(), out var headerTenantId))
            {
                return headerTenantId;
            }

            if (request.Query.TryGetValue("tenantId", out var queryValues)
                && int.TryParse(queryValues.FirstOrDefault(), out var queryTenantId))
            {
                return queryTenantId;
            }
        }

        return await GetDefaultTenantIdAsync(connection, cancellationToken);
    }
}