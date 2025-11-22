using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using WebApplication1.Data;
using WebApplication1.Models;
using Xunit;

namespace WebApplication1.Tests.Integration
{
    public class ObstacleE2ETests
    {
        [Fact]
        public async Task DataForm_Post_Draft_CreatesObstacleInDatabase()
        {
            var dbName = "ObstacleE2E_Db";

            // Create ONE provider (important!)
            var factory = new IntegrationFactory();
            var provider = factory.CreateServiceProvider(dbName);

            var db = provider.GetRequiredService<ApplicationDbContext>();

            // Seed category so CategoryId = 1 is valid
            db.ObstacleCategories.Add(new ObstacleCategory
            {
                Id = 1,
                Name = "TestCategory"
            });

            db.SaveChanges();

            // Create controller using the SAME provider
            var controller = factory.CreateObstacleController(provider);

            // Build form data
            var form = new FormCollection(new Dictionary<string, StringValues>
            {
                { "ObstacleName", "Test obstacle from E2E" },
                { "IsDraft", "true" },
                { "HeightInputRaw", "100" },
                { "ObstacleDescription", "E2E test obstacle" },
                { "Latitude", "60.0" },
                { "Longitude", "10.0" },
                { "CategoryId", "1" }
            });

            // Act
            var result = await controller.DataForm(form, "true", null);

            // Assert: check using SAME DB instance
            var obstacleInDb = db.Obstacles
                .SingleOrDefault(o => o.ObstacleName == "Test obstacle from E2E");

            Assert.NotNull(obstacleInDb);
            Assert.Equal("E2E test obstacle", obstacleInDb.ObstacleDescription);
            Assert.Equal(ReportStatus.NotApproved, obstacleInDb.Status);
        }
    }
}
