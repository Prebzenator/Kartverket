using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Data
{
    /// <summary>
    /// Entity Framework Core database context for the application.
    /// Provides access to all database entities via DbSet properties.
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        /// <summary>
        /// Initializes the database context with configuration options.
        /// </summary>
        /// <param name="options">Database context options provided by the DI container.</param>
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// Represents the Obstacles table in the database.
        /// Stores user-submitted reports of physical obstacles.
        /// </summary>
        public DbSet<ObstacleData> Obstacles { get; set; } = null!;
    }
}
