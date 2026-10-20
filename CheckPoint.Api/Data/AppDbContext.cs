using CheckPoint.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CheckPoint.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<University> Universities => Set<University>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<User> Users => Set<User>();
    public DbSet<EducationalResource> EducationalResources => Set<EducationalResource>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<University>(entity =>
        {
            entity.Property(u => u.Name).IsRequired().HasMaxLength(200);
            entity.Property(u => u.City).IsRequired().HasMaxLength(100);
            entity.HasIndex(u => u.Name).IsUnique();
        });

        modelBuilder.Entity<Course>(entity =>
        {
            entity.Property(c => c.Name).IsRequired().HasMaxLength(200);
            entity.Property(c => c.Code).IsRequired().HasMaxLength(20);
            entity.HasIndex(c => new { c.UniversityId, c.Code }).IsUnique();

            entity.HasOne(c => c.University)
                  .WithMany(u => u.Courses)
                  .HasForeignKey(c => c.UniversityId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(user => user.Username).IsRequired().HasMaxLength(50);
            entity.Property(user => user.Email).IsRequired().HasMaxLength(256);
            entity.Property(user => user.PasswordHash).IsRequired().HasMaxLength(100);
            entity.Property(user => user.Role).IsRequired().HasMaxLength(20);
            entity.HasIndex(user => user.Username).IsUnique();
            entity.HasIndex(user => user.Email).IsUnique();
        });

        modelBuilder.Entity<EducationalResource>(entity =>
        {
            entity.Property(resource => resource.Title).IsRequired().HasMaxLength(200);
            entity.Property(resource => resource.Description).HasMaxLength(2000);
            entity.Property(resource => resource.OriginalFileName).IsRequired().HasMaxLength(260);
            entity.Property(resource => resource.StoredFileName).IsRequired().HasMaxLength(100);
            entity.Property(resource => resource.ContentType).IsRequired().HasMaxLength(100);
            entity.Property(resource => resource.Type).IsRequired();

            entity.HasIndex(resource => resource.UniversityId);
            entity.HasIndex(resource => resource.CourseId);
            entity.HasIndex(resource => resource.Type);
            entity.HasIndex(resource => resource.UploadedByUserId);

            entity.HasOne(resource => resource.University)
                .WithMany(university => university.Resources)
                .HasForeignKey(resource => resource.UniversityId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(resource => resource.Course)
                .WithMany(course => course.Resources)
                .HasForeignKey(resource => resource.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(resource => resource.UploadedByUser)
                .WithMany(user => user.UploadedResources)
                .HasForeignKey(resource => resource.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}