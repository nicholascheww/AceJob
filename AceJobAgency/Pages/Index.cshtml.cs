using Microsoft.AspNetCore.Mvc.RazorPages;
using AceJobAgency.Model;
using System.Linq;
using System.Text;
using System;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using AceJobAgency.ViewModels;

namespace AceJobAgency.Pages
{
    public class IndexModel : PageModel
    {
        private readonly AuthDbContext _context;
        public User CurrentUser { get; set; }
        public string UnencryptedNRIC { get; set; }

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

        /// <summary>
        /// Decrypts the encrypted NRIC using AES with a fixed key and IV.
        /// Ensure that your encryption method (used during registration) uses the same key and IV.
        /// </summary>
        /// <param name="encryptedNRIC">The encrypted NRIC string.</param>
        /// <returns>The plain text NRIC, or an error message if decryption fails.</returns>
        private string DecryptNRIC(string encryptedNRIC)
        {
            // IMPORTANT: The key and IV must match those used in your EncryptNRIC method.
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
                // Optionally log or handle the exception as needed
                return string.Empty;
            }
        }

    }
}

