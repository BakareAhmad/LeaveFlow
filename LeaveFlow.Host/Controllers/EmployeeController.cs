using LeaveFlow.Application.Dtos;
using LeaveFlow.Application.Interfaces.Services;
using LeaveFlow.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LeaveFlow.Host.Controllers
{
    [Authorize(Roles = RoleNames.Admin)]
    public class EmployeeController(
        IEmployeeService employeeService,
        IDepartmentService departmentService,
        IRoleService roleService) : Controller
    {
        private async Task LoadSelectionsAsync(
            Guid? selectedDepartmentId = null,
            Guid? selectedRoleId = null)
        {
            var departmentsResponse = await departmentService.GetAllAsync();
            var rolesResponse = await roleService.GetAllAsync();

            ViewBag.Departments = new SelectList(
                departmentsResponse.Data ?? new List<DepartmentDto>(),
                nameof(DepartmentDto.Id),
                nameof(DepartmentDto.Name),
                selectedDepartmentId);

            var assignableRoles = rolesResponse.Data?
                .Where(r => r.Name != RoleNames.Admin)
                .ToList() ?? new List<RoleDto>();

            ViewBag.Roles = new SelectList(
                assignableRoles,
                nameof(RoleDto.Id),
                nameof(RoleDto.Name),
                selectedRoleId);
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var response = await employeeService.GetAllAsync();

            return View(response.Data);
        }

        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var response = await employeeService.GetByIdAsync(id);

            if (!response.Status || response.Data is null)
            {
                TempData["Error"] = response.Message;
                return RedirectToAction(nameof(Index));
            }

            return View(response.Data);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadSelectionsAsync();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RegisterEmployeeRequest request)
        {
            if (!ModelState.IsValid)
            {
                await LoadSelectionsAsync(request.DepartmentId, request.RoleId);

                return View(request);
            }

            var response = await employeeService.CreateAsync(request);

            if (!response.Status)
            {
                ModelState.AddModelError(string.Empty, response.Message);
                await LoadSelectionsAsync(request.DepartmentId, request.RoleId);

                return View(request);
            }

            TempData["Success"] = response.Message;

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            if (id == SystemIds.AdminEmployeeId)
            {
                TempData["Error"] = "The system administrator account cannot be edited.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var response = await employeeService.GetByIdAsync(id);

            if (!response.Status || response.Data is null)
            {
                TempData["Error"] = response.Message;
                return RedirectToAction(nameof(Index));
            }

            var request = new UpdateEmployeeRequest
            {
                FirstName = response.Data!.FirstName,
                LastName = response.Data.LastName,
                Email = response.Data.Email,
                PhoneNumber = response.Data.PhoneNumber,
                DepartmentId = response.Data.DepartmentId,
                RoleId = response.Data.RoleId
            };

            await LoadSelectionsAsync(request.DepartmentId, request.RoleId);

            return View(request);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, UpdateEmployeeRequest request)
        {
            if (id == SystemIds.AdminEmployeeId)
            {
                TempData["Error"] = "The system administrator account cannot be edited.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (!ModelState.IsValid)
            {
                await LoadSelectionsAsync(request.DepartmentId, request.RoleId);

                return View(request);
            }

            var response = await employeeService.UpdateAsync(id, request);

            if (!response.Status)
            {
                ModelState.AddModelError(string.Empty, response.Message);
                await LoadSelectionsAsync(request.DepartmentId, request.RoleId);

                return View(request);
            }

            TempData["Success"] = response.Message;

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(Guid id)
        {
            if (id == SystemIds.AdminEmployeeId)
            {
                TempData["Error"] = "The system administrator account cannot be deleted.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var response = await employeeService.GetByIdAsync(id);

            if (!response.Status || response.Data is null)
            {
                TempData["Error"] = response.Message;
                return RedirectToAction(nameof(Index));
            }

            return View(response.Data);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            if (id == SystemIds.AdminEmployeeId)
            {
                TempData["Error"] = "The system administrator account cannot be deleted.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var response = await employeeService.DeleteAsync(id);

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
