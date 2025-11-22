using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

// A lightweight ITempDataProvider for tests that returns an empty dictionary and no-ops on save.
// Avoids bringing DataProtection or cookie-based TempData dependencies into unit tests.

namespace WebApplication1.Tests.Unit.Helpers
{
    public class TestTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context)
        {
            return new Dictionary<string, object>();
        }

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
            // no-op for tests
        }
    }
}
