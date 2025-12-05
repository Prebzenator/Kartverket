namespace WebApplication1.ViewModels
{
    /// <summary>
    /// Represents a single user item in the admin user list.
    /// Used for displaying basic user details and roles.
    /// </summary>
    public class AdminUserListItemViewModel
    {
        // Unique identifier for the user
        public string Id { get; set; } = string.Empty;

        public string? Email { get; set; }
        public string? FullName { get; set; }
        public string? Organization { get; set; }

        // Comma-separated list of roles assigned to the user
        public string Roles { get; set; } = string.Empty;
    }
}
