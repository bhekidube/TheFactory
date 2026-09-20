using TheFactory.Contracts;

namespace TheFactory.Services;

public interface IStaffService
{
    Task<IReadOnlyCollection<StaffDto>> GetStaffAsync(
        int schoolId,
        string? search,
        string? status,
        CancellationToken cancellationToken = default);

    Task<StaffDto?> GetStaffByIdAsync(int schoolId, int staffId, CancellationToken cancellationToken = default);
    Task<StaffDto> CreateStaffAsync(int schoolId, StaffUpsertRequest request, CancellationToken cancellationToken = default);
    Task<StaffDto?> UpdateStaffAsync(int schoolId, int staffId, StaffUpsertRequest request, CancellationToken cancellationToken = default);
    Task<bool> ArchiveStaffAsync(int schoolId, int staffId, CancellationToken cancellationToken = default);
}