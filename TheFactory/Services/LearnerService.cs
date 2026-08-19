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

    public async Task<IReadOnlyCollection<ClassDto>> GetClassesForCurrentSchoolAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await _sqlConnectionService.GetSqlConnectionAsync(cancellationToken);
        var tenantId = await ResolveTenantIdAsync(connection, cancellationToken);
        if (!tenantId.HasValue)
        {
            return Array.Empty<ClassDto>();
        }

        var classes = new List<ClassDto>();
        using (var command = new SqlCommand(
            @"SELECT c.Id, c.SchoolId, c.Name, c.Grade, c.TeacherId, u.Name
              FROM institution.Class AS c
              LEFT JOIN [User] AS u ON u.UserId = c.TeacherId
              WHERE c.SchoolId = @SchoolId
              ORDER BY c.Name ASC;",
            connection))
        {
            command.Parameters.AddWithValue("@SchoolId", tenantId.Value);

            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                classes.Add(new ClassDto
                {
                    Id = reader.GetInt32(0),
                    SchoolId = reader.GetInt32(1),
                    Name = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    Grade = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                    TeacherId = reader.GetInt32(4),
                    TeacherName = reader.IsDBNull(5) ? string.Empty : reader.GetString(5)
                });
            }
        }

        foreach (var schoolClass in classes)
        {
            using var learnersCommand = new SqlCommand(
                @"SELECT ce.LearnerId
                  FROM institution.ClassEnrolment AS ce
                  WHERE ce.ClassId = @ClassId
                  ORDER BY ce.LearnerId ASC;",
                connection);
            learnersCommand.Parameters.AddWithValue("@ClassId", schoolClass.Id);

            var learnerIds = new List<int>();
            using var learnersReader = await learnersCommand.ExecuteReaderAsync(cancellationToken);
            while (await learnersReader.ReadAsync(cancellationToken))
            {
                learnerIds.Add(learnersReader.GetInt32(0));
            }

            schoolClass.LearnerIds = learnerIds;
        }

        return classes;
    }

    public async Task<ClassDetailDto?> GetClassByIdAsync(int classId, CancellationToken cancellationToken = default)
    {
        using var connection = await _sqlConnectionService.GetSqlConnectionAsync(cancellationToken);
        var tenantId = await ResolveTenantIdAsync(connection, cancellationToken);
        if (!tenantId.HasValue)
        {
            return null;
        }

        ClassDetailDto? detail;
        using (var classCommand = new SqlCommand(
            @"SELECT c.Id, c.SchoolId, c.Name, c.Grade, c.TeacherId, u.Name
              FROM institution.Class AS c
              LEFT JOIN [User] AS u ON u.UserId = c.TeacherId
              WHERE c.Id = @ClassId
                AND c.SchoolId = @SchoolId;",
            connection))
        {
            classCommand.Parameters.AddWithValue("@ClassId", classId);
            classCommand.Parameters.AddWithValue("@SchoolId", tenantId.Value);

            using var reader = await classCommand.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            detail = new ClassDetailDto
            {
                Id = reader.GetInt32(0),
                SchoolId = reader.GetInt32(1),
                Name = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                Grade = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                TeacherId = reader.GetInt32(4),
                TeacherName = reader.IsDBNull(5) ? string.Empty : reader.GetString(5)
            };
        }

        var learners = new List<LearnerLookupDto>();
        using (var learnersCommand = new SqlCommand(
            @"SELECT l.Id, l.FirstName, l.Surname, l.Grade
              FROM institution.ClassEnrolment AS ce
              INNER JOIN institution.Learner AS l ON l.Id = ce.LearnerId
              WHERE ce.ClassId = @ClassId
                AND l.TenantId = @TenantId
                AND l.IsArchived = 0
              ORDER BY l.FirstName ASC, l.Surname ASC;",
            connection))
        {
            learnersCommand.Parameters.AddWithValue("@ClassId", classId);
            learnersCommand.Parameters.AddWithValue("@TenantId", tenantId.Value);

            using var reader = await learnersCommand.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                learners.Add(new LearnerLookupDto
                {
                    LearnerId = reader.GetInt32(0),
                    FirstName = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                    Surname = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    Grade = reader.IsDBNull(3) ? string.Empty : reader.GetString(3)
                });
            }
        }

        var subjects = new List<ClassAssignedSubjectDto>();
        using (var subjectsCommand = new SqlCommand(
            @"SELECT s.Id, s.Name, ISNULL(s.Code, '')
              FROM institution.ClassSubject AS cs
              INNER JOIN institution.Subject AS s ON s.Id = cs.SubjectId
              WHERE cs.ClassId = @ClassId
                AND s.TenantId = @TenantId
                AND s.IsActive = 1
              ORDER BY s.Name ASC;",
            connection))
        {
            subjectsCommand.Parameters.AddWithValue("@ClassId", classId);
            subjectsCommand.Parameters.AddWithValue("@TenantId", tenantId.Value);

            using var reader = await subjectsCommand.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                subjects.Add(new ClassAssignedSubjectDto
                {
                    SubjectId = reader.GetInt32(0),
                    Name = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                    Code = reader.IsDBNull(2) ? string.Empty : reader.GetString(2)
                });
            }
        }

        detail.Learners = learners;
        detail.Subjects = subjects;
        return detail;
    }

    public async Task<IReadOnlyCollection<TeacherLookupDto>> SearchTeachersAsync(string query, CancellationToken cancellationToken = default)
    {
        var searchTerm = query?.Trim();
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return Array.Empty<TeacherLookupDto>();
        }

        using var connection = await _sqlConnectionService.GetSqlConnectionAsync(cancellationToken);
        var hasIsActiveColumn = await HasColumnAsync(connection, "dbo", "User", "IsActive", cancellationToken);
        var activeFilter = hasIsActiveColumn ? "AND ISNULL(u.IsActive, 1) = 1" : string.Empty;

        using var command = new SqlCommand(
            $@"SELECT DISTINCT TOP (20)
                    u.UserId,
                    u.Name,
                    u.Email,
                    ISNULL(ur.Name, '') AS RoleName
               FROM [User] AS u
               LEFT JOIN SystemUserRole AS sur ON sur.UserId = u.UserId
               LEFT JOIN OperatorUser AS ou ON ou.UserId = u.UserId
               LEFT JOIN OperatorUserRole AS our ON our.OperatorUserId = ou.OperatorUserId
               LEFT JOIN [UserRole] AS ur ON ur.UserRoleId = COALESCE(our.UserRoleId, sur.UserRoleId, u.UserRoleId)
               WHERE (u.Name LIKE @Search OR u.Email LIKE @Search)
                 AND (
                    ur.Name LIKE '%Teacher%'
                    OR ur.Name LIKE '%Staff%'
                    OR ur.Name LIKE '%Admin%')
                 {activeFilter}
               ORDER BY u.Name ASC, u.Email ASC;",
            connection);
        command.Parameters.AddWithValue("@Search", $"%{searchTerm}%");

        var teachers = new List<TeacherLookupDto>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            teachers.Add(new TeacherLookupDto
            {
                TeacherId = reader.GetInt32(0),
                Name = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                Email = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                Role = reader.IsDBNull(3) ? string.Empty : reader.GetString(3)
            });
        }

        return teachers;
    }

    public async Task<IReadOnlyCollection<LearnerLookupDto>> SearchLearnersAsync(string query, CancellationToken cancellationToken = default)
    {
        var searchTerm = query?.Trim();
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return Array.Empty<LearnerLookupDto>();
        }

        using var connection = await _sqlConnectionService.GetSqlConnectionAsync(cancellationToken);
        var tenantId = await ResolveTenantIdAsync(connection, cancellationToken);
        if (!tenantId.HasValue)
        {
            return Array.Empty<LearnerLookupDto>();
        }

        using var command = new SqlCommand(
            @"SELECT TOP (30) Id, FirstName, Surname, Grade
              FROM institution.Learner
              WHERE TenantId = @TenantId
                AND IsArchived = 0
                AND (
                    FirstName LIKE @Search
                    OR Surname LIKE @Search
                    OR CONCAT(FirstName, ' ', Surname) LIKE @Search
                    OR CAST(Id AS NVARCHAR(20)) LIKE @Search)
              ORDER BY FirstName ASC, Surname ASC;",
            connection);
        command.Parameters.AddWithValue("@TenantId", tenantId.Value);
        command.Parameters.AddWithValue("@Search", $"%{searchTerm}%");

        var learners = new List<LearnerLookupDto>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            learners.Add(new LearnerLookupDto
            {
                LearnerId = reader.GetInt32(0),
                FirstName = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                Surname = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                Grade = reader.IsDBNull(3) ? string.Empty : reader.GetString(3)
            });
        }

        return learners;
    }

    public async Task<IReadOnlyCollection<ClassAssignedSubjectDto>> SearchSubjectsAsync(string query, CancellationToken cancellationToken = default)
    {
        var searchTerm = query?.Trim();
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return Array.Empty<ClassAssignedSubjectDto>();
        }

        using var connection = await _sqlConnectionService.GetSqlConnectionAsync(cancellationToken);
        var tenantId = await ResolveTenantIdAsync(connection, cancellationToken);
        if (!tenantId.HasValue)
        {
            return Array.Empty<ClassAssignedSubjectDto>();
        }

        using var command = new SqlCommand(
            @"SELECT TOP (20) Id, Name, ISNULL(Code, '')
              FROM institution.Subject
              WHERE TenantId = @TenantId
                AND IsActive = 1
                AND (Name LIKE @Search OR Code LIKE @Search)
              ORDER BY Name ASC;",
            connection);
        command.Parameters.AddWithValue("@TenantId", tenantId.Value);
        command.Parameters.AddWithValue("@Search", $"%{searchTerm}%");

        var subjects = new List<ClassAssignedSubjectDto>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            subjects.Add(new ClassAssignedSubjectDto
            {
                SubjectId = reader.GetInt32(0),
                Name = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                Code = reader.IsDBNull(2) ? string.Empty : reader.GetString(2)
            });
        }

        return subjects;
    }

    public async Task<ClassDto> CreateClassAsync(CreateClassRequestDto request, CancellationToken cancellationToken = default)
    {
        using var connection = await _sqlConnectionService.GetSqlConnectionAsync(cancellationToken);
        var tenantId = await ResolveTenantIdAsync(connection, cancellationToken);
        if (!tenantId.HasValue)
        {
            throw new InvalidOperationException("No tenant found in institution.Tenant.");
        }

        var hasIsActiveColumn = await HasColumnAsync(connection, "dbo", "User", "IsActive", cancellationToken);
        var activeFilter = hasIsActiveColumn ? "AND ISNULL(IsActive, 1) = 1" : string.Empty;

        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            using (var teacherCommand = new SqlCommand(
                $@"SELECT COUNT(1)
                   FROM [User]
                   WHERE UserId = @TeacherId
                   {activeFilter};",
                connection,
                transaction))
            {
                teacherCommand.Parameters.AddWithValue("@TeacherId", request.TeacherId);
                var teacherExists = Convert.ToInt32(await teacherCommand.ExecuteScalarAsync(cancellationToken)) > 0;
                if (!teacherExists)
                {
                    throw new InvalidOperationException("Selected teacher was not found or is inactive.");
                }
            }

            var classId = await GetNextIdAsync(connection, transaction, "institution.Class", cancellationToken);
            using (var insertClassCommand = new SqlCommand(
                @"INSERT INTO institution.Class (Id, SchoolId, Name, Grade, TeacherId)
                  VALUES (@Id, @SchoolId, @Name, @Grade, @TeacherId);",
                connection,
                transaction))
            {
                insertClassCommand.Parameters.AddWithValue("@Id", classId);
                insertClassCommand.Parameters.AddWithValue("@SchoolId", tenantId.Value);
                insertClassCommand.Parameters.AddWithValue("@Name", request.Name.Trim());
                insertClassCommand.Parameters.AddWithValue("@Grade", request.Grade.Trim());
                insertClassCommand.Parameters.AddWithValue("@TeacherId", request.TeacherId);
                await insertClassCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            var distinctLearners = request.LearnerIds
                .Where(id => id > 0)
                .Distinct()
                .ToArray();

            foreach (var learnerId in distinctLearners)
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
                    continue;
                }

                using var insertEnrolmentCommand = new SqlCommand(
                    @"INSERT INTO institution.ClassEnrolment (ClassId, LearnerId)
                      VALUES (@ClassId, @LearnerId);",
                    connection,
                    transaction);
                insertEnrolmentCommand.Parameters.AddWithValue("@ClassId", classId);
                insertEnrolmentCommand.Parameters.AddWithValue("@LearnerId", learnerId);
                await insertEnrolmentCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);

            return new ClassDto
            {
                Id = classId,
                SchoolId = tenantId.Value,
                Name = request.Name.Trim(),
                Grade = request.Grade.Trim(),
                TeacherId = request.TeacherId,
                LearnerIds = distinctLearners
            };
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<ClassAssignedSubjectDto?> AssignSubjectToClassAsync(int classId, int subjectId, CancellationToken cancellationToken = default)
    {
        using var connection = await _sqlConnectionService.GetSqlConnectionAsync(cancellationToken);
        var tenantId = await ResolveTenantIdAsync(connection, cancellationToken);
        if (!tenantId.HasValue)
        {
            return null;
        }

        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            using (var classCheckCommand = new SqlCommand(
                @"SELECT COUNT(1)
                  FROM institution.Class
                  WHERE Id = @ClassId
                    AND SchoolId = @SchoolId;",
                connection,
                transaction))
            {
                classCheckCommand.Parameters.AddWithValue("@ClassId", classId);
                classCheckCommand.Parameters.AddWithValue("@SchoolId", tenantId.Value);
                var classExists = Convert.ToInt32(await classCheckCommand.ExecuteScalarAsync(cancellationToken)) > 0;
                if (!classExists)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return null;
                }
            }

            string subjectName;
            string subjectCode;
            using (var subjectCommand = new SqlCommand(
                @"SELECT TOP (1) Name, ISNULL(Code, '')
                  FROM institution.Subject
                  WHERE Id = @SubjectId
                    AND TenantId = @TenantId
                    AND IsActive = 1;",
                connection,
                transaction))
            {
                subjectCommand.Parameters.AddWithValue("@SubjectId", subjectId);
                subjectCommand.Parameters.AddWithValue("@TenantId", tenantId.Value);

                using var subjectReader = await subjectCommand.ExecuteReaderAsync(cancellationToken);
                if (!await subjectReader.ReadAsync(cancellationToken))
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return null;
                }

                subjectName = subjectReader.IsDBNull(0) ? string.Empty : subjectReader.GetString(0);
                subjectCode = subjectReader.IsDBNull(1) ? string.Empty : subjectReader.GetString(1);
            }

            using (var existsCommand = new SqlCommand(
                @"SELECT COUNT(1)
                  FROM institution.ClassSubject
                  WHERE ClassId = @ClassId
                    AND SubjectId = @SubjectId;",
                connection,
                transaction))
            {
                existsCommand.Parameters.AddWithValue("@ClassId", classId);
                existsCommand.Parameters.AddWithValue("@SubjectId", subjectId);

                var exists = Convert.ToInt32(await existsCommand.ExecuteScalarAsync(cancellationToken)) > 0;
                if (!exists)
                {
                    using var insertCommand = new SqlCommand(
                        @"INSERT INTO institution.ClassSubject (ClassId, SubjectId)
                          VALUES (@ClassId, @SubjectId);",
                        connection,
                        transaction);
                    insertCommand.Parameters.AddWithValue("@ClassId", classId);
                    insertCommand.Parameters.AddWithValue("@SubjectId", subjectId);
                    await insertCommand.ExecuteNonQueryAsync(cancellationToken);
                }
            }

            await transaction.CommitAsync(cancellationToken);
            return new ClassAssignedSubjectDto
            {
                SubjectId = subjectId,
                Name = subjectName,
                Code = subjectCode
            };
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
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