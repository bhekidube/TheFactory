using Microsoft.EntityFrameworkCore;
using TheFactory.Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<BusTrip> BusTrips { get; set; }
    public DbSet<Location> Locations { get; set; }
}
