using System;
using System.Threading.Tasks;
using AceJobAgency.Model;
using AceJobAgency.ViewModels; // Assuming your AuditLog model is here

namespace AceJobAgency.Services
{
    public interface IAuditLogService
    {
        Task LogAuditEventAsync(string userEmail, string action);
    }

    public class AuditLogService : IAuditLogService
    {
        private readonly AuthDbContext _context;

        public AuditLogService(AuthDbContext context)
        {
            _context = context;
        }

        public async Task LogAuditEventAsync(string userEmail, string action)
        {
            var auditEntry = new AuditLog
            {
                UserEmail = userEmail,
                Action = action,
                Timestamp = DateTime.UtcNow
            };
            _context.AuditLogs.Add(auditEntry);
            await _context.SaveChangesAsync();
        }
    }
}
