using LeaveFlow.Application.Dtos;
using LeaveFlow.Application.Interfaces.Services;
using LeaveFlow.Domain.Constants;
using LeaveFlow.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace LeaveFlow.Host.Controllers
{
    [Authorize]
    public class LeaveRequestController(
        ILeaveRequestService leaveRequestService,
        ILeaveTypeService leaveTypeService) : Controller
    {
        private bool TryGetEmployeeId(out Guid employeeId)
        {
            return Guid.TryParse(User.FindFirstValue("EmployeeId"), out employeeId);
        }

        private bool TryGetUserId(out Guid userId)
        {
            return Guid.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out userId);
        }

        private bool IsSystemAdmin()
        {
            return TryGetUserId(out var userId) && userId == SystemIds.AdminUserId;
        }

        private bool CanReviewLeaveRequests()
        {
            return User.IsInRole(RoleNames.Manager) || IsSystemAdmin();
        }

        private async Task LoadLeaveTypesAsync(Guid? selectedLeaveTypeId = null)
        {
            var response = await leaveTypeService.GetAllAsync();

            ViewBag.LeaveTypes = new SelectList(
                response.Data ?? new List<LeaveTypeDto>(),
                nameof(LeaveTypeDto.Id),
                nameof(LeaveTypeDto.Name),
                selectedLeaveTypeId);
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (IsSystemAdmin())
            {
                return RedirectToAction(nameof(Manage));
            }

            if (!TryGetEmployeeId(out var employeeId))
            {
                return Unauthorized();
            }

            var response = await leaveRequestService.GetByEmployeeIdAsync(employeeId);

            return View(response.Data ?? new List<LeaveRequestDto>());
        }

        [HttpGet]
        public async Task<IActionResult> Apply()
        {
            if (IsSystemAdmin())
            {
                return Forbid();
            }

            await LoadLeaveTypesAsync();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(ApplyLeaveRequest request)
        {
            if (IsSystemAdmin())
            {
                return Forbid();
            }

            if (!TryGetEmployeeId(out var employeeId))
            {
                return Unauthorized();
            }

            if (!ModelState.IsValid)
            {
                await LoadLeaveTypesAsync(request.LeaveTypeId);
                return View(request);
            }

            var response = await leaveRequestService.ApplyAsync(employeeId, request);

            if (!response.Status)
            {
                ModelState.AddModelError(string.Empty, response.Message);
                await LoadLeaveTypesAsync(request.LeaveTypeId);
                return View(request);
            }

            TempData["Success"] = response.Message;

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            if (!TryGetEmployeeId(out var employeeId))
            {
                return Unauthorized();
            }

            var response = await leaveRequestService.GetByIdAsync(id);

            if (!response.Status || response.Data is null)
            {
                TempData["Error"] = response.Message;
                return RedirectToAction(nameof(Index));
            }

            if (!IsSystemAdmin() && response.Data.EmployeeId == employeeId)
            {
                return View(response.Data);
            }

            if (!CanReviewLeaveRequests() || !TryGetUserId(out var reviewerUserId))
            {
                return Forbid();
            }

            var reviewableResponse = await leaveRequestService.GetReviewableByIdAsync(
                id,
                reviewerUserId);

            if (!reviewableResponse.Status || reviewableResponse.Data is null)
            {
                return Forbid();
            }

            return View(reviewableResponse.Data);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            if (IsSystemAdmin())
            {
                return Forbid();
            }

            if (!TryGetEmployeeId(out var employeeId))
            {
                return Unauthorized();
            }

            var response = await leaveRequestService.GetByIdAsync(id);

            if (!response.Status || response.Data is null)
            {
                TempData["Error"] = response.Message;
                return RedirectToAction(nameof(Index));
            }

            if (response.Data.EmployeeId != employeeId)
            {
                return Forbid();
            }

            if (response.Data.Status != LeaveStatus.Pending)
            {
                TempData["Error"] = "Only pending leave requests can be updated.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var request = new UpdateLeaveRequest
            {
                LeaveTypeId = response.Data.LeaveTypeId,
                StartDate = response.Data.StartDate,
                EndDate = response.Data.EndDate,
                Reason = response.Data.Reason
            };

            await LoadLeaveTypesAsync(request.LeaveTypeId);

            return View(request);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, UpdateLeaveRequest request)
        {
            if (IsSystemAdmin())
            {
                return Forbid();
            }

            if (!TryGetEmployeeId(out var employeeId))
            {
                return Unauthorized();
            }

            if (!ModelState.IsValid)
            {
                await LoadLeaveTypesAsync(request.LeaveTypeId);
                return View(request);
            }

            var response = await leaveRequestService.UpdateAsync(id, employeeId, request);

            if (!response.Status)
            {
                ModelState.AddModelError(string.Empty, response.Message);
                await LoadLeaveTypesAsync(request.LeaveTypeId);
                return View(request);
            }

            TempData["Success"] = response.Message;

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> Cancel(Guid id)
        {
            if (IsSystemAdmin())
            {
                return Forbid();
            }

            if (!TryGetEmployeeId(out var employeeId))
            {
                return Unauthorized();
            }

            var response = await leaveRequestService.GetByIdAsync(id);

            if (!response.Status || response.Data is null)
            {
                TempData["Error"] = response.Message;
                return RedirectToAction(nameof(Index));
            }

            if (response.Data.EmployeeId != employeeId)
            {
                return Forbid();
            }

            if (response.Data.Status != LeaveStatus.Pending)
            {
                TempData["Error"] = "Only pending leave requests can be cancelled.";
                return RedirectToAction(nameof(Details), new { id });
            }

            return View(response.Data);
        }

        [HttpPost]
        [ActionName(nameof(Cancel))]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelConfirmed(Guid id)
        {
            if (IsSystemAdmin())
            {
                return Forbid();
            }

            if (!TryGetEmployeeId(out var employeeId))
            {
                return Unauthorized();
            }

            var response = await leaveRequestService.CancelAsync(id, employeeId);

            if (!response.Status)
            {
                TempData["Error"] = response.Message;
                return RedirectToAction(nameof(Details), new { id });
            }

            TempData["Success"] = response.Message;

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Authorize(Roles = RoleNames.ManagerOrAdmin)]
        public async Task<IActionResult> Manage()
        {
            if (!TryGetUserId(out var reviewerUserId))
            {
                return Unauthorized();
            }

            var response = await leaveRequestService.GetReviewableAsync(reviewerUserId);

            return View(response.Data ?? new List<LeaveRequestDto>());
        }

        [HttpGet]
        [Authorize(Roles = RoleNames.ManagerOrAdmin)]
        public async Task<IActionResult> Review(Guid id)
        {
            if (!TryGetUserId(out var reviewerUserId))
            {
                return Unauthorized();
            }

            var response = await leaveRequestService.GetReviewableByIdAsync(
                id,
                reviewerUserId);

            if (!response.Status || response.Data is null)
            {
                TempData["Error"] = response.Message;
                return RedirectToAction(nameof(Manage));
            }

            if (response.Data.Status != LeaveStatus.Pending)
            {
                TempData["Error"] = "This leave request has already been reviewed.";
                return RedirectToAction(nameof(Details), new { id });
            }

            ViewBag.LeaveRequest = response.Data;

            return View(new ReviewLeaveRequest());
        }

        [HttpPost]
        [Authorize(Roles = RoleNames.ManagerOrAdmin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Review(Guid id, ReviewLeaveRequest request)
        {
            if (!TryGetUserId(out var reviewerUserId))
            {
                return Unauthorized();
            }

            if (!ModelState.IsValid)
            {
                var leaveRequestResponse = await leaveRequestService.GetReviewableByIdAsync(
                    id,
                    reviewerUserId);
                ViewBag.LeaveRequest = leaveRequestResponse.Data;
                return View(request);
            }

            var response = await leaveRequestService.ReviewAsync(
                id,
                reviewerUserId,
                request);

            if (!response.Status)
            {
                ModelState.AddModelError(string.Empty, response.Message);
                var leaveRequestResponse = await leaveRequestService.GetReviewableByIdAsync(
                    id,
                    reviewerUserId);
                ViewBag.LeaveRequest = leaveRequestResponse.Data;
                return View(request);
            }

            TempData["Success"] = response.Message;

            return RedirectToAction(nameof(Manage));
        }
    }
}
