using LeaveFlow.Application.Dtos;
using LeaveFlow.Application.Interfaces.Services;
using LeaveFlow.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeaveFlow.Host.Controllers
{
    [Authorize(Roles = RoleNames.Admin)]
    public class LeaveTypeController(ILeaveTypeService leaveTypeService) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var response = await leaveTypeService.GetAllAsync();

            return View(response.Data);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateLeaveTypeRequest request)
        {
            if (!ModelState.IsValid)
            {
                return View(request);
            }

            var response = await leaveTypeService.CreateAsync(request);

            if (!response.Status)
            {
                ModelState.AddModelError(string.Empty, response.Message);

                return View(request);
            }

            TempData["Success"] = response.Message;

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var response = await leaveTypeService.GetByIdAsync(id);

            if (!response.Status || response.Data is null)
            {
                TempData["Error"] = response.Message;

                return RedirectToAction(nameof(Index));
            }

            var request = new UpdateLeaveTypeRequest
            {
                Name = response.Data.Name,
                MaximumDays = response.Data.MaximumDays,
                Description = response.Data.Description
            };

            return View(request);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, UpdateLeaveTypeRequest request)
        {
            if (!ModelState.IsValid)
            {
                return View(request);
            }

            var response = await leaveTypeService.UpdateAsync(id, request);

            if (!response.Status)
            {
                ModelState.AddModelError(string.Empty, response.Message);

                return View(request);
            }

            TempData["Success"] = response.Message;

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(Guid id)
        {
            var response = await leaveTypeService.GetByIdAsync(id);

            if (!response.Status || response.Data is null)
            {
                TempData["Error"] = response.Message;

                return RedirectToAction(nameof(Index));
            }

            return View(response.Data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName(nameof(Delete))]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var response = await leaveTypeService.DeleteAsync(id);

            if (!response.Status)
            {
                TempData["Error"] = response.Message;

                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = response.Message;

            return RedirectToAction(nameof(Index));
        }
    }
}
