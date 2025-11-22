using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using WebApplication1.Data;
using WebApplication1.Models;
using Xunit;

namespace WebApplication1.Tests.Integration
{
    /// <summary>
    /// Authorization-related tests for ObstacleController.
    /// Verifies that a user cannot edit an obstacle owned by someone else.
    /// </summary>
    public class AuthTests
    {
        [Fact]
        public async Task Get_AdminEndpoint_WithoutOwnership_ReturnsForbid()
        {
            var dbName = "AuthTest_Db";
            var factory = new IntegrationFactory();
            var provider = factory.CreateServiceProvider(dbName);
            var db = provider.GetRequiredService<ApplicationDbContext>();

            // Seed DB with an obstacle owned by a DIFFERENT user
            var obstacle = new ObstacleData
            {
                ObstacleName = "Owned by someone else",
                ObstacleDescription = "Testing ownership",
                ReportedByUserId = "other-user-id",
                ReportedAt = DateTime.UtcNow,
                DateData = DateTime.UtcNow,
                Latitude = 60,
                Longitude = 10,
                Status = ReportStatus.NotApproved,
                CategoryId = 1
            };

            db.Obstacles.Add(obstacle);
            await db.SaveChangesAsync();

            // Create controller
            var controller = factory.CreateObstacleController(provider);

            // Override user on the controller to be "test-user-id"
            controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(
                        new ClaimsIdentity(
                            new[]
                            {
                                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                                new Claim(ClaimTypes.Name, "testuser"),
                                new Claim(ClaimTypes.Role, "Pilot")
                            },
                            "TestAuth"))
                }
            };

            // Build form collection for editing (id points to obstacle owned by other-user-id)
            var dict = new Dictionary<string, StringValues>
            {
                { "ObstacleName", "Trying to edit someone else's obstacle" },
                { "ObstacleDescription", "Should not be allowed to change" },
                { "Latitude", "61.0" },
                { "Longitude", "11.0" },
                { "CategoryId", "1" }
            };
            var form = new FormCollection(dict);

            // Act: try to edit obstacle with id belonging to other-user-id
            var result = await controller.DataForm(form, "true", obstacle.Id);

            // Assert: controller should return Forbid()
            var forbidResult = Assert.IsType<Microsoft.AspNetCore.Mvc.ForbidResult>(result);
            Assert.NotNull(forbidResult);
        }
    }
}
