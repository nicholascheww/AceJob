using AceJobAgency.Model;
using AceJobAgency.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace AceJobAgency.Pages
{
    public class LogoutModel : PageModel
    {
        // Inject your AuthDbContext if you wish to log the logout event
        private readonly AuthDbContext _context;
        public LogoutModel(AuthDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Retrieve the email from the session or the User claims
            var userEmail = HttpContext.Session.GetString("UserEmail") ?? User.Identity.Name;

            // Log the logout event
            await LogAuditEvent(userEmail, "User Logged Out");

            // Sign the user out and clear the session
            await HttpContext.SignOutAsync("MyCookieAuth");
            HttpContext.Session.Clear();

            // Redirect to the login page
            return RedirectToPage("/Login");
        }

        private async Task LogAuditEvent(string userEmail, string action)
        {
            // Create and save the audit log entry.
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
