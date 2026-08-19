using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using TheFactory.Contracts;
using TheFactory.Services;

namespace TheFactory.Controllers;

public sealed class CreateSchoolRequest
{
    public string Name { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
}

public sealed class AssignUserRoleRequest
{
    public int UserId { get; set; }
    public int UserRoleId { get; set; }
}

public sealed class UserLookupResult
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

[ApiController]
[Route("api")]
public class ReportsController : ControllerBase
{
    private readonly ILearnerService _learnerService;
    private readonly IReportService _reportService;
    private readonly IPdfGeneratorService _pdfGeneratorService;
    private readonly SqlConnectionService _sqlConnectionService;

    public ReportsController(
        ILearnerService learnerService,
        IReportService reportService,
        IPdfGeneratorService pdfGeneratorService,
        SqlConnectionService sqlConnectionService)
    {
        _learnerService = learnerService;
        _reportService = reportService;
        _pdfGeneratorService = pdfGeneratorService;
        _sqlConnectionService = sqlConnectionService;
    }

    [HttpGet("schools")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> GetSchools(CancellationToken cancellationToken)
    {
        var schools = new List<object>();

        using var connection = await _sqlConnectionService.GetSqlConnectionAsync(cancellationToken);
        using var command = new SqlCommand(
            @"SELECT Id, Name
              FROM institution.Tenant
              ORDER BY Name ASC;",
            connection);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            schools.Add(new
            {
                id = reader.GetInt32(0),
                name = reader.IsDBNull(1) ? string.Empty : reader.GetString(1)
            });
        }

        return Ok(schools);
    }

    [HttpPost("schools")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> CreateSchool([FromBody] CreateSchoolRequest request, CancellationToken cancellationToken)
    {
        if (!IsSystemAdminRequest())
        {
            return Forbid();
        }

        if (request is null || string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { error = "School name is required." });
        }

        using var connection = await _sqlConnectionService.GetSqlConnectionAsync(cancellationToken);

        using var existsCommand = new SqlCommand(
            @"SELECT COUNT(1)
              FROM institution.Tenant
              WHERE Name = @Name;",
            connection);
        existsCommand.Parameters.AddWithValue("@Name", request.Name.Trim());

        var existingCount = Convert.ToInt32(await existsCommand.ExecuteScalarAsync(cancellationToken));
        if (existingCount > 0)
        {
            return BadRequest(new { error = "A tenant with this name already exists." });
        }

        using var idCommand = new SqlCommand("SELECT ISNULL(MAX(Id), 0) + 1 FROM institution.Tenant;", connection);
        var newId = Convert.ToInt32(await idCommand.ExecuteScalarAsync(cancellationToken));

        using var insertCommand = new SqlCommand(
            @"INSERT INTO institution.Tenant (Id, Name, LogoUrl)
              VALUES (@Id, @Name, @LogoUrl);",
            connection);
        insertCommand.Parameters.AddWithValue("@Id", newId);
        insertCommand.Parameters.AddWithValue("@Name", request.Name.Trim());
        insertCommand.Parameters.AddWithValue("@LogoUrl", string.IsNullOrWhiteSpace(request.LogoUrl)
            ? DBNull.Value
            : request.LogoUrl.Trim());

        await insertCommand.ExecuteNonQueryAsync(cancellationToken);

        return Created($"/api/schools/{newId}", new
        {
            id = newId,
            name = request.Name.Trim()
        });
    }

    [HttpGet("userroles")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> GetUserRoles(CancellationToken cancellationToken)
    {
        if (!IsSystemAdminRequest())
        {
            return Forbid();
        }

        var roles = new List<object>();

        using var connection = await _sqlConnectionService.GetSqlConnectionAsync(cancellationToken);
        using var command = new SqlCommand(
            @"SELECT UserRoleId, Name
              FROM [UserRole]
              ORDER BY Name ASC;",
            connection);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            roles.Add(new
            {
                userRoleId = reader.GetInt32(0),
                name = reader.IsDBNull(1) ? string.Empty : reader.GetString(1)
            });
        }

        return Ok(roles);
    }

    [HttpGet("userroles/users")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> SearchUsers([FromQuery] string query, CancellationToken cancellationToken)
    {
        if (!IsSystemAdminRequest())
        {
            return Forbid();
        }

        var searchTerm = query?.Trim();
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return Ok(Array.Empty<UserLookupResult>());
        }

        var results = new List<UserLookupResult>();

        using var connection = await _sqlConnectionService.GetSqlConnectionAsync(cancellationToken);
        using var command = new SqlCommand(
            @"SELECT TOP (10) UserId, Name, Email
              FROM [User]
              WHERE Name LIKE @Search OR Email LIKE @Search
              ORDER BY Name ASC, Email ASC;",
            connection);
        command.Parameters.AddWithValue("@Search", $"%{searchTerm}%");

        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new UserLookupResult
            {
                UserId = reader.GetInt32(0),
                Name = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                Email = reader.IsDBNull(2) ? string.Empty : reader.GetString(2)
            });
        }

        return Ok(results);
    }

    [HttpPost("userroles/assign")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> AssignUserRole([FromBody] AssignUserRoleRequest request, CancellationToken cancellationToken)
    {
        if (!IsSystemAdminRequest())
        {
            return Forbid();
        }

        if (request is null || request.UserId <= 0 || request.UserRoleId <= 0)
        {
            return BadRequest(new { error = "UserId and UserRoleId are required." });
        }

        using var connection = await _sqlConnectionService.GetSqlConnectionAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();

        try
        {
            using var roleExistsCommand = new SqlCommand(
                "SELECT COUNT(1) FROM [UserRole] WHERE UserRoleId = @UserRoleId;",
                connection,
                transaction);
            roleExistsCommand.Parameters.AddWithValue("@UserRoleId", request.UserRoleId);
            var roleCount = Convert.ToInt32(await roleExistsCommand.ExecuteScalarAsync(cancellationToken));
            if (roleCount == 0)
            {
                transaction.Rollback();
                return BadRequest(new { error = "Selected role does not exist." });
            }

            using var userCommand = new SqlCommand(
                "SELECT TOP (1) UserId FROM [User] WHERE UserId = @UserId;",
                connection,
                transaction);
            userCommand.Parameters.AddWithValue("@UserId", request.UserId);
            var userIdRaw = await userCommand.ExecuteScalarAsync(cancellationToken);
            if (userIdRaw is null)
            {
                transaction.Rollback();
                return NotFound(new { error = "User not found." });
            }

            var userId = Convert.ToInt32(userIdRaw);

            using var updateUserRoleCommand = new SqlCommand(
                "UPDATE [User] SET UserRoleId = @UserRoleId WHERE UserId = @UserId;",
                connection,
                transaction);
            updateUserRoleCommand.Parameters.AddWithValue("@UserRoleId", request.UserRoleId);
            updateUserRoleCommand.Parameters.AddWithValue("@UserId", userId);
            await updateUserRoleCommand.ExecuteNonQueryAsync(cancellationToken);

            using var operatorUserCommand = new SqlCommand(
                "SELECT TOP (1) OperatorUserId FROM [OperatorUser] WHERE UserId = @UserId;",
                connection,
                transaction);
            operatorUserCommand.Parameters.AddWithValue("@UserId", userId);
            var operatorUserIdRaw = await operatorUserCommand.ExecuteScalarAsync(cancellationToken);

            if (operatorUserIdRaw is not null)
            {
                var operatorUserId = Convert.ToInt32(operatorUserIdRaw);

                using var operatorRoleCountCommand = new SqlCommand(
                    "SELECT COUNT(1) FROM [OperatorUserRole] WHERE OperatorUserId = @OperatorUserId;",
                    connection,
                    transaction);
                operatorRoleCountCommand.Parameters.AddWithValue("@OperatorUserId", operatorUserId);
                var operatorRoleCount = Convert.ToInt32(await operatorRoleCountCommand.ExecuteScalarAsync(cancellationToken));

                if (operatorRoleCount > 0)
                {
                    using var updateOperatorRoleCommand = new SqlCommand(
                        "UPDATE [OperatorUserRole] SET UserRoleId = @UserRoleId WHERE OperatorUserId = @OperatorUserId;",
                        connection,
                        transaction);
                    updateOperatorRoleCommand.Parameters.AddWithValue("@UserRoleId", request.UserRoleId);
                    updateOperatorRoleCommand.Parameters.AddWithValue("@OperatorUserId", operatorUserId);
                    await updateOperatorRoleCommand.ExecuteNonQueryAsync(cancellationToken);
                }
                else
                {
                    using var insertOperatorRoleCommand = new SqlCommand(
                        "INSERT INTO [OperatorUserRole] (UserRoleId, OperatorUserId) VALUES (@UserRoleId, @OperatorUserId);",
                        connection,
                        transaction);
                    insertOperatorRoleCommand.Parameters.AddWithValue("@UserRoleId", request.UserRoleId);
                    insertOperatorRoleCommand.Parameters.AddWithValue("@OperatorUserId", operatorUserId);
                    await insertOperatorRoleCommand.ExecuteNonQueryAsync(cancellationToken);
                }
            }
            else
            {
                using var systemRoleCountCommand = new SqlCommand(
                    "SELECT COUNT(1) FROM [SystemUserRole] WHERE UserId = @UserId;",
                    connection,
                    transaction);
                systemRoleCountCommand.Parameters.AddWithValue("@UserId", userId);
                var systemRoleCount = Convert.ToInt32(await systemRoleCountCommand.ExecuteScalarAsync(cancellationToken));

                if (systemRoleCount > 0)
                {
                    using var updateSystemRoleCommand = new SqlCommand(
                        "UPDATE [SystemUserRole] SET UserRoleId = @UserRoleId WHERE UserId = @UserId;",
                        connection,
                        transaction);
                    updateSystemRoleCommand.Parameters.AddWithValue("@UserRoleId", request.UserRoleId);
                    updateSystemRoleCommand.Parameters.AddWithValue("@UserId", userId);
                    await updateSystemRoleCommand.ExecuteNonQueryAsync(cancellationToken);
                }
                else
                {
                    using var insertSystemRoleCommand = new SqlCommand(
                        "INSERT INTO [SystemUserRole] (UserId, UserRoleId) VALUES (@UserId, @UserRoleId);",
                        connection,
                        transaction);
                    insertSystemRoleCommand.Parameters.AddWithValue("@UserId", userId);
                    insertSystemRoleCommand.Parameters.AddWithValue("@UserRoleId", request.UserRoleId);
                    await insertSystemRoleCommand.ExecuteNonQueryAsync(cancellationToken);
                }
            }

            transaction.Commit();
            return Ok(new { message = "User role updated successfully." });
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Gets all active learners for the current school.
    /// </summary>
    [HttpGet("learners")]
    [ProducesResponseType(typeof(IReadOnlyCollection<LearnerDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<LearnerDto>>> GetLearners(CancellationToken cancellationToken)
    {
        var learners = await _learnerService.GetLearnersForCurrentSchoolAsync(cancellationToken);
        return Ok(learners);
    }

    [HttpGet("classes")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ClassDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ClassDto>>> GetClasses(CancellationToken cancellationToken)
    {
        var classes = await _learnerService.GetClassesForCurrentSchoolAsync(cancellationToken);
        return Ok(classes);
    }

    [HttpGet("classes/{id:int}")]
    [ProducesResponseType(typeof(ClassDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClassDetailDto>> GetClassById(int id, CancellationToken cancellationToken)
    {
        var classDetail = await _learnerService.GetClassByIdAsync(id, cancellationToken);
        if (classDetail is null)
        {
            return NotFound();
        }

        return Ok(classDetail);
    }

    [HttpGet("teachers/search")]
    [ProducesResponseType(typeof(IReadOnlyCollection<TeacherLookupDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<TeacherLookupDto>>> SearchTeachers([FromQuery] string q, CancellationToken cancellationToken)
    {
        var teachers = await _learnerService.SearchTeachersAsync(q, cancellationToken);
        return Ok(teachers);
    }

    [HttpGet("learners/search")]
    [ProducesResponseType(typeof(IReadOnlyCollection<LearnerLookupDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<LearnerLookupDto>>> SearchLearners([FromQuery] string q, CancellationToken cancellationToken)
    {
        var learners = await _learnerService.SearchLearnersAsync(q, cancellationToken);
        return Ok(learners);
    }

    [HttpGet("subjects/search")]
    [ProducesResponseType(typeof(IReadOnlyCollection<ClassAssignedSubjectDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<ClassAssignedSubjectDto>>> SearchSubjects([FromQuery] string q, CancellationToken cancellationToken)
    {
        var subjects = await _learnerService.SearchSubjectsAsync(q, cancellationToken);
        return Ok(subjects);
    }

    [HttpPost("classes")]
    [ProducesResponseType(typeof(ClassDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ClassDto>> CreateClass([FromBody] CreateClassRequestDto request, CancellationToken cancellationToken)
    {
        if (!TryValidateCreateClassRequest(request, out var validationError))
        {
            return BadRequest(validationError);
        }

        try
        {
            var createdClass = await _learnerService.CreateClassAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetClasses), new { id = createdClass.Id }, createdClass);
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to create class: {ex.Message}");
        }
    }

    [HttpPost("classes/{id:int}/subjects")]
    [ProducesResponseType(typeof(ClassAssignedSubjectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClassAssignedSubjectDto>> AssignSubjectToClass(
        int id,
        [FromBody] AssignClassSubjectRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request is null || request.SubjectId <= 0)
        {
            return BadRequest("SubjectId is required.");
        }

        try
        {
            var assignedSubject = await _learnerService.AssignSubjectToClassAsync(id, request.SubjectId, cancellationToken);
            if (assignedSubject is null)
            {
                return NotFound();
            }

            return Ok(assignedSubject);
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to assign subject: {ex.Message}");
        }
    }

    [HttpGet("work")]
    [ProducesResponseType(typeof(IReadOnlyCollection<WorkDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<WorkDto>>> GetWorkItems(CancellationToken cancellationToken)
    {
        var workItems = await _learnerService.GetWorkItemsForCurrentSchoolAsync(cancellationToken);
        return Ok(workItems);
    }

    [HttpPost("work")]
    [ProducesResponseType(typeof(WorkDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<WorkDto>> CreateWorkItem([FromBody] WorkUpsertRequestDto request, CancellationToken cancellationToken)
    {
        if (!TryValidateWorkRequest(request, out var validationError))
        {
            return BadRequest(validationError);
        }

        try
        {
            var created = await _learnerService.CreateWorkItemAsync(request, cancellationToken);
            return Created($"/api/work/{created.Id}", created);
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to create work item: {ex.Message}");
        }
    }

    [HttpPut("work/{id:int}")]
    [ProducesResponseType(typeof(WorkDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkDto>> UpdateWorkItem(int id, [FromBody] WorkUpsertRequestDto request, CancellationToken cancellationToken)
    {
        if (!TryValidateWorkRequest(request, out var validationError))
        {
            return BadRequest(validationError);
        }

        try
        {
            var updated = await _learnerService.UpdateWorkItemAsync(id, request, cancellationToken);
            if (updated is null)
            {
                return NotFound();
            }

            return Ok(updated);
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to update work item: {ex.Message}");
        }
    }

    [HttpDelete("work/{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ArchiveWorkItem(int id, CancellationToken cancellationToken)
    {
        try
        {
            var archived = await _learnerService.ArchiveWorkItemAsync(id, cancellationToken);
            if (!archived)
            {
                return NotFound();
            }

            return Ok(new { message = "Work item archived successfully." });
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to archive work item: {ex.Message}");
        }
    }

    [HttpGet("subjects")]
    [ProducesResponseType(typeof(IReadOnlyCollection<SubjectDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<SubjectDto>>> GetSubjectsForCurriculum(CancellationToken cancellationToken)
    {
        var subjects = await _learnerService.GetAllSubjectsForCurrentTenantAsync(cancellationToken);
        return Ok(subjects);
    }

    [HttpPost("subjects")]
    [ProducesResponseType(typeof(SubjectDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SubjectDto>> CreateSubject([FromBody] SubjectUpsertRequestDto request, CancellationToken cancellationToken)
    {
        if (!TryValidateSubjectUpsertRequest(request, out var validationError))
        {
            return BadRequest(validationError);
        }

        try
        {
            var created = await _learnerService.CreateSubjectAsync(request, cancellationToken);
            return Created($"/api/subjects/{created.Id}", created);
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to create subject: {ex.Message}");
        }
    }

    [HttpPost("subjects/{id:int}")]
    [ProducesResponseType(typeof(SubjectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubjectDto>> UpdateSubject(int id, [FromBody] SubjectUpsertRequestDto request, CancellationToken cancellationToken)
    {
        if (!TryValidateSubjectUpsertRequest(request, out var validationError))
        {
            return BadRequest(validationError);
        }

        try
        {
            var updated = await _learnerService.UpdateSubjectAsync(id, request, cancellationToken);
            if (updated is null)
            {
                return NotFound();
            }

            return Ok(updated);
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to update subject: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets a learner by ID.
    /// </summary>
    [HttpGet("learners/{id:int}")]
    [ProducesResponseType(typeof(LearnerDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LearnerDetailDto>> GetLearnerById(int id, CancellationToken cancellationToken)
    {
        var learner = await _learnerService.GetLearnerDetailByIdAsync(id, cancellationToken);
        if (learner is null)
        {
            return NotFound();
        }

        return Ok(learner);
    }

    [HttpGet("learners/{id:int}/academic-records")]
    [ProducesResponseType(typeof(IReadOnlyCollection<LearnerAcademicRecordDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<LearnerAcademicRecordDto>>> GetLearnerAcademicRecords(int id, CancellationToken cancellationToken)
    {
        var records = await _learnerService.GetLearnerAcademicRecordsAsync(id, cancellationToken);
        return Ok(records);
    }

    /// <summary>
    /// Registers a new learner.
    /// </summary>
    [HttpPost("learners")]
    [ProducesResponseType(typeof(LearnerDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LearnerDto>> CreateLearner([FromBody] LearnerDto learnerDto, CancellationToken cancellationToken)
    {
        if (!TryValidateLearnerDto(learnerDto, out var validationError))
        {
            return BadRequest(validationError);
        }

        try
        {
            var createdLearner = await _learnerService.CreateLearnerAsync(learnerDto, cancellationToken);
            return CreatedAtAction(nameof(GetLearnerById), new { id = createdLearner.Id }, createdLearner);
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to register learner: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates learner profile details by ID.
    /// </summary>
    [HttpPut("learners/{id:int}")]
    [ProducesResponseType(typeof(LearnerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LearnerDto>> UpdateLearner(int id, [FromBody] LearnerDto learnerDto, CancellationToken cancellationToken)
    {
        if (!TryValidateLearnerDto(learnerDto, out var validationError))
        {
            return BadRequest(validationError);
        }

        try
        {
            var updatedLearner = await _learnerService.UpdateLearnerAsync(id, learnerDto, cancellationToken);
            if (updatedLearner is null)
            {
                return NotFound();
            }

            return Ok(updatedLearner);
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to update learner: {ex.Message}");
        }
    }

    /// <summary>
    /// Deletes or archives learner profile details by ID.
    /// </summary>
    [HttpDelete("learners/{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteLearner(int id, CancellationToken cancellationToken)
    {
        try
        {
            var wasArchived = await _learnerService.ArchiveLearnerAsync(id, cancellationToken);
            if (!wasArchived)
            {
                return NotFound();
            }

            return Ok(new { Message = "Learner archived successfully." });
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to archive learner: {ex.Message}");
        }
    }

    /// <summary>
    /// Adds or updates subject score for a learner.
    /// </summary>
    [HttpPost("reports/{learnerId:int}/scores")]
    [ProducesResponseType(typeof(SubjectScoreDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubjectScoreDto>> UpsertSubjectScore(int learnerId, [FromBody] SubjectScoreDto subjectScoreDto, CancellationToken cancellationToken)
    {
        if (!TryValidateSubjectScoreDto(subjectScoreDto, out var validationError))
        {
            return BadRequest(validationError);
        }

        try
        {
            var updatedScore = await _learnerService.UpsertSubjectScoreAsync(learnerId, subjectScoreDto, cancellationToken);
            if (updatedScore is null)
            {
                return NotFound();
            }

            return Ok(updatedScore);
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to add or update score: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets learner performance report by learner ID.
    /// </summary>
    [HttpGet("reports/{learnerId:int}")]
    [ProducesResponseType(typeof(LearnerReportResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LearnerReportResponseDto>> GetReport(int learnerId, CancellationToken cancellationToken)
    {
        var report = await _reportService.GetLearnerReportAsync(learnerId, cancellationToken);
        if (report is null)
        {
            return NotFound();
        }

        var response = new LearnerReportResponseDto
        {
            SchoolName = report.SchoolName,
            LearnerName = $"{report.FirstName} {report.Surname}",
            Grade = report.Grade,
            Subjects = report.Subjects,
            Average = report.Average
        };

        return Ok(response);
    }

    /// <summary>
    /// Gets learner performance report PDF by learner ID.
    /// </summary>
    [HttpGet("reports/{learnerId:int}/pdf")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetReportPdf(int learnerId, CancellationToken cancellationToken)
    {
        var report = await _reportService.GetLearnerReportAsync(learnerId, cancellationToken);
        if (report is null)
        {
            return NotFound();
        }

        var pdfBytes = await _pdfGeneratorService.GenerateLearnerReportPdfAsync(report, cancellationToken);
        var fileName = $"{report.FirstName}_{report.Surname}_Report.pdf";

        return File(pdfBytes, "application/pdf", fileName);
    }

    private static bool TryValidateLearnerDto(LearnerDto learnerDto, out string error)
    {
        if (learnerDto is null)
        {
            error = "Learner payload is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(learnerDto.FirstName))
        {
            error = "FirstName is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(learnerDto.Surname))
        {
            error = "Surname is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(learnerDto.Grade))
        {
            error = "Grade is required.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private bool IsSystemAdminRequest()
    {
        if (!Request.Headers.TryGetValue("X-User-Role", out var roleHeader))
        {
            return false;
        }

        return string.Equals(roleHeader.FirstOrDefault(), "SystemAdmin", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryValidateSubjectScoreDto(SubjectScoreDto subjectScoreDto, out string error)
    {
        if (subjectScoreDto is null)
        {
            error = "Subject score payload is required.";
            return false;
        }

        if (subjectScoreDto.SubjectId <= 0)
        {
            error = "SubjectId is required.";
            return false;
        }

        if (subjectScoreDto.PossibleMark <= 0)
        {
            error = "PossibleMark must be greater than zero.";
            return false;
        }

        if (subjectScoreDto.PupilMark < 0 || subjectScoreDto.PupilMark > subjectScoreDto.PossibleMark)
        {
            error = "PupilMark must be between 0 and PossibleMark.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static bool TryValidateCreateClassRequest(CreateClassRequestDto request, out string error)
    {
        if (request is null)
        {
            error = "Class payload is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            error = "Class name is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.Grade))
        {
            error = "Class grade is required.";
            return false;
        }

        if (request.TeacherId <= 0)
        {
            error = "Teacher is required.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static bool TryValidateSubjectUpsertRequest(SubjectUpsertRequestDto request, out string error)
    {
        if (request is null)
        {
            error = "Subject payload is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            error = "Subject name is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.Code))
        {
            error = "Subject code is required.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static bool TryValidateWorkRequest(WorkUpsertRequestDto request, out string error)
    {
        if (request is null)
        {
            error = "Work payload is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            error = "Title is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.WorkType))
        {
            error = "WorkType is required.";
            return false;
        }

        if (request.ClassId <= 0)
        {
            error = "ClassId is required.";
            return false;
        }

        if (request.MaxScore < 0)
        {
            error = "MaxScore cannot be negative.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}