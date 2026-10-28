using LeaveFlow.Application.Dtos;
using LeaveFlow.Application.Interfaces.Services;
using LeaveFlow.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;

namespace LeaveFlow.Host.Controllers
{
    [Authorize(Roles = RoleNames.ManagerOrAdmin)]
    public class ReportController(IOperationsReportService service) : Controller
    {
        private bool TryUser(out Guid id) => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out id);

        [HttpGet]
        public async Task<IActionResult> Index(OperationsReportFilterRequest filter)
        {
            if (!TryUser(out var id)) return Unauthorized();
            var r = await service.GetAsync(id, filter);
            if (!r.Status) TempData["Error"] = r.Message;
            return View(r.Data ?? new OperationsReportDto());
        }

        [HttpGet]
        public async Task<IActionResult> ExportCsv(OperationsReportFilterRequest filter)
        {
            if (!TryUser(out var id)) return Unauthorized();
            var r = await service.ExportCsvAsync(id, filter);
            return !r.Status || r.Data is null
                ? BadRequest()
                : File(Encoding.UTF8.GetBytes(r.Data), "text/csv", $"leaveflow-operations-report-{DateTime.UtcNow:yyyyMMdd}.csv");
        }

        [HttpGet]
        public async Task<IActionResult> ExportPdf(OperationsReportFilterRequest filter)
        {
            if (!TryUser(out var id)) return Unauthorized();
            var r = await service.ExportPdfAsync(id, filter);
            if (!r.Status || r.Data is null)
            {
                TempData["Error"] = r.Message;
                return RedirectToAction(nameof(Index), filter);
            }

            return File(r.Data, "application/pdf", $"leaveflow-operations-report-{DateTime.UtcNow:yyyyMMdd}.pdf");
        }
    }
}
