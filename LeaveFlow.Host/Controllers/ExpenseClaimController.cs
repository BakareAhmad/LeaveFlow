using LeaveFlow.Application.Dtos;
using LeaveFlow.Application.Interfaces.Services;
using LeaveFlow.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace LeaveFlow.Host.Controllers
{
    [Authorize]
    public class ExpenseClaimController(
        IExpenseClaimService service,
        IWorkTaskService taskService,
        IWebHostEnvironment environment) : Controller
    {
        private bool TryUser(out Guid id) => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out id);
        private bool TryEmployee(out Guid id) => Guid.TryParse(User.FindFirstValue("EmployeeId"), out id);

        private async Task LoadTasksAsync(Guid employeeId)
        {
            var tasks = (await taskService.GetMyTasksAsync(employeeId)).Data ?? [];
            ViewBag.Tasks = new SelectList(tasks.Select(t => new { t.Id, t.Title }), "Id", "Title");
        }

        [HttpGet]
        [Authorize(Roles = RoleNames.Manager)]
        public async Task<IActionResult> Index()
        {
            if (!TryEmployee(out var id)) return Unauthorized();
            var response = await service.GetMyClaimsAsync(id);
            return View(response.Data ?? []);
        }

        [HttpGet]
        [Authorize(Roles = RoleNames.Admin)]
        public async Task<IActionResult> ReviewQueue()
        {
            if (!TryUser(out var id)) return Unauthorized();
            var response = await service.GetReviewQueueAsync(id);
            return View(response.Data ?? []);
        }

        [HttpGet]
        [Authorize(Roles = RoleNames.Admin)]
        public async Task<IActionResult> History()
        {
            if (!TryUser(out var id)) return Unauthorized();
            var response = await service.GetClaimHistoryAsync(id);
            return View(response.Data ?? []);
        }

        [HttpGet]
        [Authorize(Roles = RoleNames.Manager)]
        public async Task<IActionResult> Create()
        {
            if (!TryEmployee(out var id)) return Unauthorized();
            await LoadTasksAsync(id);
            return View();
        }

        [HttpPost]
        [Authorize(Roles = RoleNames.Manager)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateExpenseClaimRequest request)
        {
            if (!TryEmployee(out var id)) return Unauthorized();
            if (!ModelState.IsValid)
            {
                await LoadTasksAsync(id);
                return View(request);
            }

            var r = await service.CreateDraftAsync(id, request);
            if (!r.Status || r.Data is null)
            {
                ModelState.AddModelError(string.Empty, r.Message);
                await LoadTasksAsync(id);
                return View(request);
            }

            TempData["Success"] = r.Message;
            return RedirectToAction(nameof(Details), new { id = r.Data.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            if (!TryUser(out var uid)) return Unauthorized();
            var r = await service.GetByIdAsync(id, uid);
            return !r.Status || r.Data is null ? Forbid() : View(r.Data);
        }

        [HttpPost]
        [Authorize(Roles = RoleNames.Manager)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddItem(Guid id, AddExpenseItemRequest request)
        {
            if (!TryEmployee(out var eid)) return Unauthorized();
            var r = await service.AddItemAsync(id, eid, request);
            TempData[r.Status ? "Success" : "Error"] = r.Message;
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [Authorize(Roles = RoleNames.Manager)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteItem(Guid id, Guid itemId)
        {
            if (!TryEmployee(out var eid)) return Unauthorized();
            var r = await service.DeleteItemAsync(id, itemId, eid);
            TempData[r.Status ? "Success" : "Error"] = r.Message;
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [Authorize(Roles = RoleNames.Manager)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(Guid id)
        {
            if (!TryEmployee(out var eid)) return Unauthorized();
            var r = await service.SubmitAsync(id, eid);
            TempData[r.Status ? "Success" : "Error"] = r.Message;
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [Authorize(Roles = RoleNames.Admin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Review(Guid id, ReviewExpenseClaimRequest request)
        {
            if (!TryUser(out var uid)) return Unauthorized();
            var r = await service.ReviewAsync(id, uid, request);
            TempData[r.Status ? "Success" : "Error"] = r.Message;
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [Authorize(Roles = RoleNames.Manager)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(Guid id, IFormFile file)
        {
            if (!TryEmployee(out var employeeId)) return Unauthorized();
            var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".pdf", ".png", ".jpg", ".jpeg", ".webp" };
            var extension = Path.GetExtension(file.FileName);
            if (file.Length == 0 || file.Length > 5 * 1024 * 1024 || !allowed.Contains(extension))
            {
                TempData["Error"] = "Upload a PDF, PNG, or JPG file up to 5 MB.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var folder = Path.Combine(environment.ContentRootPath, "App_Data", "ExpenseReceipts");
            Directory.CreateDirectory(folder);
            var storedName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            var path = Path.Combine(folder, storedName);

            await using (var stream = System.IO.File.Create(path))
            {
                await file.CopyToAsync(stream);
            }

            var r = await service.AddAttachmentAsync(id, employeeId, new AddExpenseAttachmentRequest
            {
                OriginalFileName = Path.GetFileName(file.FileName),
                StoredFileName = storedName,
                ContentType = file.ContentType,
                FileSize = file.Length
            });

            if (!r.Status)
            {
                System.IO.File.Delete(path);
                TempData["Error"] = r.Message;
            }
            else
            {
                TempData["Success"] = "Receipt attached successfully.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> ViewReceipt(Guid attachmentId)
        {
            if (!TryUser(out var uid)) return Unauthorized();
            var r = await service.GetAttachmentAsync(attachmentId, uid);
            if (!r.Status || r.Data is null) return Forbid();

            var path = Path.Combine(environment.ContentRootPath, "App_Data", "ExpenseReceipts", r.Data.StoredFileName);
            if (!System.IO.File.Exists(path)) return NotFound();

            return PhysicalFile(path, r.Data.ContentType);
        }

        [HttpGet]
        public async Task<IActionResult> Download(Guid attachmentId)
        {
            if (!TryUser(out var uid)) return Unauthorized();
            var r = await service.GetAttachmentAsync(attachmentId, uid);
            if (!r.Status || r.Data is null) return Forbid();

            var path = Path.Combine(environment.ContentRootPath, "App_Data", "ExpenseReceipts", r.Data.StoredFileName);
            if (!System.IO.File.Exists(path)) return NotFound();

            return PhysicalFile(path, r.Data.ContentType, r.Data.OriginalFileName);
        }
    }
}
