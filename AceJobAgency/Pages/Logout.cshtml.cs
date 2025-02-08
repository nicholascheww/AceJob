using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AceJobAgency.Pages
{
    public class LogoutModel : PageModel
    {
        public void OnGet() { }

        public async Task<IActionResult> OnPostLogoutAsync()
        {
            // Clear session data
            HttpContext.Session.Clear();

            // Sign out the user and clear the authentication cookie
            await HttpContext.SignOutAsync("MyCookieAuth");

            // Redirect to the login page
            return RedirectToPage("Login");
        }

        public IActionResult OnPostDontLogout()
        {
            // Redirect back to the home page (or wherever you want)
            return RedirectToPage("Index");
        }
    }
}
