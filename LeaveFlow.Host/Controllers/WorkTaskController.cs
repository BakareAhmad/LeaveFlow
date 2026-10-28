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
    public class WorkTaskController(IWorkTaskService service, IEmployeeService employeeService, IOrganisationAccessService accessService) : Controller
    {
        private bool TryUser(out Guid id) => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out id);
        private bool TryEmployee(out Guid id) => Guid.TryParse(User.FindFirstValue("EmployeeId"), out id);

        private async Task LoadAssigneesAsync()
        {
            if (!TryUser(out var userId)) { ViewBag.Assignees = new SelectList(Array.Empty<EmployeeDto>()); return; }
            var context = await accessService.GetContextAsync(userId);
            var employees = (await employeeService.GetAllAsync()).Data ?? [];
            var allowed = context.Data?.IsSystemAdmin == true
                ? employees.Where(e => e.Role == RoleNames.Manager)
                : employees.Where(e => e.Role == RoleNames.Employee && e.DepartmentId == context.Data?.DepartmentId);
            ViewBag.Assignees = new SelectList(allowed.Select(e => new { e.Id, Name = $"{e.FirstName} {e.LastName}" }), "Id", "Name");
        }

        [HttpGet] public async Task<IActionResult> Index() { if (!TryEmployee(out var id)) return Unauthorized(); return View((await service.GetMyTasksAsync(id)).Data ?? []); }
        [HttpGet, Authorize(Roles = RoleNames.ManagerOrAdmin)] public async Task<IActionResult> ReviewQueue() { if (!TryUser(out var id)) return Unauthorized(); return View((await service.GetReviewQueueAsync(id)).Data ?? []); }
        [HttpGet, Authorize(Roles = RoleNames.ManagerOrAdmin)] public async Task<IActionResult> Create() { await LoadAssigneesAsync(); return View(); }
        [HttpPost, Authorize(Roles = RoleNames.ManagerOrAdmin), ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateWorkTaskRequest request)
        {
            if (!TryUser(out var id)) return Unauthorized();
            if (!ModelState.IsValid) { await LoadAssigneesAsync(); return View(request); }
            var result = await service.CreateAsync(id, request); if (!result.Status) { ModelState.AddModelError(string.Empty, result.Message); await LoadAssigneesAsync(); return View(request); }
            TempData["Success"] = result.Message; return RedirectToAction(nameof(ReviewQueue));
        }
        [HttpGet] public async Task<IActionResult> Details(Guid id) { if (!TryUser(out var uid)) return Unauthorized(); var r = await service.GetByIdAsync(id, uid); return !r.Status || r.Data is null ? Forbid() : View(r.Data); }
        [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Start(Guid id) { if (!TryEmployee(out var eid)) return Unauthorized(); var r = await service.StartAsync(id, eid); TempData[r.Status ? "Success" : "Error"] = r.Message; return RedirectToAction(nameof(Details), new { id }); }
        [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> AddProgress(Guid id, AddTaskProgressRequest request) { if (!TryEmployee(out var eid)) return Unauthorized(); if (!ModelState.IsValid) return RedirectToAction(nameof(Details), new { id }); var r = await service.AddProgressAsync(id, eid, request); TempData[r.Status ? "Success" : "Error"] = r.Message; return RedirectToAction(nameof(Details), new { id }); }
        [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Complete(Guid id) { if (!TryEmployee(out var eid)) return Unauthorized(); var r = await service.CompleteAsync(id, eid); TempData[r.Status ? "Success" : "Error"] = r.Message; return RedirectToAction(nameof(Details), new { id }); }
        [HttpPost, Authorize(Roles = RoleNames.ManagerOrAdmin), ValidateAntiForgeryToken] public async Task<IActionResult> Review(Guid id, ReviewWorkTaskRequest request) { if (!TryUser(out var uid)) return Unauthorized(); var r = await service.ReviewAsync(id, uid, request); TempData[r.Status ? "Success" : "Error"] = r.Message; return RedirectToAction(nameof(Details), new { id }); }
    }
}
