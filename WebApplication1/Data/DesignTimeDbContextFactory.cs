using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using WebApplication1.Data;


/// <summary>
/// Factory used by EF Core at design-time to create an ApplicationDbContext.
/// Enables migrations and other tooling to run without needing the full application.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    /// <summary>
    /// Creates a new instance of ApplicationDbContext using appsettings.json.
    /// Called automatically by EF Core tools at design-time.
    /// </summary>
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // Load configuration from appsettings.json in the current directory
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        // Build DbContext options using MySQL provider
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseMySql(
            config.GetConnectionString("DefaultConnection"),
            new MySqlServerVersion(new Version(11, 0, 0)) // Adjust version if necessary
        );

        // Return a new ApplicationDbContext with the configured options
        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
