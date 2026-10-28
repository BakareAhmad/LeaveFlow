using LeaveFlow.Application.Interfaces.Services;
using LeaveFlow.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeaveFlow.Host.Controllers
{
    [Authorize(Roles = RoleNames.Admin)]
    public class RoleController(IRoleService roleService) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var response = await roleService.GetAllAsync();

            return View(response.Data);
        }
    }
}
