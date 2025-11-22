using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

// Provides a reusable factory for creating a Mock<UserManager<TUser>> with required constructor dependencies.
// Tests should configure specific method setups (e.g., GetUserAsync) on the returned mock.

namespace WebApplication1.Tests.Unit.Helpers
{
    public static class MockHelpers
    {
        public static Mock<UserManager<TUser>> CreateUserManagerMock<TUser>() where TUser : class
        {
            var store = new Mock<IUserStore<TUser>>();
            var mgr = new Mock<UserManager<TUser>>(
                store.Object,
                new Mock<IOptions<IdentityOptions>>().Object,
                new Mock<IPasswordHasher<TUser>>().Object,
                new IUserValidator<TUser>[0],
                new IPasswordValidator<TUser>[0],
                new Mock<ILookupNormalizer>().Object,
                new Mock<IdentityErrorDescriber>().Object,
                new Mock<IServiceProvider>().Object,
                new Mock<ILogger<UserManager<TUser>>>().Object);

            // Do NOT setup Dispose() or other non-virtual members — Moq throws for those.
            return mgr;
        }
    }
}
