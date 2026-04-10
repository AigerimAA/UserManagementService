using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserManagement.Data;
using UserManagement.Models.Entities;
using UserManagement.Models.ViewModels;

namespace UserManagement.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        private readonly AppDbContext _dbContext;

        public UserController(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var users = await _dbContext.Users
                .OrderByDescending(u => u.LastLoginTime)
                .ToListAsync();

            var viewModel = new UserManagementViewModel
            {
                Users = users
            };

            if (TempData["SuccessMessage"] != null)
                viewModel.SuccessMessage = TempData["SuccessMessage"]?.ToString();
            if (TempData["ErrorMessage"] != null)
                viewModel.ErrorMessage = TempData["ErrorMessage"]?.ToString();

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Block([FromBody] List<int> userIds)
        {
            if (userIds == null || !userIds.Any())
            {
                return Json(new { success = false, message = "No users selected" });
            }

            var users = await _dbContext.Users
                .Where(u => userIds.Contains(u.Id))
                .ToListAsync();

            foreach (var user in users)
            {
                if (user.Status != Models.Enums.UserStatus.Blocked)
                {
                    user.Status = Models.Enums.UserStatus.Blocked;
                }
            }

            await _dbContext.SaveChangesAsync();
            return Json(new { success = true, message = $"{users.Count} user(s) blocked" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unblock([FromBody] List<int> userIds)
        {
            if (userIds == null || !userIds.Any())
            {
                return Json(new { success = false, message = "No users selected" });
            }

            var users = await _dbContext.Users
                .Where(u => userIds.Contains(u.Id))
                .ToListAsync();

            foreach (var user in users)
            {
                if (user.Status == Models.Enums.UserStatus.Blocked)
                {
                    user.Status = Models.Enums.UserStatus.Active;
                }
            }

            await _dbContext.SaveChangesAsync();
            return Json(new { success = true, message = $"{users.Count} user(s) unblocked"}); 
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete([FromBody] List<int> userIds)
        {
            if (userIds == null || !userIds.Any())
            {
                return Json(new { success = false, message = "No users selected" });
            }

            var users = await _dbContext.Users
                .Where(u => userIds.Contains(u.Id))
                .ToListAsync();

            _dbContext.Users.RemoveRange(users);
            await _dbContext.SaveChangesAsync();

            return Json(new { success = true, message = $"{users.Count} user(s) deleted" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUnverified()
        {
            var unverifiedUsers = await _dbContext.Users
                .Where(u => u.Status == Models.Enums.UserStatus.Unverified)
                .ToListAsync();

            var count = unverifiedUsers.Count;
            _dbContext.Users.RemoveRange(unverifiedUsers);
            await _dbContext.SaveChangesAsync();

            return Json(new { success = true, message = $"{count} unverified user(s) deleted" });
        }
    }
}
