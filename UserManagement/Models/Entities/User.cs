using System.ComponentModel.DataAnnotations;
using UserManagement.Models.Enums;

namespace UserManagement.Models.Entities
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public DateTime LastLoginTime { get; set; }
        public DateTime RegistrationTime { get; set; }
        public UserStatus Status { get; set; } = UserStatus.Unverified;
        public Guid? EmailConfirmationToken { get; set; }
    }
}
