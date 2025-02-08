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

                // Encrypt NRIC before storing
                string encryptedNRIC = EncryptNRIC(RModel.NRIC);

                // Handle Resume Upload
                string resumePath = await SaveResumeAsync(RModel.Resume);

                var user = new User
                {
                    FirstName = RModel.FirstName,
                    LastName = RModel.LastName,
                    Gender = RModel.Gender,
                    NRIC = encryptedNRIC,
                    Email = RModel.Email,
                    Password = HashPassword(RModel.Password), // Hash the password before storing
                    DateOfBirth = RModel.DateOfBirth,
                    Resume = resumePath,
                    WhoAmI = RModel.WhoAmI
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
                var secretKey = "6Lcoq9AqAAAAADyI_lH2czSHZzXJt_-Qc3JeHzv_"; // Replace with your reCAPTCHA secret key
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