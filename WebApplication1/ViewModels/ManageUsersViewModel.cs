using System.Collections.Generic;

/// <summary>
/// ViewModel used for displaying a list of users in the admin user management page.
/// Contains only the user list; user creation is handled by a separate ViewModel.
/// </summary>
namespace WebApplication1.ViewModels
{
    public class ManageUsersViewModel
    {
        // Keep create user in different ViewModel
        public List<AdminUserListItemViewModel> Users { get; set; } = new List<AdminUserListItemViewModel>();
    }
}
