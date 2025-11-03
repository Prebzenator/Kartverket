using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Data
{
    /// <summary>
    /// EF Core database context for the application.
    /// Inherits IdentityDbContext to provide AspNetUsers/AspNetRoles tables.
    /// </summary>
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// Represents the Obstacles table in the database.
        /// </summary>
        public DbSet<ObstacleData> Obstacles { get; set; } = null!;
    }
}
