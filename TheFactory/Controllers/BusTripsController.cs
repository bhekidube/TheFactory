using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using TheFactory.Models;

[ApiController]
[Route("api/[controller]")]
public class BusTripsController : ControllerBase
{
    private readonly AppDbContext _context;

    public BusTripsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public ActionResult<IEnumerable<BusTrip>> GetBusTrips()
    {
        return _context.BusTrips.ToList();
    }
}