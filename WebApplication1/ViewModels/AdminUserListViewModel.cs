namespace WebApplication1.ViewModels
{
    public class AdminUserListItemViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? FullName { get; set; }
        public string? Organization { get; set; }
        public string Roles { get; set; } = string.Empty;
    }
}
