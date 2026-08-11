using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using TheFactory.Contracts;
using TheFactory.Services;

namespace TheFactory.Controllers;

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

    /// <summary>
    /// Gets a learner by ID.
    /// </summary>
    [HttpGet("learners/{id:int}")]
    [ProducesResponseType(typeof(LearnerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LearnerDto>> GetLearnerById(int id, CancellationToken cancellationToken)
    {
        var learner = await _learnerService.GetLearnerByIdAsync(id, cancellationToken);
        if (learner is null)
        {
            return NotFound();
        }

        return Ok(learner);
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

    private static bool TryValidateSubjectScoreDto(SubjectScoreDto subjectScoreDto, out string error)
    {
        if (subjectScoreDto is null)
        {
            error = "Subject score payload is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(subjectScoreDto.Subject))
        {
            error = "Subject is required.";
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
}