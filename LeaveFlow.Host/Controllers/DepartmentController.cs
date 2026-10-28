using LeaveFlow.Application.Dtos;
using LeaveFlow.Application.Interfaces.Services;
using LeaveFlow.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeaveFlow.Host.Controllers
{
    [Authorize(Roles = RoleNames.Admin)]
    public class DepartmentController(IDepartmentService departmentService) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var response = await departmentService.GetAllAsync();

            return View(response.Data);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateDepartmentRequest request)
        {
            if (!ModelState.IsValid)
            {
                return View(request);
            }

            var response = await departmentService.CreateAsync(request);

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
            var response = await departmentService.GetByIdAsync(id);

            if (!response.Status || response.Data is null)
            {
                TempData["Error"] = response.Message;

                return RedirectToAction(nameof(Index));
            }

            var request = new UpdateDepartmentRequest
            {
                Name = response.Data.Name
            };

            return View(request);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, UpdateDepartmentRequest request)
        {
            if (!ModelState.IsValid)
            {
                return View(request);
            }

            var response = await departmentService.UpdateAsync(id, request);

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
            var response = await departmentService.GetByIdAsync(id);

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
            var response = await departmentService.DeleteAsync(id);

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
