using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using WebApplication1.Controllers;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Tests.Unit.Fixtures;
using WebApplication1.Tests.Unit.Helpers;
using Xunit;

// Controller unit tests for DataForm that use an in-memory DbContext, a mocked UserManager, and a minimal RequestServices.
// Verifies create-as-draft, invalid model handling, and authorization behavior without starting the web host.

namespace WebApplication1.Tests.Unit.Controllers
{
    public class ObstacleControllerTests
    {
        private static ClaimsPrincipal CreateClaimsPrincipal(string userId, string role = "")
        {
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };
            if (!string.IsNullOrEmpty(role)) claims.Add(new Claim(ClaimTypes.Role, role));
            return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        }

        // Helper to configure minimal RequestServices for controller
        private static void ConfigureRequestServices(Controller controller)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddControllers();
            services.AddSingleton<ITempDataProvider, TestTempDataProvider>();
            services.AddSingleton<ITempDataDictionaryFactory, TempDataDictionaryFactory>();
            controller.ControllerContext.HttpContext.RequestServices = services.BuildServiceProvider();
        }

        [Fact]
        public async Task DataForm_Post_CreateNewReport_AsDraft_SavesToDb()
        {
            using var fixture = new InMemoryDbFixture();
            await using var ctx = fixture.CreateContext();

            var userMgrMock = MockHelpers.CreateUserManagerMock<ApplicationUser>();
            var testUser = new ApplicationUser { Id = "user-1" };
            userMgrMock.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(testUser);

            var controller = new ObstacleController(ctx, userMgrMock.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = CreateClaimsPrincipal(testUser.Id) }
            };
            ConfigureRequestServices(controller);

            var formDict = new Dictionary<string, StringValues>
            {
                ["ObstacleName"] = new StringValues("New Obstacle"),
                ["HeightInputRaw"] = new StringValues("10"),
                ["ObstacleDescription"] = new StringValues("desc"),
                ["Latitude"] = new StringValues("59.9"),
                ["Longitude"] = new StringValues("10.8"),
                ["CategoryId"] = new StringValues("1")
            };
            var form = new FormCollection(formDict);

            var result = await controller.DataForm(form, "true", null);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Overview", viewResult.ViewName);

            Assert.Single(ctx.Obstacles);
            var saved = ctx.Obstacles.First();
            Assert.Equal("New Obstacle", saved.ObstacleName);
            Assert.Equal(ReportStatus.NotApproved, saved.Status);
        }

        [Fact]
        public async Task DataForm_Post_Submit_InvalidModel_ReturnsViewAndDoesNotSave()
        {
            using var fixture = new InMemoryDbFixture();
            await using var ctx = fixture.CreateContext();

            var userMgrMock = MockHelpers.CreateUserManagerMock<ApplicationUser>();
            var testUser = new ApplicationUser { Id = "user-2" };
            userMgrMock.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(testUser);

            var controller = new ObstacleController(ctx, userMgrMock.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = CreateClaimsPrincipal(testUser.Id) }
            };
            ConfigureRequestServices(controller);

            var formDict = new Dictionary<string, StringValues>
            {
                ["HeightInputRaw"] = StringValues.Empty,
                ["ObstacleDescription"] = StringValues.Empty
            };
            var form = new FormCollection(formDict);

            var result = await controller.DataForm(form, "false", null);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.IsType<ObstacleData>(viewResult.Model);
            Assert.Empty(ctx.Obstacles);
        }

        [Fact]
        public async Task DataForm_Post_EditByNonOwner_ReturnsForbid()
        {
            using var fixture = new InMemoryDbFixture();
            await using var ctx = fixture.CreateContext();

            var existing = new ObstacleData
            {
                ObstacleName = "Owned",
                ObstacleDescription = "Existing obstacle description",
                ReporterName = "Owner",
                ReportedByUserId = "owner",
                Status = ReportStatus.Pending,
                ReportedAt = System.DateTime.UtcNow,
                DateData = System.DateTime.UtcNow
            };
            ctx.Obstacles.Add(existing);
            await ctx.SaveChangesAsync();

            var userMgrMock = MockHelpers.CreateUserManagerMock<ApplicationUser>();
            var currentUser = new ApplicationUser { Id = "not-owner" };
            userMgrMock.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(currentUser);

            var controller = new ObstacleController(ctx, userMgrMock.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = CreateClaimsPrincipal(currentUser.Id) }
            };
            ConfigureRequestServices(controller);

            var formDict = new Dictionary<string, StringValues>
            {
                ["ObstacleName"] = new StringValues("Attempted edit")
            };
            var form = new FormCollection(formDict);

            var result = await controller.DataForm(form, "false", existing.Id);

            Assert.IsType<ForbidResult>(result);
        }
    }
}
