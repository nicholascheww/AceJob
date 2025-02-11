using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace AceJobAgency.ViewModels
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        public string Gender { get; set; }

        [Required]
        public string NRIC { get; set; }

        [Required]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{12,}$",
            ErrorMessage = "Password must be at least 12 characters long and contain a mix of upper-case, lower-case, numbers, and special characters.")]
        public string Password { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [Required]
        public String Resume { get; set; } // File Upload Handling

        [Required]
        public string WhoAmI { get; set; }
        public string? SessionToken { get; set; }

        public int FailedLoginAttempts { get; set; } = 0;
        public DateTime? LockoutEnd { get; set; }
        [Required]
        [DataType(DataType.Date)]
        public DateTime PasswordLastChanged { get; set; }
        public virtual ICollection<PasswordHistory> PasswordHistory { get; set; }
    }
}
