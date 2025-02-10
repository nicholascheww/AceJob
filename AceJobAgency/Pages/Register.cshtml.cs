using AceJobAgency.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System;
using AceJobAgency.Model;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Text.Encodings.Web;

namespace AceJobAgency.Pages
{
    public class RegisterModel : PageModel
    {
        private readonly AuthDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public RegisterModel(AuthDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        [BindProperty]
        public Register RModel { get; set; }

        public void OnGet() { }
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                // Retrieve reCAPTCHA token from the form
                string recaptchaResponse = Request.Form["g-recaptcha-response"];

                if (!await ValidateRecaptchaAsync(recaptchaResponse))
                {
                    ModelState.AddModelError(string.Empty, "reCAPTCHA verification failed. Please try again.");
                    return Page();
                }

                if (!IsValidPassword(RModel.Password))
                {
                    ModelState.AddModelError("Password", "Password must be at least 12 characters long, with a mix of uppercase, lowercase, numbers, and special characters.");
                    return Page();
                }

                // Check if email already exists
                bool emailExists = await _context.Users.AnyAsync(u => u.Email == RModel.Email);
                if (emailExists)
                {
                    ModelState.AddModelError("Email", "This email is already registered. Please use a different email.");
                    return Page();
                }

                // Encrypt NRIC before storing
                string encryptedNRIC = EncryptNRIC(RModel.NRIC);

                // Handle Resume Upload
                string resumePath = await SaveResumeAsync(RModel.Resume);

                // Input sanitation and validation
                string sanitizedEmail = HtmlEncoder.Default.Encode(RModel.Email.Trim().ToLower());
                var existingUser = _context.Users.FirstOrDefault(u => u.Email == sanitizedEmail);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Email", "Email is already taken.");
                    return Page();
                }

                // Sanitize and validate all fields
                string sanitizedFirstName = HtmlEncoder.Default.Encode(RModel.FirstName.Trim());
                string sanitizedLastName = HtmlEncoder.Default.Encode(RModel.LastName.Trim());
                string sanitizedWhoAmI = HtmlEncoder.Default.Encode(RModel.WhoAmI?.Trim()); ;

                var user = new User
                {
                    FirstName = sanitizedFirstName,
                    LastName = sanitizedLastName,
                    Gender = RModel.Gender,
                    NRIC = encryptedNRIC,
                    Email = sanitizedEmail,
                    Password = HashPassword(RModel.Password), // Hash the password before storing
                    DateOfBirth = RModel.DateOfBirth,
                    Resume = resumePath,
                    WhoAmI = sanitizedWhoAmI
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                return RedirectToPage("Index");
            }

            return Page();
        }
        private string EncryptNRIC(string nric)
        {
            // Use a fixed key and IV so that decryption can use the same values.
            byte[] key = Encoding.UTF8.GetBytes("0123456789abcdef0123456789abcdef"); // 32 bytes for AES-256
            byte[] iv = new byte[16]; // 16 bytes IV (all zeros in this example; not secure for production)

            using (Aes aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.Key = key;
                aes.IV = iv;

                using (ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                {
                    byte[] nricBytes = Encoding.UTF8.GetBytes(nric);
                    byte[] encryptedBytes = encryptor.TransformFinalBlock(nricBytes, 0, nricBytes.Length);
                    return Convert.ToBase64String(encryptedBytes);
                }
            }
        }


        private async Task<string> SaveResumeAsync(IFormFile resumeFile)
        {
            if (resumeFile != null && (resumeFile.ContentType == "application/pdf" || resumeFile.ContentType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document"))
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "resumes");
                Directory.CreateDirectory(uploadsFolder);
                string filePath = Path.Combine(uploadsFolder, Guid.NewGuid() + Path.GetExtension(resumeFile.FileName));

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await resumeFile.CopyToAsync(fileStream);
                }
                return filePath;
            }
            return null;
        }

        private bool IsValidPassword(string password)
        {
            return password.Length >= 12 &&
                   password.Any(char.IsUpper) &&
                   password.Any(char.IsLower) &&
                   password.Any(char.IsDigit) &&
                   password.Any(ch => "!@#$%^&*()_+[]{}|;:,.<>?".Contains(ch));
        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
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

                if (json.TryGetValue("success", out var success) && success.ToString() == "True")
                {
                    return true;
                }

                return false;
            }
        }
    }
}