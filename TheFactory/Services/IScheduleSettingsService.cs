using TheFactory.Contracts;

namespace TheFactory.Services;

public interface IScheduleSettingsService
{
    Task<ScheduleColumnVisibilityResponse> GetColumnVisibilityAsync(CancellationToken cancellationToken);
    Task UpdateColumnVisibilityAsync(
        UpdateScheduleColumnVisibilityRequest request,
        int updatedBy,
        CancellationToken cancellationToken);
    Task<bool> IsSystemAdminAsync(int userId, CancellationToken cancellationToken);
}
