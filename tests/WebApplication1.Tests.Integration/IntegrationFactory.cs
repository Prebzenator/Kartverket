using System;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using WebApplication1.Controllers;
using WebApplication1.Data;
using WebApplication1.Models;

namespace WebApplication1.Tests.Integration
{
    /// <summary>
    /// Helper factory for integration-style tests.
    /// Creates an in-memory ApplicationDbContext and an ObstacleController
    /// with a fake authenticated Pilot user and working TempData.
    /// </summary>
    public class IntegrationFactory
    {
        public IServiceProvider CreateServiceProvider(string dbName)
        {
            var services = new ServiceCollection();

            // InMemory EF Core database
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(dbName));

            // Fake UserManager<ApplicationUser> that always returns a test user
            var store = new Mock<IUserStore<ApplicationUser>>();
            var userManagerMock = new Mock<UserManager<ApplicationUser>>(
                store.Object, null, null, null, null, null, null, null, null);

            userManagerMock
                .Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(new ApplicationUser
                {
                    Id = "test-user-id",
                    FullName = "Test User",
                    Organization = "Test Org"
                });

            services.AddSingleton<UserManager<ApplicationUser>>(userManagerMock.Object);

            return services.BuildServiceProvider();
        }

        /// <summary>
        /// Creates an ObstacleController instance using the given service provider.
        /// The controller will think there is a logged-in Pilot user and has TempData set.
        /// </summary>
        public ObstacleController CreateObstacleController(IServiceProvider provider)
        {
            var db = provider.GetRequiredService<ApplicationDbContext>();
            var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();

            var controller = new ObstacleController(db, userManager);

            // Fake authenticated user with Pilot role
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                new Claim(ClaimTypes.Name, "testuser"),
                new Claim(ClaimTypes.Role, "Pilot")
            };

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext
            {
                User = principal,
                RequestServices = provider
            };

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Set up TempData with a simple in-memory provider so TempData["..."] works
            var tempDataProvider = new DummyTempDataProvider();
            controller.TempData = new TempDataDictionary(httpContext, tempDataProvider);

            return controller;
        }
    }

    /// <summary>
    /// Very simple TempData provider for tests.
    /// Stores TempData in an in-memory dictionary only for the lifetime of the request.
    /// </summary>
    public class DummyTempDataProvider : ITempDataProvider
    {
        private readonly Dictionary<string, object> _storage = new();

        public IDictionary<string, object> LoadTempData(HttpContext context)
        {
            // Return a new dictionary with current values
            return new Dictionary<string, object>(_storage);
        }

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
            _storage.Clear();
            foreach (var kvp in values)
            {
                _storage[kvp.Key] = kvp.Value;
            }
        }
    }
}
