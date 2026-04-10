using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UserManagement.Data;
using UserManagement.Models.Entities;
using UserManagement.Models.ViewModels;
using UserManagement.Models.Enums;
using UserManagement.Services;
using UserManagement.Middleware;

namespace UserManagement.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _dbContext;
        private readonly IEmailService _emailService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AccountController(AppDbContext dbContext, IEmailService emailService, IHttpContextAccessor httpContextAccessor)
        {
            _dbContext = dbContext;
            _emailService = emailService;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "User");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existingUser = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Email == model.Email);

            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "This email is already registered!");
                return View(model);
            }

            var user = new User
            {
                Name = model.Name,
                Email = model.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                RegistrationTime = DateTime.UtcNow,
                LastLoginTime = DateTime.UtcNow,
                Status = Models.Enums.UserStatus.Unverified,
                EmailConfirmationToken = Guid.NewGuid()
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            var confirmationLink = Url.Action(
                "ConfirmEmail",
                "Account",
                new { token = user.EmailConfirmationToken },
                protocol: HttpContext.Request.Scheme);

            _ = Task.Run(() => _emailService.SendConfirmationEmailAsync(user.Email, confirmationLink!));

            TempData["SuccessMessage"] = "Registration successful! Please check your email to confirm your account.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(Guid token)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.EmailConfirmationToken == token);

            if (user == null)
            {
                TempData["ErrorMessage"] = "Invalid confirmation token";
                return RedirectToAction("Login");
            }

            if (user.Status == UserStatus.Unverified)
            {
                user.Status = UserStatus.Active;
                user.EmailConfirmationToken = null;
                await _dbContext.SaveChangesAsync();
                TempData["SuccessMessage"] = "Email confirmed successfully!";
            }
            else
            {
                TempData["InfoMessage"] = "Email already confirmed";
            }

            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null, bool blocked = false, string? message = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "User");
            }

            if (blocked && !string.IsNullOrEmpty(message))
            {
                TempData["ErrorMessage"] = message;
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                ModelState.AddModelError("", "Invalid email or password");
                return View(model);
            }
            if (user.Status == UserStatus.Blocked)
            {
                ModelState.AddModelError("", "Your account has been blocked");
                return View(model);
            }

            user.LastLoginTime = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            HttpContext.Session.SetString("UserId", user.Id.ToString());

            return RedirectToAction("Index", "User");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogOut()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
