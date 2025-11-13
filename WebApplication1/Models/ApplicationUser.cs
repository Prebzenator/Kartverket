using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    // Extend IdentityUser with Name, Organization, and MustChangePassword flag
    public class ApplicationUser : IdentityUser
    {
        // Nullable to support existing users, but Required for new registrations
        [StringLength(100)]
        public string? FullName { get; set; }

        [StringLength(100)]
        public string? Organization { get; set; }

        // Flag to enforce password change on first login
        public bool MustChangePassword { get; set; } = false;
    }
}
