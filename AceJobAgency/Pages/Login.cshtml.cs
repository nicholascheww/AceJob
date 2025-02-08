using AceJobAgency.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AceJobAgency.Model;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Filters;
using AceJobAgency.Model;
using AceJobAgency.ViewModels;
using AceJobAgency.Services;

namespace AceJobAgency.Pages
{
    public class LoginModel : PageModel
    {
        private readonly AuthDbContext _context;
        private readonly IAuditLogService _auditLogService;
        public bool IsLoggedIn { get; set; }

        public LoginModel(AuthDbContext context, IAuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }

        [BindProperty]
        public Login LModel { get; set; }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                var user = _context.Users.FirstOrDefault(u => u.Email == LModel.Email);

                // Check if the user exists and if the account is locked
                if (user != null && user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
                {
                    ModelState.AddModelError("", "Your account is locked. Please try again later.");
                    return Page();
                }

                // Validate credentials
                if (user == null || !VerifyPassword(LModel.Password, user.Password))
                {
                    // If user exists, increase failed login attempts
                    if (user != null)
                    {
                        user.FailedLoginAttempts++;
                        // Lock out the account after 3 failed attempts (e.g., lock for 15 minutes)
                        if (user.FailedLoginAttempts >= 3)
                        {
                            user.LockoutEnd = DateTime.UtcNow.AddMinutes(15);
                        }
                        _context.Users.Update(user);
                        await _context.SaveChangesAsync();
                    }
                    ModelState.AddModelError("", "Username or Password incorrect");
                    return Page();
                }

                // Successful login: reset the counter
                user.FailedLoginAttempts = 0;
                user.LockoutEnd = null;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                // Generate a session token and store session info as you did
                string sessionToken = Guid.NewGuid().ToString();
                var newSession = new UserSessions
                {
                    UserEmail = user.Email,
                    SessionToken = sessionToken,
                    CreatedAt = DateTime.UtcNow
                };
                _context.UserSessions.Add(newSession);
                await _context.SaveChangesAsync();

                HttpContext.Session.Clear();
                HttpContext.Session.SetString("UserEmail", LModel.Email);
                HttpContext.Session.SetString("SessionToken", sessionToken);
                HttpContext.Session.SetString("SessionStartTime", DateTime.UtcNow.ToString());

                // Create claims for authentication
                var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, LModel.Email),
            new Claim(ClaimTypes.Email, LModel.Email),
            new Claim("Department", "HR")
        };

                var identity = new ClaimsIdentity(claims, "MyCookieAuth");
                ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(identity);
                await HttpContext.SignInAsync("MyCookieAuth", claimsPrincipal);

                // Log successful login
                await _auditLogService.LogAuditEventAsync(user.Email, "Successful Login");

                // Redirect to the homepage after successful login
                return RedirectToPage("/Index");
            }
            return Page();
        }


        private bool VerifyPassword(string enteredPassword, string storedHashedPassword)
        {
            // Hash the entered password and compare with the stored hash
            using (var sha256 = SHA256.Create())
            {
                var hashedEnteredPassword = sha256.ComputeHash(Encoding.UTF8.GetBytes(enteredPassword));
                string hashedEnteredPasswordBase64 = Convert.ToBase64String(hashedEnteredPassword);

                return storedHashedPassword == hashedEnteredPasswordBase64;
            }
        }

        private string GenerateSessionToken()
        {
            using var rng = new RNGCryptoServiceProvider();
            byte[] tokenData = new byte[32];
            rng.GetBytes(tokenData);
            return Convert.ToBase64String(tokenData);
        }
    }
}
