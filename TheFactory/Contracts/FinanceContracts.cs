namespace TheFactory.Contracts;

public sealed class FinanceDashboardDto
{
    public decimal TotalBilled { get; set; }
    public decimal TotalCollected { get; set; }
    public decimal OutstandingBalance { get; set; }
    public IReadOnlyCollection<FinanceLearnerBalanceDto> Learners { get; set; } = Array.Empty<FinanceLearnerBalanceDto>();
}

public sealed class FinanceLearnerBalanceDto
{
    public int LearnerId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string Surname { get; set; } = string.Empty;
    public string Grade { get; set; } = string.Empty;
    public decimal Billed { get; set; }
    public decimal Collected { get; set; }
    public decimal Balance { get; set; }
}

public sealed class FeeStructureDto
{
    public int FeeStructureId { get; set; }
    public int SchoolId { get; set; }
    public string Grade { get; set; } = string.Empty;
    public string Term { get; set; } = string.Empty;
    public int AcademicYear { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public sealed class FeeStructureUpsertRequest
{
    public string Grade { get; set; } = string.Empty;
    public string Term { get; set; } = string.Empty;
    public int AcademicYear { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public sealed class StudentLedgerEntryDto
{
    public int TransactionId { get; set; }
    public int LearnerId { get; set; }
    public DateTime Date { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal RunningBalance { get; set; }
    public string? Notes { get; set; }
    public string? ReferenceNumber { get; set; }
    public int RecordedBy { get; set; }
    public bool IsVoided { get; set; }
}

public sealed class LedgerTransactionUpsertRequest
{
    public DateTime Date { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
    public string? ReferenceNumber { get; set; }
}

public sealed class BulkInvoiceRequest
{
    public int FeeStructureId { get; set; }
}

public sealed class BulkInvoiceResultDto
{
    public int InvoiceCount { get; set; }
    public int LearnerCount { get; set; }
    public string Message { get; set; } = string.Empty;
}
