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

namespace AceJobAgency.Pages
{
    public class LoginModel : PageModel
    {
        private readonly AuthDbContext _context;
        public bool IsLoggedIn { get; set; }

        public LoginModel(AuthDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Login LModel { get; set; }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                var user = _context.Users.FirstOrDefault(u => u.Email == LModel.Email);

                if (user == null || !VerifyPassword(LModel.Password, user.Password))
                {
                    ModelState.AddModelError("", "Username or Password incorrect");
                    return Page();
                }

                // Generate a new session token for this login instance
                string sessionToken = Guid.NewGuid().ToString();

                // Store the session token in the UserSessions table
                var newSession = new UserSessions
                {
                    UserEmail = user.Email,
                    SessionToken = sessionToken,
                    CreatedAt = DateTime.UtcNow
                };

                _context.UserSessions.Add(newSession);
                await _context.SaveChangesAsync();

                // Store session details in HttpContext
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
