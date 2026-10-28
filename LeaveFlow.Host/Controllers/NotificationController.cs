using LeaveFlow.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LeaveFlow.Host.Controllers
{
    [Authorize]
    public class NotificationController(INotificationService service) : Controller
    {
        private bool TryUser(out Guid id) => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out id);
        [HttpGet] public async Task<IActionResult> Index() { if (!TryUser(out var id)) return Unauthorized(); return View((await service.GetForUserAsync(id)).Data ?? []); }
        [HttpGet] public async Task<IActionResult> UnreadCount() { if (!TryUser(out var id)) return Unauthorized(); return Json(new { count = (await service.GetUnreadCountAsync(id)).Data }); }
        [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> MarkRead(Guid id, string? returnUrl = null) { if (!TryUser(out var uid)) return Unauthorized(); await service.MarkReadAsync(id, uid); return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : RedirectToAction(nameof(Index)); }
        [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> MarkAllRead() { if (!TryUser(out var uid)) return Unauthorized(); await service.MarkAllReadAsync(uid); return RedirectToAction(nameof(Index)); }
    }
}
