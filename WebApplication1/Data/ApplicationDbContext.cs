using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<ObstacleData> Obstacles { get; set; } = null!;
        public DbSet<ObstacleCategory> ObstacleCategories { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // Viktig for Identity

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
