using TheFactory.Contracts;

namespace TheFactory.Services;

public interface IStaffService
{
    Task<IReadOnlyCollection<StaffDto>> GetStaffAsync(
        int schoolId,
        string? search,
        string? status,
        CancellationToken cancellationToken = default);

    Task<StaffDto?> GetStaffByNationalIdAsync(int schoolId, string nationalId, CancellationToken cancellationToken = default);
    Task<StaffDto> CreateStaffAsync(int schoolId, StaffUpsertRequest request, CancellationToken cancellationToken = default);
    Task<StaffDto?> UpdateStaffAsync(int schoolId, string nationalId, StaffUpsertRequest request, CancellationToken cancellationToken = default);
    Task<StaffDto?> UpdateLegacyStaffAsync(int schoolId, int legacyId, StaffUpsertRequest request, CancellationToken cancellationToken = default);
    Task<bool> ArchiveStaffAsync(int schoolId, string nationalId, CancellationToken cancellationToken = default);
    Task<bool> ArchiveLegacyStaffAsync(int schoolId, int legacyId, CancellationToken cancellationToken = default);
}