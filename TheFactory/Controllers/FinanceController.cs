using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using TheFactory.Contracts;
using TheFactory.Services;

namespace TheFactory.Controllers;

[ApiController]
[Route("api/schools/{schoolId:int}/finance")]
public sealed class FinanceController : ControllerBase
{
    private static readonly HashSet<string> AllowedRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "SystemAdmin",
        "Admin",
        "OperatorAdmin",
        "Operator"
    };

    private readonly IFinanceService _financeService;

    public FinanceController(IFinanceService financeService)
    {
        _financeService = financeService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(FinanceDashboardDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<FinanceDashboardDto>> GetDashboard(int schoolId, CancellationToken cancellationToken)
    {
        if (!TryGetRequestUserId(out _))
        {
            return Forbid();
        }

        if (schoolId <= 0)
        {
            return BadRequest(new { error = "A valid school id is required." });
        }

        return Ok(await _financeService.GetDashboardAsync(schoolId, cancellationToken));
    }

    [HttpGet("learners/{learnerId:int}/ledger")]
    [ProducesResponseType(typeof(IReadOnlyCollection<StudentLedgerEntryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<StudentLedgerEntryDto>>> GetLedger(
        int schoolId,
        int learnerId,
        CancellationToken cancellationToken)
    {
        if (!TryGetRequestUserId(out _))
        {
            return Forbid();
        }

        if (schoolId <= 0 || learnerId <= 0)
        {
            return BadRequest(new { error = "A valid school and learner id are required." });
        }

        return Ok(await _financeService.GetLedgerAsync(schoolId, learnerId, cancellationToken));
    }

    [HttpGet("fee-structures")]
    [ProducesResponseType(typeof(IReadOnlyCollection<FeeStructureDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<FeeStructureDto>>> GetFeeStructures(
        int schoolId,
        CancellationToken cancellationToken)
    {
        if (!TryGetRequestUserId(out _))
        {
            return Forbid();
        }

        if (schoolId <= 0)
        {
            return BadRequest(new { error = "A valid school id is required." });
        }

        return Ok(await _financeService.GetFeeStructuresAsync(schoolId, cancellationToken));
    }

    [HttpPost("fee-structures")]
    [ProducesResponseType(typeof(FeeStructureDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<FeeStructureDto>> CreateFeeStructure(
        int schoolId,
        [FromBody] FeeStructureUpsertRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetRequestUserId(out var userId))
        {
            return Forbid();
        }

        var validationError = ValidateFeeStructure(schoolId, request);
        if (validationError is not null)
        {
            return BadRequest(new { error = validationError });
        }

        try
        {
            var feeStructure = await _financeService.CreateFeeStructureAsync(schoolId, request, userId, cancellationToken);
            return CreatedAtAction(nameof(GetFeeStructures), new { schoolId }, feeStructure);
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            return Conflict(new { error = "A fee structure already exists for this grade, term, year, and category." });
        }
    }

    [HttpPost("learners/{learnerId:int}/transactions")]
    [ProducesResponseType(typeof(StudentLedgerEntryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StudentLedgerEntryDto>> CreateTransaction(
        int schoolId,
        int learnerId,
        [FromBody] LedgerTransactionUpsertRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetRequestUserId(out var userId))
        {
            return Forbid();
        }

        var validationError = ValidateLedgerRequest(schoolId, request, learnerId);
        if (validationError is not null)
        {
            return BadRequest(new { error = validationError });
        }

        var entry = await _financeService.CreateTransactionAsync(schoolId, learnerId, request, userId, cancellationToken);
        return entry is null ? NotFound() : StatusCode(StatusCodes.Status201Created, entry);
    }

    [HttpPut("transactions/{transactionId:int}")]
    [ProducesResponseType(typeof(StudentLedgerEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StudentLedgerEntryDto>> UpdateTransaction(
        int schoolId,
        int transactionId,
        [FromBody] LedgerTransactionUpsertRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetRequestUserId(out var userId))
        {
            return Forbid();
        }

        var validationError = ValidateLedgerRequest(schoolId, request);
        if (validationError is not null)
        {
            return BadRequest(new { error = validationError });
        }

        if (transactionId <= 0)
        {
            return BadRequest(new { error = "A valid transaction id is required." });
        }

        var entry = await _financeService.UpdateTransactionAsync(schoolId, transactionId, request, userId, cancellationToken);
        return entry is null ? NotFound() : Ok(entry);
    }

    [HttpPost("transactions/{transactionId:int}/void")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> VoidTransaction(int schoolId, int transactionId, CancellationToken cancellationToken)
    {
        if (!TryGetRequestUserId(out var userId))
        {
            return Forbid();
        }

        if (schoolId <= 0 || transactionId <= 0)
        {
            return BadRequest(new { error = "A valid school and transaction id are required." });
        }

        return await _financeService.VoidTransactionAsync(schoolId, transactionId, userId, cancellationToken)
            ? NoContent()
            : NotFound();
    }

    [HttpPost("invoices")]
    [ProducesResponseType(typeof(BulkInvoiceResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BulkInvoiceResultDto>> GenerateInvoices(
        int schoolId,
        [FromBody] BulkInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetRequestUserId(out var userId))
        {
            return Forbid();
        }

        if (schoolId <= 0 || request is null || request.FeeStructureId <= 0)
        {
            return BadRequest(new { error = "A valid school and fee structure are required." });
        }

        var result = await _financeService.GenerateInvoicesAsync(schoolId, request.FeeStructureId, userId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    private bool TryGetRequestUserId(out int userId)
    {
        userId = 0;
        return Request.Headers.TryGetValue("X-User-Role", out var roleHeader)
            && AllowedRoles.Contains(roleHeader.FirstOrDefault() ?? string.Empty)
            && Request.Headers.TryGetValue("X-User-Id", out var userIdHeader)
            && int.TryParse(userIdHeader.FirstOrDefault(), out userId)
            && userId > 0;
    }

    private static string? ValidateFeeStructure(int schoolId, FeeStructureUpsertRequest? request)
    {
        if (schoolId <= 0 || request is null)
        {
            return "A valid school and fee structure are required.";
        }

        if (string.IsNullOrWhiteSpace(request.Grade)
            || string.IsNullOrWhiteSpace(request.Term)
            || string.IsNullOrWhiteSpace(request.Category)
            || string.IsNullOrWhiteSpace(request.Description))
        {
            return "Grade, term, category, and description are required.";
        }

        if (request.Grade.Length > 100 || request.Term.Length > 40
            || request.Category.Length > 80 || request.Description.Length > 240)
        {
            return "Fee structure text fields exceed their maximum lengths.";
        }

        if (request.AcademicYear is < 2000 or > 2100 || request.Amount <= 0 || request.Amount > 9999999999999999.99m)
        {
            return "Provide a valid academic year and a positive fee amount.";
        }

        return null;
    }

    private static string? ValidateLedgerRequest(int schoolId, LedgerTransactionUpsertRequest? request, int? learnerId = null)
    {
        if (schoolId <= 0 || learnerId is <= 0 || request is null)
        {
            return "A valid school, learner, and transaction are required.";
        }

        if (!request.Type.Equals("DEBIT", StringComparison.OrdinalIgnoreCase)
            && !request.Type.Equals("CREDIT", StringComparison.OrdinalIgnoreCase))
        {
            return "Transaction type must be DEBIT or CREDIT.";
        }

        if (string.IsNullOrWhiteSpace(request.Category)
            || request.Amount <= 0
            || request.Amount > 9999999999999999.99m
            || request.Date == default)
        {
            return "A valid date, category, and positive transaction amount are required.";
        }

        if (request.Notes?.Length > 500 || request.ReferenceNumber?.Length > 80)
        {
            return "Notes must be 500 characters or fewer and references must be 80 characters or fewer.";
        }

        return null;
    }
}
