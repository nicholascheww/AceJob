namespace AceJobAgency.ViewModels
{
    public class PasswordHistory
    {
        public int Id { get; set; }
        public int UserId { get; set; }  // Foreign Key
        public string UserEmail { get; set; }
        public string PasswordHash { get; set; }
        public DateTime DateChanged { get; set; }

        public virtual User User { get; set; }  // Navigation property
    }

}
