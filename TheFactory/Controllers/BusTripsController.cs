using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TheFactory.Models;

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

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BusTrip>>> GetBusTrips()
    {
        var accessToken = await GetAccessToken();

        var conn = _context.Database.GetDbConnection();
        if (conn is SqlConnection sqlConn)
        {
            sqlConn.AccessToken = accessToken;
        }

        // Use async EF Core call
        var trips = await _context.BusTrips.ToListAsync();
        return trips;
    }

    private async Task<string> GetAccessToken()
    {
        var tenantId = "00159d0c-a64d-4739-91a6-442253d89666";
        var clientId = "3e58a20c-ffee-49ff-8be0-7156d4fb1deb";
        var clientSecret = "REMOVED_SECRET";
        var sqlResource = "https://database.windows.net/";

        var app = ConfidentialClientApplicationBuilder.Create(clientId)
            .WithClientSecret(clientSecret)
            .WithAuthority(new Uri($"https://login.microsoftonline.com/{tenantId}"))
            .Build();

        var result = await app.AcquireTokenForClient(new[] { sqlResource + "/.default" }).ExecuteAsync();
        return result.AccessToken;
    }

    private async Task UseSqlConnection()
    {
        var accessToken = await GetAccessToken();
        var connectionString = _configuration.GetConnectionString("AzureSqlDb");
        using (var conn = new SqlConnection(connectionString))
        {
            conn.AccessToken = accessToken;
            conn.Open();
            // ... use your connection ...
        }
    }
}