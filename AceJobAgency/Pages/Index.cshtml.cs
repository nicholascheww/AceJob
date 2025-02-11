using Microsoft.AspNetCore.Mvc.RazorPages;
using AceJobAgency.Model;
using System.Linq;
using System.Text;
using System;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using AceJobAgency.ViewModels;
using System.Collections.Generic;
using System.Text.Encodings.Web;


namespace AceJobAgency.Pages
{
    public class IndexModel : PageModel
    {
        private readonly AuthDbContext _context;
        private const int MaxPasswordHistory = 2;  // Max number of previous passwords stored

        public User CurrentUser { get; set; }
        public string UnencryptedNRIC { get; set; }
        [BindProperty]
        public string CurrentPassword { get; set; }
        [BindProperty]
        public string NewPassword { get; set; }
        [BindProperty]
        public string ConfirmPassword { get; set; }

        public IndexModel(AuthDbContext context)
        {
            _context = context;
        }

        public void OnGet()
        {
            // Retrieve the user's email from session or claims
            string email = HttpContext.Session.GetString("UserEmail") ?? User.Identity?.Name;
            if (!string.IsNullOrEmpty(email))
            {
                CurrentUser = _context.Users.FirstOrDefault(u => u.Email == email);
                if (CurrentUser != null)
                {
                    // Decrypt the NRIC to show the plain text version
                    UnencryptedNRIC = DecryptNRIC(CurrentUser.NRIC);
                }
            }
        }

        public IActionResult OnPostChangePassword(string currentPassword, string newPassword)
        {
            string email = HttpContext.Session.GetString("UserEmail") ?? User.Identity?.Name;

            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError(string.Empty, "User is not authenticated.");
                return Page();
            }

            // Retrieve the user from the database
            CurrentUser = _context.Users.FirstOrDefault(u => u.Email == email);

            if (CurrentUser == null)
            {
                ModelState.AddModelError(string.Empty, "User not found.");
                return Page();
            }

            // Verify the current password
            if (!VerifyPassword(currentPassword, CurrentUser.Password))
            {
                ModelState.AddModelError(string.Empty, "Current password is incorrect.");
                return Page();
            }

            // Check if the new password is the same as any of the previous passwords
            var passwordHistory = _context.PasswordHistory
                                          .Where(ph => ph.UserId == CurrentUser.Id)
                                          .Select(ph => ph.PasswordHash)
                                          .ToList();

            var newPasswordHash = HashPassword(newPassword);

            if (passwordHistory.Contains(newPasswordHash))
            {
                ModelState.AddModelError(string.Empty, "You cannot reuse your previous passwords.");
                return Page(); // Return the page with error messages
            }

            // Hash the new password and update
            CurrentUser.Password = newPasswordHash;
            CurrentUser.PasswordLastChanged = DateTime.Now;
            AddPasswordToHistory(CurrentUser, newPasswordHash);

            // Save changes to the database
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Password successfully changed!";
            return RedirectToPage(); // Optionally redirect to the same page
        }





        private string DecryptNRIC(string encryptedNRIC)
        {
            byte[] key = Encoding.UTF8.GetBytes("0123456789abcdef0123456789abcdef"); // 32 bytes for AES-256
            byte[] iv = new byte[16]; // Same IV as in encryption (all zeros in this example)

            try
            {
                byte[] cipherBytes = Convert.FromBase64String(encryptedNRIC);
                using (Aes aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = iv;
                    using (ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                    {
                        byte[] plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
                        return Encoding.UTF8.GetString(plainBytes);
                    }
                }
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashBytes);
            }
        }

        private bool VerifyPassword(string password, string storedHash)
        {
            string passwordHash = HashPassword(password);
            return passwordHash == storedHash;
        }

        private bool IsPasswordInHistory(string newPassword)
        {
            // Get password history for the user
            var passwordHistory = CurrentUser.PasswordHistory;

            if (passwordHistory == null || passwordHistory.Count == 0)
            {
                return false; // No password history found
            }

            // Check if the new password exists in the password history
            foreach (var history in passwordHistory)
            {
                if (history.PasswordHash == HashPassword(newPassword))
                {
                    return true;
                }
            }

            return false;
        }

        public void AddPasswordToHistory(User currentUser, string newPasswordHash)
        {
            // Ensure that the user exists and has an Id
            if (currentUser == null || string.IsNullOrEmpty(currentUser.Email) || currentUser.Id == 0)
            {
                throw new InvalidOperationException("User or UserId is invalid.");
            }

            // Prevent setting the new password if it's the same as one of the previous ones
            var existingPassword = _context.PasswordHistory
                                            .Where(ph => ph.UserId == currentUser.Id)
                                            .Select(ph => ph.PasswordHash)
                                            .ToList();

            if (existingPassword.Contains(newPasswordHash))
            {
                ModelState.AddModelError("newPassword", "You cannot use the same password as your previous one.");
            }

            // Create a new PasswordHistory record
            var passwordHistory = new PasswordHistory
            {
                UserId = currentUser.Id, // Set the UserId to the current user's Id
                UserEmail = currentUser.Email,
                PasswordHash = newPasswordHash,
                DateChanged = DateTime.UtcNow
            };

            // Add the new password history record to the context
            _context.PasswordHistory.Add(passwordHistory);
        }

        public string DecodeText(string encodedText)
        {
            byte[] textBytes = Convert.FromBase64String(encodedText);
            return Encoding.UTF8.GetString(textBytes);
        }



    }
}