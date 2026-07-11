using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Blocko.Persistence;
using Bolcko.Domain.Entities.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bolcko.Web.App.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly BlockoDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly Blocko.Services.Interfaces.Notifications.INotificationService _notificationService;

        public NotificationController(BlockoDbContext context, UserManager<User> userManager, Blocko.Services.Interfaces.Notifications.INotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int page = 1)
        {
            var userIdString = _userManager.GetUserId(User);
            if (!int.TryParse(userIdString, out var userId))
            {
                return RedirectToAction("Login", "Account", new { area = "Shop" });
            }

            int pageSize = 15;
            var query = _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt);

            var totalCount = await query.CountAsync();
            var notifications = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            ViewBag.HasNextPage = page < ViewBag.TotalPages;
            ViewBag.HasPreviousPage = page > 1;

            return View(notifications);
        }

        [HttpGet]
        public async Task<IActionResult> TestSend()
        {
            var userIdString = _userManager.GetUserId(User);
            if (!int.TryParse(userIdString, out var userId))
            {
                return Content("Not logged in");
            }

            try
            {
                await _notificationService.SendNotificationToUserAsync(userId, "إشعار تجريبي", "هذا إشعار تجريبي للتحقق من الاتصال بالوقت الفعلي وصوت التنبيه.", "/Admin");
                return Content("Success! Notification sent to userId: " + userId);
            }
            catch (Exception ex)
            {
                return Content("Error sending notification: " + ex.ToString());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRecent()
        {
            var userIdString = _userManager.GetUserId(User);
            if (!int.TryParse(userIdString, out var userId))
            {
                return Unauthorized();
            }

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .Select(n => new
                {
                    n.Id,
                    n.Title,
                    n.Message,
                    n.ActionUrl,
                    n.IsRead,
                    CreatedAt = n.CreatedAt.ToString("yyyy-MM-dd HH:mm")
                })
                .ToListAsync();

            var unreadCount = await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);

            return Json(new { success = true, notifications, unreadCount });
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userIdString = _userManager.GetUserId(User);
            if (!int.TryParse(userIdString, out var userId))
            {
                return Unauthorized();
            }

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }

            return Json(new { success = false, message = "Notification not found" });
        }

        [HttpPost]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userIdString = _userManager.GetUserId(User);
            if (!int.TryParse(userIdString, out var userId))
            {
                return Unauthorized();
            }

            var unreadNotifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var n in unreadNotifications)
            {
                n.IsRead = true;
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
    }
}
