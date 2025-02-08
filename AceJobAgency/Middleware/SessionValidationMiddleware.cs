using AceJobAgency.Model;
using AceJobAgency.Model;
using Microsoft.EntityFrameworkCore;
namespace AceJobAgency.Middleware
{
    public class SessionValidationMiddleware
    {
        private readonly RequestDelegate _next;

        public SessionValidationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            var userEmail = context.Session.GetString("UserEmail");
            var sessionToken = context.Session.GetString("SessionToken");

            if (!string.IsNullOrEmpty(userEmail) && !string.IsNullOrEmpty(sessionToken))
            {
                var dbContext = context.RequestServices.GetRequiredService<AuthDbContext>();

                // Check if the session exists in the database
                bool isValidSession = await dbContext.UserSessions
                    .AnyAsync(us => us.UserEmail == userEmail && us.SessionToken == sessionToken);

                if (!isValidSession)
                {
                    // If session is invalid, clear it and redirect to login
                    context.Session.Clear();
                    context.Response.Redirect("/Login?sessionExpired=true");
                    return;
                }
            }

            await _next(context);
        }
    }
}