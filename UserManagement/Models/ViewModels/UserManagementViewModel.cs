using UserManagement.Models.Entities;

namespace UserManagement.Models.ViewModels
{
    public class UserManagementViewModel
    {
        public List<User> Users { get; set; } = new();
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
