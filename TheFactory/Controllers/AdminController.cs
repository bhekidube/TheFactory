using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;

public class AdminSummary
{
    public int TotalUsers { get; set; }
    public int TotalOperators { get; set; }
    public int TotalRoutes { get; set; }
    public int TotalTickets { get; set; }
    public string UserRole { get; set; }
}

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly SqlConnectionService _sqlService;

    public AdminController(SqlConnectionService sqlService)
    {
        _sqlService = sqlService;
    }

    [HttpGet("Summary")]
    public async Task<IActionResult> GetSummary()
    {
        var summary = new AdminSummary();

        using (var conn = await _sqlService.GetSqlConnectionAsync())
        {
            using (var cmd = new SqlCommand("SELECT COUNT(*) FROM [User]", conn))
                summary.TotalUsers = (int)await cmd.ExecuteScalarAsync();

            using (var cmd = new SqlCommand("SELECT Name FROM [UserRole] WHERE UserRoleId = 2", conn))
                summary.UserRole = (string)await cmd.ExecuteScalarAsync();

            using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Operator", conn))
                summary.TotalOperators = (int)await cmd.ExecuteScalarAsync();

            using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Route", conn))
                summary.TotalRoutes = (int)await cmd.ExecuteScalarAsync();

            using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Ticket", conn))
                summary.TotalTickets = (int)await cmd.ExecuteScalarAsync();

        }

        return Ok(summary);
    }
}