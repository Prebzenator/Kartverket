using System.Collections.Generic;

namespace WebApplication1.ViewModels
{
    public class ManageUsersViewModel
    {
        // Keep create user in different ViewModel
        public List<AdminUserListItemViewModel> Users { get; set; } = new List<AdminUserListItemViewModel>();
    }
}
