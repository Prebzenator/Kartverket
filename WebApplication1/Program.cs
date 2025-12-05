using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using WebApplication1.Data;
using WebApplication1.Models;

/// <summary>
/// Main entry point for the WebApplication1 ASP.NET Core application.
/// Configures services, middleware, and database connections.
/// </summary>


// Set default culture to en-US for consistent formatting
var defaultCulture = new CultureInfo("en-US");
CultureInfo.DefaultThreadCurrentCulture = defaultCulture;
CultureInfo.DefaultThreadCurrentUICulture = defaultCulture;

// Application builder initialization
// Create the WebApplication builder
var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// Supports both MySQL/MariaDB and SQLite(LocalDev) based on connection string
// Configure ApplicationDbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var conn = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Missing connection string 'DefaultConnection'.");

    // If EF is running locally, use localhost instead of 'db'
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
        // MySQL / MariaDB configuration
        var serverVersion = new MySqlServerVersion(new Version(11, 0));

        options.UseMySql(conn, serverVersion, mySqlOptions =>
        {
            mySqlOptions.EnableRetryOnFailure(
                maxRetryCount: 10,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);

            mySqlOptions.CommandTimeout(120);
        });
    }
});

// Configure Identity
// Sets password rules, unique email requirement, and token providers
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.User.RequireUniqueEmail = true;
})
// Use Entity Framework stores for Identity
// Give support to email confirmation and password reset tokens
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Add MVC controllers with views
builder.Services.AddControllersWithViews();

// Data Protection configuration
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/root/.aspnet/DataProtection-Keys"))
    .SetApplicationName("kartverket");

// Build the WebApplication and migrate database
var app = builder.Build();

// Apply migrations and seed data
// Includes retry logic if DB is not yet available
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
                // Apply pending migrations
                db.Database.Migrate();
                logger.LogInformation("Migrations applied successfully.");
                // Seed roles(Admin, pilot and registry administrator)
                try
                {
                    await RoleSeeder.SeedAsync(services);
                    logger.LogInformation("Role seeding completed.");
                }
                catch (Exception seedEx)
                {
                    logger.LogError(seedEx, "Role seeding failed: {Message}", seedEx.Message);
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

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Static files, routing, authentication and authorization
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapGet("/health", () => "OK");

app.Run();