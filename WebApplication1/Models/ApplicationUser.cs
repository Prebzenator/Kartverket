using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    // Extend IdentityUser with Name and Organization
    public class ApplicationUser : IdentityUser
    {
        // Nullable to support existing users, but Required for new registrations
        [StringLength(100)]
        public string? FullName { get; set; }

        [StringLength(100)]
        public string? Organization { get; set; }
    }
}