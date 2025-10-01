using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;

public class AdminSummary
{
    public int totalUsers { get; set; }
    public int totalOperators { get; set; }
    public int totalRoutes { get; set; }
    public int totalTickets { get; set; }
    public string userRole { get; set; }
    public List<string> operatorNames { get; set; } = new List<string>();
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
                summary.totalUsers = (int)await cmd.ExecuteScalarAsync();

            using (var cmd = new SqlCommand("SELECT Name FROM [UserRole] WHERE UserRoleId = 2", conn))
                summary.userRole = (string)await cmd.ExecuteScalarAsync();

            using (var cmd = new SqlCommand("SELECT Name FROM Operator", conn))
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    summary.operatorNames.Add(reader.GetString(0));
                }
            }

            using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Route", conn))
                summary.totalRoutes = (int)await cmd.ExecuteScalarAsync();

            using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Ticket", conn))
                summary.totalTickets = (int)await cmd.ExecuteScalarAsync();

            using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Operator", conn))
                summary.totalOperators = (int)await cmd.ExecuteScalarAsync();

        }

        return Ok(summary);
    }
}