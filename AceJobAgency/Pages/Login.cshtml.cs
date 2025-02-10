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
using AceJobAgency.Services;
using System.Text.Json;
using System.Text.Encodings.Web;

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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                string recaptchaResponse = Request.Form["g-recaptcha-response"];

                if (!await ValidateRecaptchaAsync(recaptchaResponse))
                {
                    ModelState.AddModelError(string.Empty, "reCAPTCHA verification failed. Please try again.");
                    return Page();
                }

                // Input sanitation and validation
                string sanitizedEmail = HtmlEncoder.Default.Encode(LModel.Email.Trim());
                var user = _context.Users.FirstOrDefault(u => u.Email == sanitizedEmail);

                if (user != null && user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
                {
                    ModelState.AddModelError("", "Your account is locked. Please try again later.");
                    return Page();
                }

                if (user == null || !VerifyPassword(LModel.Password, user.Password))
                {
                    if (user != null)
                    {
                        user.FailedLoginAttempts++;
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

        private async Task<bool> ValidateRecaptchaAsync(string recaptchaResponse)
        {
            using (var client = new HttpClient())
            {
                var secretKey = "6Ld8p9AqAAAAAGZTkbPJqZn_BvpI6qgTQ5q9JPbg"; // Replace with your reCAPTCHA secret key
                var response = await client.PostAsync(
                    $"https://www.google.com/recaptcha/api/siteverify?secret={secretKey}&response={recaptchaResponse}",
                    null
                );

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var json = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonResponse);

                return json.TryGetValue("success", out var success) && success.ToString() == "True";
            }
        }
    }
}
