using Microsoft.AspNetCore.Identity;

namespace WebApplication1.Models
{
    // Extend IdentityUser only if you need extra properties later.
    public class ApplicationUser : IdentityUser
    {
        // Example: public string FullName { get; set; }
    }
}
