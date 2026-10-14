using TheFactory.Contracts;

namespace TheFactory.Services;

public interface IFinanceService
{
    Task<FinanceDashboardDto> GetDashboardAsync(int schoolId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<StudentLedgerEntryDto>> GetLedgerAsync(int schoolId, int learnerId, CancellationToken cancellationToken = default);
    Task<FinanceTransactionDocumentDto?> GetTransactionDocumentAsync(int schoolId, int learnerId, int transactionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<FeeStructureDto>> GetFeeStructuresAsync(int schoolId, CancellationToken cancellationToken = default);
    Task<FeeStructureDto> CreateFeeStructureAsync(int schoolId, FeeStructureUpsertRequest request, int recordedBy, CancellationToken cancellationToken = default);
    Task<StudentLedgerEntryDto?> CreateTransactionAsync(int schoolId, int learnerId, LedgerTransactionUpsertRequest request, int recordedBy, CancellationToken cancellationToken = default);
    Task<StudentLedgerEntryDto?> UpdateTransactionAsync(int schoolId, int transactionId, LedgerTransactionUpsertRequest request, int updatedBy, CancellationToken cancellationToken = default);
    Task<bool> VoidTransactionAsync(int schoolId, int transactionId, int voidedBy, CancellationToken cancellationToken = default);
    Task<BulkInvoiceResultDto?> GenerateInvoicesAsync(int schoolId, int feeStructureId, int recordedBy, CancellationToken cancellationToken = default);
}
