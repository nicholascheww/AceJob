using AceJobAgency.Model;
using AceJobAgency.Middleware;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using AceJobAgency.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(10); // Set session timeout
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Configure Authentication and Authorization
builder.Services.AddAuthentication("MyCookieAuth")
    .AddCookie("MyCookieAuth", options =>
    {
        options.LoginPath = "/Login";  // Redirect to login on unauthorized access
        options.Cookie.Name = "MyCookieAuth";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
        options.SlidingExpiration = false; // Resets session timer on activity
    });

// Add services to the container
builder.Services.AddRazorPages();
builder.Services.AddDbContext<AuthDbContext>();
builder.Services.AddIdentity<IdentityUser, IdentityRole>().AddEntityFrameworkStores<AuthDbContext>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("MustBelongToHRDepartment",
        policy => policy.RequireClaim("Department", "HR"));
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();

// Build the application
var app = builder.Build();



app.UseHttpsRedirection();
app.UseSession();
app.UseStatusCodePagesWithReExecute("/Error/{0}");
app.UseExceptionHandler("/Error/General"); // Handles unhandled exceptions
app.UseHsts();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Custom Middleware (Session validation or others)
app.UseMiddleware<SessionValidationMiddleware>();

// Route for static assets
app.MapStaticAssets();

// Map Razor Pages
app.MapRazorPages();

// Run the app
app.Run();
