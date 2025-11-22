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

namespace WebApplication1.Tests.Security
{
    /// <summary>
    /// Test factory for security tests.
    /// Provides an in-memory DB and controllers with or without authentication.
    /// Also sets TempData to avoid NullReferenceException in controller actions.
    /// </summary>
    public class IntegrationFactory
    {
        public IServiceProvider CreateServiceProvider(string dbName)
        {
            var services = new ServiceCollection();

            // In-memory EF database
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(dbName));

            // Fake UserManager<ApplicationUser>
            var store = new Mock<IUserStore<ApplicationUser>>();
            var userManagerMock = new Mock<UserManager<ApplicationUser>>(
                store.Object, null, null, null, null, null, null, null, null);

            services.AddSingleton<UserManager<ApplicationUser>>(userManagerMock.Object);

            return services.BuildServiceProvider();
        }

        public ObstacleController CreateObstacleController(IServiceProvider provider, bool authenticated)
        {
            var db = provider.GetRequiredService<ApplicationDbContext>();
            var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();

            var controller = new ObstacleController(db, userManager);

            ClaimsPrincipal principal;

            if (authenticated)
            {
                principal = new ClaimsPrincipal(
                    new ClaimsIdentity(
                        new[]
                        {
                            new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                            new Claim(ClaimTypes.Role, "Pilot")
                        },
                        "TestAuth"));
            }
            else
            {
                principal = new ClaimsPrincipal(new ClaimsIdentity()); // NOT logged in
            }

            var httpContext = new DefaultHttpContext
            {
                User = principal,
                RequestServices = provider
            };

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // ADD TEMP DATA (fix NullReferenceException)
            var tempDataProvider = new DummyTempDataProvider();
            controller.TempData = new TempDataDictionary(httpContext, tempDataProvider);

            return controller;
        }
    }

    /// <summary>
    /// Simple TempData provider storing everything in memory.
    /// Fixes TempData null errors in controller.
    /// </summary>
    public class DummyTempDataProvider : ITempDataProvider
    {
        private readonly Dictionary<string, object> _storage = new();

        public IDictionary<string, object> LoadTempData(HttpContext context)
        {
            return new Dictionary<string, object>(_storage);
        }

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
            _storage.Clear();
            foreach (var kvp in values)
                _storage[kvp.Key] = kvp.Value;
        }
    }
}
