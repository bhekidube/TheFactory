using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TheFactory.Contracts;
using TheFactory.Models;
using TheFactory.Services;

namespace TheFactory.Controllers;

[ApiController]
[Route("api/classes/{classId:int}/attendance")]
public sealed class AttendanceController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly ILearnerService _learnerService;
    private readonly IPdfGeneratorService _pdfGeneratorService;

    public AttendanceController(
        AppDbContext dbContext,
        ILearnerService learnerService,
        IPdfGeneratorService pdfGeneratorService)
    {
        _dbContext = dbContext;
        _learnerService = learnerService;
        _pdfGeneratorService = pdfGeneratorService;
    }

    [HttpGet("history")]
    [ProducesResponseType(typeof(IReadOnlyCollection<DateTime>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyCollection<DateTime>>> GetHistory(
        int classId,
        CancellationToken cancellationToken)
    {
        var classDetail = await _learnerService.GetClassByIdAsync(classId, cancellationToken);
        if (classId <= 0 || classDetail is null)
        {
            return NotFound();
        }

        var dates = await _dbContext.AttendanceSessions
            .AsNoTracking()
            .Where(session => session.ClassID == classId && session.IsSubmitted)
            .OrderByDescending(session => session.SessionDate)
            .Select(session => session.SessionDate)
            .ToListAsync(cancellationToken);

        return Ok(dates);
    }

    [HttpGet("by-date")]
    [ProducesResponseType(typeof(AttendanceSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AttendanceSessionDto>> GetByDate(
        int classId,
        [FromQuery] DateTime date,
        CancellationToken cancellationToken)
    {
        if (classId <= 0 || date == default)
        {
            return BadRequest("A valid class id and attendance date are required.");
        }

        var classDetail = await _learnerService.GetClassByIdAsync(classId, cancellationToken);
        if (classDetail is null)
        {
            return NotFound();
        }

        var session = await _dbContext.AttendanceSessions
            .AsNoTracking()
            .Include(item => item.Records)
            .SingleOrDefaultAsync(
                item => item.ClassID == classId
                    && item.SessionDate == date.Date
                    && item.IsSubmitted,
                cancellationToken);
        return session is null ? NotFound() : Ok(ToDto(session, classDetail.Learners));
    }

    [HttpGet("{sessionId:int}/download")]
    [Produces("application/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(
        int classId,
        int sessionId,
        CancellationToken cancellationToken)
    {
        var classDetail = await _learnerService.GetClassByIdAsync(classId, cancellationToken);
        if (classId <= 0 || sessionId <= 0 || classDetail is null)
        {
            return NotFound();
        }

        var session = await _dbContext.AttendanceSessions
            .AsNoTracking()
            .Include(item => item.Records)
            .SingleOrDefaultAsync(
                item => item.ClassID == classId && item.SessionID == sessionId && item.IsSubmitted,
                cancellationToken);
        if (session is null)
        {
            return NotFound();
        }

        var pdfBytes = await _pdfGeneratorService.GenerateAttendanceRegisterPdfAsync(
            classDetail.Name,
            ToDto(session, classDetail.Learners),
            cancellationToken);
        var safeClassName = string.Concat(classDetail.Name.Select(character =>
            char.IsLetterOrDigit(character) ? character : '_')).Trim('_');
        return File(pdfBytes, "application/pdf", $"Attendance_{safeClassName}_{session.SessionDate:yyyy-MM-dd}.pdf");
    }

    [HttpPost("initiate")]
    [ProducesResponseType(typeof(AttendanceSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AttendanceSessionDto>> Initiate(
        int classId,
        [FromBody] InitiateAttendanceRequestDto request,
        CancellationToken cancellationToken)
    {
        if (classId <= 0 || request is null || request.SessionDate == default)
        {
            return BadRequest("A valid class id and session date are required.");
        }

        var classDetail = await _learnerService.GetClassByIdAsync(classId, cancellationToken);
        if (classDetail is null)
        {
            return NotFound();
        }

        var sessionDate = request.SessionDate.Date;
        var session = await _dbContext.AttendanceSessions
            .Include(item => item.Records)
            .SingleOrDefaultAsync(
                item => item.ClassID == classId && item.SessionDate == sessionDate,
                cancellationToken);

        if (session is null)
        {
            session = new AttendanceSession
            {
                ClassID = classId,
                SessionDate = sessionDate,
                IsSubmitted = false,
                Records = classDetail.Learners.Select(learner => new AttendanceRecord
                {
                    LearnerID = learner.LearnerId,
                    Status = "Present"
                }).ToList()
            };

            _dbContext.AttendanceSessions.Add(session);
            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
            {
                _dbContext.ChangeTracker.Clear();
                session = await _dbContext.AttendanceSessions
                    .Include(item => item.Records)
                    .SingleOrDefaultAsync(
                        item => item.ClassID == classId && item.SessionDate == sessionDate,
                        cancellationToken);
                if (session is null)
                {
                    throw;
                }
            }
        }

        if (!session.IsSubmitted)
        {
            var currentLearnerIds = classDetail.Learners.Select(learner => learner.LearnerId).ToHashSet();
            foreach (var oldRecord in session.Records.Where(record => !currentLearnerIds.Contains(record.LearnerID)).ToList())
            {
                _dbContext.AttendanceRecords.Remove(oldRecord);
            }

            var existingLearnerIds = session.Records
                .Where(record => currentLearnerIds.Contains(record.LearnerID))
                .Select(record => record.LearnerID)
                .ToHashSet();
            foreach (var learner in classDetail.Learners.Where(learner => !existingLearnerIds.Contains(learner.LearnerId)))
            {
                session.Records.Add(new AttendanceRecord
                {
                    LearnerID = learner.LearnerId,
                    Status = "Present"
                });
            }

            if (_dbContext.ChangeTracker.HasChanges())
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        return Ok(ToDto(session, classDetail.Learners));
    }

    [HttpPost("{sessionId:int}/save")]
    [ProducesResponseType(typeof(AttendanceSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AttendanceSessionDto>> Save(
        int classId,
        int sessionId,
        [FromBody] SaveAttendanceRequestDto request,
        CancellationToken cancellationToken)
    {
        if (classId <= 0 || sessionId <= 0 || request is null || request.Records is null)
        {
            return BadRequest("A valid class, session, and attendance record list are required.");
        }

        var classDetail = await _learnerService.GetClassByIdAsync(classId, cancellationToken);
        if (classDetail is null)
        {
            return NotFound();
        }

        var session = await _dbContext.AttendanceSessions
            .Include(item => item.Records)
            .SingleOrDefaultAsync(
                item => item.SessionID == sessionId && item.ClassID == classId,
                cancellationToken);
        if (session is null)
        {
            return NotFound();
        }

        if (session.IsSubmitted)
        {
            return Conflict("This attendance register has already been submitted.");
        }

        var submitted = request.Records.ToList();
        if (submitted.Any(record => record is null))
        {
            return BadRequest("Attendance records cannot contain null entries.");
        }

        var submittedIds = submitted.Select(record => record.LearnerID).ToHashSet();
        var sessionIds = session.Records.Select(record => record.LearnerID).ToHashSet();
        if (submitted.Count != submittedIds.Count
            || !submittedIds.SetEquals(sessionIds)
            || submitted.Any(record =>
                !string.Equals(record.Status, "Present", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(record.Status, "Absent", StringComparison.OrdinalIgnoreCase)))
        {
            return BadRequest("Provide exactly one Present or Absent status for every learner in this register.");
        }

        foreach (var record in session.Records)
        {
            record.Status = string.Equals(
                submitted.Single(item => item.LearnerID == record.LearnerID).Status,
                "Absent",
                StringComparison.OrdinalIgnoreCase) ? "Absent" : "Present";
        }

        session.IsSubmitted = true;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ToDto(session, classDetail.Learners));
    }

    private static AttendanceSessionDto ToDto(
        AttendanceSession session,
        IReadOnlyCollection<LearnerLookupDto> learners)
    {
        var learnersById = learners.ToDictionary(learner => learner.LearnerId);
        return new AttendanceSessionDto
        {
            SessionID = session.SessionID,
            ClassID = session.ClassID,
            SessionDate = session.SessionDate,
            IsSubmitted = session.IsSubmitted,
            Records = session.Records
                .OrderBy(record => learnersById.TryGetValue(record.LearnerID, out var learner) ? learner.FirstName : string.Empty)
                .ThenBy(record => learnersById.TryGetValue(record.LearnerID, out var learner) ? learner.Surname : string.Empty)
                .Select(record => new AttendanceRecordDto
                {
                    RecordID = record.RecordID,
                    LearnerID = record.LearnerID,
                    LearnerName = learnersById.TryGetValue(record.LearnerID, out var learner)
                        ? $"{learner.FirstName} {learner.Surname}".Trim()
                        : $"Learner #{record.LearnerID}",
                    Status = record.Status
                })
                .ToList()
        };
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        return exception.GetBaseException() is SqlException sqlException
            && sqlException.Number is 2601 or 2627;
    }
}
