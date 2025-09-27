using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using System.Data;

[ApiController]
[Route("api/[controller]")]
public class BusTripsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public BusTripsController(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    // [HttpGet]
    // public async Task<ActionResult<IEnumerable<Route>>> GetBusTrips()
    // {
    //     // var conn = await UseSqlConnection();
    //     // var command = new SqlCommand("SELECT * FROM BusTrips", conn);
    //     // var reader = await command.ExecuteReaderAsync();
    //     // var trips = new List<Route>();
    //     // while (await reader.ReadAsync())
    //     // {
    //     //     trips.Add(new Route
    //     //     {
    //     //         Operator = reader.GetString(0),
    //     //         From = reader.GetString(1),
    //     //         To = reader.GetString(2),
    //     //         DepartureDateTime = reader.GetString(3),
    //     //         ArrivalDateTime = reader.GetString(4)
    //     //     });
    //     // }
    //     // await conn.CloseAsync();
    //     // return trips;


    //     // var accessToken = await GetAccessToken();
    //     // UseSqlConnection();////// TESTING
    //     // var conn = _context.Database.GetDbConnection();
    //     // if (conn is SqlConnection sqlConn)
    //     // {
    //     //     sqlConn.AccessToken = accessToken;
    //     // }


    //     // await conn.CloseAsync();
    //     // return trips;


    //     // var accessToken = await GetAccessToken();       
    //     // UseSqlConnection();////// TESTING
    //     // var conn = _context.Database.GetDbConnection();
    //     // if (conn is SqlConnection sqlConn)
    //     // {
    //     //     sqlConn.AccessToken = accessToken;
    //     // }

    //     // // Use async EF Core call
    //     // var trips = await new Task<ActionResult<IEnumerable<Route>>>().tolistasync;
    //     // return trips;

    //     // return await new Task<ActionResult<IEnumerable<Route>>>();

    // }

    private async Task<string> GetAccessToken()
    {
        var tenantId = "00159d0c-a64d-4739-91a6-442253d89666";
        var clientId = "3e58a20c-ffee-49ff-8be0-7156d4fb1deb";
        var clientSecret = _configuration["AzureSqlClientSecret"]; // Fetched securely from Key Vault
        var sqlResource = "https://database.windows.net/";

        var app = ConfidentialClientApplicationBuilder.Create(clientId)
            .WithClientSecret(clientSecret)
            .WithAuthority(new Uri($"https://login.microsoftonline.com/{tenantId}"))
            .Build();

        var result = await app.AcquireTokenForClient(new[] { sqlResource + "/.default" }).ExecuteAsync();
        return result.AccessToken;
    }

    private async Task<SqlConnection> GetSqlConnection()
    {
        //var accessToken = await GetAccessToken();
        var connectionString = _configuration.GetConnectionString("AzureSqlDb");
        var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();
        return conn;
    }
    public static DataTable GetTableFromProcedure(string tableName, SqlConnection connection)
    {

        // Create a new DataTable to hold the results
        DataTable dataTable = new DataTable();

        using (connection)
        {
            // Open the connection
            connection.Open();

            // Create a command object for the stored procedure
            using (SqlCommand command = new SqlCommand("dbo.DynamicSelectFromTable", connection))
            {
                // Indicate that the command type is a stored procedure
                command.CommandType = CommandType.StoredProcedure;

                // Add the parameter for the table name
                command.Parameters.Add(new SqlParameter("@TableName", SqlDbType.NVarChar, 128));
                command.Parameters["@TableName"].Value = tableName;

                // Use a SqlDataAdapter to fill the DataTable
                using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                {
                    adapter.Fill(dataTable);
                }
            }
        }
        return dataTable; 
    }

    // public DataTable GetTripsFromStoredProcedure(SqlConnection connection)
    // {
    //     DataTable dataTable = new DataTable();

    //     using (SqlCommand command = new SqlCommand("dbo.GetEmployeesAndProjectsByDepartment", connection))
    //     {
    //         command.CommandType = CommandType.StoredProcedure;
    //         command.Parameters.AddWithValue("@DepartmentName", departmentName);

    //         using (SqlDataAdapter adapter = new SqlDataAdapter(command))
    //         {
    //             connection.Open();
    //             adapter.Fill(dataTable);
    //         }
    //     }
    //     return dataTable;
    // }


    [HttpPost("InsertRoute")]
    public async Task<IActionResult> InsertRoute([FromBody] RouteInsertModel model)
    {
        try
        {
            using (var conn = await GetSqlConnection())
            using (var command = new SqlCommand("dbo.InsertRoute", conn))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@OperatorId", model.OperatorId);
                command.Parameters.AddWithValue("@FromId", model.FromId);
                command.Parameters.AddWithValue("@ToId", model.ToId);
                command.Parameters.AddWithValue("@CreatedBy", model.CreatedBy);

                var result = await command.ExecuteNonQueryAsync();
                return Ok(new { RowsAffected = result });
            }
        }
        catch (SqlException ex)
        {
            // Log exception as needed
            return StatusCode(500, new { error = "A database error occurred.", details = ex.Message });
        }
        catch (Exception ex)
        {
            // Log exception as needed
            return StatusCode(500, new { error = "An unexpected error occurred.", details = ex.Message });
        }
    }

    [HttpGet("whoami")]
    public async Task<IActionResult> WhoAmI()
    {
        using (var conn = await GetSqlConnection())
        using (var command = new SqlCommand("SELECT SUSER_SNAME()", conn))
        {
            var user = (string)await command.ExecuteScalarAsync();
            return Ok(new { SqlUser = user });
        }
    }
}