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
}