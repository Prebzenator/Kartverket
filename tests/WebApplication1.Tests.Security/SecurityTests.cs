using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Controllers;
using Xunit;

namespace WebApplication1.Tests.Security
{
    /// <summary>
    /// Security-related tests based on controller attributes.
    /// Checks role requirements and anti-forgery protection.
    /// </summary>
    public class SecurityTests
    {
        [Fact]
        public void Review_Action_ShouldRequire_RegistryAdministrator_Role()
        {
            // Arrange
            var methodInfo = typeof(ObstacleController)
                .GetMethod(nameof(ObstacleController.Review));

            // Act
            var authorizeAttribute = methodInfo?
                .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
                .Cast<AuthorizeAttribute>()
                .FirstOrDefault();

            // Assert
            Assert.NotNull(authorizeAttribute);
            Assert.False(string.IsNullOrWhiteSpace(authorizeAttribute!.Roles));
            Assert.Contains("Registry Administrator", authorizeAttribute.Roles);
        }

        [Fact]
        public void DataForm_Post_ShouldHave_ValidateAntiForgeryToken()
        {
            // Find the POST version of DataForm on ObstacleController
            var postMethod = typeof(ObstacleController)
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(m =>
                    m.Name == nameof(ObstacleController.DataForm) &&
                    m.GetCustomAttributes(typeof(HttpPostAttribute), inherit: true).Any());

            // Sanity check: we actually found the method
            Assert.NotNull(postMethod);

            // Check for [ValidateAntiForgeryToken] on the POST action
            var antiForgeryAttribute = postMethod!
                .GetCustomAttributes(typeof(ValidateAntiForgeryTokenAttribute), inherit: true)
                .Cast<ValidateAntiForgeryTokenAttribute>()
                .FirstOrDefault();

            Assert.NotNull(antiForgeryAttribute);
        }
    }
}
