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
        using (var cmd = new SqlCommand(@"
        SELECT 
            L.LocationID, 
            L.Name + ' ('+ T.Name +')' AS Location,  
            T.Name AS Town, 
            C.Name AS Country
        FROM [Location] L
        INNER JOIN [Town] T ON T.TownId = L.TownId
        INNER JOIN [Country] C ON C.CountryId = L.CountryId", conn))
        using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                locations.Add(new {
                    LocationID = reader.GetInt32(0),
                    Location = reader.GetString(1),
                    Town = reader.GetString(2),
                    Country = reader.GetString(3)
                });
            }
        }
        return Ok(locations);
    }

    [HttpGet("GetAllOperators")]
    public async Task<IActionResult> GetAllOperators()
    {
        var operators = new List<object>();
        using (var conn = await _sqlService.GetSqlConnectionAsync())
        using (var cmd = new SqlCommand(@"
            SELECT OperatorId, Name
            FROM [Operator]
            ORDER BY Name
        ", conn))
        using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                operators.Add(new {
                    OperatorId = reader.GetInt32(0),
                    Name = reader.GetString(1)
                });
            }
        }
        return Ok(operators);
    }
}