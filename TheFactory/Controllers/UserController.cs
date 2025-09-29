using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;

public class UserRegisterModel
{
    public int UserRoleId { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string CellPhoneNo { get; set; }
    public string AlternateCellPhoneNo { get; set; }
    public string Password { get; set; }
}

public class UserLoginModel
{
    public string Email { get; set; }
    public string Password { get; set; }
}

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly SqlConnectionService _sqlService;

    public UserController(SqlConnectionService sqlService)
    {
        _sqlService = sqlService;
    }

    [HttpPost("Register")]
    public async Task<IActionResult> Register([FromBody] UserRegisterModel model)
    {
        var salt = Guid.NewGuid().ToString("N").Substring(0, 32);
        var passwordHash = HashPassword(model.Password, salt);

        try
        {
            using (var conn = await _sqlService.GetSqlConnectionAsync())
            using (var command = new SqlCommand(@"
                INSERT INTO [User] 
                    (UserRoleId, Name, Email, CellPhoneNo, AlternateCellPhoneNo, PasswordHash, Salt, CreatedAt)
                VALUES
                    (@UserRoleId, @Name, @Email, @CellPhoneNo, @AlternateCellPhoneNo, @PasswordHash, @Salt, SYSUTCDATETIME());
                SELECT SCOPE_IDENTITY();
            ", conn))
            {
                command.Parameters.AddWithValue("@UserRoleId", model.UserRoleId);
                command.Parameters.AddWithValue("@Name", model.Name);
                command.Parameters.AddWithValue("@Email", model.Email ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@CellPhoneNo", model.CellPhoneNo ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@AlternateCellPhoneNo", model.AlternateCellPhoneNo ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@PasswordHash", passwordHash);
                command.Parameters.AddWithValue("@Salt", salt);

                var newId = await command.ExecuteScalarAsync();
                return Ok(new { UserId = Convert.ToInt32(newId) });
            }
        }
        catch (SqlException ex)
        {
            return StatusCode(500, new { error = "A database error occurred.", details = ex.InnerException?.Message ?? ex.Message });
        }
        catch (Exception ex)
        {
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
                SELECT PasswordHash, Salt FROM [User] WHERE Email = @Email
            ", conn))
            {
                command.Parameters.AddWithValue("@Email", model.Email);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (!reader.HasRows)
                        return Unauthorized(new { error = "Invalid credentials." });

                    await reader.ReadAsync();
                    var storedHash = reader.GetString(0);
                    var salt = reader.GetString(1);

                    var inputHash = HashPassword(model.Password, salt);

                    if (storedHash == inputHash)
                    {
                        reader.Close(); // Close reader before executing another command

                        using (var updateCmd = new SqlCommand(@"
                            UPDATE [User] SET LastLogin = SYSUTCDATETIME() WHERE Email = @Email
                        ", conn))
                        {
                            updateCmd.Parameters.AddWithValue("@Email", model.Email);
                            await updateCmd.ExecuteNonQueryAsync();
                        }

                        return Ok(new { message = "Login successful.", userName = model.Email });
                    }
                    else
                    {
                        return Unauthorized(new { error = "Invalid credentials." });
                    }
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

    private static string HashPassword(string password, string salt)
    {
        using (var sha256 = SHA256.Create())
        {
            var combined = Encoding.UTF8.GetBytes(password + salt);
            var hash = sha256.ComputeHash(combined);
            return Convert.ToBase64String(hash);
        }
    }
}