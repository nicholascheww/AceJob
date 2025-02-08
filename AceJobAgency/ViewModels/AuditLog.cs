namespace AceJobAgency.ViewModels
{
    public class AuditLog
    {
        public int Id { get; set; }
        public string UserEmail { get; set; }
        public string Action { get; set; }
        public DateTime Timestamp { get; set; }
    }

}
