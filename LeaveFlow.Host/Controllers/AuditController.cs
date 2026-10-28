using LeaveFlow.Application.Interfaces.Services;
using LeaveFlow.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeaveFlow.Host.Controllers
{
    [Authorize(Roles = RoleNames.Admin)]
    public class AuditController(IAuditService service) : Controller
    {
        [HttpGet] public async Task<IActionResult> Index() => View((await service.GetRecentAsync()).Data ?? []);
    }
}
