using Microsoft.AspNetCore.Mvc;
using TheFactory.Contracts;
using TheFactory.Services;

namespace TheFactory.Controllers;

/// <summary>Reads and updates global timetable display settings.</summary>
[ApiController]
[Route("api/[controller]")]
public sealed class ScheduleSettingsController(IScheduleSettingsService scheduleSettingsService) : ControllerBase
{
    /// <summary>Gets the globally shared timetable column visibility settings.</summary>
    [HttpGet("columns")]
    [ProducesResponseType(typeof(ScheduleColumnVisibilityResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ScheduleColumnVisibilityResponse>> GetColumnVisibility(
        CancellationToken cancellationToken)
    {
        var settings = await scheduleSettingsService.GetColumnVisibilityAsync(cancellationToken);
        return Ok(settings);
    }

    /// <summary>Updates the globally shared timetable column visibility settings.</summary>
    [HttpPut("columns")]
    [ProducesResponseType(typeof(ScheduleColumnVisibilityResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ScheduleColumnVisibilityResponse>> UpdateColumnVisibility(
        [FromBody] UpdateScheduleColumnVisibilityRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetRequestUserId(out var userId) ||
            !await scheduleSettingsService.IsSystemAdminAsync(userId, cancellationToken))
        {
            return Forbid();
        }

        if (!request.Bus && !request.Departure && !request.Price && !request.Action)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "At least one timetable column must remain visible."
            });
        }

        await scheduleSettingsService.UpdateColumnVisibilityAsync(request, userId, cancellationToken);
        return Ok(new ScheduleColumnVisibilityResponse(
            request.Bus,
            request.Departure,
            request.Price,
            request.Action));
    }

    private bool TryGetRequestUserId(out int userId)
    {
        userId = 0;
        return Request.Headers.TryGetValue("X-User-Id", out var userIdHeader)
            && int.TryParse(userIdHeader.FirstOrDefault(), out userId)
            && userId > 0;
    }
}
