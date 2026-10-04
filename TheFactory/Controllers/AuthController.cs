using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using TheFactory.Services;

/// <summary>Payload for requesting a password-reset email.</summary>
public sealed record ForgotPasswordRequest
{
    [Required, EmailAddress, MaxLength(100)]
    public required string Email { get; init; }
}

/// <summary>Payload for setting a new password with a reset token.</summary>
public sealed record ResetPasswordRequest
{
    [Required, EmailAddress, MaxLength(100)]
    public required string Email { get; init; }

    [Required, MaxLength(256)]
    public required string Token { get; init; }

    [Required, MinLength(8), MaxLength(256)]
    public required string NewPassword { get; init; }
}

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private static readonly TimeSpan ResetTokenLifetime = TimeSpan.FromMinutes(30);
    private const string GenericForgotPasswordMessage =
        "If an account exists for that email address, a password reset link will be sent.";

    private readonly IPasswordResetService _passwordResetService;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IPasswordResetService passwordResetService,
        IEmailSender emailSender,
        IConfiguration configuration,
        ILogger<AuthController> logger)
    {
        _passwordResetService = passwordResetService;
        _emailSender = emailSender;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>Requests a password-reset link without disclosing whether an account exists.</summary>
    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var resetPageUrl = _configuration["PasswordReset:PageUrl"];
        if (!Uri.TryCreate(resetPageUrl, UriKind.Absolute, out var resetPageUri)
            || (resetPageUri.Scheme != Uri.UriSchemeHttps && resetPageUri.Scheme != Uri.UriSchemeHttp))
        {
            throw new InvalidOperationException(
                "PasswordReset:PageUrl must be configured as an absolute HTTP or HTTPS URL.");
        }

        var resetToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        var expiresAt = DateTimeOffset.UtcNow.Add(ResetTokenLifetime);

        if (await _passwordResetService.SaveResetTokenAsync(
                request.Email,
                resetToken,
                expiresAt,
                cancellationToken))
        {
            var resetUrl = BuildResetUrl(resetPageUri, request.Email, resetToken);
            var safeResetUrl = WebUtility.HtmlEncode(resetUrl);
            var body = $"""
                <p>We received a request to reset your password.</p>
                <p><a href="{safeResetUrl}">Reset your password</a></p>
                <p>This link expires in 30 minutes. If you did not request this, you can ignore this email.</p>
                """;

            try
            {
                await _emailSender.SendAsync(
                    request.Email,
                    "Reset your password",
                    body,
                    cancellationToken);
            }
            catch (SmtpException exception)
            {
                _logger.LogError(exception, "Failed to send a password-reset email.");
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { message = "The password-reset email could not be sent." });
            }
        }

        return Ok(new { message = GenericForgotPasswordMessage });
    }

    /// <summary>Validates a password-reset token and changes the user's password.</summary>
    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        if (!await _passwordResetService.ValidateResetTokenAsync(
                request.Email,
                request.Token,
                cancellationToken))
        {
            return BadRequest(new { message = "The password-reset token is invalid or expired." });
        }

        var salt = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        var passwordHash = UserController.HashPassword(request.NewPassword, salt);
        var updated = await _passwordResetService.UpdatePasswordAndClearTokenAsync(
            request.Email,
            request.Token,
            passwordHash,
            salt,
            cancellationToken);

        if (!updated)
        {
            return BadRequest(new { message = "The password-reset token is invalid or expired." });
        }

        return Ok(new { message = "Password has been reset successfully." });
    }

    private static string BuildResetUrl(Uri resetPageUri, string email, string token)
    {
        var builder = new UriBuilder(resetPageUri);
        var existingQuery = builder.Query.TrimStart('?');
        var resetParameters =
            $"token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(email)}";
        builder.Query = string.IsNullOrEmpty(existingQuery)
            ? resetParameters
            : $"{existingQuery}&{resetParameters}";
        return builder.Uri.AbsoluteUri;
    }
}
