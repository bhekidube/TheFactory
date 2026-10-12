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

    public UserController(SqlConnectionService sqlService, IConfiguration configuration)
    {
        _sqlService = sqlService;
        _configuration = configuration;
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

                    return Ok(new { message = "Login successful.", userName, userRole, userId, operatorId, address });
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

        if (googleUser.EmailVerified != true || string.IsNullOrWhiteSpace(googleUser.Email))
        {
            return Unauthorized(new { error = "A verified Google email address is required." });
        }

        try
        {
            using (var conn = await _sqlService.GetSqlConnectionAsync())
            using (var command = new SqlCommand(@"
                SELECT u.UserId, u.Name, ur.Name AS UserRole, OU.OperatorId,
                       u.AddressId, a.AddressLine1, a.AddressLine2, a.City, a.ProvinceState, a.PostalCode, a.Country
                FROM [User] u
                    INNER JOIN OperatorUser OU ON OU.UserId = u.UserId
                    INNER JOIN OperatorUserRole OUR ON OU.OperatorUserId = OUR.OperatorUserId
                    INNER JOIN [UserRole] ur ON ur.UserRoleId = OUR.UserRoleId
                    LEFT JOIN institution.Addresses a ON a.AddressId = u.AddressId
                WHERE u.Email = @Email
                UNION
                SELECT u.UserId, u.Name, ur.Name AS UserRole, 0,
                       u.AddressId, a.AddressLine1, a.AddressLine2, a.City, a.ProvinceState, a.PostalCode, a.Country
                FROM [User] u
                    INNER JOIN SystemUserRole SUR ON SUR.UserId = u.UserId
                    INNER JOIN [UserRole] ur ON ur.UserRoleId = SUR.UserRoleId
                    LEFT JOIN institution.Addresses a ON a.AddressId = u.AddressId
                WHERE u.Email = @Email
            ", conn))
            {
                command.Parameters.Add("@Email", SqlDbType.NVarChar, 100).Value = googleUser.Email;

                int userId;
                string userName;
                string userRole;
                int operatorId;
                AddressDto address;
                using (var reader = await command.ExecuteReaderAsync(HttpContext.RequestAborted))
                {
                    if (!reader.HasRows)
                    {
                        return Unauthorized(new
                        {
                            error = "No HambaOnline account is linked to this Google email address."
                        });
                    }

                    await reader.ReadAsync(HttpContext.RequestAborted);
                    userId = reader.GetInt32(0);
                    userName = reader.GetString(1);
                    userRole = reader.GetString(2);
                    operatorId = reader.GetInt32(3);
                    address = AddressPersistence.ReadAddress(reader, 4);
                }

                using (var updateCmd = new SqlCommand(
                    "UPDATE [User] SET LastLogin = SYSUTCDATETIME() WHERE Email = @Email",
                    conn))
                {
                    updateCmd.Parameters.Add("@Email", SqlDbType.NVarChar, 100).Value = googleUser.Email;
                    await updateCmd.ExecuteNonQueryAsync(HttpContext.RequestAborted);
                }

                return Ok(new { message = "Login successful.", userName, userRole, userId, operatorId, address });
            }
        }
        catch (SqlException)
        {
            return StatusCode(500, new { error = "A database error occurred while signing in." });
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