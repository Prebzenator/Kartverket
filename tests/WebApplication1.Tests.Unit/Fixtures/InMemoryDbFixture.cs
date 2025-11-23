using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;

// Produces a fresh EF Core in-memory ApplicationDbContext per test to run isolated, fast database operations.
// Tests should seed required data; fixture does not persist between tests.

namespace WebApplication1.Tests.Unit.Fixtures
{
    public class InMemoryDbFixture : IDisposable
    {
        public ApplicationDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var ctx = new ApplicationDbContext(options);
            ctx.Database.EnsureCreated();
            return ctx;
        }

        public void Dispose() { }
    }
}
