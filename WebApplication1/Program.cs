using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using WebApplication1.Data;
using WebApplication1.Models;
using System.Globalization;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.Extensions;
using System.IO;

// Set default culture to en-US for consistent formatting
var defaultCulture = new CultureInfo("en-US");
CultureInfo.DefaultThreadCurrentCulture = defaultCulture;
CultureInfo.DefaultThreadCurrentUICulture = defaultCulture;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

/// <summary>
/// Register ApplicationDbContext.
/// - Automatically selects SQLite if the connection string begins with "Data Source".
/// - Otherwise configures MySQL (Pomelo) with a retry policy and extended command timeout.
/// </summary>
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    // Read connection string directly to support environment variable injection
    var conn = builder.Configuration.GetConnectionString("DefaultConnection");

    // Hvis EF kjøres lokalt, bruk localhost i stedet for 'db'
    if (AppContext.BaseDirectory.Contains("dotnet-ef"))
    {
        conn = conn.Replace("server=db", "server=localhost");
    }

    Console.WriteLine("Connection string: " + (string.IsNullOrWhiteSpace(conn) ? "<empty>" : conn));

    if (string.IsNullOrWhiteSpace(conn))
    {
        throw new InvalidOperationException("Missing or empty connection string: 'ConnectionStrings:DefaultConnection'. Check appsettings or Docker environment.");
    }

    if (conn.Trim().StartsWith("Data Source", StringComparison.OrdinalIgnoreCase))
    {
        // Local file-based database for dev/testing
        options.UseSqlite(conn);
    }
    else
    {
        // MySQL / MariaDB configuration with transient-fault handling
        var serverVersion = new MySqlServerVersion(new Version(11, 0));

        options.UseMySql(conn, serverVersion, mySqlOptions =>
        {
            // Enable retry on failure to handle transient DB startup/connectivity issues
            mySqlOptions.EnableRetryOnFailure(
                maxRetryCount: 10,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);

            // Increase command timeout to allow longer-running DDL during migrations
            mySqlOptions.CommandTimeout(120);
        });
    }
});

/// <summary>
/// Identity configuration (ApplicationUser + Roles)
/// </summary>
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.User.RequireUniqueEmail = true;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

/// <summary>
/// Adds support for MVC controllers and views.
/// </summary>
builder.Services.AddControllersWithViews();

/// <summary>
/// Data Protection configuration
/// </summary>
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/root/.aspnet/DataProtection-Keys"))
    .SetApplicationName("kartverket");


var app = builder.Build();

/// <summary>
/// Apply pending EF Core migrations in Development environment.
/// This block:
/// - Creates a scope and resolves ApplicationDbContext.
/// - Retries migration application with exponential backoff.
/// - Logs progress via the configured ILogger for easier debugging inside containers.
/// - Seeds roles and an initial admin user (development convenience).
/// </summary>
try
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var env = services.GetRequiredService<IHostEnvironment>();
    var db = services.GetRequiredService<ApplicationDbContext>();

    logger.LogInformation("Environment: {EnvironmentName}", env.EnvironmentName);

    {
        logger.LogInformation("Applying migrations...");

        int attempts = 0;
        const int maxAttempts = 12;
        int delaySeconds = 2;

        while (true)
        {
            try
            {
                db.Database.Migrate();
                logger.LogInformation("Migrations applied successfully.");

                // Seed roles & admin user (development only)
                try
                {
                    // RoleSeeder.SeedAsync expects an IServiceProvider
                    // Call synchronously to keep top-level statements simple
                    await RoleSeeder.SeedAsync(services);
                    logger.LogInformation("Role seeding completed.");
                }
                catch (Exception seedEx)
                {
                    logger.LogError(seedEx, "Role seeding failed: {Message}", seedEx.Message);
                    // don't rethrow, as seeding failure shouldn't block startup
                }

                break;
            }
            catch (Exception ex)
            {
                attempts++;
                logger.LogWarning(ex, "Migration attempt {Attempt} failed.", attempts);

                if (attempts >= maxAttempts)
                {
                    logger.LogError(ex, "Max migration attempts reached, rethrowing to fail startup.");
                    throw;
                }

                logger.LogInformation("Waiting {Delay}s before retrying migration (attempt {Attempt}/{MaxAttempts})", delaySeconds, attempts, maxAttempts);
                Thread.Sleep(TimeSpan.FromSeconds(delaySeconds));
                delaySeconds = Math.Min(delaySeconds * 2, 30);
            }
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine("Startup error: " + ex.GetType().Name + " - " + ex.Message);
    throw;
}

/// <summary>
/// Configures middleware for error handling, static files, routing, authentication and authorization.
/// </summary>
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();

// Add authentication before authorization
app.UseAuthentication();
app.UseAuthorization();

/// <summary>
/// Defines the default route pattern for MVC.
/// </summary>
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

/// <summary>
/// Simple health check endpoint to verify the app is running.
/// </summary>
app.MapGet("/health", () => "OK");

app.Run();
