using Microsoft.Data.SqlClient;
using System.Data;

namespace TheFactory.Services;

public sealed class PasswordResetService : IPasswordResetService
{
    private readonly SqlConnectionService _sqlConnectionService;

    public PasswordResetService(SqlConnectionService sqlConnectionService)
    {
        _sqlConnectionService = sqlConnectionService;
    }

    public async Task<bool> SaveResetTokenAsync(
        string email,
        string resetToken,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(resetToken);

        using var connection = await _sqlConnectionService.GetSqlConnectionAsync(cancellationToken);
        using var command = new SqlCommand(
            @"UPDATE [User]
              SET ResetToken = @ResetToken,
                  ResetTokenExpiry = @ResetTokenExpiry
              WHERE Email = @Email;",
            connection);

        command.Parameters.Add("@Email", SqlDbType.NVarChar, 100).Value = email;
        command.Parameters.Add("@ResetToken", SqlDbType.NVarChar, 256).Value = resetToken;
        command.Parameters.Add("@ResetTokenExpiry", SqlDbType.DateTime2).Value = expiresAt.UtcDateTime;

        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> ValidateResetTokenAsync(
        string email,
        string resetToken,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(resetToken);

        using var connection = await _sqlConnectionService.GetSqlConnectionAsync(cancellationToken);
        using var command = new SqlCommand(
            @"SELECT CAST(CASE WHEN EXISTS (
                  SELECT 1
                  FROM [User]
                  WHERE Email = @Email
                    AND ResetToken = @ResetToken
                    AND ResetTokenExpiry > SYSUTCDATETIME()
              ) THEN 1 ELSE 0 END AS bit);",
            connection);

        command.Parameters.Add("@Email", SqlDbType.NVarChar, 100).Value = email;
        command.Parameters.Add("@ResetToken", SqlDbType.NVarChar, 256).Value = resetToken;

        return (bool)(await command.ExecuteScalarAsync(cancellationToken) ?? false);
    }

    public async Task<bool> UpdatePasswordAndClearTokenAsync(
        string email,
        string resetToken,
        string passwordHash,
        string salt,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(resetToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(salt);

        using var connection = await _sqlConnectionService.GetSqlConnectionAsync(cancellationToken);
        using var command = new SqlCommand(
            @"UPDATE [User]
              SET PasswordHash = @PasswordHash,
                  Salt = @Salt,
                  ResetToken = NULL,
                  ResetTokenExpiry = NULL
              WHERE Email = @Email
                AND ResetToken = @ResetToken
                AND ResetTokenExpiry > SYSUTCDATETIME();",
            connection);

        command.Parameters.Add("@Email", SqlDbType.NVarChar, 100).Value = email;
        command.Parameters.Add("@ResetToken", SqlDbType.NVarChar, 256).Value = resetToken;
        command.Parameters.Add("@PasswordHash", SqlDbType.NVarChar, 256).Value = passwordHash;
        command.Parameters.Add("@Salt", SqlDbType.NVarChar, 50).Value = salt;

        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }
}
