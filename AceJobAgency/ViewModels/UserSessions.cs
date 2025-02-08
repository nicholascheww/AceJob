using System.ComponentModel.DataAnnotations;

namespace AceJobAgency.ViewModels
{
    public class UserSessions
    {
        [Key]
        public int Id { get; set; }
        public string UserEmail { get; set; }
        public string SessionToken { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
