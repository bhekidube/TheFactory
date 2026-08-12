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

    public async Task<IReadOnlyCollection<SubjectDto>> GetSubjectsForCurrentTenantAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await _sqlConnectionService.GetSqlConnectionAsync(cancellationToken);
        var tenantId = await ResolveTenantIdAsync(connection, cancellationToken);
        if (!tenantId.HasValue)
        {
            return Array.Empty<SubjectDto>();
        }

        using var command = new SqlCommand(
            @"SELECT Id, Name, Code, IsActive
              FROM institution.Subject
              WHERE TenantId = @TenantId
                AND IsActive = 1
              ORDER BY Name ASC;",
            connection);
        command.Parameters.AddWithValue("@TenantId", tenantId.Value);

        var subjects = new List<SubjectDto>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            subjects.Add(new SubjectDto
            {
                Id = reader.GetInt32(0),
                Name = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                Code = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                IsActive = !reader.IsDBNull(3) && reader.GetBoolean(3)
            });
        }

        return subjects;
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
        var hasSubjectIdColumn = await HasColumnAsync(connection, "institution", "Mark", "SubjectId", cancellationToken);
        if (!hasSubjectIdColumn)
        {
            throw new InvalidOperationException("institution.Mark must contain SubjectId.");
        }

        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
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

            var subject = await GetSubjectForTenantAsync(connection, transaction, tenantId.Value, subjectScore.SubjectId, cancellationToken);
            if (subject is null || !subject.IsActive)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            using var updateCommand = new SqlCommand(
                BuildMarkUpdateCommandText(supportsTeacherComments),
                connection,
                transaction);
            AddMarkCommandParameters(updateCommand, learnerId, tenantId.Value, subject.Id, subjectScore, supportsTeacherComments);

            var affected = await updateCommand.ExecuteNonQueryAsync(cancellationToken);
            if (affected == 0)
            {
                var nextId = await GetNextIdAsync(connection, transaction, "institution.Mark", cancellationToken);
                using var insertCommand = new SqlCommand(
                    BuildMarkInsertCommandText(supportsTeacherComments),
                    connection,
                    transaction);
                insertCommand.Parameters.AddWithValue("@Id", nextId);
                AddMarkCommandParameters(insertCommand, learnerId, tenantId.Value, subject.Id, subjectScore, supportsTeacherComments);

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
            SubjectId = subjectScore.SubjectId,
            Subject = await GetSubjectDisplayNameAsync(connection, tenantId.Value, subjectScore.SubjectId, cancellationToken),
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
        var hasSubjectIdColumn = await HasColumnAsync(connection, "institution", "Mark", "SubjectId", cancellationToken);
        if (!hasSubjectIdColumn)
        {
            throw new InvalidOperationException("institution.Mark must contain SubjectId.");
        }

        using var command = new SqlCommand(
            BuildMarkSelectCommandText(supportsTeacherComments),
            connection);
        command.Parameters.AddWithValue("@LearnerId", learnerId);
        command.Parameters.AddWithValue("@TenantId", tenantId.Value);

        var results = new List<SubjectScoreDto>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var scoreValue = reader.IsDBNull(2) ? 0m : Convert.ToDecimal(reader.GetValue(2));
            var pupilMark = Convert.ToInt32(Math.Round(scoreValue, MidpointRounding.AwayFromZero));
            var grade = ResolveGrade(pupilMark, DefaultPossibleMark);

            results.Add(new SubjectScoreDto
            {
                SubjectId = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                Subject = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                PossibleMark = DefaultPossibleMark,
                PupilMark = pupilMark,
                Grade = grade,
                TeacherComments = supportsTeacherComments && !reader.IsDBNull(3) ? reader.GetString(3) : string.Empty
            });
        }

        return results;
    }

    private static void AddMarkCommandParameters(
        SqlCommand command,
        int learnerId,
        int tenantId,
        int subjectId,
        SubjectScoreDto subjectScore,
        bool supportsTeacherComments)
    {
        command.Parameters.AddWithValue("@TenantId", tenantId);
        command.Parameters.AddWithValue("@LearnerId", learnerId);
        command.Parameters.AddWithValue("@SubjectId", subjectId);
        command.Parameters.AddWithValue("@Score", Convert.ToDecimal(subjectScore.PupilMark));

        if (supportsTeacherComments)
        {
            command.Parameters.AddWithValue("@TeacherComments", string.IsNullOrWhiteSpace(subjectScore.TeacherComments)
                ? DBNull.Value
                : subjectScore.TeacherComments.Trim());
        }
    }

    private static string BuildMarkUpdateCommandText(bool supportsTeacherComments)
    {
        var setClauses = new List<string> { "Score = @Score" };
        if (supportsTeacherComments)
        {
            setClauses.Add("TeacherComments = @TeacherComments");
        }

        setClauses.Add("SubjectId = @SubjectId");

        return $@"UPDATE institution.Mark
                  SET {string.Join(",\n                      ", setClauses)}
                  WHERE LearnerId = @LearnerId
                    AND TenantId = @TenantId
                    AND SubjectId = @SubjectId;";
    }

    private static string BuildMarkInsertCommandText(bool supportsTeacherComments)
    {
        var columns = new List<string> { "Id", "TenantId", "LearnerId", "SubjectId" };
        var values = new List<string> { "@Id", "@TenantId", "@LearnerId", "@SubjectId" };

        columns.Add("Score");
        values.Add("@Score");

        if (supportsTeacherComments)
        {
            columns.Add("TeacherComments");
            values.Add("@TeacherComments");
        }

        return $@"INSERT INTO institution.Mark ({string.Join(", ", columns)})
                  VALUES ({string.Join(", ", values)});";
    }

    private static string BuildMarkSelectCommandText(bool supportsTeacherComments)
    {
        var teacherCommentsSelect = supportsTeacherComments
            ? "m.TeacherComments"
            : "CAST(NULL AS NVARCHAR(MAX)) AS TeacherComments";

        return $@"SELECT
                    m.SubjectId AS SubjectId,
                    s.Name AS Subject,
                    m.Score,
                    {teacherCommentsSelect}
                FROM institution.Mark AS m
                INNER JOIN institution.Subject AS s
                    ON s.Id = m.SubjectId
                   AND s.TenantId = m.TenantId
                WHERE m.LearnerId = @LearnerId
                  AND m.TenantId = @TenantId
                ORDER BY s.Name ASC;";
    }

    private async Task<SubjectRecord?> GetSubjectForTenantAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int tenantId,
        int subjectId,
        CancellationToken cancellationToken)
    {
        using var command = new SqlCommand(
            @"SELECT Id, Name, Code, IsActive
              FROM institution.Subject
              WHERE Id = @SubjectId
                AND TenantId = @TenantId;",
            connection,
            transaction);
        command.Parameters.AddWithValue("@SubjectId", subjectId);
        command.Parameters.AddWithValue("@TenantId", tenantId);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new SubjectRecord(
            reader.GetInt32(0),
            reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
            reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
            !reader.IsDBNull(3) && reader.GetBoolean(3));
    }

    private async Task<string> GetSubjectDisplayNameAsync(SqlConnection connection, int tenantId, int subjectId, CancellationToken cancellationToken)
    {
        using var command = new SqlCommand(
            @"SELECT TOP (1) Name
              FROM institution.Subject
              WHERE Id = @SubjectId
                AND TenantId = @TenantId;",
            connection);
        command.Parameters.AddWithValue("@SubjectId", subjectId);
        command.Parameters.AddWithValue("@TenantId", tenantId);

        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null || value == DBNull.Value ? string.Empty : Convert.ToString(value) ?? string.Empty;
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

    private sealed record SubjectRecord(int Id, string Name, string Code, bool IsActive);
}