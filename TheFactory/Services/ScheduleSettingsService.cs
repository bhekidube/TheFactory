using Microsoft.Data.SqlClient;
using TheFactory.Contracts;

namespace TheFactory.Services;

public sealed class ScheduleSettingsService(SqlConnectionService sqlConnectionService) : IScheduleSettingsService
{
    public async Task<ScheduleColumnVisibilityResponse> GetColumnVisibilityAsync(
        CancellationToken cancellationToken)
    {
        using var connection = await sqlConnectionService.GetSqlConnectionAsync(cancellationToken);
        using var command = new SqlCommand(
            @"SELECT BusVisible, DepartureVisible, PriceVisible, ActionVisible
              FROM dbo.GlobalScheduleColumnSettings
              WHERE SettingId = 1;",
            connection);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return new ScheduleColumnVisibilityResponse(true, true, true, true);
        }

        return new ScheduleColumnVisibilityResponse(
            reader.GetBoolean(0),
            reader.GetBoolean(1),
            reader.GetBoolean(2),
            reader.GetBoolean(3));
    }

    public async Task UpdateColumnVisibilityAsync(
        UpdateScheduleColumnVisibilityRequest request,
        int updatedBy,
        CancellationToken cancellationToken)
    {
        using var connection = await sqlConnectionService.GetSqlConnectionAsync(cancellationToken);
        using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
        using var command = new SqlCommand(
            @"UPDATE dbo.GlobalScheduleColumnSettings WITH (UPDLOCK, SERIALIZABLE)
              SET BusVisible = @BusVisible,
                  DepartureVisible = @DepartureVisible,
                  PriceVisible = @PriceVisible,
                  ActionVisible = @ActionVisible,
                  UpdatedBy = @UpdatedBy,
                  UpdatedAtUtc = SYSUTCDATETIME()
              WHERE SettingId = 1;

              IF @@ROWCOUNT = 0
              BEGIN
                  INSERT INTO dbo.GlobalScheduleColumnSettings
                      (SettingId, BusVisible, DepartureVisible, PriceVisible, ActionVisible, UpdatedBy)
                  VALUES
                      (1, @BusVisible, @DepartureVisible, @PriceVisible, @ActionVisible, @UpdatedBy);
              END;",
            connection,
            transaction);
        command.Parameters.AddWithValue("@BusVisible", request.Bus);
        command.Parameters.AddWithValue("@DepartureVisible", request.Departure);
        command.Parameters.AddWithValue("@PriceVisible", request.Price);
        command.Parameters.AddWithValue("@ActionVisible", request.Action);
        command.Parameters.AddWithValue("@UpdatedBy", updatedBy);

        await command.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<bool> IsSystemAdminAsync(int userId, CancellationToken cancellationToken)
    {
        using var connection = await sqlConnectionService.GetSqlConnectionAsync(cancellationToken);
        using var command = new SqlCommand(
            @"SELECT CASE WHEN EXISTS (
                    SELECT 1
                    FROM dbo.[User] AS users
                    INNER JOIN dbo.UserRole AS roles
                        ON roles.UserRoleId = 1 AND roles.Name = N'SystemAdmin'
                    WHERE users.UserId = @UserId
                      AND (
                          users.UserRoleId = roles.UserRoleId
                          OR EXISTS (
                              SELECT 1
                              FROM dbo.SystemUserRole AS systemRoles
                              WHERE systemRoles.UserId = users.UserId
                                AND systemRoles.UserRoleId = roles.UserRoleId
                          )
                      )
                ) THEN 1 ELSE 0 END;",
            connection);
        command.Parameters.AddWithValue("@UserId", userId);

        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) == 1;
    }
}
