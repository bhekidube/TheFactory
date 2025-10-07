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

public class OperatorAdminSummary
{
    public int OperatorId { get; set; }
    public string OperatorName { get; set; }
    public List<RouteSummary> Routes { get; set; } = new List<RouteSummary>();
}

public class RouteSummary
{
    public int RouteId { get; set; }
    public string OperatorType { get; set; }
    public string OperatorName { get; set; }
    public string FromLocation { get; set; }
    public string ToLocation { get; set; }
    public string Creator { get; set; }
    public DateTime CreatedDate { get; set; }
    public bool Active { get; set; }
    public int OperatorId { get; set; }
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

    [HttpGet("OperatorSummary")]
    public async Task<IActionResult> GetOperatorSummary(string operatorName)
    {
        var operatorAdminSummary = new OperatorAdminSummary();

        using (var conn = await _sqlService.GetSqlConnectionAsync())
        {
            using (var cmd = new SqlCommand("SELECT Name, OperatorId FROM [Operator] WHERE Name = @OperatorName", conn))
            {
                cmd.Parameters.AddWithValue("@OperatorName", operatorName);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        operatorAdminSummary.OperatorName = reader.GetString(0);
                        operatorAdminSummary.OperatorId = reader.GetInt32(1);
                    }
                }
            }

            using (var cmd = new SqlCommand(@"SELECT        
    r.RouteId,
    OT.Name AS OperatorType,
    O.Name AS OperatorName,
    frm.Name AS FromLocation,
    LTo.Name AS ToLocation,
    crtr.Name AS Creator,
    r.CreatedDate,
    r.Active,
    O.OperatorId
    FROM [dbo].[Route] r
    INNER JOIN Operator O ON r.OperatorId = O.OperatorId
    INNER JOIN [Location] frm ON frm.LocationId = r.FromId
    INNER JOIN [Location] LTo ON LTo.LocationId = r.ToId
    INNER JOIN [User] crtr ON crtr.UserId = r.CreatedBy
    INNER JOIN [OperatorType] OT ON OT.OperatorTypeId = O.OperatorTypeId
    WHERE O.Name = @OperatorName", conn))
            {
                cmd.Parameters.AddWithValue("@OperatorName", operatorName);
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    var routes = new List<RouteSummary>();
                    while (await reader.ReadAsync())
                    {
                        routes.Add(new RouteSummary
                        {
                            RouteId = reader.GetInt32(0),
                            OperatorType = reader.GetString(1),
                            OperatorName = reader.GetString(2),
                            FromLocation = reader.GetString(3),
                            ToLocation = reader.GetString(4),
                            Creator = reader.GetString(5),
                            CreatedDate = reader.GetDateTime(6),
                            Active = reader.GetBoolean(7),
                            OperatorId = reader.GetInt32(8)
                        });
                    }
                    operatorAdminSummary.Routes = routes;
                }
            }




        }
        return Ok(operatorAdminSummary);
    }



    
}

