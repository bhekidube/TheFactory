using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class RouteController : ControllerBase
{
    private readonly IConfiguration _config;
    public RouteController(IConfiguration config) { _config = config; }

    [HttpGet]
    public async Task<IActionResult> Routes([FromQuery] string from, [FromQuery] string to, [FromQuery] string date)
    {
        var results = new List<RouteResult>();
        using (var conn = new SqlConnection(_config.GetConnectionString("AzureSqlDb")))
        {
            await conn.OpenAsync();
            var cmd = new SqlCommand(
                "SELECT BusName, [From], [To], Date, DepartureTime, ArrivalTime, Price FROM Route WHERE [From]=@from AND [To]=@to AND Date=@date",
                conn);
            cmd.Parameters.AddWithValue("@from", from);
            cmd.Parameters.AddWithValue("@to", to);
            cmd.Parameters.AddWithValue("@date", date);

            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    results.Add(new RouteResult
                    {
                        BusName = reader["BusName"].ToString(),
                        From = reader["From"].ToString(),
                        To = reader["To"].ToString(),
                        Date = reader["Date"].ToString(),
                        DepartureTime = reader["DepartureTime"].ToString(),
                        ArrivalTime = reader["ArrivalTime"].ToString(),
                        Price = reader.GetDecimal(reader.GetOrdinal("Price"))
                    });
                }
            }
        }
        return Ok(results);
    }

    // POST: api/route
    [HttpPost]
    public async Task<IActionResult> CreateRoute([FromBody] RouteCreateDto dto)
    {
        using (var conn = new SqlConnection(_config.GetConnectionString("AzureSqlDb")))
        {
            await conn.OpenAsync();
            var cmd = new SqlCommand(@"
                INSERT INTO Route (OperatorId, FromId, ToId, Date, DepartureTime, ArrivalTime, Price, CreatedBy, CreatedDate)
                VALUES (@OperatorId, @FromId, @ToId, @Date, @DepartureTime, @ArrivalTime, @Price, @CreatedBy, GETDATE());
            ", conn);

            cmd.Parameters.AddWithValue("@OperatorId", dto.OperatorId);
            cmd.Parameters.AddWithValue("@FromId", dto.FromId);
            cmd.Parameters.AddWithValue("@ToId", dto.ToId);
            cmd.Parameters.AddWithValue("@Date", dto.Date);
            cmd.Parameters.AddWithValue("@DepartureTime", dto.DepartureTime);
            cmd.Parameters.AddWithValue("@ArrivalTime", dto.ArrivalTime);
            cmd.Parameters.AddWithValue("@Price", dto.Price);
            cmd.Parameters.AddWithValue("@CreatedBy", dto.CreatedBy);

            await cmd.ExecuteNonQueryAsync();
        }
        return Ok(new { message = "Route created successfully." });
    }

    // PUT: api/route/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateRoute(int id, [FromBody] RouteUpdateDto dto)
    {
        using (var conn = new SqlConnection(_config.GetConnectionString("AzureSqlDb")))
        {
            await conn.OpenAsync();
            var cmd = new SqlCommand(@"
                UPDATE Route
                SET OperatorId = @OperatorId,
                    FromId = @FromId,
                    ToId = @ToId,
                    Date = @Date,
                    DepartureTime = @DepartureTime,
                    ArrivalTime = @ArrivalTime,
                    Price = @Price,
                    UpdatedBy = @UpdatedBy,
                    UpdatedDate = GETDATE()
                WHERE RouteId = @RouteId;
            ", conn);

            cmd.Parameters.AddWithValue("@OperatorId", dto.OperatorId);
            cmd.Parameters.AddWithValue("@FromId", dto.FromId);
            cmd.Parameters.AddWithValue("@ToId", dto.ToId);
            cmd.Parameters.AddWithValue("@Date", dto.Date);
            cmd.Parameters.AddWithValue("@DepartureTime", dto.DepartureTime);
            cmd.Parameters.AddWithValue("@ArrivalTime", dto.ArrivalTime);
            cmd.Parameters.AddWithValue("@Price", dto.Price);
            cmd.Parameters.AddWithValue("@UpdatedBy", dto.UpdatedBy);
            cmd.Parameters.AddWithValue("@RouteId", id);

            await cmd.ExecuteNonQueryAsync();
        }
        return Ok(new { message = "Route updated successfully." });
    }
}
// DTO for route creation
public class RouteCreateDto
{
    public int OperatorId { get; set; }
    public int FromId { get; set; }
    public int ToId { get; set; }
    public string Date { get; set; }
    public string DepartureTime { get; set; }
    public string ArrivalTime { get; set; }
    public decimal Price { get; set; }
    public int CreatedBy { get; set; }
}
// DTO for route update
public class RouteUpdateDto
{
    public int OperatorId { get; set; }
    public int FromId { get; set; }
    public int ToId { get; set; }
    public string Date { get; set; }
    public string DepartureTime { get; set; }
    public string ArrivalTime { get; set; }
    public decimal Price { get; set; }
    public int UpdatedBy { get; set; }
}