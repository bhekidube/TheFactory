namespace TheFactory.Services;

public interface IPasswordResetService
{
    Task<bool> SaveResetTokenAsync(
        string email,
        string resetToken,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default);

    Task<bool> ValidateResetTokenAsync(
        string email,
        string resetToken,
        CancellationToken cancellationToken = default);

    Task<bool> UpdatePasswordAndClearTokenAsync(
        string email,
        string resetToken,
        string passwordHash,
        string salt,
        CancellationToken cancellationToken = default);
}
