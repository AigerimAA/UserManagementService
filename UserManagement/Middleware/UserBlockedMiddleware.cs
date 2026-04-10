using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using UserManagement.Data;
using UserManagement.Models.Enums;

namespace UserManagement.Middleware
{
    public class UserBlockedMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<UserBlockedMiddleware> _logger;

        public UserBlockedMiddleware(RequestDelegate next, ILogger<UserBlockedMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, AppDbContext dbContext)
        {
            var path = context.Request.Path.ToString().ToLower();

            var allowedPaths = new[]
            {
                "/account/login",
                "/account/register",
                "/account/confirmemail",
                "/account/logout",
                "/css",
                "/js",
                "/lib",
                "/favicon.ico"
            };

            foreach (var allowedPath in allowedPaths)
            {
                if (path.StartsWith(allowedPath))
                {
                    await _next(context);
                    return;
                }
            }

            var userIdClaim = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int userId))
            {
                var user = await dbContext.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    _logger.LogWarning($"User {userId} not found, redirecting to login");
                    await SignOutAndRedirect(context, "User no longer exists");
                    return;
                }

                if (user.Status == UserStatus.Blocked)
                {
                    _logger.LogWarning($"Blocked user {user.Email} attempted to access {path}");
                    await SignOutAndRedirect(context, "Your account has been blocked");
                    return;
                }

                var lastActivityKey = $"LastActivity_{userId}";
                var lastActivity = context.Session.GetString(lastActivityKey);

                if (string.IsNullOrEmpty(lastActivity) ||
                    DateTime.TryParse(lastActivity, out var last) &&
                    (DateTime.UtcNow - last).TotalMinutes > 5)
                {
                    context.Session.SetString(lastActivityKey, DateTime.UtcNow.ToString());
                }
            }
            await _next(context);
        }
        private async Task SignOutAndRedirect(HttpContext context, string message)
        {
            await context.SignOutAsync();
            context.Session.Clear();

            context.Response.Redirect($"/Account/Login?blocked=true&message={Uri.EscapeDataString(message)}");
        }
    }
}
