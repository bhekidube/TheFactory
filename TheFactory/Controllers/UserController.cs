using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Google.Apis.Auth;
using System.Security.Cryptography;
using System.Text;
using System.Data;
using System.ComponentModel.DataAnnotations;
using TheFactory.Contracts;
using TheFactory.Services;

public class UserRegisterModel
{
    public int userRoleId { get; set; }
    public required string name { get; set; }
    public string? email { get; set; }
    public string cellPhoneNo { get; set; }
    public string alternateCellPhoneNo { get; set; }
    public string password { get; set; }
    public AddressDto address { get; set; } = new();
    public int operatorId { get; set; } // <-- Add this property
}

public class UserLoginModel
{
    public string Email { get; set; }
    public string Password { get; set; }
}

public sealed record GoogleLoginModel
{
    [Required]
    public required string Credential { get; init; }
}

public sealed class GoogleRegistrationCompletionModel
{
    [Required]
    public required string Credential { get; set; }

    [Range(1, int.MaxValue)]
    public int OperatorId { get; set; }

    [Required]
    [RegularExpression(@"^\+[1-9]\d{1,14}$")]
    public required string CellPhoneNo { get; set; }

    [Required]
    [RegularExpression(@"^\+[1-9]\d{1,14}$")]
    public required string AlternateCellPhoneNo { get; set; }
}

public sealed class GoogleProfileCompletionModel
{
    [Required]
    public required string Credential { get; set; }

    [Required]
    [RegularExpression(@"^\+[1-9]\d{1,14}$")]
    public required string CellPhoneNo { get; set; }
}

public class ChangeUserRoleModel
{
    public string Email { get; set; }
    public int NewUserRoleId { get; set; }
}

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly SqlConnectionService _sqlService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<UserController> _logger;
    private readonly IAuthTokenService _authTokenService;

    public UserController(
        SqlConnectionService sqlService,
        IConfiguration configuration,
        ILogger<UserController> logger,
        IAuthTokenService authTokenService)
    {
        _sqlService = sqlService;
        _configuration = configuration;
        _logger = logger;
        _authTokenService = authTokenService;
    }

    [HttpPost("Register")]
    public async Task<IActionResult> Register([FromBody] UserRegisterModel model)
    {
        var salt = Guid.NewGuid().ToString("N").Substring(0, 32);
        var passwordHash = HashPassword(model.password, salt);

        SqlTransaction transaction = null;

        try
        {
            using (var conn = await _sqlService.GetSqlConnectionAsync())
            {
                transaction = conn.BeginTransaction();
                var addressId = await AddressPersistence.GetOrCreateAddressIdAsync(
                    conn,
                    transaction,
                    model.address,
                    HttpContext.RequestAborted);

                // 1. Insert User
                int newUserId;
                using (var command = new SqlCommand(@"
                    INSERT INTO [User] 
                        (UserRoleId, Name, Email, CellPhoneNo, AlternateCellPhoneNo, AddressId, PasswordHash, Salt, CreatedAt)
                    VALUES
                        (@UserRoleId, @Name, @Email, @CellPhoneNo, @AlternateCellPhoneNo, @AddressId, @PasswordHash, @Salt, SYSUTCDATETIME());
                    SELECT SCOPE_IDENTITY();
                ", conn, transaction))
                {
                    command.Parameters.AddWithValue("@UserRoleId", model.userRoleId);
                    command.Parameters.AddWithValue("@Name", model.name);
                    command.Parameters.AddWithValue("@Email", model.email ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@CellPhoneNo", model.cellPhoneNo ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@AlternateCellPhoneNo", model.alternateCellPhoneNo ?? (object)DBNull.Value);
                    command.Parameters.Add("@AddressId", SqlDbType.Int).Value = (object?)addressId ?? DBNull.Value;
                    command.Parameters.AddWithValue("@PasswordHash", passwordHash);
                    command.Parameters.AddWithValue("@Salt", salt);

                    newUserId = Convert.ToInt32(await command.ExecuteScalarAsync());
                }

                // 2. Insert into OperatorUser
                int operatorUserId;
                using (var command = new SqlCommand(@"
                    INSERT INTO [OperatorUser] (UserId, OperatorId)
                    VALUES (@UserId, @OperatorId);
                    SELECT SCOPE_IDENTITY();
                ", conn, transaction))
                {
                    command.Parameters.AddWithValue("@UserId", newUserId);
                    command.Parameters.AddWithValue("@OperatorId", model.operatorId); // <-- Add this line
                    operatorUserId = Convert.ToInt32(await command.ExecuteScalarAsync());
                }

                // 3. Insert into OperatorUserRole
                using (var command = new SqlCommand(@"
                    INSERT INTO [OperatorUserRole] (UserRoleId, OperatorUserId)
                    VALUES (@UserRoleId, @OperatorUserId);
                ", conn, transaction))
                {
                    command.Parameters.AddWithValue("@UserRoleId", model.userRoleId);
                    command.Parameters.AddWithValue("@OperatorUserId", operatorUserId);
                    await command.ExecuteNonQueryAsync();
                }

                transaction.Commit();

                return Ok(new { UserId = newUserId, OperatorUserId = operatorUserId });
            }
        }
        catch (SqlException ex)
        {
            // 2627 = Unique constraint error, 2601 = Duplicated key row error
            if (ex.Number == 2627 || ex.Number == 2601)
            {
                return BadRequest(new { error = "A user with this email already exists. Please use a different email address." });
            }
            return StatusCode(500, new { error = ex.Message.ToString(), details = ex.InnerException?.Message ?? ex.Message });
        }
        catch (Exception ex)
        {
            transaction?.Rollback();
            return StatusCode(500, new { error = "An unexpected error occurred.", details = ex.InnerException?.Message ?? ex.Message });
        }
    }

    [HttpPost("Login")]
    public async Task<IActionResult> Login([FromBody] UserLoginModel model)
    {
        try
        {
            using (var conn = await _sqlService.GetSqlConnectionAsync())
            using (var command = new SqlCommand(@"
                SELECT u.UserId, u.Name, u.PasswordHash, u.Salt, ur.Name AS UserRole, OU.OperatorId,
                       u.AddressId, a.AddressLine1, a.AddressLine2, a.City, a.ProvinceState, a.PostalCode, a.Country
                FROM [User] u
                    INNER JOIN OperatorUser OU ON OU.UserId = U.UserId
                    INNER JOIN OperatorUserRole OUR ON OU.OperatorUserId = OUR.OperatorUserId
                    INNER JOIN [UserRole] ur ON ur.UserRoleId = OUR.UserRoleId
                    LEFT JOIN institution.Addresses a ON a.AddressId = u.AddressId
                WHERE u.Email = @Email
                UNION
                SELECT u.UserId, u.Name, u.PasswordHash, u.Salt, UR.Name AS UserRole,0,
                       u.AddressId, a.AddressLine1, a.AddressLine2, a.City, a.ProvinceState, a.PostalCode, a.Country
                FROM [User] u
                    INNER JOIN SYSTEMUSERROLE SUR ON U.UserId = SUR.UserId
                    INNER JOIN [UserRole] ur ON uR.UserRoleId = SUR.UserRoleId
                    LEFT JOIN institution.Addresses a ON a.AddressId = u.AddressId
                WHERE u.Email = @Email
            ", conn))
            {
                command.Parameters.AddWithValue("@Email", model.Email);

                int userId = 0;
                string userName = null, storedHash = null, salt = null, userRole = null;
                int operatorId = 0;
                AddressDto address;

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (!reader.HasRows)
                        return Unauthorized(new { error = "Invalid credentials." });

                    await reader.ReadAsync();
                    userId = reader.GetInt32(0);
                    userName = reader.GetString(1);
                    storedHash = reader.GetString(2);
                    salt = reader.GetString(3);
                    userRole = reader.GetString(4);
                    operatorId = reader.GetInt32(5);
                    address = AddressPersistence.ReadAddress(reader, 6);
                } // reader is disposed and closed here

                var inputHash = HashPassword(model.Password, salt);

                if (storedHash == inputHash)
                {
                    using (var updateCmd = new SqlCommand(@"
                        UPDATE [User] SET LastLogin = SYSUTCDATETIME() WHERE Email = @Email
                    ", conn))
                    {
                        updateCmd.Parameters.AddWithValue("@Email", model.Email);
                        await updateCmd.ExecuteNonQueryAsync();
                    }

                    var token = _authTokenService.CreateToken(userId, model.Email, userName, userRole, operatorId);
                    return Ok(new { message = "Login successful.", token, userName, userRole, userId, operatorId, address });
                }
                else
                {
                    return Unauthorized(new { error = "Invalid credentials." });
                }
            }
        }
        catch (SqlException ex)
        {
            return StatusCode(500, new { error = "A database error occurred.", details = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An unexpected error occurred.", details = ex.Message });
        }
    }

    [HttpPost("GoogleLogin")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginModel model)
    {
        var clientId = _configuration["Google:ClientId"];
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new { error = "Google sign-in is not configured on the server." });
        }

        GoogleJsonWebSignature.Payload googleUser;
        try
        {
            googleUser = await GoogleJsonWebSignature.ValidateAsync(
                model.Credential,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { clientId }
                });
        }
        catch (InvalidJwtException)
        {
            return Unauthorized(new { error = "The Google credential is invalid or expired." });
        }

        if (googleUser.EmailVerified != true ||
            string.IsNullOrWhiteSpace(googleUser.Email) ||
            string.IsNullOrWhiteSpace(googleUser.Subject))
        {
            return Unauthorized(new { error = "A verified Google email and subject are required." });
        }

        try
        {
            using var connection = await _sqlService.GetSqlConnectionAsync();
            var userId = 0;
            var userName = string.Empty;
            string? storedEmail = null;
            string? cellPhoneNo = null;
            string? googleSubject = null;

            using (var subjectCommand = new SqlCommand(
                @"SELECT TOP 1 UserId, Name, Email, CellPhoneNo, GoogleSubject
                  FROM dbo.[User]
                  WHERE GoogleSubject = @GoogleSubject;",
                connection))
            {
                subjectCommand.Parameters.Add("@GoogleSubject", SqlDbType.NVarChar, 255).Value = googleUser.Subject;
                using var reader = await subjectCommand.ExecuteReaderAsync(HttpContext.RequestAborted);
                if (await reader.ReadAsync(HttpContext.RequestAborted))
                {
                    userId = reader.GetInt32(0);
                    userName = reader.GetString(1);
                    storedEmail = reader.IsDBNull(2) ? null : reader.GetString(2);
                    cellPhoneNo = reader.IsDBNull(3) ? null : reader.GetString(3);
                    googleSubject = reader.IsDBNull(4) ? null : reader.GetString(4);
                }
            }

            if (userId == 0)
            {
                using var emailCommand = new SqlCommand(
                    @"SELECT TOP 1 UserId, Name, Email, CellPhoneNo, GoogleSubject
                      FROM dbo.[User]
                      WHERE Email = @Email;",
                    connection);
                emailCommand.Parameters.Add("@Email", SqlDbType.NVarChar, 100).Value = googleUser.Email;
                using var reader = await emailCommand.ExecuteReaderAsync(HttpContext.RequestAborted);
                if (await reader.ReadAsync(HttpContext.RequestAborted))
                {
                    userId = reader.GetInt32(0);
                    userName = reader.GetString(1);
                    storedEmail = reader.IsDBNull(2) ? null : reader.GetString(2);
                    cellPhoneNo = reader.IsDBNull(3) ? null : reader.GetString(3);
                    googleSubject = reader.IsDBNull(4) ? null : reader.GetString(4);
                }
            }

            if (userId == 0)
            {
                if (googleUser.Email.Length > 100)
                {
                    return BadRequest(new { error = "The Google email address exceeds the account limit." });
                }

                var (firstName, lastName) = GetGoogleNames(googleUser);
                return Ok(new
                {
                    isNewUser = true,
                    email = googleUser.Email,
                    firstName,
                    lastName,
                    pictureUrl = googleUser.Picture
                });
            }

            if (googleSubject is not null && !string.Equals(googleSubject, googleUser.Subject, StringComparison.Ordinal))
            {
                return Conflict(new { error = "This email is already linked to a different Google account." });
            }

            if (googleSubject is null)
            {
                using var linkCommand = new SqlCommand(
                    @"UPDATE dbo.[User]
                      SET GoogleSubject = @GoogleSubject
                      WHERE UserId = @UserId AND GoogleSubject IS NULL;",
                    connection);
                linkCommand.Parameters.Add("@GoogleSubject", SqlDbType.NVarChar, 255).Value = googleUser.Subject;
                linkCommand.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                if (await linkCommand.ExecuteNonQueryAsync(HttpContext.RequestAborted) == 0)
                {
                    return Conflict(new { error = "This Google account could not be linked to the existing account." });
                }
            }

            string? userRole = null;
            var operatorId = 0;
            AddressDto address = new();
            using (var accountCommand = new SqlCommand(
                @"SELECT TOP 1 ur.Name, COALESCE(OU.OperatorId, 0),
                         a.AddressId, a.AddressLine1, a.AddressLine2, a.City, a.ProvinceState, a.PostalCode, a.Country
                  FROM dbo.[User] u
                  OUTER APPLY (
                      SELECT TOP 1 ou.OperatorId, our.UserRoleId
                      FROM dbo.OperatorUser ou
                      INNER JOIN dbo.OperatorUserRole our ON our.OperatorUserId = ou.OperatorUserId
                      WHERE ou.UserId = u.UserId
                      ORDER BY ou.OperatorId
                  ) OU
                  OUTER APPLY (
                      SELECT TOP 1 sur.UserRoleId
                      FROM dbo.SystemUserRole sur
                      WHERE sur.UserId = u.UserId
                  ) SUR
                  INNER JOIN dbo.[UserRole] ur ON ur.UserRoleId = COALESCE(OU.UserRoleId, SUR.UserRoleId)
                  LEFT JOIN institution.Addresses a ON a.AddressId = u.AddressId
                  WHERE u.UserId = @UserId;",
                connection))
            {
                accountCommand.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                using var reader = await accountCommand.ExecuteReaderAsync(HttpContext.RequestAborted);
                if (!await reader.ReadAsync(HttpContext.RequestAborted))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new { error = "The HambaOnline account has no assigned role." });
                }

                userRole = reader.GetString(0);
                operatorId = reader.GetInt32(1);
                address = AddressPersistence.ReadAddress(reader, 2);
            }

            using (var updateCmd = new SqlCommand(
                "UPDATE dbo.[User] SET LastLogin = SYSUTCDATETIME() WHERE UserId = @UserId",
                connection))
            {
                updateCmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                await updateCmd.ExecuteNonQueryAsync(HttpContext.RequestAborted);
            }

            var token = _authTokenService.CreateToken(userId, storedEmail ?? googleUser.Email, userName, userRole!, operatorId);
            return Ok(new { message = "Login successful.", isNewUser = false, token, userName, userRole, userId, operatorId, cellPhoneNo, address });
        }
        catch (SqlException)
        {
            return StatusCode(500, new { error = "A database error occurred while signing in." });
        }
    }

    [HttpPost("CompleteGoogleRegistration")]
    public async Task<IActionResult> CompleteGoogleRegistration([FromBody] GoogleRegistrationCompletionModel model)
    {
        var clientId = _configuration["Google:ClientId"];
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "Google sign-in is not configured on the server." });
        }

        GoogleJsonWebSignature.Payload googleUser;
        try
        {
            googleUser = await GoogleJsonWebSignature.ValidateAsync(
                model.Credential,
                new GoogleJsonWebSignature.ValidationSettings { Audience = new[] { clientId } });
        }
        catch (InvalidJwtException)
        {
            return Unauthorized(new { error = "The Google credential is invalid or expired." });
        }

        if (googleUser.EmailVerified != true || string.IsNullOrWhiteSpace(googleUser.Email) || string.IsNullOrWhiteSpace(googleUser.Subject))
        {
            return Unauthorized(new { error = "A verified Google email and subject are required." });
        }

        if (googleUser.Email.Length > 100 ||
            !IsValidInternationalPhone(model.CellPhoneNo) ||
            !IsValidInternationalPhone(model.AlternateCellPhoneNo))
        {
            return BadRequest(new
            {
                error = "The Google email is too long or the primary and alternate phone numbers are invalid."
            });
        }

        try
        {
            using var connection = await _sqlService.GetSqlConnectionAsync();
            using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
            int customerRoleId;
            using (var roleCommand = new SqlCommand(
                "SELECT UserRoleId FROM dbo.UserRole WHERE Name = N'Customer';",
                connection,
                transaction))
            {
                var roleValue = await roleCommand.ExecuteScalarAsync(HttpContext.RequestAborted);
                if (roleValue is null || roleValue is DBNull)
                {
                    await transaction.RollbackAsync(HttpContext.RequestAborted);
                    return StatusCode(StatusCodes.Status500InternalServerError, new { error = "The Customer role is not configured." });
                }

                customerRoleId = Convert.ToInt32(roleValue);
            }

            using (var operatorCommand = new SqlCommand(
                "SELECT COUNT(1) FROM dbo.Operator WHERE OperatorId = @OperatorId;",
                connection,
                transaction))
            {
                operatorCommand.Parameters.Add("@OperatorId", SqlDbType.Int).Value = model.OperatorId;
                if (Convert.ToInt32(await operatorCommand.ExecuteScalarAsync(HttpContext.RequestAborted)) == 0)
                {
                    await transaction.RollbackAsync(HttpContext.RequestAborted);
                    return BadRequest(new { error = "The selected operator does not exist." });
                }
            }

            using (var existingUserCommand = new SqlCommand(
                @"SELECT TOP 1 GoogleSubject
                  FROM dbo.[User] WITH (UPDLOCK, HOLDLOCK)
                  WHERE Email = @Email OR GoogleSubject = @GoogleSubject;",
                connection,
                transaction))
            {
                existingUserCommand.Parameters.Add("@Email", SqlDbType.NVarChar, 100).Value = googleUser.Email;
                existingUserCommand.Parameters.Add("@GoogleSubject", SqlDbType.NVarChar, 255).Value = googleUser.Subject;
                var existingSubject = await existingUserCommand.ExecuteScalarAsync(HttpContext.RequestAborted);
                if (existingSubject is not null)
                {
                    await transaction.RollbackAsync(HttpContext.RequestAborted);
                    if (existingSubject is DBNull || string.Equals(Convert.ToString(existingSubject), googleUser.Subject, StringComparison.Ordinal))
                    {
                        return await GoogleLogin(new GoogleLoginModel { Credential = model.Credential });
                    }

                    return Conflict(new { error = "This email is already linked to a different Google account." });
                }
            }

            var (firstName, lastName) = GetGoogleNames(googleUser);
            var userName = string.Join(' ', new[] { firstName, lastName }.Where(part => !string.IsNullOrWhiteSpace(part)));
            if (string.IsNullOrWhiteSpace(userName))
            {
                userName = googleUser.Email.Split('@')[0];
            }
            if (userName.Length > 100)
            {
                userName = userName[..100];
            }

            var salt = Guid.NewGuid().ToString("N");
            var unusablePassword = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
            var passwordHash = HashPassword(unusablePassword, salt);
            int userId;
            using (var insertUserCommand = new SqlCommand(
                @"INSERT INTO dbo.[User]
                                        (UserRoleId, Name, Email, CellPhoneNo, AlternateCellPhoneNo, PasswordHash, Salt, CreatedAt, LastLogin, GoogleSubject)
                  VALUES
                                        (@UserRoleId, @Name, @Email, @CellPhoneNo, @AlternateCellPhoneNo, @PasswordHash, @Salt, SYSUTCDATETIME(), SYSUTCDATETIME(), @GoogleSubject);
                  SELECT CONVERT(INT, SCOPE_IDENTITY());",
                connection,
                transaction))
            {
                insertUserCommand.Parameters.Add("@UserRoleId", SqlDbType.Int).Value = customerRoleId;
                insertUserCommand.Parameters.Add("@Name", SqlDbType.NVarChar, 100).Value = userName;
                insertUserCommand.Parameters.Add("@Email", SqlDbType.NVarChar, 100).Value = googleUser.Email;
                insertUserCommand.Parameters.Add("@CellPhoneNo", SqlDbType.NVarChar, 30).Value = model.CellPhoneNo!;
                insertUserCommand.Parameters.Add("@AlternateCellPhoneNo", SqlDbType.NVarChar, 30).Value = model.AlternateCellPhoneNo!;
                insertUserCommand.Parameters.Add("@PasswordHash", SqlDbType.NVarChar, 256).Value = passwordHash;
                insertUserCommand.Parameters.Add("@Salt", SqlDbType.NVarChar, 50).Value = salt;
                insertUserCommand.Parameters.Add("@GoogleSubject", SqlDbType.NVarChar, 255).Value = googleUser.Subject;
                userId = Convert.ToInt32(await insertUserCommand.ExecuteScalarAsync(HttpContext.RequestAborted));
            }

            int operatorUserId;
            using (var insertOperatorUserCommand = new SqlCommand(
                @"INSERT INTO dbo.OperatorUser (UserId, OperatorId)
                  VALUES (@UserId, @OperatorId);
                  SELECT CONVERT(INT, SCOPE_IDENTITY());",
                connection,
                transaction))
            {
                insertOperatorUserCommand.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                insertOperatorUserCommand.Parameters.Add("@OperatorId", SqlDbType.Int).Value = model.OperatorId;
                operatorUserId = Convert.ToInt32(await insertOperatorUserCommand.ExecuteScalarAsync(HttpContext.RequestAborted));
            }

            using (var insertRoleCommand = new SqlCommand(
                @"INSERT INTO dbo.OperatorUserRole (UserRoleId, OperatorUserId)
                  VALUES (@UserRoleId, @OperatorUserId);",
                connection,
                transaction))
            {
                insertRoleCommand.Parameters.Add("@UserRoleId", SqlDbType.Int).Value = customerRoleId;
                insertRoleCommand.Parameters.Add("@OperatorUserId", SqlDbType.Int).Value = operatorUserId;
                await insertRoleCommand.ExecuteNonQueryAsync(HttpContext.RequestAborted);
            }

            await transaction.CommitAsync(HttpContext.RequestAborted);
            var token = _authTokenService.CreateToken(userId, googleUser.Email, userName, "Customer", model.OperatorId);
            return Ok(new
            {
                message = "Google account registered successfully.",
                token,
                userName,
                userRole = "Customer",
                userId,
                operatorId = model.OperatorId,
                cellPhoneNo = model.CellPhoneNo
            });
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627)
        {
            _logger.LogInformation(exception, "Google registration raced with another account creation for {Email}.", googleUser.Email);
            return Conflict(new { error = "An account with this Google email already exists. Sign in to continue." });
        }
    }

    private static (string FirstName, string LastName) GetGoogleNames(GoogleJsonWebSignature.Payload googleUser)
    {
        var firstName = googleUser.GivenName?.Trim() ?? string.Empty;
        var lastName = googleUser.FamilyName?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(googleUser.Name))
        {
            var nameParts = googleUser.Name.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            firstName = nameParts.FirstOrDefault() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(lastName) && nameParts.Length > 1)
            {
                lastName = nameParts[1];
            }
        }

        return (firstName, lastName);
    }

    private static bool IsValidInternationalPhone(string? phoneNumber)
    {
        return !string.IsNullOrWhiteSpace(phoneNumber) &&
            System.Text.RegularExpressions.Regex.IsMatch(phoneNumber, @"^\+[1-9]\d{1,14}$");
    }

    [HttpPost("CompleteGoogleProfile")]
    public async Task<IActionResult> CompleteGoogleProfile([FromBody] GoogleProfileCompletionModel model)
    {
        var clientId = _configuration["Google:ClientId"];
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new { error = "Google sign-in is not configured on the server." });
        }

        GoogleJsonWebSignature.Payload googleUser;
        try
        {
            googleUser = await GoogleJsonWebSignature.ValidateAsync(
                model.Credential,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { clientId }
                });
        }
        catch (InvalidJwtException)
        {
            return Unauthorized(new { error = "The Google credential is invalid or expired." });
        }

        if (googleUser.EmailVerified != true || string.IsNullOrWhiteSpace(googleUser.Email))
        {
            return Unauthorized(new { error = "A verified Google email address is required." });
        }

        try
        {
            using var conn = await _sqlService.GetSqlConnectionAsync();
            using var command = new SqlCommand(
                "UPDATE [User] SET CellPhoneNo = @CellPhoneNo WHERE Email = @Email",
                conn);
            command.Parameters.Add("@CellPhoneNo", SqlDbType.NVarChar, 30).Value = model.CellPhoneNo;
            command.Parameters.Add("@Email", SqlDbType.NVarChar, 100).Value = googleUser.Email;

            if (await command.ExecuteNonQueryAsync(HttpContext.RequestAborted) == 0)
            {
                return NotFound(new { error = "No HambaOnline account is linked to this Google email address." });
            }

            return Ok(new { cellPhoneNo = model.CellPhoneNo });
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Database error while completing a Google-authenticated user profile.");
            return StatusCode(500, new { error = "A database error occurred while saving your cellphone number." });
        }
    }

    [HttpPost("ChangeUserRole")]
    public async Task<IActionResult> ChangeUserRole([FromBody] ChangeUserRoleModel model)
    {
        // Optionally: Check if the current user is an admin (implement your own authorization logic here)

        try
        {
            using (var conn = await _sqlService.GetSqlConnectionAsync())
            using (var command = new SqlCommand(@"
                UPDATE OPR
                SET OPR.UserRoleId = @NewUserRoleId
                FROM OperatorUserRole OPR
                INNER JOIN OperatorUser OU ON OPR.OperatorUserId = OU.OperatorUserId
                INNER JOIN [User] U ON OU.UserId = U.UserId
                WHERE U.Email = @Email
            ", conn))
            {
                command.Parameters.AddWithValue("@NewUserRoleId", model.NewUserRoleId);
                command.Parameters.AddWithValue("@Email", model.Email);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                if (rowsAffected > 0)
                {
                    return Ok(new { message = "User role updated successfully." });
                }
                else
                {
                    return NotFound(new { error = "User not found." });
                }
            }
        }
        catch (SqlException ex)
        {
            return StatusCode(500, new { error = "A database error occurred.", details = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An unexpected error occurred.", details = ex.Message });
        }
    }

    internal static string HashPassword(string password, string salt)
    {
        using (var sha256 = SHA256.Create())
        {
            var combined = Encoding.UTF8.GetBytes(password + salt);
            var hash = sha256.ComputeHash(combined);
            return Convert.ToBase64String(hash);
        }
    }
}

// Each controller action creates its own SqlConnection instance.
// All SqlDataReader objects are disposed before executing further commands on the same connection.
// Do not share SqlConnection or SqlDataReader instances across requests or threads.