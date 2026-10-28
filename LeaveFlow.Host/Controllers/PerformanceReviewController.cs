using LeaveFlow.Application.Dtos;
using LeaveFlow.Application.Interfaces.Services;
using LeaveFlow.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace LeaveFlow.Host.Controllers
{
    [Authorize]
    public class PerformanceReviewController(IPerformanceReviewService service) : Controller
    {
        private Guid? GetCurrentUserId()
        {
            var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (Guid.TryParse(raw, out var id))
            {
                return id;
            }
            return null;
        }

        private Guid? GetCurrentEmployeeId()
        {
            var raw = User.FindFirstValue("EmployeeId");
            if (Guid.TryParse(raw, out var id))
            {
                return id;
            }
            return null;
        }

        private Guid? GetCurrentDepartmentId()
        {
            var raw = User.FindFirstValue("DepartmentId");
            if (Guid.TryParse(raw, out var id))
            {
                return id;
            }
            return null;
        }

        [HttpGet]
        [Authorize(Roles = RoleNames.Admin)]
        public async Task<IActionResult> Cycles()
        {
            var response = await service.GetCyclesAsync();
            return View(response.Data ?? new List<PerformanceReviewCycleDto>());
        }

        [HttpPost]
        [Authorize(Roles = RoleNames.Admin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCycle(CreatePerformanceCycleRequest request)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var result = await service.CreateCycleAsync(userId.Value, request);
            TempData[result.Status ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Cycles));
        }

        [HttpPost]
        [Authorize(Roles = RoleNames.Admin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OpenCycle(Guid id)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var result = await service.OpenCycleAsync(id, userId.Value);
            TempData[result.Status ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Cycles));
        }

        [HttpPost]
        [Authorize(Roles = RoleNames.Admin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CloseCycle(Guid id)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var result = await service.CloseCycleAsync(id, userId.Value);
            TempData[result.Status ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Cycles));
        }

        [HttpGet]
        [Authorize(Roles = RoleNames.Admin)]
        public async Task<IActionResult> CycleReports(Guid cycleId)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var result = await service.GetCycleDepartmentReportsAsync(cycleId, userId.Value);
            if (!result.Status)
            {
                TempData["Error"] = result.Message;
                return RedirectToAction(nameof(Cycles));
            }

            ViewBag.CycleId = cycleId;
            return View(result.Data ?? new List<DepartmentReviewReportDto>());
        }

        [HttpGet]
        public async Task<IActionResult> DepartmentReport(Guid cycleId, Guid? departmentId = null)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            Guid targetDeptId;
            if (User.IsInRole(RoleNames.Admin))
            {
                if (!departmentId.HasValue)
                {
                    return RedirectToAction(nameof(CycleReports), new { cycleId });
                }
                targetDeptId = departmentId.Value;
            }
            else
            {
                var deptId = GetCurrentDepartmentId();
                if (!deptId.HasValue)
                {
                    TempData["Error"] = "Department not found in your session.";
                    return RedirectToAction("Index", "Home");
                }
                targetDeptId = deptId.Value;
            }

            var result = await service.GetDepartmentReportAsync(cycleId, targetDeptId, userId.Value);
            if (!result.Status || result.Data == null)
            {
                TempData["Error"] = result.Message;
                if (User.IsInRole(RoleNames.Admin))
                {
                    return RedirectToAction(nameof(Cycles));
                }
                return RedirectToAction(nameof(ReviewQueue));
            }

            return View(result.Data);
        }

        [HttpPost]
        [Authorize(Roles = RoleNames.Manager)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitDepartmentReport(
            Guid cycleId,
            Guid departmentId,
            SubmitDepartmentReportRequest request)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var result = await service.SubmitDepartmentReportAsync(
                cycleId,
                departmentId,
                userId.Value,
                request);

            TempData[result.Status ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(DepartmentReport), new { cycleId, departmentId });
        }

        [HttpGet]
        public async Task<IActionResult> MyReviews()
        {
            // Managers don't have their own performance reviews; they review their staff.
            if (User.IsInRole(RoleNames.Manager))
            {
                return RedirectToAction(nameof(ReviewQueue));
            }

            // Admin manages cycles from the Administration area.
            if (User.IsInRole(RoleNames.Admin))
            {
                return RedirectToAction(nameof(Cycles));
            }

            var employeeId = GetCurrentEmployeeId();
            if (!employeeId.HasValue)
            {
                return Unauthorized();
            }

            var result = await service.GetMyReviewsAsync(employeeId.Value);
            return View(result.Data ?? new List<PerformanceReviewDto>());
        }

        [HttpGet]
        [Authorize(Roles = RoleNames.Manager)]
        public async Task<IActionResult> ReviewQueue()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var result = await service.GetReviewQueueAsync(userId.Value);
            return View(result.Data ?? new List<PerformanceReviewDto>());
        }

        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var result = await service.GetByIdAsync(id, userId.Value);
            if (!result.Status || result.Data == null)
            {
                TempData["Error"] = result.Message;
                return Forbid();
            }

            return View(result.Data);
        }

        [HttpPost]
        [Authorize(Roles = RoleNames.Manager)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveReview(Guid id, SaveManagerReviewRequest request)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            if (!ModelState.IsValid)
            {
                var review = await service.GetByIdAsync(id, userId.Value);
                return View(nameof(Details), review.Data);
            }

            var result = await service.SaveManagerReviewAsync(id, userId.Value, request);
            TempData[result.Status ? "Success" : "Error"] = result.Message;
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
