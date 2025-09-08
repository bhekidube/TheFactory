using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using TheFactory.Models;

[ApiController]
[Route("api/[controller]")]
public class LocationController : ControllerBase
{
    private readonly AppDbContext _context;

    public LocationController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public ActionResult<IEnumerable<Location>> GetLocations()
    {
        return _context.Locations.ToList();
    }
}