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

    [HttpPost("InsertRouteTrip")]
    public async Task<IActionResult> InsertRouteTrip([FromBody] RouteTripInsertModel model)
    {
        try
        {
            using (var conn = await GetSqlConnection())
            using (var command = new SqlCommand(@"
                INSERT INTO [dbo].[RouteTrip] 
                    ([RouteId], [DepartureDateTime], [ArrivalDateTime], [Price], [CreatedBy], [CreatedDate], [UpdatedBy], [UpdatedDate], [Notes], [Active])
                VALUES 
                    (@RouteId, @DepartureDateTime, @ArrivalDateTime, @Price, @CreatedBy, GETDATE(), @UpdatedBy, @UpdatedDate, @Notes, @Active);
                SELECT SCOPE_IDENTITY();
            ", conn))
            {
                command.Parameters.AddWithValue("@RouteId", model.RouteId);
                command.Parameters.AddWithValue("@DepartureDateTime", model.DepartureDateTime);
                command.Parameters.AddWithValue("@ArrivalDateTime", model.ArrivalDateTime);
                command.Parameters.AddWithValue("@Price", model.Price);
                command.Parameters.AddWithValue("@CreatedBy", model.CreatedBy);
                command.Parameters.AddWithValue("@UpdatedBy", (object?)model.UpdatedBy ?? DBNull.Value);
                command.Parameters.AddWithValue("@UpdatedDate", (object?)model.UpdatedDate ?? DBNull.Value);
                command.Parameters.AddWithValue("@Notes", (object?)model.Notes ?? DBNull.Value);
                command.Parameters.AddWithValue("@Active", model.Active);

                var insertedId = Convert.ToInt32(await command.ExecuteScalarAsync());
                return Ok(new { TripId = insertedId });
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

    [HttpPut("UpdateRouteTrip")]
    public async Task<IActionResult> UpdateRouteTrip([FromBody] RouteTripUpdateModel model)
    {
        try
        {
            using (var conn = await GetSqlConnection())
            using (var command = new SqlCommand(@"
                UPDATE [dbo].[RouteTrip]
                SET
                    DepartureDateTime = @DepartureDateTime,
                    ArrivalDateTime = @ArrivalDateTime,
                    Price = @Price,
                    Notes = @Notes,
                    Active = @Active,
                    UpdatedBy = @UpdatedBy,
                    UpdatedDate = GETDATE()
                WHERE TripId = @TripId
            ", conn))
            {
                command.Parameters.AddWithValue("@TripId", model.TripId);
                command.Parameters.AddWithValue("@DepartureDateTime", model.DepartureDateTime);
                command.Parameters.AddWithValue("@ArrivalDateTime", model.ArrivalDateTime);
                command.Parameters.AddWithValue("@Price", model.Price);
                command.Parameters.AddWithValue("@Notes", (object?)model.Notes ?? DBNull.Value);
                command.Parameters.AddWithValue("@Active", model.Active);
                command.Parameters.AddWithValue("@UpdatedBy", model.UpdatedBy);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                return Ok(new { RowsAffected = rowsAffected });
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

    [HttpGet("GetTripsByRoute/{routeId}")]
    public async Task<IActionResult> GetTripsByRoute(int routeId)
    {
        try
        {
            var trips = new List<RouteTripDto>();
            using (var conn = await GetSqlConnection())
            using (var command = new SqlCommand(@"
                SELECT 
                    TripId,
                    RouteId,
                    DepartureDateTime,
                    ArrivalDateTime,
                    Price,
                    CreatedBy,
                    CreatedDate,
                    UpdatedBy,
                    UpdatedDate,
                    Notes,
                    Active
                FROM [dbo].[RouteTrip]
                WHERE RouteId = @RouteId
                ORDER BY DepartureDateTime DESC
            ", conn))
            {
                command.Parameters.AddWithValue("@RouteId", routeId);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        trips.Add(new RouteTripDto
                        {
                            TripId = reader.GetInt32(0),
                            RouteId = reader.GetInt32(1),
                            DepartureDateTime = reader.GetDateTime(2),
                            ArrivalDateTime = reader.GetDateTime(3),
                            Price = reader.GetDecimal(4),
                            CreatedBy = reader.GetInt32(5),
                            CreatedDate = reader.GetDateTime(6),
                            UpdatedBy = reader.IsDBNull(7) ? (int?)null : reader.GetInt32(7),
                            UpdatedDate = reader.IsDBNull(8) ? (DateTime?)null : reader.GetDateTime(8),
                            Notes = reader.IsDBNull(9) ? null : reader.GetString(9),
                            Active = reader.GetBoolean(10)
                        });
                    }
                }
            }
            return Ok(trips);
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

    [HttpGet("GetRouteTrips")]
    public async Task<IActionResult> GetRouteTrips([FromQuery] DateTime departureDate, [FromQuery] int fromId, [FromQuery] int toId)
    {
        try
        {
            var trips = new List<object>();
            using (var conn = await GetSqlConnection())
            using (var command = new SqlCommand(@"
                SELECT 
                    FORMAT(GETDATE(), 'MMM d, yyyy') AS FormattedDate,
                    RT.TripId,
                    RT.RouteId,
                    CASE WHEN RT.Notes LIKE '%Daily%' THEN DATEADD(SECOND, DATEDIFF(SECOND, 0, CAST(RT.DepartureDateTime AS time)), CAST(@DepartureDate AS datetime)) ELSE RT.DepartureDateTime END AS DepartureDateTime,
                    RT.ArrivalDateTime,
                    RT.Price,
                    RT.CreatedBy,
                    RT.CreatedDate,
                    RT.UpdatedBy,
                    RT.UpdatedDate,
                    RT.Notes,
                    RT.Active,
                    R.RouteId AS Route_RouteId,
                    O.OperatorId AS Operator_OperatorId,
                    O.Name AS OperatorName
                FROM [dbo].[RouteTrip] RT
                INNER JOIN [ROUTE] R ON RT.RouteId = R.RouteId
                INNER JOIN Operator O ON O.OperatorId = R.OperatorId
                INNER JOIN [Location] toL on r.ToId = toL.LocationId
                WHERE 
                (CAST(RT.DepartureDateTime AS DATE) = CAST(@DepartureDate AS DATE) OR RT.Notes LIKE '%Daily%')
                AND rt.Active = 1
                AND R.FromId = @FromId
                AND toL.TownId IN (SELECT TownId FROM LOCATION WHERE LocationId = @ToId)
                ORDER BY RT.DepartureDateTime ASC
            ", conn))
            {
                command.Parameters.AddWithValue("@DepartureDate", departureDate.Date);
                command.Parameters.AddWithValue("@FromId", fromId);
                command.Parameters.AddWithValue("@ToId", toId);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        trips.Add(new
                        {
                            FormattedDate = reader.GetString(0),
                            TripId = reader.GetInt32(1),
                            RouteId = reader.GetInt32(2),
                            DepartureDateTime = reader.GetDateTime(3),
                            ArrivalDateTime = reader.GetDateTime(4),
                            Price = reader.GetDecimal(5),
                            CreatedBy = reader.GetInt32(6),
                            CreatedDate = reader.GetDateTime(7),
                            UpdatedBy = reader.IsDBNull(8) ? (int?)null : reader.GetInt32(8),
                            UpdatedDate = reader.IsDBNull(9) ? (DateTime?)null : reader.GetDateTime(9),
                            Notes = reader.IsDBNull(10) ? null : reader.GetString(10),
                            Active = reader.GetBoolean(11),
                            Route_RouteId = reader.GetInt32(12),
                            Operator_OperatorId = reader.GetInt32(13),
                            OperatorName = reader.GetString(14)
                        });
                    }
                }
            }
            return Ok(trips);
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
}

// Place this model in the same file or in a shared models file
public class RouteTripInsertModel
{
    public int RouteId { get; set; }
    public DateTime DepartureDateTime { get; set; }
    public DateTime ArrivalDateTime { get; set; }
    public decimal Price { get; set; }
    public int CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public string? Notes { get; set; }
    public bool Active { get; set; }
}

// DTO for returning trip data
public class RouteTripDto
{
    public int TripId { get; set; }
    public int RouteId { get; set; }
    public DateTime DepartureDateTime { get; set; }
    public DateTime ArrivalDateTime { get; set; }
    public decimal Price { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public string? Notes { get; set; }
    public bool Active { get; set; }
}

// Place this model in the same file or in a shared models file
public class RouteTripUpdateModel
{
    public int TripId { get; set; }
    public DateTime DepartureDateTime { get; set; }
    public DateTime ArrivalDateTime { get; set; }
    public decimal Price { get; set; }
    public string? Notes { get; set; }
    public bool Active { get; set; }
    public int UpdatedBy { get; set; }
}