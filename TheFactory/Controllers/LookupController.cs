using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

[ApiController]
[Route("api/[controller]")]
public class LookupController : ControllerBase
{
    private readonly SqlConnectionService _sqlService;

    public LookupController(SqlConnectionService sqlService)
    {
        _sqlService = sqlService;
    }

    [HttpGet("Locations")]
    public async Task<IActionResult> GetLocations()
    {
        var locations = new List<object>();
        using (var conn = await _sqlService.GetSqlConnectionAsync())
        using (var cmd = new SqlCommand("SELECT LocationID, Name FROM [Location]", conn))
        using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                locations.Add(new {
                    LocationID = reader.GetInt32(0),
                    Name = reader.GetString(1)
                });
            }
        }
        return Ok(locations);
    }
}