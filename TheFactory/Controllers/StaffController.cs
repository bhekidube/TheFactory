using Microsoft.AspNetCore.Mvc;
using TheFactory.Contracts;
using TheFactory.Services;

namespace TheFactory.Controllers;

[ApiController]
[Route("api/schools/{schoolId:int}/staff")]
public sealed class StaffController : ControllerBase
{
    private readonly IStaffService _staffService;

    public StaffController(IStaffService staffService)
    {
        _staffService = staffService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<StaffDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<StaffDto>>> GetStaff(
        int schoolId,
        [FromQuery] string? search,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        if (schoolId <= 0)
        {
            return BadRequest(new { error = "A valid school id is required." });
        }

        var staff = await _staffService.GetStaffAsync(schoolId, search, status, cancellationToken);
        return Ok(staff);
    }

    [HttpPost]
    [ProducesResponseType(typeof(StaffDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<StaffDto>> CreateStaff(
        int schoolId,
        [FromBody] StaffUpsertRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateRequest(schoolId, request);
        if (validationError is not null)
        {
            return BadRequest(new { error = validationError });
        }

        try
        {
            var staff = await _staffService.CreateStaffAsync(schoolId, request, cancellationToken);
            return CreatedAtAction(nameof(GetStaffById), new { schoolId, id = staff.Id }, staff);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(StaffDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StaffDto>> GetStaffById(int schoolId, int id, CancellationToken cancellationToken)
    {
        var staff = await _staffService.GetStaffByIdAsync(schoolId, id, cancellationToken);
        return staff is null ? NotFound() : Ok(staff);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(StaffDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StaffDto>> UpdateStaff(
        int schoolId,
        int id,
        [FromBody] StaffUpsertRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateRequest(schoolId, request);
        if (validationError is not null)
        {
            return BadRequest(new { error = validationError });
        }

        try
        {
            var staff = await _staffService.UpdateStaffAsync(schoolId, id, request, cancellationToken);
            return staff is null ? NotFound() : Ok(staff);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPatch("{id:int}/archive")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ArchiveStaff(int schoolId, int id, CancellationToken cancellationToken)
    {
        return await _staffService.ArchiveStaffAsync(schoolId, id, cancellationToken)
            ? NoContent()
            : NotFound();
    }

    private static string? ValidateRequest(int schoolId, StaffUpsertRequest? request)
    {
        if (schoolId <= 0)
        {
            return "A valid school id is required.";
        }

        if (request is null)
        {
            return "Staff payload is required.";
        }

        if (string.IsNullOrWhiteSpace(request.FirstName))
        {
            return "First name is required.";
        }

        if (string.IsNullOrWhiteSpace(request.Surname))
        {
            return "Surname is required.";
        }

        if (string.IsNullOrWhiteSpace(request.Role))
        {
            return "Role is required.";
        }

        if (!new[] { "Teacher", "Admin", "Support" }.Contains(request.Role.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            return "Role must be Teacher, Admin, or Support.";
        }

        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
        {
            return "A valid email address is required.";
        }

        if (!string.IsNullOrWhiteSpace(request.Status)
            && !request.Status.Equals("Active", StringComparison.OrdinalIgnoreCase)
            && !request.Status.Equals("Archived", StringComparison.OrdinalIgnoreCase))
        {
            return "Status must be Active or Archived.";
        }

        return null;
    }
}