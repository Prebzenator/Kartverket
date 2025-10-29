using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using WebApplication1.Data;
using System.Globalization;
using System.Threading;

/// <summary>
/// Entry point for the web application.
/// Configures services, middleware, database resilience options and launches the web server.
/// </summary>
///
/// Notes:
/// - Connection resilience is enabled for MySQL via EnableRetryOnFailure to tolerate
///   transient database startup/connectivity errors.
/// - The migration application is wrapped in a retry loop so the app will wait
///   for the database to become available during container startup.
/// - Default culture is set to en-US to keep date/number formatting consistent across environments.
/// - Connection string is read directly from configuration to support Docker environment variables.
/// </summary>

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
    var conn = configuration["ConnectionStrings:DefaultConnection"];
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
                maxRetryCount: 6,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null);

            // Increase command timeout to allow longer-running DDL during migrations
            mySqlOptions.CommandTimeout(60);
        });
    }
});

/// <summary>
/// Adds support for MVC controllers and views.
/// </summary>
builder.Services.AddControllersWithViews();

var app = builder.Build();

/// <summary>
/// Apply pending EF Core migrations in Development environment.
/// This block:
/// - Creates a scope and resolves ApplicationDbContext.
/// - Retries migration application a configurable number of times with delays.
/// - Logs progress to the console for easier debugging inside containers.
/// </summary>
try
{
    using var scope = app.Services.CreateScope();
    var env = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    Console.WriteLine("Environment: " + env.EnvironmentName);

    if (env.IsDevelopment())
    {
        Console.WriteLine("Applying migrations...");

        var attempts = 0;
        const int maxAttempts = 6;
        const int delaySeconds = 5;

        while (true)
        {
            try
            {
                db.Database.Migrate();
                Console.WriteLine("Migrations applied successfully.");
                break;
            }
            catch (Exception ex)
            {
                attempts++;
                Console.WriteLine($"Migration attempt {attempts} failed: {ex.GetType().Name} - {ex.Message}");

                if (attempts >= maxAttempts)
                {
                    Console.WriteLine("Exceeded maximum migration attempts. Throwing exception to surface startup error.");
                    throw;
                }

                Console.WriteLine($"Waiting {delaySeconds} seconds before retrying...");
                Thread.Sleep(TimeSpan.FromSeconds(delaySeconds));
            }
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine("Startup error: " + ex.GetType().Name + " - " + ex.Message);
}

/// <summary>
/// Configures middleware for error handling, static files, routing, and authorization.
/// </summary>
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
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
