using Microsoft.EntityFrameworkCore;
using TheFactory.Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<BusTrip> BusTrips { get; set; }
    public DbSet<Location> Locations { get; set; }
    public DbSet<Mark> Marks { get; set; }
    public DbSet<Subject> Subjects { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Mark>(entity =>
        {
            entity.ToTable("Mark", "institution");
            entity.HasKey(mark => mark.Id);
            entity.Property(mark => mark.TeacherComments);
            entity.HasOne(mark => mark.Subject)
                .WithMany(subject => subject.Marks)
                .HasForeignKey(mark => mark.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Subject>(entity =>
        {
            entity.ToTable("Subject", "institution");
            entity.HasKey(subject => subject.Id);
            entity.Property(subject => subject.Name).HasMaxLength(200);
            entity.Property(subject => subject.Code).HasMaxLength(50);
        });
    }
}
