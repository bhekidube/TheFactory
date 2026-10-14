using Microsoft.EntityFrameworkCore;
using TheFactory.Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<BusTrip> BusTrips { get; set; }
    public DbSet<Location> Locations { get; set; }
    public DbSet<Mark> Marks { get; set; }
    public DbSet<Subject> Subjects { get; set; }
    public DbSet<AttendanceSession> AttendanceSessions { get; set; }
    public DbSet<AttendanceRecord> AttendanceRecords { get; set; }

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

        modelBuilder.Entity<AttendanceSession>(entity =>
        {
            entity.ToTable("AttendanceSessions", "institution");
            entity.HasKey(session => session.SessionID);
            entity.Property(session => session.SessionID).ValueGeneratedOnAdd();
            entity.Property(session => session.SessionDate).HasColumnType("date");
            entity.HasIndex(session => new { session.ClassID, session.SessionDate }).IsUnique();
            entity.HasOne<AttendanceClassReference>()
                .WithMany()
                .HasForeignKey(session => session.ClassID)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AttendanceRecord>(entity =>
        {
            entity.ToTable("AttendanceRecords", "institution");
            entity.HasKey(record => record.RecordID);
            entity.Property(record => record.RecordID).ValueGeneratedOnAdd();
            entity.Property(record => record.Status).HasMaxLength(10).IsRequired();
            entity.HasIndex(record => new { record.SessionID, record.LearnerID }).IsUnique();
            entity.HasOne(record => record.Session)
                .WithMany(session => session.Records)
                .HasForeignKey(record => record.SessionID)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<AttendanceLearnerReference>()
                .WithMany()
                .HasForeignKey(record => record.LearnerID)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AttendanceClassReference>(entity =>
        {
            entity.ToTable("Class", "institution");
            entity.HasKey(item => item.Id);
        });

        modelBuilder.Entity<AttendanceLearnerReference>(entity =>
        {
            entity.ToTable("Learner", "institution");
            entity.HasKey(item => item.Id);
        });
    }
}
