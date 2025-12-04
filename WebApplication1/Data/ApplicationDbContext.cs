using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Data
{
    /// <summary>
    /// Entity Framework Core database context for the application.
    /// Extends IdentityDbContext to include ASP.NET Identity tables,
    /// and exposes DbSets for obstacle data and categories.
    /// </summary>
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
// Initialize the DbContext with options
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
// Database tables
// EF core will create these tables based on the models
        public DbSet<ObstacleData> Obstacles { get; set; } = null!;
        public DbSet<ObstacleCategory> ObstacleCategories { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
// Seed initial data for obstacle categories
            modelBuilder.Entity<ObstacleCategory>().HasData(
                new ObstacleCategory { Id = 1, Name = "Mast or Tower" },
                new ObstacleCategory { Id = 2, Name = "Power Line" },
                new ObstacleCategory { Id = 3, Name = "Construction Crane" },
                new ObstacleCategory { Id = 4, Name = "Cable" },
                new ObstacleCategory { Id = 5, Name = "Other" }
            );
        }
    }
}